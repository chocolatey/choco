﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace Chocolatey.Tests.Infrastructure.App.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using Chocolatey.Infrastructure.Adapters;
    using Chocolatey.Infrastructure.App;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Domain;
    using Chocolatey.Infrastructure.App.Domain.Installers;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.Commands;
    using Chocolatey.Infrastructure.Results;
    using Moq;
    using global::NuGet.Packaging;
    using global::NuGet.Versioning;
    using IFileSystem = Chocolatey.Infrastructure.Filesystem.IFileSystem;

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
            protected Mock<IPackageMetadata> package = new Mock<IPackageMetadata>();
            protected ConcurrentDictionary<string, PackageResult> packageResults = new ConcurrentDictionary<string, PackageResult>();
            protected PackageResult packageResult;
            protected ChocolateyPackageInformation packageInformation;
            protected IList<RegistryApplicationKey> registryKeys = new List<RegistryApplicationKey>();
            protected IInstaller installerType = new CustomInstaller();

            protected readonly string expectedDisplayName = "WinDirStat";
            protected readonly string originalUninstallString = @"""C:\Program Files (x86)\WinDirStat\Uninstall.exe""";
            protected readonly string expectedUninstallString = @"C:\Program Files (x86)\WinDirStat\Uninstall.exe";

            public override void Context()
            {
                CommandExecutor.InitializeWith(new Lazy<IFileSystem>(() => fileSystem.Object), () => process.Object);

                service = new AutomaticUninstallerService(packageInfoService.Object, fileSystem.Object, registryService.Object, commandExecutor.Object);
                service.WaitForCleanup = false;
                config.Features.AutoUninstaller = true;
                config.PromptForConfirmation = false;
                config.PackageNames = "regular";
                package.Setup(p => p.Id).Returns("regular");
                package.Setup(p => p.Version).Returns(new NuGetVersion("1.2.0"));
                packageResult = new PackageResult(package.Object, "c:\\packages\\thispackage");
                packageInformation = new ChocolateyPackageInformation(package.Object);
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = expectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = originalUninstallString,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = installerType.InstallerType,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
                packageInfoService.Setup(s => s.Get(package.Object)).Returns(packageInformation);
                packageResults.GetOrAdd("regular", packageResult);

                fileSystem.Setup(f => f.DirectoryExists(registryKeys.FirstOrDefault().InstallLocation)).Returns(true);
                registryService.Setup(r => r.InstallerKeyExists(registryKeys.FirstOrDefault().KeyPath)).Returns(true);
                fileSystem.Setup(f => f.GetFullPath(expectedUninstallString)).Returns(expectedUninstallString);
                fileSystem.Setup(x => x.FileExists(expectedUninstallString)).Returns(true);

                var field = typeof(ApplicationParameters).GetField("AllowPrompts");
                field.SetValue(null, false);
            }
        }

        public class When_autouninstall_feature_is_off : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                config.Features.AutoUninstaller = false;
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - AutoUninstaller feature is not enabled."), Times.Once);
            }

            [Fact]
            public void Should_not_get_package_information()
            {
                packageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Never);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_an_autoUninstaller_skip_file_exists : AutomaticUninstallerServiceSpecsBase
        {
            private string skipFileName = ".skipAutoUninstall";
            IEnumerable<string> fileList = new List<string>() {"c:\\.skipAutoUninstall"};
            public override void Context()
            {
                base.Context();
                fileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), ".skipAutoUninstall*", SearchOption.AllDirectories)).Returns(fileList);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - Package contains a skip file ('" + skipFileName + "')."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_registry_snapshot_is_null : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                packageInformation.RegistrySnapshot = null;
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - No registry snapshot."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_package_is_missing : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                packageInformation.Package = null;
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - No package in package information."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_registry_keys_are_empty : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                packageInformation.RegistrySnapshot = new Registry("123", null);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - No registry keys in snapshot."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_install_location_does_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                fileSystem.ResetCalls();
                fileSystem.Setup(f => f.DirectoryExists(registryKeys.FirstOrDefault().InstallLocation)).Returns(false);
                fileSystem.Setup(x => x.FileExists(expectedUninstallString)).Returns(true);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - '{0}' appears to have been uninstalled already by other means.".FormatWith(expectedDisplayName)), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_install_location_is_empty : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                fileSystem.ResetCalls();
                registryKeys.Clear();
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = expectedDisplayName,
                        InstallLocation = string.Empty,
                        UninstallString = originalUninstallString,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat"
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
                fileSystem.Setup(x => x.FileExists(expectedUninstallString)).Returns(true);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                packageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                var args = installerType.BuildUninstallCommandArguments().TrimSafe();
                commandExecutor.Verify(
                    c => c.Execute(expectedUninstallString, args, It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_uninstall_string_is_empty : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                fileSystem.ResetCalls();
                registryKeys.Clear();
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = expectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = string.Empty,
                        HasQuietUninstall = false,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat"
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
                fileSystem.Setup(x => x.FileExists(expectedUninstallString)).Returns(true);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - '{0}' does not have an uninstall string.".FormatWith(expectedDisplayName)), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_registry_location_does_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                registryService.ResetCalls();
                registryService.Setup(r => r.InstallerKeyExists(registryKeys.FirstOrDefault().KeyPath)).Returns(false);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - '{0}' appears to have been uninstalled already by other means.".FormatWith(expectedDisplayName)), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_registry_location_and_install_location_both_do_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                fileSystem.ResetCalls();
                fileSystem.Setup(f => f.DirectoryExists(registryKeys.FirstOrDefault().InstallLocation)).Returns(false);
                fileSystem.Setup(x => x.FileExists(expectedUninstallString)).Returns(true);
                registryService.ResetCalls();
                registryService.Setup(r => r.InstallerKeyExists(registryKeys.FirstOrDefault().KeyPath)).Returns(false);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - '{0}' appears to have been uninstalled already by other means.".FormatWith(expectedDisplayName)), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_uninstall_exe_does_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                fileSystem.ResetCalls();
                fileSystem.Setup(f => f.DirectoryExists(registryKeys.FirstOrDefault().InstallLocation)).Returns(true);
                fileSystem.Setup(f => f.GetFullPath(expectedUninstallString)).Returns(expectedUninstallString);
                fileSystem.Setup(x => x.FileExists(expectedUninstallString)).Returns(false);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - The uninstaller file no longer exists. \"" + expectedUninstallString + "\""), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_AutomaticUninstallerService_is_run_normally : AutomaticUninstallerServiceSpecsBase
        {
            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                packageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                commandExecutor.Verify(
                    c =>
                        c.Execute(
                            expectedUninstallString,
                            installerType.BuildUninstallCommandArguments().TrimSafe(),
                            It.IsAny<int>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_uninstall_string_is_split_by_quotes : AutomaticUninstallerServiceSpecsBase
        {
            private readonly string uninstallStringWithQuoteSeparation = @"""C:\Program Files (x86)\WinDirStat\Uninstall.exe"" ""WinDir Stat""";

            public override void Context()
            {
                base.Context();
                registryKeys.Clear();
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = expectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = uninstallStringWithQuoteSeparation,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = installerType.InstallerType,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                packageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                commandExecutor.Verify(
                    c =>
                        c.Execute(
                            expectedUninstallString,
                            "\"WinDir Stat\"".TrimSafe(),
                            It.IsAny<int>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_uninstall_string_has_ampersand_quot : AutomaticUninstallerServiceSpecsBase
        {
            private readonly string uninstallStringWithAmpersandQuot = @"&quot;C:\Program Files (x86)\WinDirStat\Uninstall.exe&quot; /SILENT";

            public override void Context()
            {
                base.Context();
                registryKeys.Clear();
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = expectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = uninstallStringWithAmpersandQuot,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = installerType.InstallerType,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
            }

            public override void Because()
            {
                MockLogger.LogMessagesToConsole = true;
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                packageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                commandExecutor.Verify(
                    c =>
                        c.Execute(
                            expectedUninstallString,
                            "/SILENT".TrimSafe(),
                            It.IsAny<int>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_uninstall_string_has_multiple_file_paths : AutomaticUninstallerServiceSpecsBase
        {
            private readonly string uninstallStringPointingToPath = @"C:\Programs\WinDirStat\Uninstall.exe D:\Programs\WinDirStat";
            protected readonly string expectedUninstallStringMultiplePaths = @"C:\Programs\WinDirStat\Uninstall.exe";

            public override void Context()
            {
                base.Context();
                registryKeys.Clear();
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = expectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = uninstallStringPointingToPath,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = installerType.InstallerType,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
                fileSystem.Setup(x => x.FileExists(expectedUninstallStringMultiplePaths)).Returns(true);
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                packageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                commandExecutor.Verify(
                    c =>
                        c.Execute(
                            expectedUninstallStringMultiplePaths,
                            @"D:\Programs\WinDirStat".TrimSafe(),
                            It.IsAny<int>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_AutomaticUninstallerService_cannot_determine_silent_install_arguments : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                registryKeys.Clear();
                commandExecutor.ResetCalls();
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = "{0} {1}".FormatWith(originalUninstallString, "/bob"),
                        HasQuietUninstall = false,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = InstallerType.Unknown,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
                fileSystem.Setup(x => x.CombinePaths(config.CacheLocation, "chocolatey", It.IsAny<string>(), It.IsAny<string>())).Returns("");
            }

            // under normal circumstances, it prompts so the user can decide, but if -y is passed it will skip

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - Installer type was not detected and no silent uninstall key exists."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                commandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_AutomaticUninstallerService_is_passed_uninstall_arguments_from_command_line : AutomaticUninstallerServiceSpecsBase
        {
            IInstaller _installerType = new InnoSetupInstaller();

            public override void Context()
            {
                base.Context();
                registryKeys.Clear();
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = expectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = originalUninstallString,
                        HasQuietUninstall = false,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = _installerType.InstallerType,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);

                config.InstallArguments = "/bob /nope";
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                packageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor_appending_passed_arguments()
            {
                var uninstallArgs = _installerType.BuildUninstallCommandArguments().TrimSafe();

                uninstallArgs += " {0}".FormatWith(config.InstallArguments);

                commandExecutor.Verify(
                    c =>
                        c.Execute(
                            expectedUninstallString,
                            uninstallArgs,
                            It.IsAny<int>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_AutomaticUninstallerService_is_passed_overriding_uninstall_arguments_from_command_line : AutomaticUninstallerServiceSpecsBase
        {
            IInstaller _installerType = new InnoSetupInstaller();

            public override void Context()
            {
                base.Context();
                registryKeys.Clear();
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = expectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = originalUninstallString,
                        HasQuietUninstall = false,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = _installerType.InstallerType,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);

                config.InstallArguments = "/bob /nope";
                config.OverrideArguments = true;
            }

            public override void Because()
            {
                service.Run(packageResult, config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                packageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor_with_only_passed_arguments()
            {
                commandExecutor.Verify(
                    c =>
                        c.Execute(
                            expectedUninstallString,
                            config.InstallArguments,
                            It.IsAny<int>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_AutomaticUninstallerService_defines_uninstall_switches : AutomaticUninstallerServiceSpecsBase
        {
            private Action because;
            private readonly string registryUninstallArgs = "/bob";
            private readonly string logLocation = "c:\\yes\\dude\\1.2.3-beta";

            public override void Because()
            {
                because = () => service.Run(packageResult, config);
            }

            public void Reset()
            {
                Context();
                registryKeys.Clear();
                commandExecutor.ResetCalls();
            }

            private void TestInstallerType(IInstaller installer, bool hasQuietUninstallString)
            {
                Reset();
                registryKeys.Add(
                    new RegistryApplicationKey
                    {
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = "{0} {1}".FormatWith(originalUninstallString, registryUninstallArgs),
                        HasQuietUninstall = hasQuietUninstallString,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = installer.InstallerType,
                    });
                packageInformation.RegistrySnapshot = new Registry("123", registryKeys);
                fileSystem.Setup(x => x.CombinePaths(config.CacheLocation, "chocolatey", It.IsAny<string>(), It.IsAny<string>())).Returns(logLocation);

                because();

                var installerTypeArgs = installer.BuildUninstallCommandArguments().TrimSafe().Replace(InstallTokens.PackageLocation, logLocation);

                var uninstallArgs = !hasQuietUninstallString ? registryUninstallArgs.TrimSafe() + " " + installerTypeArgs : registryUninstallArgs.TrimSafe();

                commandExecutor.Verify(
                    c =>
                        c.Execute(
                            expectedUninstallString,
                            uninstallArgs.TrimSafe(),
                            It.IsAny<int>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<bool>()),
                    Times.Once);
            }

            //[Fact]
            //public void Should_use_CustomInstaller_uninstall_args_when_installtype_is_unknown_and_has_quiet_uninstall_is_false()
            //{
            //    test_installertype(new CustomInstaller(), hasQuietUninstallString: false);
            //}

            [Fact]
            public void Should_use_registry_uninstall_args_when_installtype_is_unknown_and_has_quiet_uninstall_is_true()
            {
                TestInstallerType(new CustomInstaller(), hasQuietUninstallString: true);
            }

            [Fact]
            public void Should_use_MsiInstaller_uninstall_args_when_installtype_is_msi_and_has_quiet_uninstall_is_false()
            {
                TestInstallerType(new MsiInstaller(), hasQuietUninstallString: false);
            }

            [Fact]
            public void Should_use_registry_uninstall_args_when_installtype_is_msi_and_has_quiet_uninstall_is_true()
            {
                TestInstallerType(new MsiInstaller(), hasQuietUninstallString: true);
            }

            [Fact]
            public void Should_use_InnoSetupInstaller_uninstall_args_when_installtype_is_innosetup_and_has_quiet_uninstall_is_false()
            {
                TestInstallerType(new InnoSetupInstaller(), hasQuietUninstallString: false);
            }

            [Fact]
            public void Should_use_registry_uninstall_args_when_installtype_is_innosetup_and_has_quiet_uninstall_is_true()
            {
                TestInstallerType(new InnoSetupInstaller(), hasQuietUninstallString: true);
            }

            [Fact]
            public void Should_use_InstallShieldInstaller_uninstall_args_when_installtype_is_installshield_and_has_quiet_uninstall_is_false()
            {
                TestInstallerType(new InstallShieldInstaller(), hasQuietUninstallString: false);
            }

            [Fact]
            public void Should_use_registry_uninstall_args_when_installtype_is_installshield_and_has_quiet_uninstall_is_true()
            {
                TestInstallerType(new InstallShieldInstaller(), hasQuietUninstallString: true);
            }

            [Fact]
            public void Should_use_NsisInstaller_uninstall_args_when_installtype_is_nsis_and_has_quiet_uninstall_is_false()
            {
                TestInstallerType(new NsisInstaller(), hasQuietUninstallString: false);
            }

            [Fact]
            public void Should_use_registry_uninstall_args_when_installtype_is_nsis_and_has_quiet_uninstall_is_true()
            {
                TestInstallerType(new NsisInstaller(), hasQuietUninstallString: true);
            }
        }
    }
}
