// Copyright © 2017 - 2018 Chocolatey Software, Inc
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
    using bdddoc.core;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.results;
    using NuGet;
    using NUnit.Framework;
    using Should;
    using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

    public class UninstallScenarios
    {
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
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "badpackage*" + Constants.PackageExtension);
                Configuration.SkipPackageInstallProvider = true;
                Scenario.install_package(Configuration, "badpackage", "1.0");
                Configuration.SkipPackageInstallProvider = false;

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                CommandExecutor = new CommandExecutor(NUnitSetup.Container.GetInstance<IFileSystem>());
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_noop_uninstalling_a_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Noop = true;
            }

            public override void Because()
            {
                Service.uninstall_noop(Configuration);
            }

            [Fact]
            public void should_not_uninstall_a_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_that_it_would_have_uninstalled_a_package()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("Would have uninstalled installpackage v1.0.0")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_that_it_would_have_run_a_powershell_script()
            {
                MockLogger.contains_message("chocolateyuninstall.ps1").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_that_it_would_have_run_powershell_modification_script()
            {
                MockLogger.contains_message("chocolateyBeforeModify.ps1").ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_noop_uninstalling_a_package_that_does_not_exist : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "somethingnonexisting";
                Configuration.Noop = true;
            }

            public override void Because()
            {
                Service.uninstall_noop(Configuration);
            }

            [Fact]
            public void should_contain_a_message_that_it_was_unable_to_find_package()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Error).or_empty_list_if_null())
                {
                    if (message.Contains("somethingnonexisting is not installed. Cannot uninstall a non-existent package")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_uninstalling_a_package_happy_path : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Because()
            {
                Results = Service.uninstall_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_any_files_created_during_the_install()
            {
                var generatedFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "simplefile.txt");

                File.Exists(generatedFile).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            public void should_have_executed_chocolateyBeforeModify_script()
            {
                MockLogger.contains_message("installpackage 1.0.0 Before Modification", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            public void should_have_executed_chocolateyUninstall_script()
            {
                MockLogger.contains_message("installpackage 1.0.0 Uninstalled", LogLevel.Info).ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_force_uninstalling_a_package : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.uninstall_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_uninstalling_packages_with_packages_config : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                var packagesConfig = "{0}\\context\\testing.packages.config".format_with(Scenario.get_top_level());
                Configuration.PackageNames = Configuration.Input = packagesConfig;
            }

            public override void Because()
            {
            }

            [Fact]
            [ExpectedException(typeof(ApplicationException))]
            public void should_throw_an_error_that_it_is_not_allowed()
            {
                Results = Service.uninstall_run(Configuration);
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_uninstalling_a_package_with_readonly_files : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                var fileToSetReadOnly = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                var fileSystem = new DotNetFileSystem();
                fileSystem.ensure_file_attribute_set(fileToSetReadOnly, FileAttributes.ReadOnly);
            }

            public override void Because()
            {
                Results = Service.uninstall_run(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_uninstall_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_message_that_it_uninstalled_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("uninstalled 1/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                _packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                _packageResult.Warning.ShouldBeFalse();
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_uninstalling_a_package_with_a_read_and_delete_share_locked_file : ScenariosBase
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
            }

            public override void Because()
            {
                Results = Service.uninstall_run(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_uninstall_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_not_be_able_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_that_it_uninstalled_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("uninstalled 1/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                _packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                _packageResult.Warning.ShouldBeFalse();
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_uninstalling_a_package_with_an_exclusively_locked_file : ScenariosBase
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
            }

            public override void Because()
            {
                Results = Service.uninstall_run(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_not_be_able_to_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_old_files_in_directory()
            {
                var shimFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.Exists(shimFile).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_that_it_was_not_able_to_uninstall()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("uninstalled 0/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_a_successful_package_result()
            {
                _packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                _packageResult.Warning.ShouldBeFalse();
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_uninstalling_a_package_with_added_files : ScenariosBase
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
                Results = Service.uninstall_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_keep_the_added_file()
            {
                var fileAdded = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "dude.txt");

                File.Exists(fileAdded).ShouldBeTrue();
            }

            [Fact]
            public void should_delete_everything_but_the_added_file_from_the_package_directory()
            {
                var files = Directory.GetFiles(Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames));

                foreach (var file in files.or_empty_list_if_null())
                {
                    Path.GetFileName(file).ShouldEqual("dude.txt", "Expected files were not deleted.");
                }
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_uninstalling_a_package_with_changed_files : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                File.WriteAllText(fileChanged, "hellow");
            }

            public override void Because()
            {
                Results = Service.uninstall_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_keep_the_changed_file()
            {
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");

                File.Exists(fileChanged).ShouldBeTrue();
            }

            [Fact]
            public void should_delete_everything_but_the_changed_file_from_the_package_directory()
            {
                var files = Directory.GetFiles(Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames));

                foreach (var file in files.or_empty_list_if_null())
                {
                    Path.GetFileName(file).ShouldEqual("chocolateyInstall.ps1", "Expected files were not deleted.");
                }
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_force_uninstalling_a_package_with_added_and_changed_files : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                File.WriteAllText(fileChanged, "hellow");
                var fileAdded = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "dude.txt");
                File.WriteAllText(fileAdded, "hellow");
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.uninstall_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_not_keep_the_added_file()
            {
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "dude.txt");

                File.Exists(fileChanged).ShouldBeFalse();
            }

            [Fact]
            public void should_not_keep_the_changed_file()
            {
                var fileChanged = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");

                File.Exists(fileChanged).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_delete_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void config_should_match_package_result_name()
            {
                packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }
        }

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_uninstalling_a_package_that_does_not_exist : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "somethingnonexisting";
            }

            public override void Because()
            {
                Results = Service.uninstall_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_contain_a_message_that_it_was_unable_to_find_package()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Error).or_empty_list_if_null())
                {
                    if (message.Contains("somethingnonexisting is not installed. Cannot uninstall a non-existent package")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_uninstalled_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void should_have_an_error_package_result()
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

        [Concern(typeof(ChocolateyUninstallCommand))]
        public class when_uninstalling_a_package_that_errors : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "badpackage";
            }

            public override void Because()
            {
                Results = Service.uninstall_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_not_remove_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_still_have_the_package_file_in_the_directory()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                File.Exists(packageFile).ShouldBeTrue();
            }

            [Fact]
            public void should_not_put_the_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bad", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_not_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_was_unable_to_install_a_package()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeFalse();
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }

            [Fact]
            public void should_have_an_error_package_result()
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
            public void should_have_expected_error_in_package_result()
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
    }
}
