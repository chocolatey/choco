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
using System.Linq;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.app.registration;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.platforms;
using chocolatey.infrastructure.results;
using chocolatey.infrastructure.services;
using Moq;
using NuGet.Packaging;
using NuGet.Versioning;
using FluentAssertions;
using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

namespace chocolatey.tests.infrastructure.app.services
{
    // Each overridable protected member of the service is overridden here by a service that records when it
    // is called, to prove it is called from the right place and that its result is what the service uses.
    public class ChocolateyPackageServiceSeamSpecs
    {
        public class RecordingPackageService : ChocolateyPackageService
        {
            public readonly List<string> Events = new List<string>();
            public ChocolateyPackageInformation InformationToReturn;
            public ChocolateyPackageInformation InformationUpdated;
            public bool SkipUpdatingInformation;
            public bool SkipInstallingShims;
            public bool SkipUninstallingShims;
            public bool SkipRemovingInformation;
            public string DisplayNamePrefix;
            public string ParametersFromTheHook;

            public RecordingPackageService(
                INugetService nugetService,
                IPowershellService powershellService,
                IShimGenerationService shimgenService,
                IFileSystem fileSystem,
                IRegistryService registryService,
                IChocolateyPackageInformationService packageInfoService,
                IFilesService filesService,
                IAutomaticUninstallerService autoUninstallerService,
                IXmlService xmlService,
                IConfigTransformService configTransformService,
                IContainerResolver containerResolver)
                : base(nugetService, powershellService, shimgenService, fileSystem, registryService, packageInfoService, filesService, autoUninstallerService, xmlService, configTransformService, containerResolver)
            {
            }

            protected override ChocolateyPackageInformation GetPackageInformation(PackageResult packageResult, ChocolateyConfiguration config)
            {
                Events.Add("GetPackageInformation:" + packageResult.Name);
                return InformationToReturn ?? base.GetPackageInformation(packageResult, config);
            }

            protected override void UpdatePackageInformation(ChocolateyPackageInformation pkgInfo)
            {
                Events.Add("UpdatePackageInformation");
                InformationUpdated = pkgInfo;
                if (!SkipUpdatingInformation)
                {
                    base.UpdatePackageInformation(pkgInfo);
                }
            }

            protected override void InstallShims(ChocolateyConfiguration config, PackageResult packageResult)
            {
                Events.Add("InstallShims:" + packageResult.Name);
                if (!SkipInstallingShims)
                {
                    base.InstallShims(config, packageResult);
                }
            }

            protected override void UninstallShims(ChocolateyConfiguration config, PackageResult packageResult)
            {
                Events.Add("UninstallShims:" + packageResult.Name);
                if (!SkipUninstallingShims)
                {
                    base.UninstallShims(config, packageResult);
                }
            }

            protected override void RemovePackageInformation(ChocolateyConfiguration config, PackageResult packageResult)
            {
                Events.Add("RemovePackageInformation:" + packageResult.Name);
                if (!SkipRemovingInformation)
                {
                    base.RemovePackageInformation(config, packageResult);
                }
            }

            protected override string GetPackageDisplayName(PackageResult packageResult)
            {
                return DisplayNamePrefix == null ? base.GetPackageDisplayName(packageResult) : DisplayNamePrefix + packageResult.Name;
            }

            protected override void ApplyAdditionalPackagesConfigSettings(ChocolateyConfiguration packageConfig, PackagesConfigFilePackageSetting pkgSettings)
            {
                Events.Add("ApplyAdditionalPackagesConfigSettings:" + pkgSettings.Id);
                if (ParametersFromTheHook != null)
                {
                    packageConfig.PackageParameters = ParametersFromTheHook;
                }

                base.ApplyAdditionalPackagesConfigSettings(packageConfig, pkgSettings);
            }
        }

        public abstract class ChocolateyPackageServiceSeamSpecsBase : TinySpec
        {
            protected RecordingPackageService Service;
            protected Mock<INugetService> NugetService;
            protected Mock<IPowershellService> PowershellService;
            protected Mock<IShimGenerationService> ShimGenerationService;
            protected Mock<IFileSystem> FileSystem;
            protected Mock<IRegistryService> RegistryService;
            protected Mock<IChocolateyPackageInformationService> PackageInformationService;
            protected Mock<IFilesService> FilesService;
            protected Mock<IAutomaticUninstallerService> AutomaticUninstallerService;
            protected Mock<IXmlService> XmlService;
            protected Mock<IConfigTransformService> ConfigTransformService;
            protected Mock<IContainerResolver> ContainerResolver;
            protected ChocolateyConfiguration Configuration;
            protected Mock<IPackageMetadata> Package;
            protected PackageResult Result;
            protected ChocolateyPackageInformation Information;
            private int _exitCodeBefore;

            public override void Context()
            {
                _exitCodeBefore = Environment.ExitCode;
                NugetService = new Mock<INugetService>();
                PowershellService = new Mock<IPowershellService>();
                ShimGenerationService = new Mock<IShimGenerationService>();
                FileSystem = new Mock<IFileSystem>();
                RegistryService = new Mock<IRegistryService>();
                PackageInformationService = new Mock<IChocolateyPackageInformationService>();
                FilesService = new Mock<IFilesService>();
                AutomaticUninstallerService = new Mock<IAutomaticUninstallerService>();
                XmlService = new Mock<IXmlService>();
                ConfigTransformService = new Mock<IConfigTransformService>();
                ContainerResolver = new Mock<IContainerResolver>();
                MockLogger.Reset();

                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string[]>()))
                    .Returns<string, string[]>((left, right) => System.IO.Path.Combine(new[] { left }.Concat(right).ToArray()));

                Configuration = new ChocolateyConfiguration();
                Configuration.Information.PlatformType = PlatformType.Windows;
                Configuration.SkipPackageInstallProvider = true;

                Package = new Mock<IPackageMetadata>();
                Package.Setup(x => x.Id).Returns("bob");
                Package.Setup(x => x.Version).Returns(NuGetVersion.Parse("1.0.0"));
                Result = new PackageResult(Package.Object, @"c:\lib\bob");
                Information = new ChocolateyPackageInformation(Package.Object);
                PackageInformationService.Setup(x => x.Get(Package.Object)).Returns(Information);

                Service = new RecordingPackageService(
                    NugetService.Object,
                    PowershellService.Object,
                    ShimGenerationService.Object,
                    FileSystem.Object,
                    RegistryService.Object,
                    PackageInformationService.Object,
                    FilesService.Object,
                    AutomaticUninstallerService.Object,
                    XmlService.Object,
                    ConfigTransformService.Object,
                    ContainerResolver.Object);
            }

            // Handling a reboot sets the exit code of the process, which the other specs must not see.
            public override void AfterObservations()
            {
                base.AfterObservations();
                Environment.ExitCode = _exitCodeBefore;
            }

            protected bool Logged(string text)
            {
                return MockLogger.Messages.Values.SelectMany(messages => messages).Any(message => message.Contains(text));
            }
        }

        public class When_handling_the_result_of_an_install_with_a_service_that_overrides_nothing : ChocolateyPackageServiceSeamSpecsBase
        {
            public override void Because()
            {
                Service.HandlePackageResult(Result, Configuration, CommandNameType.Install);
            }

            [Fact]
            public void Should_read_the_information_of_the_package_it_is_handling()
            {
                PackageInformationService.Verify(x => x.Get(Package.Object), Times.Once);
            }

            [Fact]
            public void Should_save_the_information_it_read()
            {
                PackageInformationService.Verify(x => x.Save(Information), Times.Once);
            }

            [Fact]
            public void Should_create_the_shims_of_the_package()
            {
                ShimGenerationService.Verify(x => x.Install(Configuration, Result), Times.Once);
            }

            [Fact]
            public void Should_call_the_members_in_the_order_the_result_is_handled()
            {
                Service.Events.Should().ContainInOrder("GetPackageInformation:bob", "InstallShims:bob", "UpdatePackageInformation");
            }

            [Fact]
            public void Should_show_the_package_by_its_id()
            {
                Logged("bob package files install completed").Should().BeTrue();
                Logged("The install of bob was successful.").Should().BeTrue();
            }
        }

        public class When_handling_the_result_of_an_install_with_a_service_that_keeps_the_information_itself : ChocolateyPackageServiceSeamSpecsBase
        {
            private ChocolateyPackageInformation _other;

            public override void Context()
            {
                base.Context();

                _other = new ChocolateyPackageInformation(Package.Object);
                Service.InformationToReturn = _other;
                Service.SkipUpdatingInformation = true;
            }

            public override void Because()
            {
                Service.HandlePackageResult(Result, Configuration, CommandNameType.Install);
            }

            [Fact]
            public void Should_use_the_information_the_service_returned()
            {
                Service.InformationUpdated.Should().BeSameAs(_other);
            }

            [Fact]
            public void Should_not_read_the_information_itself()
            {
                PackageInformationService.Verify(x => x.Get(It.IsAny<IPackageMetadata>()), Times.Never);
            }

            [Fact]
            public void Should_leave_saving_the_information_to_the_service()
            {
                PackageInformationService.Verify(x => x.Save(It.IsAny<ChocolateyPackageInformation>()), Times.Never);
            }
        }

        public class When_handling_the_result_of_an_install_with_a_service_that_creates_the_shims_itself : ChocolateyPackageServiceSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Service.SkipInstallingShims = true;
            }

            public override void Because()
            {
                Service.HandlePackageResult(Result, Configuration, CommandNameType.Install);
            }

            [Fact]
            public void Should_ask_the_service_for_the_shims()
            {
                Service.Events.Should().Contain("InstallShims:bob");
            }

            [Fact]
            public void Should_not_create_the_shims_itself()
            {
                ShimGenerationService.Verify(x => x.Install(It.IsAny<ChocolateyConfiguration>(), It.IsAny<PackageResult>()), Times.Never);
            }

            [Fact]
            public void Should_still_save_the_information()
            {
                PackageInformationService.Verify(x => x.Save(Information), Times.Once);
            }
        }

        public class When_handling_the_result_of_an_install_with_a_service_that_shows_the_package_under_another_name : ChocolateyPackageServiceSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Service.DisplayNamePrefix = "Shown as ";
            }

            public override void Because()
            {
                Service.HandlePackageResult(Result, Configuration, CommandNameType.Install);
            }

            [Fact]
            public void Should_use_that_name_while_the_result_is_handled()
            {
                Logged("Shown as bob package files install completed").Should().BeTrue();
            }

            [Fact]
            public void Should_use_that_name_when_it_reports_the_install_as_successful()
            {
                Logged("The install of Shown as bob was successful.").Should().BeTrue();
            }
        }

        public abstract class UninstallSeamSpecsBase : ChocolateyPackageServiceSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.Features.RemovePackageInformationOnUninstall = true;
            }
        }

        public class When_handling_an_uninstall_with_a_service_that_overrides_nothing : UninstallSeamSpecsBase
        {
            public override void Because()
            {
                Service.HandlePackageUninstall(Result, Configuration);
            }

            [Fact]
            public void Should_remove_the_shims_of_the_package()
            {
                ShimGenerationService.Verify(x => x.Uninstall(Configuration, Result), Times.Once);
            }

            [Fact]
            public void Should_remove_the_information_of_the_package()
            {
                PackageInformationService.Verify(x => x.Remove(Package.Object), Times.Once);
            }

            [Fact]
            public void Should_remove_the_shims_before_the_information()
            {
                Service.Events.Should().ContainInOrder("UninstallShims:bob", "RemovePackageInformation:bob");
            }
        }

        public class When_handling_an_uninstall_with_a_service_that_removes_the_shims_and_information_itself : UninstallSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Service.SkipUninstallingShims = true;
                Service.SkipRemovingInformation = true;
            }

            public override void Because()
            {
                Service.HandlePackageUninstall(Result, Configuration);
            }

            [Fact]
            public void Should_ask_the_service_for_both()
            {
                Service.Events.Should().Contain(new[] { "UninstallShims:bob", "RemovePackageInformation:bob" });
            }

            [Fact]
            public void Should_not_remove_the_shims_itself()
            {
                ShimGenerationService.Verify(x => x.Uninstall(It.IsAny<ChocolateyConfiguration>(), It.IsAny<PackageResult>()), Times.Never);
            }

            [Fact]
            public void Should_not_remove_the_information_itself()
            {
                PackageInformationService.Verify(x => x.Remove(It.IsAny<IPackageMetadata>()), Times.Never);
            }
        }

        public class When_handling_an_uninstall_when_the_information_is_not_to_be_removed : ChocolateyPackageServiceSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.Features.RemovePackageInformationOnUninstall = false;
            }

            public override void Because()
            {
                Service.HandlePackageUninstall(Result, Configuration);
            }

            [Fact]
            public void Should_not_ask_the_service_to_remove_the_information()
            {
                Service.Events.Should().NotContain("RemovePackageInformation:bob");
            }
        }

        public abstract class PackagesConfigSpecsBase : ChocolateyPackageServiceSeamSpecsBase
        {
            protected readonly List<string> ParametersSeenByTheInstall = new List<string>();
            protected ConcurrentDictionary<string, PackageResult> Results;
            protected bool InstallFails;
            protected bool InstallWarns;
            protected int ExitCodeOfTheInstall;
            protected bool KeyResultsByInstance;

            protected virtual int EntryCount
            {
                get { return 2; }
            }

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = @"C:\test\packages.config";
                Configuration.Sources = @"C:\test";
                ContainerResolver.Setup(c => c.ResolveAll<IInstallSourceRunner>()).Returns(new IInstallSourceRunner[0]);
                FileSystem.Setup(f => f.GetFullPath(Configuration.PackageNames)).Returns(Configuration.PackageNames);
                FileSystem.Setup(f => f.FileExists(Configuration.PackageNames)).Returns(true);

                // The same package twice, as two instances of it are, with different parameters.
                XmlService.Setup(x => x.Deserialize<PackagesConfigFileSettings>(Configuration.PackageNames))
                    .Returns(new PackagesConfigFileSettings
                    {
                        Packages = new HashSet<PackagesConfigFilePackageSetting>(Enumerable.Range(1, EntryCount).Select(n => new PackagesConfigFilePackageSetting
                        {
                            Id = "bob",
                            PackageParameters = n == 1 ? "first standard parameters" : n == 2 ? "second standard parameters" : "standard parameters " + n
                        }))
                    });

                var installs = 0;
                NugetService.Setup(n => n.Install(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()))
                    .Returns<ChocolateyConfiguration, Action<PackageResult, ChocolateyConfiguration>, Action<PackageResult, ChocolateyConfiguration>>((config, action, beforeModify) =>
                    {
                        var results = new ConcurrentDictionary<string, PackageResult>();

                        // After the entries of the packages.config, the rest of the input is installed, and here that is nothing.
                        if (string.IsNullOrWhiteSpace(config.PackageNames))
                        {
                            return results;
                        }

                        ParametersSeenByTheInstall.Add(config.PackageParameters);
                        installs++;
                        var key = KeyResultsByInstance ? "bob:instance" + installs : "bob";
                        results[key] = new PackageResult(Package.Object, @"c:\lib\bob") { Instance = "instance" + installs };
                        if (InstallFails)
                        {
                            results[key].Messages.Add(new ResultMessage(ResultType.Error, "the install failed"));
                        }

                        if (InstallWarns)
                        {
                            results[key].Messages.Add(new ResultMessage(ResultType.Warn, "it warned"));
                        }

                        results[key].ExitCode = ExitCodeOfTheInstall;
                        return results;
                    });
            }
        }

        public class When_installing_the_same_package_twice_from_a_packages_config_and_the_installs_return_the_same_key : PackagesConfigSpecsBase
        {
            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_collect_the_two_results_under_the_one_key_the_install_returned()
            {
                Results.Keys.Should().Equal("bob");
            }

            [Fact]
            public void Should_keep_the_first_result()
            {
                Results["bob"].Instance.Should().Be("instance1");
            }

            [Fact]
            public void Should_install_each_entry_with_the_parameters_it_has_in_the_packages_config()
            {
                ParametersSeenByTheInstall.Should().Equal("first standard parameters", "second standard parameters");
            }
        }

        public class When_installing_the_same_package_twice_from_a_packages_config_and_the_installs_key_their_results_apart : PackagesConfigSpecsBase
        {
            public override void Context()
            {
                base.Context();

                KeyResultsByInstance = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_collect_each_result_under_the_key_the_install_gave_it()
            {
                Results.Keys.Should().BeEquivalentTo("bob:instance1", "bob:instance2");
            }
        }

        public class When_installing_from_a_packages_config_with_a_service_that_adds_to_each_entry : PackagesConfigSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Service.ParametersFromTheHook = "parameters from the service";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_be_asked_about_each_entry_of_the_packages_config()
            {
                Service.Events.Count(e => e == "ApplyAdditionalPackagesConfigSettings:bob").Should().Be(2);
            }

            [Fact]
            public void Should_apply_what_the_service_adds_after_the_standard_settings()
            {
                ParametersSeenByTheInstall.Should().Equal("parameters from the service", "parameters from the service");
            }
        }
        public class When_installing_from_a_packages_config_and_it_fails_with_a_service_that_shows_the_package_under_another_name : PackagesConfigSpecsBase
        {
            public override void Context()
            {
                base.Context();

                InstallFails = true;
                Service.DisplayNamePrefix = "Shown as ";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_list_the_failed_package_under_that_name_in_the_summary()
            {
                Logged(" - Shown as bob").Should().BeTrue();
            }

            [Fact]
            public void Should_not_list_it_under_its_id()
            {
                Logged(" - bob").Should().BeFalse();
            }
        }

        public class When_handling_a_failed_install_with_a_service_that_shows_the_package_under_another_name : ChocolateyPackageServiceSeamSpecsBase
        {
            public override void Context()
            {
                base.Context();

                Service.DisplayNamePrefix = "Shown as ";
                Configuration.PromptForConfirmation = false;
                Result.ExitCode = 1;
                Result.Messages.Add(new ResultMessage(ResultType.Error, "it failed"));
            }

            public override void Because()
            {
                Service.HandlePackageResult(Result, Configuration, CommandNameType.Install);
            }

            [Fact]
            public void Should_use_that_name_when_it_reports_the_files_as_failed_with_an_exit_code()
            {
                Logged("Shown as bob package files install failed with exit code 1").Should().BeTrue();
            }

            [Fact]
            public void Should_use_that_name_when_it_reports_the_install_as_not_successful()
            {
                Logged("The install of Shown as bob was NOT successful.").Should().BeTrue();
            }
        }

        public class When_handling_a_failed_install_that_stops_the_run_with_a_service_that_shows_the_package_under_another_name : ChocolateyPackageServiceSeamSpecsBase
        {
            private Exception _thrown;

            public override void Context()
            {
                base.Context();

                Service.DisplayNamePrefix = "Shown as ";
                Configuration.PromptForConfirmation = false;
                Configuration.Features.StopOnFirstPackageFailure = true;
                Result.ExitCode = 1;
                Result.Messages.Add(new ResultMessage(ResultType.Error, "it failed"));
            }

            public override void Because()
            {
                try
                {
                    Service.HandlePackageResult(Result, Configuration, CommandNameType.Install);
                }
                catch (Exception ex)
                {
                    _thrown = ex;
                }
            }

            [Fact]
            public void Should_stop_with_a_message_that_uses_that_name()
            {
                _thrown.Should().BeOfType<ApplicationException>();
                _thrown.Message.Should().Contain("Stopping further execution as Shown as bob has failed install");
            }
        }

        public class When_handling_an_install_that_needs_a_reboot_with_a_service_that_shows_the_package_under_another_name : ChocolateyPackageServiceSeamSpecsBase
        {
            private Exception _thrown;

            public override void Context()
            {
                base.Context();

                Service.DisplayNamePrefix = "Shown as ";
                Configuration.Features.ExitOnRebootDetected = true;
                Result.ExitCode = 3010;
            }

            public override void Because()
            {
                try
                {
                    Service.HandlePackageResult(Result, Configuration, CommandNameType.Install);
                }
                catch (Exception ex)
                {
                    _thrown = ex;
                }
            }

            [Fact]
            public void Should_stop_the_run()
            {
                _thrown.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_say_which_package_needs_the_reboot_by_that_name()
            {
                Logged("after installing/upgrading").Should().BeTrue();
                Logged("package 'Shown as bob' - stopping further execution").Should().BeTrue();
            }
        }

        public class When_handling_a_failed_uninstall_with_a_service_that_shows_the_package_under_another_name : UninstallSeamSpecsBase
        {
            private Exception _thrown;

            public override void Context()
            {
                base.Context();

                Service.DisplayNamePrefix = "Shown as ";
                Configuration.PromptForConfirmation = false;
                Result.ExitCode = 1;
                Result.Messages.Add(new ResultMessage(ResultType.Error, "it failed"));
            }

            public override void Because()
            {
                try
                {
                    Service.HandlePackageUninstall(Result, Configuration);
                }
                catch (Exception ex)
                {
                    _thrown = ex;
                }
            }

            [Fact]
            public void Should_use_that_name_when_it_reports_the_uninstall_as_not_successful()
            {
                Logged("Shown as bob uninstall not successful.").Should().BeTrue();
            }

            [Fact]
            public void Should_stop_the_uninstall_with_a_message_that_uses_that_name()
            {
                _thrown.Should().BeOfType<ApplicationException>();
                _thrown.Message.Should().Contain("Shown as bob uninstall not successful.");
            }
        }

        public class When_handling_an_uninstall_that_needs_a_reboot_with_a_service_that_shows_the_package_under_another_name : UninstallSeamSpecsBase
        {
            private Exception _thrown;

            public override void Context()
            {
                base.Context();

                Service.DisplayNamePrefix = "Shown as ";
                Configuration.Features.ExitOnRebootDetected = true;
                Result.ExitCode = 3010;
            }

            public override void Because()
            {
                try
                {
                    Service.HandlePackageUninstall(Result, Configuration);
                }
                catch (Exception ex)
                {
                    _thrown = ex;
                }
            }

            [Fact]
            public void Should_stop_the_run()
            {
                _thrown.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_say_which_package_needs_the_reboot_by_that_name()
            {
                Logged("after uninstalling").Should().BeTrue();
                Logged("package 'Shown as bob' - stopping further execution").Should().BeTrue();
            }
        }

        public class When_installing_from_a_packages_config_and_it_warns_with_a_service_that_shows_the_package_under_another_name : PackagesConfigSpecsBase
        {
            public override void Context()
            {
                base.Context();

                InstallWarns = true;
                Service.DisplayNamePrefix = "Shown as ";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_list_the_package_with_a_warning_under_that_name_in_the_summary()
            {
                Logged(" - Shown as bob - it warned").Should().BeTrue();
            }
        }

        public class When_installing_from_a_packages_config_and_it_needs_a_reboot_with_a_service_that_shows_the_package_under_another_name : PackagesConfigSpecsBase
        {
            public override void Context()
            {
                base.Context();

                ExitCodeOfTheInstall = 3010;
                Service.DisplayNamePrefix = "Shown as ";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_list_the_package_that_needs_a_reboot_under_that_name_in_the_summary()
            {
                Logged(" - Shown as bob (exit code 3010)").Should().BeTrue();
            }
        }

        public class When_installing_five_packages_from_a_packages_config_with_a_service_that_shows_the_package_under_another_name : PackagesConfigSpecsBase
        {
            protected override int EntryCount
            {
                get { return 5; }
            }

            public override void Context()
            {
                base.Context();

                Service.DisplayNamePrefix = "Shown as ";
                KeyResultsByInstance = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_list_every_package_under_that_name_in_the_long_summary()
            {
                Results.Count.Should().Be(5);
                Logged(" - Shown as bob v1.0.0").Should().BeTrue();
            }
        }
    }
}
