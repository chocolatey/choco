// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.tests.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Moq;
    using NuGet;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.results;
    using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

    public class AutomaticUninstallerServiceSpecs
    {
        public abstract class AutomaticUninstallerServiceSpecsBase : TinySpec
        {
            protected AutomaticUninstallerService service;
            protected Mock<IChocolateyPackageInformationService> packageInfoService = new Mock<IChocolateyPackageInformationService>();
            protected Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            protected Mock<IProcess> process = new Mock<IProcess>();
            protected Mock<IRegistryService> registryService = new Mock<IRegistryService>();
            protected Mock<ICommandExecutor> commandExecutor = new Mock<ICommandExecutor>();
            protected ChocolateyConfiguration config = new ChocolateyConfiguration();
            protected Mock<IPackage> package = new Mock<IPackage>();
            protected ConcurrentDictionary<string, PackageResult> packageResults = new ConcurrentDictionary<string, PackageResult>();
            protected PackageResult packageResult;
            protected ChocolateyPackageInformation packageInformation;
            protected IList<RegistryApplicationKey> registryKeys = new List<RegistryApplicationKey>();
            protected IInstaller installerType = new CustomInstaller();

            protected readonly string originalUninstallString = @"""C:\Program Files (x86)\WinDirStat\Uninstall.exe""";
            protected readonly string expectedUninstallString = @"C:\Program Files (x86)\WinDirStat\Uninstall.exe";

            public override void Context()
            {
                CommandExecutor.initialize_with(new Lazy<IFileSystem>(() => fileSystem.Object), () => process.Object);

                service = new AutomaticUninstallerService(packageInfoService.Object, fileSystem.Object, registryService.Object, commandExecutor.Object);
                service.WaitForCleanup = false;
                config.Features.AutoUninstaller = true;
                config.PromptForConfirmation = false;
                package.Setup(p => p.Id).Returns("regular");
                package.Setup(p => p.Version).Returns(new SemanticVersion("1.2.0"));
                packageResult = new PackageResult(package.Object, null);
                packageInformation = new ChocolateyPackageInformation(package.Object);
                registryKeys.Add(new RegistryApplicationKey
                    {
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = originalUninstallString,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = installerType.InstallerType,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
                packageInfoService.Setup(s => s.get_package_information(package.Object)).Returns(packageInformation);
                packageResults.GetOrAdd("regular", packageResult);

                fileSystem.Setup(f => f.directory_exists(registryKeys.FirstOrDefault().InstallLocation)).Returns(true);
                registryService.Setup(r => r.installer_value_exists(registryKeys.FirstOrDefault().KeyPath, ApplicationParameters.RegistryValueInstallLocation)).Returns(true);
                fileSystem.Setup(f => f.get_full_path(expectedUninstallString)).Returns(expectedUninstallString);
            }
        }

        public class when_autouninstall_feature_is_off : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                config.Features.AutoUninstaller = false;
            }

            public override void Because()
            {
                service.run(packageResult, config);
            }

            [Fact]
            public void should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - AutoUninstaller feature is not enabled."), Times.Once);
            }

            [Fact]
            public void should_not_get_package_information()
            {
                packageInfoService.Verify(s => s.get_package_information(It.IsAny<IPackage>()), Times.Never);
            }

            [Fact]
            public void should_not_call_command_executor()
            {
                commandExecutor.Verify(c => c.execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Never);
            }
        }

        public class when_registry_snapshot_is_null : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                packageInformation.RegistrySnapshot = null;
            }

            public override void Because()
            {
                service.run(packageResult, config);
            }

            [Fact]
            public void should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - No registry snapshot."), Times.Once);
            }

            [Fact]
            public void should_not_call_command_executor()
            {
                commandExecutor.Verify(c => c.execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Never);
            }
        }

        public class when_registry_keys_are_empty : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                packageInformation.RegistrySnapshot = new Registry("123", null);
            }

            public override void Because()
            {
                service.run(packageResult, config);
            }

            [Fact]
            public void should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - No registry keys in snapshot."), Times.Once);
            }

            [Fact]
            public void should_not_call_command_executor()
            {
                commandExecutor.Verify(c => c.execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Never);
            }
        }

        public class when_install_location_does_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                fileSystem.ResetCalls();
                fileSystem.Setup(f => f.directory_exists(registryKeys.FirstOrDefault().InstallLocation)).Returns(false);
            }

            public override void Because()
            {
                service.run(packageResult, config);
            }

            [Fact]
            public void should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - The application appears to have been uninstalled already by other means."), Times.Once);
            }

            [Fact]
            public void should_not_call_command_executor()
            {
                commandExecutor.Verify(c => c.execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Never);
            }
        }

        public class when_install_location_is_empty : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                fileSystem.ResetCalls();
                registryKeys.Clear();
                registryKeys.Add(new RegistryApplicationKey
                    {
                        InstallLocation = string.Empty,
                        UninstallString = originalUninstallString,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat"
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
            }

            public override void Because()
            {
                service.run(packageResult, config);
            }

            [Fact]
            public void should_call_get_package_information()
            {
                packageInfoService.Verify(s => s.get_package_information(It.IsAny<IPackage>()), Times.Once);
            }

            [Fact]
            public void should_call_command_executor()
            {
                var args = installerType.build_uninstall_command_arguments().trim_safe();
                commandExecutor.Verify(c => c.execute(expectedUninstallString, args, It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Once);
            }
        }

        public class when_registry_location_does_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                registryService.ResetCalls();
                registryService.Setup(r => r.installer_value_exists(registryKeys.FirstOrDefault().KeyPath, ApplicationParameters.RegistryValueInstallLocation)).Returns(false);
            }

            public override void Because()
            {
                service.run(packageResult, config);
            }

            [Fact]
            public void should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - The application appears to have been uninstalled already by other means."), Times.Once);
            }

            [Fact]
            public void should_not_call_command_executor()
            {
                commandExecutor.Verify(c => c.execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Never);
            }
        }

        public class when_registry_location_and_install_location_both_do_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                fileSystem.ResetCalls();
                fileSystem.Setup(f => f.directory_exists(registryKeys.FirstOrDefault().InstallLocation)).Returns(false);
                registryService.ResetCalls();
                registryService.Setup(r => r.installer_value_exists(registryKeys.FirstOrDefault().KeyPath, ApplicationParameters.RegistryValueInstallLocation)).Returns(false);
            }

            public override void Because()
            {
                service.run(packageResult, config);
            }

            [Fact]
            public void should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - The application appears to have been uninstalled already by other means."), Times.Once);
            }

            [Fact]
            public void should_not_call_command_executor()
            {
                commandExecutor.Verify(c => c.execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Never);
            }
        }

        public class when_AutomaticUninstallerService_is_run_normally : AutomaticUninstallerServiceSpecsBase
        {
            public override void Because()
            {
                service.run(packageResult, config);
            }

            [Fact]
            public void should_call_get_package_information()
            {
                packageInfoService.Verify(s => s.get_package_information(It.IsAny<IPackage>()), Times.Once);
            }

            [Fact]
            public void should_call_command_executor()
            {
                commandExecutor.Verify(c => c.execute(expectedUninstallString, installerType.build_uninstall_command_arguments().trim_safe(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Once);
            }
        }

        public class when_AutomaticUninstallerService_cannot_determine_silent_install_arguments : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                registryKeys.Clear();
                commandExecutor.ResetCalls();
                registryKeys.Add(new RegistryApplicationKey
                    {
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = "{0} {1}".format_with(originalUninstallString, "/bob"),
                        HasQuietUninstall = false,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = InstallerType.Unknown,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
                fileSystem.Setup(x => x.combine_paths(config.CacheLocation, "chocolatey", It.IsAny<string>(), It.IsAny<string>())).Returns("");
            }

            // under normal circumstances, it prompts so the user can decide, but if -y is passed it will skip

            public override void Because()
            {
                service.run(packageResult, config);
            }

            [Fact]
            public void should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - Installer type was not detected and no silent uninstall key exists."), Times.Once);
            }

            [Fact]
            public void should_not_call_command_executor()
            {
                commandExecutor.Verify(c => c.execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Never);
            }
        }

        public class when_AutomaticUninstallerService_defines_uninstall_switches : AutomaticUninstallerServiceSpecsBase
        {
            private Action because;
            private readonly string registryUninstallArgs = "/bob";
            private readonly string logLocation = "c:\\yes\\dude\\1.2.3-beta";

            public override void Because()
            {
                because = () => service.run(packageResult, config);
            }

            public void reset()
            {
                Context();
                registryKeys.Clear();
                commandExecutor.ResetCalls();
            }

            private void test_installertype(IInstaller installer, bool hasQuietUninstallString)
            {
                reset();
                registryKeys.Add(new RegistryApplicationKey
                    {
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = "{0} {1}".format_with(originalUninstallString, registryUninstallArgs),
                        HasQuietUninstall = hasQuietUninstallString,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = installer.InstallerType,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
                fileSystem.Setup(x => x.combine_paths(config.CacheLocation, "chocolatey", It.IsAny<string>(), It.IsAny<string>())).Returns(logLocation);

                because();

                var installerTypeArgs = installer.build_uninstall_command_arguments().trim_safe().Replace(InstallTokens.PACKAGE_LOCATION, logLocation);

                var uninstallArgs = !hasQuietUninstallString ? registryUninstallArgs.trim_safe() + " " + installerTypeArgs : registryUninstallArgs.trim_safe();

                commandExecutor.Verify(c => c.execute(expectedUninstallString, uninstallArgs.trim_safe(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()), Times.Once);
            }

            //[Fact]
            //public void should_use_CustomInstaller_uninstall_args_when_installtype_is_unknown_and_has_quiet_uninstall_is_false()
            //{
            //    test_installertype(new CustomInstaller(), hasQuietUninstallString: false);
            //}  

            [Fact]
            public void should_use_registry_uninstall_args_when_installtype_is_unknown_and_has_quiet_uninstall_is_true()
            {
                test_installertype(new CustomInstaller(), hasQuietUninstallString: true);
            }

            [Fact]
            public void should_use_MsiInstaller_uninstall_args_when_installtype_is_msi_and_has_quiet_uninstall_is_false()
            {
                test_installertype(new MsiInstaller(), hasQuietUninstallString: false);
            }

            [Fact]
            public void should_use_registry_uninstall_args_when_installtype_is_msi_and_has_quiet_uninstall_is_true()
            {
                test_installertype(new MsiInstaller(), hasQuietUninstallString: true);
            }

            [Fact]
            public void should_use_InnoSetupInstaller_uninstall_args_when_installtype_is_innosetup_and_has_quiet_uninstall_is_false()
            {
                test_installertype(new InnoSetupInstaller(), hasQuietUninstallString: false);
            }

            [Fact]
            public void should_use_registry_uninstall_args_when_installtype_is_innosetup_and_has_quiet_uninstall_is_true()
            {
                test_installertype(new InnoSetupInstaller(), hasQuietUninstallString: true);
            }

            [Fact]
            public void should_use_InstallShieldInstaller_uninstall_args_when_installtype_is_installshield_and_has_quiet_uninstall_is_false()
            {
                test_installertype(new InstallShieldInstaller(), hasQuietUninstallString: false);
            }

            [Fact]
            public void should_use_registry_uninstall_args_when_installtype_is_installshield_and_has_quiet_uninstall_is_true()
            {
                test_installertype(new InstallShieldInstaller(), hasQuietUninstallString: true);
            }

            [Fact]
            public void should_use_NsisInstaller_uninstall_args_when_installtype_is_nsis_and_has_quiet_uninstall_is_false()
            {
                test_installertype(new NsisInstaller(), hasQuietUninstallString: false);
            }

            [Fact]
            public void should_use_registry_uninstall_args_when_installtype_is_nsis_and_has_quiet_uninstall_is_true()
            {
                test_installertype(new NsisInstaller(), hasQuietUninstallString: true);
            }
        }
    }
}