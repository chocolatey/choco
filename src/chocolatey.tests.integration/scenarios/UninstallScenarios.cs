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

namespace chocolatey.tests.integration.scenarios
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.results;
    using NuGet.Configuration;
    using NUnit.Framework;
    using FluentAssertions;
    using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

    public class UninstallScenarios
    {
        [ConcernFor("uninstall")]
        public abstract class ScenariosBase : TinySpec
        {
            protected ConcurrentDictionary<string, PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;
            protected CommandExecutor CommandExecutor;

            public override void Context()
            {
                Configuration = Scenario.Uninstall();
                Scenario.Reset(Configuration);
                Configuration.PackageNames = Configuration.Input = "installpackage";
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");
                Scenario.AddPackagesToSourceLocation(Configuration, "badpackage*" + NuGetConstants.PackageExtension);
                Configuration.SkipPackageInstallProvider = true;
                Scenario.InstallPackage(Configuration, "badpackage", "1.0");
                Configuration.SkipPackageInstallProvider = false;

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                CommandExecutor = new CommandExecutor(NUnitSetup.Container.GetInstance<IFileSystem>());
            }
        }

        public class When_noop_uninstalling_a_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Noop = true;
            }

            public override void Because()
            {
                Service.UninstallDryRun(Configuration);
            }

            [Fact]
            public void Should_not_uninstall_a_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_would_have_uninstalled_a_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Would have uninstalled installpackage v1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_contain_a_message_that_it_would_have_run_a_powershell_script()
            {
                MockLogger.ContainsMessage("chocolateyuninstall.ps1").Should().BeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_contain_a_message_that_it_would_have_run_powershell_modification_script()
            {
                MockLogger.ContainsMessage("chocolateyBeforeModify.ps1").Should().BeTrue();
            }
        }

        public class When_noop_uninstalling_a_package_that_does_not_exist : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "somethingnonexisting";
                Configuration.Noop = true;
            }

            public override void Because()
            {
                Service.UninstallDryRun(Configuration);
            }

            [Fact]
            public void Should_contain_a_message_that_it_was_unable_to_find_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("somethingnonexisting is not installed. Cannot uninstall a non-existent package"));
            }
        }

        public class When_uninstalling_a_package_happy_path : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_delete_any_files_created_during_the_install()
            {
                var generatedFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "simplefile.txt");

                FileAssert.DoesNotExist(generatedFile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyUninstall_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage 1.0.0 Uninstalled"));
            }
        }

        public class When_force_uninstalling_a_package : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }
        }

        public class When_uninstalling_packages_with_packages_config : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                var packagesConfig = "{0}{1}context{1}testing.packages.config".FormatWith(Scenario.GetTopLevel(), Path.DirectorySeparatorChar);
                Configuration.PackageNames = Configuration.Input = packagesConfig;
            }

            public override void Because()
            {
            }

            [Fact]
            public void Should_throw_an_error_that_it_is_not_allowed()
            {
                Action m = () => Service.Uninstall(Configuration);

                m.Should().Throw<ApplicationException>();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_uninstalling_a_package_with_readonly_files : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                var fileToSetReadOnly = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                var fileSystem = new DotNetFileSystem();
                fileSystem.EnsureFileAttributeSet(fileToSetReadOnly, FileAttributes.ReadOnly);
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_uninstall_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("uninstalled 1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_uninstalling_a_package_with_a_read_and_delete_share_locked_file : ScenariosBase
        {
            private PackageResult _packageResult;

            private FileStream _fileStream;

            public override void Context()
            {
                base.Context();
                var fileToOpen = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                _fileStream = new FileStream(fileToOpen, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                _fileStream.Close();
                _fileStream.Dispose();
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_uninstall_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            // Locking is inconsistent between client and server operating systems in .NET 4.8.
            // On a server, if a file is Read and delete locked it can't be deleted, but on client systems it can.
            [Fact, Platform(Exclude = "WindowsServer10")]
            public void Should_have_deleted_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            // Locking is inconsistent between client and server operating systems in .NET 4.8.
            // On a server, if a file is Read and delete locked it can't be deleted, but on client systems it can.
            [Fact, Platform("WindowsServer10")]
            public void Should_not_have_deleted_the_rollback_on_server()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("uninstalled 1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_uninstalling_a_package_with_an_exclusively_locked_file : ScenariosBase
        {
            private PackageResult _packageResult;

            private FileStream _fileStream;

            public override void Context()
            {
                base.Context();
                var fileToOpen = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                _fileStream = new FileStream(fileToOpen, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                _fileStream.Close();
                _fileStream.Dispose();
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_be_able_to_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_contain_old_files_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                FileAssert.DoesNotExist(shimFile);
            }

            [Fact]
            public void Should_keep_locked_file_in_directory()
            {
                var lockedFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");

                FileAssert.Exists(lockedFile);
            }

            [Fact]
            public void Should_contain_a_message_about_not_all_files_are_removed()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Unable to delete all existing package files. There will be leftover files requiring manual cleanup"));
            }

            [Fact]
            public void Should_contain_a_message_that_it_was_not_able_to_uninstall()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("uninstalled 0/1"));
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }
        }

        public class When_uninstalling_a_package_with_added_files : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                var fileAdded = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "dude.txt");
                File.WriteAllText(fileAdded, "hellow");
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_keep_the_added_file()
            {
                var fileAdded = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "dude.txt");

                FileAssert.Exists(fileAdded);
            }

            [Fact]
            public void Should_delete_everything_but_the_added_file_from_the_package_directory()
            {
                var files = Directory.GetFiles(Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames));

                foreach (var file in files.OrEmpty())
                {
                    Path.GetFileName(file).Should().Be("dude.txt", "Expected files were not deleted.");
                }
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }
        }

        public class When_uninstalling_a_package_with_changed_files : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                var fileChanged = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");
                File.WriteAllText(fileChanged, "hellow");
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_keep_the_changed_file()
            {
                var fileChanged = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");

                FileAssert.Exists(fileChanged);
            }

            [Fact]
            public void Should_delete_everything_but_the_changed_file_from_the_package_directory()
            {
                var files = Directory.GetFiles(Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames));

                foreach (var file in files.OrEmpty())
                {
                    Path.GetFileName(file).Should().Be("chocolateyInstall.ps1", "Expected files were not deleted.");
                }
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }
        }

        public class When_force_uninstalling_a_package_with_added_and_changed_files : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                var fileChanged = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");
                File.WriteAllText(fileChanged, "hellow");
                var fileAdded = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "dude.txt");
                File.WriteAllText(fileAdded, "hellow");
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_keep_the_added_file()
            {
                var fileChanged = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "dude.txt");

                FileAssert.DoesNotExist(fileChanged);
            }

            [Fact]
            public void Should_not_keep_the_changed_file()
            {
                var fileChanged = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");

                FileAssert.DoesNotExist(fileChanged);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }
        }

        public class When_uninstalling_a_package_that_does_not_exist : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "somethingnonexisting";
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_contain_a_message_that_it_was_unable_to_find_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("somethingnonexisting is not installed. Cannot uninstall a non-existent package"));
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("0/1"));
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Should_have_an_error_package_result()
            {
                _packageResult.Messages.Should().Contain(m => m.MessageType == ResultType.Error);
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_uninstalling_a_package_that_errors : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "badpackage";
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_remove_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_still_have_the_package_file_in_the_directory()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                FileAssert.Exists(packageFile);
            }

            [Fact]
            public void Should_put_the_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bad", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_was_unable_to_install_a_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("0/1"));
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Should_have_an_error_package_result()
            {
                _packageResult.Messages.Should().Contain(m => m.MessageType == ResultType.Error);
            }

            [Fact]
            public void Should_have_expected_error_in_package_result()
            {
                Results.Should().AllSatisfy(r =>
                    r.Value.Messages.Should().Contain(m =>
                        m.MessageType == ResultType.Error &&
                        m.Message.Contains("chocolateyUninstall.ps1")));
            }
        }


        public class When_uninstalling_a_hook_package : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "scriptpackage.hook";
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + ".1.0.0" + NuGetConstants.PackageExtension);
                Service.Install(Configuration);
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            public void Should_remove_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames.Replace(".hook", string.Empty));

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }
        }

        public class When_uninstalling_a_package_happy_path_with_hooks : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "scriptpackage.hook" + "*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "scriptpackage.hook", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_delete_any_files_created_during_the_install()
            {
                var generatedFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "simplefile.txt");

                FileAssert.DoesNotExist(generatedFile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyUninstall_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage 1.0.0 Uninstalled"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_all_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-uninstall-all.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_all_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-uninstall-all.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_installpackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-uninstall-installpackage.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_installpackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-uninstall-installpackage.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_upgradepackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("pre-uninstall-upgradepackage.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_beforemodify_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-beforemodify-all.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_beforemodify_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-beforemodify-all.ps1 hook ran for installpackage 1.0.0"));
            }
        }

        public class When_uninstalling_a_package_with_uppercase_id : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "UpperCase" + "*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "UpperCase", "1.1.0");

                Configuration.PackageNames = Configuration.Input = "UpperCase";
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("UpperCase 1.1.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyUninstall_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("UpperCase 1.1.0 Uninstalled"));
            }
        }

        public class When_uninstalling_a_package_with_non_normalized_version : ScenariosBase
        {
            private PackageResult _packageResult;

            private string _nonNormalizedVersion = "0004.0004.00005.00";
            private string _normalizedVersion = "4.4.5";

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Scenario.AddChangedVersionPackageToSourceLocation(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, _nonNormalizedVersion);
                Scenario.InstallPackage(Configuration, "upgradepackage", _nonNormalizedVersion);
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_delete_any_files_created_during_the_install()
            {
                var generatedFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "simplefile.txt");

                FileAssert.DoesNotExist(generatedFile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage {0} Before Modification".FormatWith(_normalizedVersion)));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyUninstall_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage {0} Uninstalled".FormatWith(_normalizedVersion)));
            }
        }

        public class When_uninstalling_a_package_with_remove_dependencies_with_beforeModify : ScenariosBase
        {
            private const string TargetPackageName = "hasdependencywithbeforemodify";
            private const string DependencyName = "isdependencywithbeforemodify";

            public override void Context()
            {
                base.Context();

                Scenario.AddPackagesToSourceLocation(Configuration, "{0}.*".FormatWith(TargetPackageName) + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "{0}.*".FormatWith(DependencyName) + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, TargetPackageName, "1.0.0");
                Scenario.InstallPackage(Configuration, DependencyName, "1.0.0");

                Configuration.PackageNames = Configuration.Input = TargetPackageName;
                Configuration.ForceDependencies = true;
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
            }

            [Fact]
            public void Should_uninstall_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", TargetPackageName, "{0}.nupkg".FormatWith(TargetPackageName));
                FileAssert.DoesNotExist(packageFile);
            }

            [Fact]
            public void Should_uninstall_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", DependencyName, "{0}.nupkg".FormatWith(DependencyName));
                FileAssert.DoesNotExist(packageFile);
            }

            [Fact]
            public void Should_contain_a_message_that_everything_uninstalled_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("uninstalled 2/2"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_target_package_beforeModify()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Ran BeforeModify: {0} {1}".FormatWith(TargetPackageName, "1.0.0")));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_dependency_package_beforeModify()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Ran BeforeModify: {0} {1}".FormatWith(DependencyName, "1.0.0")));
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeTrue());
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                Results.Should().AllSatisfy(r => r.Value.Inconclusive.Should().BeFalse());
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                Results.Should().AllSatisfy(r => r.Value.Warning.Should().BeFalse());

            }
        }
    }
}
