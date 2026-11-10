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
using chocolatey.infrastructure.app.nuget;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.results;
using chocolatey.infrastructure.services;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NUnit.Framework;
using FluentAssertions;
using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

namespace chocolatey.tests.integration.scenarios
{
    // An extension can install packages outside of lib (the licensed extension does, for package instances).
    // Those packages still need the packages they depend on, so GetExternalInstalledDependencyInfos lets the
    // service be told about them and the install, upgrade and uninstall resolutions keep them satisfied.
    public class ExternalInstalledDependencyScenarios
    {
        // A service that reports packages installed outside lib, as the extension's service does.
        public class NugetServiceWithExternalDependencies : NugetService
        {
            private readonly IEnumerable<SourcePackageDependencyInfo> _externalDependencies;

            public NugetServiceWithExternalDependencies(
                IFileSystem fileSystem,
                ILogger nugetLogger,
                IChocolateyPackageInformationService packageInfoService,
                IFilesService filesService,
                IRuleService ruleService,
                IEnumerable<SourcePackageDependencyInfo> externalDependencies)
                : base(fileSystem, nugetLogger, packageInfoService, filesService, ruleService)
            {
                _externalDependencies = externalDependencies;
            }

            protected override IEnumerable<SourcePackageDependencyInfo> GetExternalInstalledDependencyInfos(ChocolateyConfiguration config)
            {
                return _externalDependencies;
            }

            public IEnumerable<SourcePackageDependencyInfo> DefaultExternalInstalledDependencyInfos(ChocolateyConfiguration config)
            {
                return base.GetExternalInstalledDependencyInfos(config);
            }
        }

        public abstract class ScenariosBase : TinySpec
        {
            protected ConcurrentDictionary<string, PackageResult> Results;
            protected ChocolateyConfiguration Configuration;

            // The package outside lib that needs the given dependency, under an identity no package in lib has.
            protected static SourcePackageDependencyInfo OutsideLibPackageNeeding(string dependencyId, string versionRange)
            {
                return new SourcePackageDependencyInfo(
                    new PackageIdentity("outsidelibpackage", NuGetVersion.Parse("1.0.0")),
                    new[] { new PackageDependency(dependencyId, VersionRange.Parse(versionRange)) },
                    true,
                    NugetCommon.GetLocalRepository(),
                    null,
                    null);
            }

            protected static NugetServiceWithExternalDependencies ServiceReporting(params SourcePackageDependencyInfo[] externalDependencies)
            {
                var container = NUnitSetup.Container;

                return new NugetServiceWithExternalDependencies(
                    container.GetInstance<IFileSystem>(),
                    container.GetInstance<ILogger>(),
                    container.GetInstance<IChocolateyPackageInformationService>(),
                    container.GetInstance<IFilesService>(),
                    container.GetInstance<IRuleService>(),
                    externalDependencies);
            }

            protected static string LibPathFor(string id)
            {
                return Path.Combine(Scenario.GetTopLevel(), "lib", id);
            }

            protected static string InstalledVersionOf(string id)
            {
                var nupkg = Path.Combine(LibPathFor(id), id + NuGetConstants.PackageExtension);
                if (!File.Exists(nupkg))
                {
                    return null;
                }

                using (var reader = new PackageArchiveReader(nupkg))
                {
                    return reader.NuspecReader.GetVersion().ToNormalizedString();
                }
            }

            protected bool Said(string text)
            {
                return Results != null && Results.Values.Any(r => r.Messages.Any(m => m.Message.Contains(text)));
            }
        }

        [ConcernFor("uninstall")]
        public abstract class UninstallScenariosBase : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.Uninstall();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Configuration.PackageNames = Configuration.Input = "isdependency";
            }
        }

        public class When_the_external_installed_dependencies_are_not_overridden : UninstallScenariosBase
        {
            private IEnumerable<SourcePackageDependencyInfo> _dependencies;

            public override void Because()
            {
                _dependencies = ServiceReporting().DefaultExternalInstalledDependencyInfos(Configuration);
            }

            [Fact]
            public void Should_report_no_packages_outside_lib()
            {
                _dependencies.Should().BeEmpty();
            }
        }

        public class When_uninstalling_a_package_that_nothing_outside_lib_needs : UninstallScenariosBase
        {
            public override void Because()
            {
                Results = ServiceReporting().Uninstall(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_remove_the_package()
            {
                Directory.Exists(LibPathFor("isdependency")).Should().BeFalse();
            }
        }

        public class When_uninstalling_a_package_and_the_external_dependencies_are_null : UninstallScenariosBase
        {
            public override void Because()
            {
                var service = new NugetServiceWithExternalDependencies(
                    NUnitSetup.Container.GetInstance<IFileSystem>(),
                    NUnitSetup.Container.GetInstance<ILogger>(),
                    NUnitSetup.Container.GetInstance<IChocolateyPackageInformationService>(),
                    NUnitSetup.Container.GetInstance<IFilesService>(),
                    NUnitSetup.Container.GetInstance<IRuleService>(),
                    null);

                Results = service.Uninstall(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_remove_the_package_as_if_there_were_none()
            {
                Directory.Exists(LibPathFor("isdependency")).Should().BeFalse();
            }
        }

        public class When_uninstalling_a_package_that_a_package_outside_lib_needs : UninstallScenariosBase
        {
            public override void Because()
            {
                Results = ServiceReporting(OutsideLibPackageNeeding("isdependency", "1.0.0")).Uninstall(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_leave_the_package_installed()
            {
                Directory.Exists(LibPathFor("isdependency")).Should().BeTrue();
            }

            [Fact]
            public void Should_say_which_package_depends_on_it()
            {
                Said("depends on it").Should().BeTrue();
            }

            [Fact]
            public void Should_say_the_package_that_depends_on_it()
            {
                Said("outsidelibpackage").Should().BeTrue();
            }
        }

        public class When_force_uninstalling_a_package_that_a_package_outside_lib_needs : UninstallScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = ServiceReporting(OutsideLibPackageNeeding("isdependency", "1.0.0")).Uninstall(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_remove_the_package_as_it_does_for_a_package_in_lib()
            {
                Directory.Exists(LibPathFor("isdependency")).Should().BeFalse();
            }
        }

        [ConcernFor("install")]
        public abstract class InstallScenariosBase : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.Install();
                Scenario.Reset(Configuration);

                // isexactversiondependency 1.0.1 is installed, and hasdependency 1.1.0 needs it at 1.1.0.
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.1.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1.0.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1.1.0*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isexactversiondependency", "1.0.1");

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Configuration.Version = "1.1.0";
            }
        }

        public class When_installing_a_package_that_needs_another_version_of_a_dependency_and_nothing_outside_lib_needs_it : InstallScenariosBase
        {
            public override void Because()
            {
                Results = ServiceReporting().Install(Configuration, null);
            }

            [Fact]
            public void Should_install_the_package()
            {
                InstalledVersionOf("hasdependency").Should().Be("1.1.0");
            }

            [Fact]
            public void Should_change_the_dependency_to_the_version_the_package_needs()
            {
                InstalledVersionOf("isexactversiondependency").Should().Be("1.1.0");
            }
        }

        public class When_installing_a_package_that_needs_another_version_of_a_dependency_than_a_package_outside_lib_accepts : InstallScenariosBase
        {
            public override void Because()
            {
                Results = ServiceReporting(OutsideLibPackageNeeding("isexactversiondependency", "[1.0.1]")).Install(Configuration, null);
            }

            [Fact]
            public void Should_say_that_the_dependency_conflicts_with_the_package_outside_lib()
            {
                // Without the dependencies of that package in the resolution, nothing is installed and nothing is said.
                Results.Values.Should().Contain(r => r.Messages.Any(m => m.MessageType == ResultType.Error
                    && m.Message.Contains("outsidelibpackage")
                    && m.Message.Contains("isexactversiondependency (= 1.0.1)")));
            }
            [Fact]
            public void Should_not_install_the_package()
            {
                InstalledVersionOf("hasdependency").Should().BeNull();
            }

            [Fact]
            public void Should_leave_the_dependency_at_the_version_the_package_outside_lib_accepts()
            {
                InstalledVersionOf("isexactversiondependency").Should().Be("1.0.1");
            }
        }

        public class When_installing_a_package_and_a_package_outside_lib_needs_a_dependency_that_is_not_installed : InstallScenariosBase
        {
            public override void Because()
            {
                Results = ServiceReporting(OutsideLibPackageNeeding("dependencynobodyinstalled", "[1.0.0]")).Install(Configuration, null);
            }

            [Fact]
            public void Should_install_the_package()
            {
                InstalledVersionOf("hasdependency").Should().Be("1.1.0");
            }

            [Fact]
            public void Should_not_report_an_error()
            {
                Results.Values.Should().NotContain(r => r.Messages.Any(m => m.MessageType == ResultType.Error));
            }

            [Fact]
            public void Should_not_install_the_missing_dependency()
            {
                InstalledVersionOf("dependencynobodyinstalled").Should().BeNull();
            }
        }

        [ConcernFor("upgrade")]
        public abstract class UpgradeScenariosBase : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.Upgrade();
                Scenario.Reset(Configuration);

                // hasdependency 1.0.1 is installed with isexactversiondependency 1.0.1, and 1.1.0 needs it at 1.1.0.
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.0.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.1.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1.0.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1.1.0*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.1");

                Configuration.PackageNames = Configuration.Input = "hasdependency";
            }
        }

        public class When_upgrading_a_package_that_needs_another_version_of_a_dependency_and_nothing_outside_lib_needs_it : UpgradeScenariosBase
        {
            public override void Because()
            {
                Results = ServiceReporting().Upgrade(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                InstalledVersionOf("hasdependency").Should().Be("1.1.0");
            }

            [Fact]
            public void Should_change_the_dependency_to_the_version_the_package_needs()
            {
                InstalledVersionOf("isexactversiondependency").Should().Be("1.1.0");
            }
        }

        public class When_upgrading_a_package_that_needs_another_version_of_a_dependency_than_a_package_outside_lib_accepts : UpgradeScenariosBase
        {
            public override void Because()
            {
                Results = ServiceReporting(OutsideLibPackageNeeding("isexactversiondependency", "[1.0.1]")).Upgrade(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_say_that_the_dependency_conflicts_with_the_package_outside_lib()
            {
                // Without the dependencies of that package in the resolution, nothing is upgraded and nothing is said.
                Results.Values.Should().Contain(r => r.Messages.Any(m => m.MessageType == ResultType.Error
                    && m.Message.Contains("outsidelibpackage")
                    && m.Message.Contains("isexactversiondependency (= 1.0.1)")));
            }
            [Fact]
            public void Should_not_upgrade_the_package()
            {
                InstalledVersionOf("hasdependency").Should().Be("1.0.1");
            }

            [Fact]
            public void Should_leave_the_dependency_at_the_version_the_package_outside_lib_accepts()
            {
                InstalledVersionOf("isexactversiondependency").Should().Be("1.0.1");
            }
        }

        public class When_upgrading_a_package_and_a_package_outside_lib_needs_a_dependency_that_is_not_installed : UpgradeScenariosBase
        {
            public override void Because()
            {
                Results = ServiceReporting(OutsideLibPackageNeeding("dependencynobodyinstalled", "[1.0.0]")).Upgrade(Configuration, null, performAction: true);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                InstalledVersionOf("hasdependency").Should().Be("1.1.0");
            }

            [Fact]
            public void Should_not_report_an_error()
            {
                Results.Values.Should().NotContain(r => r.Messages.Any(m => m.MessageType == ResultType.Error));
            }

            [Fact]
            public void Should_not_install_the_missing_dependency()
            {
                InstalledVersionOf("dependencynobodyinstalled").Should().BeNull();
            }
        }
    }
}
