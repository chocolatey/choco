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
    using Should;
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
                Configuration = Scenario.uninstall();
                Scenario.reset(Configuration);
                Configuration.PackageNames = Configuration.Input = "installpackage";
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" +  NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "badpackage*" +  NuGetConstants.PackageExtension);
                Configuration.SkipPackageInstallProvider = true;
                Scenario.install_package(Configuration, "badpackage", "1.0");
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
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_would_have_uninstalled_a_package()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("Would have uninstalled installpackage v1.0.0")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_contain_a_message_that_it_would_have_run_a_powershell_script()
            {
                MockLogger.ContainsMessage("chocolateyuninstall.ps1").ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_contain_a_message_that_it_would_have_run_powershell_modification_script()
            {
                MockLogger.ContainsMessage("chocolateyBeforeModify.ps1").ShouldBeTrue();
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
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Error).OrEmpty())
                {
                    if (message.Contains("somethingnonexisting is not installed. Cannot uninstall a non-existent package")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }
        }

        public class When_uninstalling_a_package_happy_path : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_delete_any_files_created_during_the_install()
            {
                var generatedFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "simplefile.txt");

                FileAssert.DoesNotExist(generatedFile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script()
            {
                MockLogger.ContainsMessage("installpackage 1.0.0 Before Modification", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyUninstall_script()
            {
                MockLogger.ContainsMessage("installpackage 1.0.0 Uninstalled", LogLevel.Info).ShouldBeTrue();
            }
        }

        public class When_force_uninstalling_a_package : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }
        }

        public class When_uninstalling_packages_with_packages_config : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                var packagesConfig = "{0}{1}context{1}testing.packages.config".FormatWith(Scenario.get_top_level(), Path.DirectorySeparatorChar);
                Configuration.PackageNames = Configuration.Input = packagesConfig;
            }

            public override void Because()
            {
            }

            [Fact]
            public void Should_throw_an_error_that_it_is_not_allowed()
            {
                Action m = () => Service.Uninstall(Configuration);

                m.ShouldThrow<ApplicationException>();
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
                var fileToSetReadOnly = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
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
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_uninstalled_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("uninstalled 1/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.ShouldBeFalse();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_uninstalling_a_package_with_a_read_and_delete_share_locked_file : ScenariosBase
        {
            private PackageResult _packageResult;

            private FileStream fileStream;

            public override void Context()
            {
                base.Context();
                var fileToOpen = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                fileStream = new FileStream(fileToOpen, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                fileStream.Close();
                fileStream.Dispose();
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_uninstall_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [Pending("Does not work under .Net 4.8, See issue #2690")]
            [Broken]
            public void Should_not_be_able_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_uninstalled_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("uninstalled 1/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.ShouldBeFalse();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_uninstalling_a_package_with_an_exclusively_locked_file : ScenariosBase
        {
            private PackageResult _packageResult;

            private FileStream fileStream;

            public override void Context()
            {
                base.Context();
                var fileToOpen = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                fileStream = new FileStream(fileToOpen, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                fileStream.Close();
                fileStream.Dispose();
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_be_able_to_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_old_files_in_directory()
            {
                var shimFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "console.exe");

                FileAssert.Exists(shimFile);
            }

            [Fact]
            public void Should_contain_a_message_that_it_was_not_able_to_uninstall()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("uninstalled 0/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                _packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.ShouldBeFalse();
            }
        }

        public class When_uninstalling_a_package_with_added_files : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                var fileAdded = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "dude.txt");
                File.WriteAllText(fileAdded, "hellow");
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_keep_the_added_file()
            {
                var fileAdded = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "dude.txt");

                FileAssert.Exists(fileAdded);
            }

            [Fact]
            public void Should_delete_everything_but_the_added_file_from_the_package_directory()
            {
                var files = Directory.GetFiles(Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames));

                foreach (var file in files.OrEmpty())
                {
                    Path.GetFileName(file).ShouldEqual("dude.txt", "Expected files were not deleted.");
                }
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }
        }

        public class When_uninstalling_a_package_with_changed_files : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");
                File.WriteAllText(fileChanged, "hellow");
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_keep_the_changed_file()
            {
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");

                FileAssert.Exists(fileChanged);
            }

            [Fact]
            public void Should_delete_everything_but_the_changed_file_from_the_package_directory()
            {
                var files = Directory.GetFiles(Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames));

                foreach (var file in files.OrEmpty())
                {
                    Path.GetFileName(file).ShouldEqual("chocolateyInstall.ps1", "Expected files were not deleted.");
                }
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }
        }

        public class When_force_uninstalling_a_package_with_added_and_changed_files : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");
                File.WriteAllText(fileChanged, "hellow");
                var fileAdded = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "dude.txt");
                File.WriteAllText(fileAdded, "hellow");
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_keep_the_added_file()
            {
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "dude.txt");

                FileAssert.DoesNotExist(fileChanged);
            }

            [Fact]
            public void Should_not_keep_the_changed_file()
            {
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");

                FileAssert.DoesNotExist(fileChanged);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }
        }

        public class When_uninstalling_a_package_that_does_not_exist : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "somethingnonexisting";
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_contain_a_message_that_it_was_unable_to_find_package()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Error).OrEmpty())
                {
                    if (message.Contains("somethingnonexisting is not installed. Cannot uninstall a non-existent package")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("0/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Should_have_an_error_package_result()
            {
                bool errorFound = false;
                foreach (var message in packageResult.Messages)
                {
                    if (message.MessageType == ResultType.Error)
                    {
                        errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_uninstalling_a_package_that_errors : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "badpackage";
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_remove_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_still_have_the_package_file_in_the_directory()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames +  NuGetConstants.PackageExtension);
                FileAssert.Exists(packageFile);
            }

            [Fact]
            public void Should_not_put_the_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bad", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_was_unable_to_install_a_package()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("0/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Should_have_an_error_package_result()
            {
                bool errorFound = false;
                foreach (var message in packageResult.Messages)
                {
                    if (message.MessageType == ResultType.Error)
                    {
                        errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_expected_error_in_package_result()
            {
                bool errorFound = false;
                foreach (var message in packageResult.Messages)
                {
                    if (message.MessageType == ResultType.Error)
                    {
                        if (message.Message.Contains("chocolateyUninstall.ps1")) errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }


        public class When_uninstalling_a_hook_package : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "scriptpackage.hook";
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + ".1.0.0" + NuGetConstants.PackageExtension);
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
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            public void Should_remove_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.get_top_level(), "hooks", Configuration.PackageNames.Replace(".hook", string.Empty));

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }
        }

        public class When_uninstalling_a_package_happy_path_with_hooks : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.add_packages_to_source_location(Configuration, "scriptpackage.hook" + "*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "scriptpackage.hook", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_delete_any_files_created_during_the_install()
            {
                var generatedFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "simplefile.txt");

                FileAssert.DoesNotExist(generatedFile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script()
            {
                MockLogger.ContainsMessage("installpackage 1.0.0 Before Modification", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyUninstall_script()
            {
                MockLogger.ContainsMessage("installpackage 1.0.0 Uninstalled", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_all_hook_script()
            {
                MockLogger.ContainsMessage("pre-uninstall-all.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_all_hook_script()
            {
                MockLogger.ContainsMessage("post-uninstall-all.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_installpackage_hook_script()
            {
                MockLogger.ContainsMessage("pre-uninstall-installpackage.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_installpackage_hook_script()
            {
                MockLogger.ContainsMessage("post-uninstall-installpackage.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_upgradepackage_hook_script()
            {
                MockLogger.ContainsMessage("pre-uninstall-upgradepackage.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeFalse();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_beforemodify_hook_script()
            {
                MockLogger.ContainsMessage("pre-beforemodify-all.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_beforemodify_hook_script()
            {
                MockLogger.ContainsMessage("post-beforemodify-all.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }
        }

        public class When_uninstalling_a_package_with_uppercase_id : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.add_packages_to_source_location(Configuration, "UpperCase" + "*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "UpperCase", "1.1.0");

                Configuration.PackageNames = Configuration.Input = "UpperCase";
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script()
            {
                MockLogger.ContainsMessage("UpperCase 1.1.0 Before Modification", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyUninstall_script()
            {
                MockLogger.ContainsMessage("UpperCase 1.1.0 Uninstalled", LogLevel.Info).ShouldBeTrue();
            }
        }

        public class When_uninstalling_a_package_with_non_normalized_version : ScenariosBase
        {
            private PackageResult packageResult;

            private string NonNormalizedVersion = "0004.0004.00005.00";

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Scenario.add_changed_version_package_to_source_location(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, NonNormalizedVersion);
                Scenario.install_package(Configuration, "upgradepackage", NonNormalizedVersion);
            }

            public override void Because()
            {
                Results = Service.Uninstall(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_delete_any_files_created_during_the_install()
            {
                var generatedFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "simplefile.txt");

                FileAssert.DoesNotExist(generatedFile);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).OrEmpty())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script()
            {
                MockLogger.ContainsMessage("upgradepackage {0} Before Modification".FormatWith(NonNormalizedVersion), LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyUninstall_script()
            {
                MockLogger.ContainsMessage("upgradepackage {0} Uninstalled".FormatWith(NonNormalizedVersion), LogLevel.Info).ShouldBeTrue();
            }
        }

        public class When_uninstalling_a_package_with_remove_dependencies_with_beforeModify : ScenariosBase
        {
            private const string TargetPackageName = "hasdependencywithbeforemodify";
            private const string DependencyName = "isdependencywithbeforemodify";

            public override void Context()
            {
                base.Context();

                Scenario.add_packages_to_source_location(Configuration, "{0}.*".FormatWith(TargetPackageName) + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "{0}.*".FormatWith(DependencyName) + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, TargetPackageName, "1.0.0");
                Scenario.install_package(Configuration, DependencyName, "1.0.0");

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
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", TargetPackageName, "{0}.nupkg".FormatWith(TargetPackageName));
                File.Exists(packageFile).ShouldBeFalse();
            }

            [Fact]
            public void Should_uninstall_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", DependencyName, "{0}.nupkg".FormatWith(DependencyName));
                File.Exists(packageFile).ShouldBeFalse();
            }

            [Fact]
            public void Should_contain_a_message_that_everything_uninstalled_successfully()
            {
                MockLogger.ContainsMessage("uninstalled 2/2", LogLevel.Warn).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_target_package_beforeModify()
            {
                MockLogger.ContainsMessage("Ran BeforeModify: {0} {1}".FormatWith(TargetPackageName, "1.0.0"), LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_dependency_package_beforeModify()
            {
                MockLogger.ContainsMessage("Ran BeforeModify: {0} {1}".FormatWith(DependencyName, "1.0.0"), LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }

            }
        }
    }
}
