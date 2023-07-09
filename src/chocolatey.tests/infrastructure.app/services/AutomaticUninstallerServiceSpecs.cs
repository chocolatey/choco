// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.tests.infrastructure.app.services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.domain.installers;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.results;
    using Moq;
    using NuGet.Packaging;
    using NuGet.Versioning;
    using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

    public class AutomaticUninstallerServiceSpecs
    {
        public abstract class AutomaticUninstallerServiceSpecsBase : TinySpec
        {
            protected AutomaticUninstallerService Service;
            protected Mock<IChocolateyPackageInformationService> PackageInfoService = new Mock<IChocolateyPackageInformationService>();
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Mock<IProcess> Process = new Mock<IProcess>();
            protected Mock<IRegistryService> RegistryService = new Mock<IRegistryService>();
            protected Mock<ICommandExecutor> CommandExecutor = new Mock<ICommandExecutor>();
            protected ChocolateyConfiguration Config = new ChocolateyConfiguration();
            protected Mock<IPackageMetadata> Package = new Mock<IPackageMetadata>();
            protected ConcurrentDictionary<string, PackageResult> PackageResults = new ConcurrentDictionary<string, PackageResult>();
            protected PackageResult PackageResult;
            protected ChocolateyPackageInformation PackageInformation;
            protected IList<RegistryApplicationKey> RegistryKeys = new List<RegistryApplicationKey>();
            protected IInstaller InstallerType = new CustomInstaller();

            protected readonly string ExpectedDisplayName = "WinDirStat";
            protected readonly string OriginalUninstallString = @"""C:\Program Files (x86)\WinDirStat\Uninstall.exe""";
            protected readonly string ExpectedUninstallString = @"C:\Program Files (x86)\WinDirStat\Uninstall.exe";

            public override void Context()
            {
                chocolatey.infrastructure.commands.CommandExecutor.InitializeWith(new Lazy<IFileSystem>(() => FileSystem.Object), () => Process.Object);

                Service = new AutomaticUninstallerService(PackageInfoService.Object, FileSystem.Object, RegistryService.Object, CommandExecutor.Object);
                Service.WaitForCleanup = false;
                Config.Features.AutoUninstaller = true;
                Config.PromptForConfirmation = false;
                Config.PackageNames = "regular";
                Package.Setup(p => p.Id).Returns("regular");
                Package.Setup(p => p.Version).Returns(new NuGetVersion("1.2.0"));
                PackageResult = new PackageResult(Package.Object, "c:\\packages\\thispackage");
                PackageInformation = new ChocolateyPackageInformation(Package.Object);
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = ExpectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = OriginalUninstallString,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = InstallerType.InstallerType,
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);
                PackageInfoService.Setup(s => s.Get(Package.Object)).Returns(PackageInformation);
                PackageResults.GetOrAdd("regular", PackageResult);

                FileSystem.Setup(f => f.DirectoryExists(RegistryKeys.FirstOrDefault().InstallLocation)).Returns(true);
                RegistryService.Setup(r => r.InstallerKeyExists(RegistryKeys.FirstOrDefault().KeyPath)).Returns(true);
                FileSystem.Setup(f => f.GetFullPath(ExpectedUninstallString)).Returns(ExpectedUninstallString);
                FileSystem.Setup(x => x.FileExists(ExpectedUninstallString)).Returns(true);

                var field = typeof(ApplicationParameters).GetField("AllowPrompts");
                field.SetValue(null, false);
            }
        }

        public class When_autouninstall_feature_is_off : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Config.Features.AutoUninstaller = false;
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - AutoUninstaller feature is not enabled."), Times.Once);
            }

            [Fact]
            public void Should_not_get_package_information()
            {
                PackageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Never);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_an_autoUninstaller_skip_file_exists : AutomaticUninstallerServiceSpecsBase
        {
            private string _skipFileName = ".skipAutoUninstall";
            IEnumerable<string> _fileList = new List<string>() { "c:\\.skipAutoUninstall" };
            public override void Context()
            {
                base.Context();
                FileSystem.Setup(f => f.GetFiles(It.IsAny<string>(), ".skipAutoUninstall*", SearchOption.AllDirectories)).Returns(_fileList);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - Package contains a skip file ('" + _skipFileName + "')."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_registry_snapshot_is_null : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                PackageInformation.RegistrySnapshot = null;
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - No registry snapshot."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_package_is_missing : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                PackageInformation.Package = null;
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - No package in package information."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_registry_keys_are_empty : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                PackageInformation.RegistrySnapshot = new Registry("123", null);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - No registry keys in snapshot."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_install_location_does_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                FileSystem.ResetCalls();
                FileSystem.Setup(f => f.DirectoryExists(RegistryKeys.FirstOrDefault().InstallLocation)).Returns(false);
                FileSystem.Setup(x => x.FileExists(ExpectedUninstallString)).Returns(true);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - '{0}' appears to have been uninstalled already by other means.".FormatWith(ExpectedDisplayName)), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_install_location_is_empty : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                FileSystem.ResetCalls();
                RegistryKeys.Clear();
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = ExpectedDisplayName,
                        InstallLocation = string.Empty,
                        UninstallString = OriginalUninstallString,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat"
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);
                FileSystem.Setup(x => x.FileExists(ExpectedUninstallString)).Returns(true);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                PackageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                var args = InstallerType.BuildUninstallCommandArguments().TrimSafe();
                CommandExecutor.Verify(
                    c => c.Execute(ExpectedUninstallString, args, It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_uninstall_string_is_empty : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                FileSystem.ResetCalls();
                RegistryKeys.Clear();
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = ExpectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = string.Empty,
                        HasQuietUninstall = false,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat"
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);
                FileSystem.Setup(x => x.FileExists(ExpectedUninstallString)).Returns(true);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - '{0}' does not have an uninstall string.".FormatWith(ExpectedDisplayName)), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_registry_location_does_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                RegistryService.ResetCalls();
                RegistryService.Setup(r => r.InstallerKeyExists(RegistryKeys.FirstOrDefault().KeyPath)).Returns(false);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - '{0}' appears to have been uninstalled already by other means.".FormatWith(ExpectedDisplayName)), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_registry_location_and_install_location_both_do_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                FileSystem.ResetCalls();
                FileSystem.Setup(f => f.DirectoryExists(RegistryKeys.FirstOrDefault().InstallLocation)).Returns(false);
                FileSystem.Setup(x => x.FileExists(ExpectedUninstallString)).Returns(true);
                RegistryService.ResetCalls();
                RegistryService.Setup(r => r.InstallerKeyExists(RegistryKeys.FirstOrDefault().KeyPath)).Returns(false);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - '{0}' appears to have been uninstalled already by other means.".FormatWith(ExpectedDisplayName)), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_uninstall_exe_does_not_exist : AutomaticUninstallerServiceSpecsBase
        {
            public override void Context()
            {
                base.Context();
                FileSystem.ResetCalls();
                FileSystem.Setup(f => f.DirectoryExists(RegistryKeys.FirstOrDefault().InstallLocation)).Returns(true);
                FileSystem.Setup(f => f.GetFullPath(ExpectedUninstallString)).Returns(ExpectedUninstallString);
                FileSystem.Setup(x => x.FileExists(ExpectedUninstallString)).Returns(false);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - The uninstaller file no longer exists. \"" + ExpectedUninstallString + "\""), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
                    c => c.Execute(It.IsAny<String>(), It.IsAny<String>(), It.IsAny<int>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<Action<object, DataReceivedEventArgs>>(), It.IsAny<bool>()),
                    Times.Never);
            }
        }

        public class When_AutomaticUninstallerService_is_run_normally : AutomaticUninstallerServiceSpecsBase
        {
            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                PackageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                CommandExecutor.Verify(
                    c =>
                        c.Execute(
                            ExpectedUninstallString,
                            InstallerType.BuildUninstallCommandArguments().TrimSafe(),
                            It.IsAny<int>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_uninstall_string_is_split_by_quotes : AutomaticUninstallerServiceSpecsBase
        {
            private readonly string _uninstallStringWithQuoteSeparation = @"""C:\Program Files (x86)\WinDirStat\Uninstall.exe"" ""WinDir Stat""";

            public override void Context()
            {
                base.Context();
                RegistryKeys.Clear();
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = ExpectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = _uninstallStringWithQuoteSeparation,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = InstallerType.InstallerType,
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                PackageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                CommandExecutor.Verify(
                    c =>
                        c.Execute(
                            ExpectedUninstallString,
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
            private readonly string _uninstallStringWithAmpersandQuot = @"&quot;C:\Program Files (x86)\WinDirStat\Uninstall.exe&quot; /SILENT";

            public override void Context()
            {
                base.Context();
                RegistryKeys.Clear();
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = ExpectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = _uninstallStringWithAmpersandQuot,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = InstallerType.InstallerType,
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);
            }

            public override void Because()
            {
                MockLogger.LogMessagesToConsole = true;
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                PackageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                CommandExecutor.Verify(
                    c =>
                        c.Execute(
                            ExpectedUninstallString,
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
            private readonly string _uninstallStringPointingToPath = @"C:\Programs\WinDirStat\Uninstall.exe D:\Programs\WinDirStat";
            protected readonly string ExpectedUninstallStringMultiplePaths = @"C:\Programs\WinDirStat\Uninstall.exe";

            public override void Context()
            {
                base.Context();
                RegistryKeys.Clear();
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = ExpectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = _uninstallStringPointingToPath,
                        HasQuietUninstall = true,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = InstallerType.InstallerType,
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);
                FileSystem.Setup(x => x.FileExists(ExpectedUninstallStringMultiplePaths)).Returns(true);
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                PackageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor()
            {
                CommandExecutor.Verify(
                    c =>
                        c.Execute(
                            ExpectedUninstallStringMultiplePaths,
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
                RegistryKeys.Clear();
                CommandExecutor.ResetCalls();
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = "{0} {1}".FormatWith(OriginalUninstallString, "/bob"),
                        HasQuietUninstall = false,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = chocolatey.infrastructure.app.domain.InstallerType.Unknown,
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);
                FileSystem.Setup(x => x.CombinePaths(Config.CacheLocation, "chocolatey", It.IsAny<string>(), It.IsAny<string>())).Returns("");
            }

            // under normal circumstances, it prompts so the user can decide, but if -y is passed it will skip

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_log_why_it_skips_auto_uninstaller()
            {
                MockLogger.Verify(l => l.Info(" Skipping auto uninstaller - Installer type was not detected and no silent uninstall key exists."), Times.Once);
            }

            [Fact]
            public void Should_not_call_command_executor()
            {
                CommandExecutor.Verify(
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
                RegistryKeys.Clear();
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = ExpectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = OriginalUninstallString,
                        HasQuietUninstall = false,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = _installerType.InstallerType,
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);

                Config.InstallArguments = "/bob /nope";
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                PackageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor_appending_passed_arguments()
            {
                var uninstallArgs = _installerType.BuildUninstallCommandArguments().TrimSafe();

                uninstallArgs += " {0}".FormatWith(Config.InstallArguments);

                CommandExecutor.Verify(
                    c =>
                        c.Execute(
                            ExpectedUninstallString,
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
                RegistryKeys.Clear();
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        DisplayName = ExpectedDisplayName,
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = OriginalUninstallString,
                        HasQuietUninstall = false,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = _installerType.InstallerType,
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);

                Config.InstallArguments = "/bob /nope";
                Config.OverrideArguments = true;
            }

            public override void Because()
            {
                Service.Run(PackageResult, Config);
            }

            [Fact]
            public void Should_call_get_package_information()
            {
                PackageInfoService.Verify(s => s.Get(It.IsAny<IPackageMetadata>()), Times.Once);
            }

            [Fact]
            public void Should_call_command_executor_with_only_passed_arguments()
            {
                CommandExecutor.Verify(
                    c =>
                        c.Execute(
                            ExpectedUninstallString,
                            Config.InstallArguments,
                            It.IsAny<int>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<Action<object, DataReceivedEventArgs>>(),
                            It.IsAny<bool>()),
                    Times.Once);
            }
        }

        public class When_AutomaticUninstallerService_defines_uninstall_switches : AutomaticUninstallerServiceSpecsBase
        {
            private Action _because;
            private readonly string _registryUninstallArgs = "/bob";
            private readonly string _logLocation = "c:\\yes\\dude\\1.2.3-beta";

            public override void Because()
            {
                _because = () => Service.Run(PackageResult, Config);
            }

            public void Reset()
            {
                Context();
                RegistryKeys.Clear();
                CommandExecutor.ResetCalls();
            }

            private void TestInstallerType(IInstaller installer, bool hasQuietUninstallString)
            {
                Reset();
                RegistryKeys.Add(
                    new RegistryApplicationKey
                    {
                        InstallLocation = @"C:\Program Files (x86)\WinDirStat",
                        UninstallString = "{0} {1}".FormatWith(OriginalUninstallString, _registryUninstallArgs),
                        HasQuietUninstall = hasQuietUninstallString,
                        KeyPath = @"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\WinDirStat",
                        InstallerType = installer.InstallerType,
                    });
                PackageInformation.RegistrySnapshot = new Registry("123", RegistryKeys);
                FileSystem.Setup(x => x.CombinePaths(Config.CacheLocation, "chocolatey", It.IsAny<string>(), It.IsAny<string>())).Returns(_logLocation);

                _because();

                var installerTypeArgs = installer.BuildUninstallCommandArguments().TrimSafe().Replace(InstallTokens.PackageLocation, _logLocation);

                var uninstallArgs = !hasQuietUninstallString ? _registryUninstallArgs.TrimSafe() + " " + installerTypeArgs : _registryUninstallArgs.TrimSafe();

                CommandExecutor.Verify(
                    c =>
                        c.Execute(
                            ExpectedUninstallString,
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
