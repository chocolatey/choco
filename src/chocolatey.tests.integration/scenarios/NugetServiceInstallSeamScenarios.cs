// Copyright © 2017 - 2025 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.app.nuget;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.results;
using chocolatey.infrastructure.services;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using NUnit.Framework;
using FluentAssertions;
using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

namespace chocolatey.tests.integration.scenarios
{
    // An extension can change where a package is installed, whether it is, whether it is kept once extracted, how it
    // is shown, and what is said when one that is not installed is uninstalled, by overriding protected members of the
    // service. Each of them is overridden here by a service that records when it is called, to prove it is called from
    // the right place and that what it returns is what the install does.
    public class NugetServiceInstallSeamScenarios
    {
        public class ConfigurableNugetService : NugetService
        {
            public readonly List<string> Events = new List<string>();
            public bool SkipInstall;
            public IEnumerable<SourcePackageDependencyInfo> ExternalDependencies;
            public string ResolutionErrorMessage;
            public string RefusalReason;
            public string RefusedId;
            public bool KeepRefusedFiles;
            public string DisplayNamePrefix;
            public string NotInstalledMessage;
            public string PackagesLocation;
            public Func<ChocolateyPackagePathResolver> ResolverFactory;
            public ChocolateyPackagePathResolver PreparedResolver;
            public ChocolateyPackagePathResolver SkipCheckedResolver;

            public ConfigurableNugetService(
                IFileSystem fileSystem,
                ILogger nugetLogger,
                IChocolateyPackageInformationService packageInfoService,
                IFilesService filesService,
                IRuleService ruleService)
                : base(fileSystem, nugetLogger, packageInfoService, filesService, ruleService)
            {
            }

            protected override ChocolateyPackagePathResolver GetInstallPathResolver(ChocolateyConfiguration config)
            {
                Events.Add("GetInstallPathResolver");
                return ResolverFactory != null ? ResolverFactory() : base.GetInstallPathResolver(config);
            }

            protected override string GetInstallPackagesLocation(ChocolateyConfiguration config)
            {
                Events.Add("GetInstallPackagesLocation");
                return PackagesLocation ?? base.GetInstallPackagesLocation(config);
            }

            protected override void PrepareInstallPathResolver(ChocolateyConfiguration config, string packageName, ChocolateyPackagePathResolver pathResolver)
            {
                Events.Add("PrepareInstallPathResolver:" + packageName);
                PreparedResolver = pathResolver;
                base.PrepareInstallPathResolver(config, packageName, pathResolver);
            }

            protected override bool ShouldSkipInstall(ChocolateyConfiguration config, string packageName, PackageResult installedPackage, ChocolateyPackagePathResolver pathResolver, ConcurrentDictionary<string, PackageResult> packageResults)
            {
                Events.Add("ShouldSkipInstall:" + packageName + ":installed=" + (installedPackage != null));
                SkipCheckedResolver = pathResolver;
                return SkipInstall || base.ShouldSkipInstall(config, packageName, installedPackage, pathResolver, packageResults);
            }

            protected override string GetInstallRefusalReason(ChocolateyConfiguration config, ChocolateyPackageMetadata packageMetadata, ChocolateyPackagePathResolver pathResolver)
            {
                Events.Add("GetInstallRefusalReason:" + packageMetadata.Id);

                if (RefusalReason != null && (RefusedId == null || RefusedId.Equals(packageMetadata.Id, StringComparison.OrdinalIgnoreCase)))
                {
                    return RefusalReason;
                }

                return base.GetInstallRefusalReason(config, packageMetadata, pathResolver);
            }

            protected override void RemoveRefusedPackageFiles(ChocolateyConfiguration config, string installedPath)
            {
                Events.Add("RemoveRefusedPackageFiles:" + installedPath);
                if (!KeepRefusedFiles)
                {
                    base.RemoveRefusedPackageFiles(config, installedPath);
                }
            }

            protected override void OnPackageExtracted(ChocolateyConfiguration config, PackageResult packageResult, ChocolateyPackagePathResolver pathResolver, SourcePackageDependencyInfo packageDependencyInfo)
            {
                Events.Add("OnPackageExtracted:" + packageResult.Name);
                packageResult.Messages.Add(new ResultMessage(ResultType.Note, "annotated by the extension"));
                base.OnPackageExtracted(config, packageResult, pathResolver, packageDependencyInfo);
            }

            protected override string GetPackageDisplayName(PackageResult packageResult)
            {
                Events.Add("GetPackageDisplayName:" + packageResult.Name);
                if (DisplayNamePrefix == null)
                {
                    return base.GetPackageDisplayName(packageResult);
                }

                var annotated = packageResult.Messages.Any(m => m.Message == "annotated by the extension");
                return DisplayNamePrefix + packageResult.Name + " annotated=" + annotated;
            }

            protected override IEnumerable<SourcePackageDependencyInfo> GetExternalInstalledDependencyInfos(ChocolateyConfiguration config)
            {
                return ExternalDependencies ?? base.GetExternalInstalledDependencyInfos(config);
            }

            protected override string GetDependencyResolutionErrorMessage(ChocolateyConfiguration config, IEnumerable<IPackageSearchMetadata> packagesToInstall, NuGetResolverConstraintException exception)
            {
                Events.Add("GetDependencyResolutionErrorMessage:" + string.Join(",", packagesToInstall.Select(p => p.Identity.Id)));
                return ResolutionErrorMessage ?? base.GetDependencyResolutionErrorMessage(config, packagesToInstall, exception);
            }

            protected override string GetPackageNotInstalledMessage(ChocolateyConfiguration config, string packageName)
            {
                Events.Add("GetPackageNotInstalledMessage:" + packageName);
                return NotInstalledMessage ?? base.GetPackageNotInstalledMessage(config, packageName);
            }
        }

        [ConcernFor("install")]
        public abstract class InstallScenariosBase : TinySpec
        {
            protected ConcurrentDictionary<string, PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected ConfigurableNugetService Service;
            protected string TempRoot;

            public override void Context()
            {
                Configuration = Scenario.Install();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage.1.0.0*" + NuGetConstants.PackageExtension);
                Configuration.PackageNames = Configuration.Input = "installpackage";

                var container = NUnitSetup.Container;
                Service = new ConfigurableNugetService(
                    container.GetInstance<IFileSystem>(),
                    container.GetInstance<ILogger>(),
                    container.GetInstance<IChocolateyPackageInformationService>(),
                    container.GetInstance<IFilesService>(),
                    container.GetInstance<IRuleService>());
            }

            protected void Install()
            {
                Results = Service.Install(Configuration, (result, config) => Service.Events.Add("ContinueAction:" + result.Name));
            }

            protected static string LibPackage
            {
                get { return Path.Combine(Scenario.GetTopLevel(), "lib", "installpackage"); }
            }

            protected bool Called(string eventPrefix)
            {
                return Service.Events.Any(e => e.StartsWith(eventPrefix, StringComparison.Ordinal));
            }

            public override void AfterObservations()
            {
                base.AfterObservations();

                if (TempRoot != null && Directory.Exists(TempRoot))
                {
                    Directory.Delete(TempRoot, recursive: true);
                }
            }
        }

        public class When_installing_a_package_with_a_service_that_overrides_nothing : InstallScenariosBase
        {
            public override void Because()
            {
                Install();
            }

            [Fact]
            public void Should_install_the_package_under_lib()
            {
                Directory.Exists(LibPackage).Should().BeTrue();
            }

            [Fact]
            public void Should_call_each_install_member_from_its_place_in_the_install()
            {
                Service.Events.Should().ContainInOrder(
                    "GetInstallPathResolver",
                    "GetInstallPackagesLocation",
                    "PrepareInstallPathResolver:installpackage",
                    "ShouldSkipInstall:installpackage:installed=False",
                    "GetInstallRefusalReason:installpackage",
                    "OnPackageExtracted:installpackage",
                    "GetPackageDisplayName:installpackage",
                    "ContinueAction:installpackage");
            }

            [Fact]
            public void Should_prepare_and_check_with_the_same_resolver()
            {
                Service.PreparedResolver.Should().NotBeNull();
                Service.SkipCheckedResolver.Should().BeSameAs(Service.PreparedResolver);
            }

            [Fact]
            public void Should_show_the_package_by_its_id()
            {
                MockLogger.ContainsMessage("installpackage v1.0.0").Should().BeTrue();
            }
        }

        public class When_installing_a_package_that_the_service_skips : InstallScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Service.SkipInstall = true;
            }

            public override void Because()
            {
                Install();
            }

            [Fact]
            public void Should_not_install_the_package()
            {
                Directory.Exists(LibPackage).Should().BeFalse();
            }

            [Fact]
            public void Should_ask_before_anything_is_extracted()
            {
                Called("ShouldSkipInstall:installpackage:installed=False").Should().BeTrue();
                Called("GetInstallRefusalReason").Should().BeFalse();
                Called("OnPackageExtracted").Should().BeFalse();
                Called("ContinueAction").Should().BeFalse();
            }
        }

        public class When_installing_a_package_that_the_service_refuses_once_it_is_extracted : InstallScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Service.RefusalReason = "installpackage may not be installed here.";
            }

            public override void Because()
            {
                Install();
            }

            [Fact]
            public void Should_remove_the_files_that_were_extracted()
            {
                Directory.Exists(LibPackage).Should().BeFalse();
            }

            [Fact]
            public void Should_report_the_reason_as_an_error()
            {
                Results.Values.Should().Contain(r => r.Messages.Any(m => m.MessageType == ResultType.Error && m.Message == "installpackage may not be installed here."));
            }

            [Fact]
            public void Should_fail_the_result()
            {
                Results.Values.Should().Contain(r => r.ExitCode == 1);
            }

            [Fact]
            public void Should_not_go_on_to_annotate_or_run_the_package()
            {
                Called("OnPackageExtracted").Should().BeFalse();
                Called("ContinueAction").Should().BeFalse();
            }

            [Fact]
            public void Should_remove_the_files_through_the_member_for_it_with_the_folder_they_were_extracted_to()
            {
                Service.Events.Should().Contain("RemoveRefusedPackageFiles:" + LibPackage);
            }
        }

        public class When_the_service_refuses_a_package_and_removes_the_files_itself : InstallScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Service.RefusalReason = "installpackage may not be installed here.";
                Service.KeepRefusedFiles = true;
            }

            public override void Because()
            {
                Install();
            }

            [Fact]
            public void Should_leave_removing_the_files_to_the_service()
            {
                Directory.Exists(LibPackage).Should().BeTrue();
            }

            [Fact]
            public void Should_have_asked_the_service_to_remove_them()
            {
                Called("RemoveRefusedPackageFiles:").Should().BeTrue();
            }
        }

        public class When_the_service_shows_a_package_under_another_name : InstallScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Service.DisplayNamePrefix = "Shown as ";
            }

            public override void Because()
            {
                Install();
            }

            [Fact]
            public void Should_use_that_name_in_the_output()
            {
                MockLogger.ContainsMessage("Shown as installpackage").Should().BeTrue();
            }

            [Fact]
            public void Should_show_it_after_the_service_has_annotated_the_result()
            {
                MockLogger.ContainsMessage("Shown as installpackage annotated=True").Should().BeTrue();
            }

            [Fact]
            public void Should_still_use_the_id_for_the_result()
            {
                Results.Keys.Should().Contain("installpackage");
            }
        }

        public class When_the_service_installs_packages_to_another_location : InstallScenariosBase
        {
            public override void Context()
            {
                base.Context();

                TempRoot = Path.Combine(Path.GetTempPath(), "seam-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(TempRoot);
                var fileSystem = NUnitSetup.Container.GetInstance<IFileSystem>();
                Service.PackagesLocation = TempRoot;
                Service.ResolverFactory = () => new ChocolateyPackagePathResolver(TempRoot, fileSystem);
            }

            public override void Because()
            {
                Install();
            }

            [Fact]
            public void Should_install_the_package_there()
            {
                File.Exists(Path.Combine(TempRoot, "installpackage", "installpackage.nupkg")).Should().BeTrue();
            }

            [Fact]
            public void Should_not_install_it_under_lib()
            {
                Directory.Exists(LibPackage).Should().BeFalse();
            }
        }

        // Only the location is reported here, not the resolver, so what is observed is the folder the install creates for it.
        public class When_the_service_reports_another_packages_location : InstallScenariosBase
        {
            private string _reported;

            public override void Context()
            {
                base.Context();

                TempRoot = Path.Combine(Path.GetTempPath(), "seam-" + Guid.NewGuid().ToString("N"));
                _reported = Path.Combine(TempRoot, "reported-location");
                Service.PackagesLocation = _reported;
            }

            public override void Because()
            {
                try
                {
                    Install();
                }
                catch (Exception)
                {
                    // The resolver still lays packages under lib, which disagrees with the location reported, so an
                    // install that fails because of that is not what is being checked.
                }
            }

            [Fact]
            public void Should_create_the_location_it_reports()
            {
                Directory.Exists(_reported).Should().BeTrue();
            }
        }
        [ConcernFor("uninstall")]
        public class When_uninstalling_a_package_that_is_not_installed_with_a_service_that_overrides_nothing : InstallScenariosBase
        {
            public override void Because()
            {
                Configuration = Scenario.Uninstall();
                Configuration.PackageNames = Configuration.Input = "somethingnonexisting";
                Results = Service.Uninstall(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_say_it_is_not_installed_and_cannot_be_uninstalled()
            {
                Results.Values.Should().Contain(r => r.Messages.Any(m => m.MessageType == ResultType.Error && m.Message.Contains("somethingnonexisting is not installed. Cannot uninstall a non-existent package.")));
            }
        }

        [ConcernFor("uninstall")]
        public class When_uninstalling_a_package_that_is_not_installed_with_a_service_that_words_it_differently : InstallScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Service.NotInstalledMessage = "There is nothing called somethingnonexisting to uninstall.";
            }

            public override void Because()
            {
                Configuration = Scenario.Uninstall();
                Configuration.PackageNames = Configuration.Input = "somethingnonexisting";
                Results = Service.Uninstall(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_report_the_message_of_the_service()
            {
                Results.Values.Should().Contain(r => r.Messages.Any(m => m.MessageType == ResultType.Error && m.Message.Contains("There is nothing called somethingnonexisting to uninstall.")));
            }

            [Fact]
            public void Should_not_report_the_default_message()
            {
                Results.Values.Should().NotContain(r => r.Messages.Any(m => m.Message.Contains("Cannot uninstall a non-existent package.")));
            }
        }

        // A package outside lib needs isexactversiondependency at exactly 1.0.1, so hasdependency, which needs it at 1.1.0,
        // cannot be installed or upgraded.
        public abstract class ResolutionConflictScenariosBase : InstallScenariosBase
        {
            protected const string Explanation = "hasdependency cannot be installed here, and this says why.";

            public override void Context()
            {
                base.Context();

                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.0.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.1.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1.0.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1.1.0*" + NuGetConstants.PackageExtension);

                Service.ExternalDependencies = new[]
                {
                    new SourcePackageDependencyInfo(
                        new PackageIdentity("outsidelibpackage", NuGetVersion.Parse("1.0.0")),
                        new[] { new PackageDependency("isexactversiondependency", VersionRange.Parse("[1.0.1]")) },
                        true,
                        NugetCommon.GetLocalRepository(),
                        null,
                        null)
                };
            }

            protected bool ReportedError(string text)
            {
                return Results.Values.Any(r => r.Messages.Any(m => m.MessageType == ResultType.Error && m.Message.Contains(text)));
            }
        }

        public class When_installing_a_package_whose_dependencies_cannot_be_resolved_with_a_service_that_overrides_nothing : ResolutionConflictScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Scenario.InstallPackage(Configuration, "isexactversiondependency", "1.0.1");
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Configuration.Version = "1.1.0";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration, null);
            }

            [Fact]
            public void Should_report_the_resolver_message()
            {
                ReportedError("outsidelibpackage").Should().BeTrue();
            }
        }

        public class When_installing_a_package_whose_dependencies_cannot_be_resolved_with_a_service_that_explains_it : ResolutionConflictScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Scenario.InstallPackage(Configuration, "isexactversiondependency", "1.0.1");
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Configuration.Version = "1.1.0";
                Service.ResolutionErrorMessage = Explanation;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration, null);
            }

            [Fact]
            public void Should_ask_the_service_with_the_package_being_installed()
            {
                Service.Events.Should().Contain("GetDependencyResolutionErrorMessage:hasdependency");
            }

            [Fact]
            public void Should_report_the_message_of_the_service()
            {
                ReportedError(Explanation).Should().BeTrue();
            }

            [Fact]
            public void Should_not_report_the_resolver_message()
            {
                ReportedError("outsidelibpackage").Should().BeFalse();
            }
        }

        public class When_upgrading_a_package_whose_dependencies_cannot_be_resolved_with_a_service_that_explains_it : ResolutionConflictScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration = Scenario.Upgrade();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.0.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.1.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1.0.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1.1.0*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.1");
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Service.ResolutionErrorMessage = Explanation;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_ask_the_service_with_the_package_being_upgraded()
            {
                Service.Events.Should().Contain("GetDependencyResolutionErrorMessage:hasdependency");
            }

            [Fact]
            public void Should_report_the_message_of_the_service()
            {
                ReportedError(Explanation).Should().BeTrue();
            }
        }

        // hasdependency 2.0.1 is installed with isdependency 1.0.0. Version 2.1.0 replaces isdependency and also
        // needs isexactversiondependency, which is new to the machine.
        [ConcernFor("upgrade")]
        public abstract class UpgradeScenariosBase : InstallScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration = Scenario.Upgrade();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.2.0.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.2.1.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.2.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.2.0.0*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "hasdependency", "2.0.1");

                Configuration.PackageNames = Configuration.Input = "hasdependency";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration, (result, config) => Service.Events.Add("ContinueAction:" + result.Name), performAction: true);
            }

            protected static string LibPath(string id)
            {
                return Path.Combine(Scenario.GetTopLevel(), "lib", id);
            }
        }

        public class When_upgrading_a_package_and_the_service_refuses_a_dependency_that_is_new_to_the_machine : UpgradeScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Service.RefusedId = "isexactversiondependency";
                Service.RefusalReason = "isexactversiondependency may not be installed here.";
            }

            [Fact]
            public void Should_ask_the_service_about_the_new_dependency()
            {
                Service.Events.Should().Contain("GetInstallRefusalReason:isexactversiondependency");
            }

            [Fact]
            public void Should_record_the_reason_as_an_error_for_the_dependency()
            {
                Results.Values.Should().Contain(r => r.Messages.Any(m => m.MessageType == ResultType.Error && m.Message == "isexactversiondependency may not be installed here."));
            }

            [Fact]
            public void Should_remove_the_files_of_the_refused_dependency()
            {
                Service.Events.Should().Contain(e => e.StartsWith("RemoveRefusedPackageFiles:", StringComparison.Ordinal));
                Directory.Exists(LibPath("isexactversiondependency")).Should().BeFalse();
            }

            [Fact]
            public void Should_not_upgrade_the_package_that_needs_it()
            {
                Results.Values.Should().NotContain(r => r.Name == "hasdependency" && r.Version == "2.1.0" && r.Success);
                File.ReadAllText(Path.Combine(LibPath("hasdependency"), "hasdependency.nuspec")).Should().Contain("<version>2.0.1</version>");
            }
        }

        public class When_upgrading_a_package_with_a_service_that_refuses_nothing : UpgradeScenariosBase
        {
            [Fact]
            public void Should_upgrade_the_package()
            {
                File.ReadAllText(Path.Combine(LibPath("hasdependency"), "hasdependency.nuspec")).Should().Contain("<version>2.1.0</version>");
            }

            [Fact]
            public void Should_ask_the_service_only_about_the_package_that_is_new_to_the_machine()
            {
                Service.Events.Where(e => e.StartsWith("GetInstallRefusalReason:", StringComparison.Ordinal))
                    .Should().Equal("GetInstallRefusalReason:isexactversiondependency");
            }
        }
    }
}
