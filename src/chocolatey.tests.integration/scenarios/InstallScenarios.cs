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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.XPath;
    using bdddoc.core;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.results;
    using NuGet;
    using Should;
    using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

    public class InstallScenarios
    {
        public abstract class ScenariosBase : TinySpec
        {
            protected ConcurrentDictionary<string, PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;
            protected CommandExecutor CommandExecutor;

            public override void Context()
            {
                Configuration = Scenario.install();
                Scenario.reset(Configuration);
                Configuration.PackageNames = Configuration.Input = "installpackage";
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "badpackage.1*" + Constants.PackageExtension);

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                CommandExecutor = new CommandExecutor(NUnitSetup.Container.GetInstance<IFileSystem>());
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_noop_installing_a_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Noop = true;
            }

            public override void Because()
            {
                Service.install_noop(Configuration);
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_message_that_it_would_have_used_Nuget_to_install_a_package()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Info).or_empty_list_if_null())
                {
                    if (message.Contains("would have used NuGet to install packages")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_that_it_would_have_run_a_powershell_script()
            {
                MockLogger.contains_message("chocolateyinstall.ps1", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_a_message_that_it_would_have_run_powershell_modification_script()
            {
                MockLogger.contains_message("chocolateyBeforeModify.ps1", LogLevel.Info).ShouldBeFalse();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_noop_installing_a_package_that_does_not_exist : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "somethingnonexisting";
                Configuration.Noop = true;
            }

            public override void Because()
            {
                Service.install_noop(Configuration);
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_message_that_it_would_have_used_Nuget_to_install_a_package()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Info).or_empty_list_if_null())
                {
                    if (message.Contains("would have used NuGet to install packages")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_that_it_was_unable_to_find_package()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Error).or_empty_list_if_null())
                {
                    if (message.Contains("somethingnonexisting not installed. The package was not found with the source(s) listed")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_happy_path : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                File.Exists(shimfile).ShouldBeTrue();
            }

            [Fact]
            public void should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                File.Exists(shimfile).ShouldBeTrue();
            }

            [Fact]
            public void should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "not.installed.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "casemismatch.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.get_top_level(), "extensions", Configuration.PackageNames);

                Directory.Exists(extensionsDirectory).ShouldBeFalse();
            }

            [Fact]
            public void should_have_a_console_shim_that_is_set_for_non_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");
                CommandExecutor.execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                var messageFound = false;

                foreach (var message in messages.or_empty_list_if_null())
                {
                    if (string.IsNullOrWhiteSpace(message)) continue;
                    if (message.Contains("is gui? False")) messageFound = true;
                }

                messageFound.ShouldBeTrue("GUI false message not found");
            }

            [Fact]
            public void should_have_a_graphical_shim_that_is_set_for_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");
                CommandExecutor.execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                var messageFound = false;

                foreach (var message in messages.or_empty_list_if_null())
                {
                    if (string.IsNullOrWhiteSpace(message)) continue;
                    if (message.Contains("is gui? True")) messageFound = true;
                }

                messageFound.ShouldBeTrue("GUI true message not found");
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }

            [Fact]
            public void should_have_executed_chocolateyInstall_script()
            {
                MockLogger.contains_message("installpackage v1.0.0 has been installed", LogLevel.Info).ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_packages_with_packages_config : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                var packagesConfig = "{0}\\context\\testing.packages.config".format_with(Scenario.get_top_level());
                Configuration.PackageNames = Configuration.Input = packagesConfig;
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "upgradepackage*" + Constants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    if (packageResult.Value.Name.is_equal_to("missingpackage")) continue;

                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_expected_packages_in_the_lib_directory()
            {
                var packagesExpected = new List<string>
                {
                    "installpackage",
                    "hasdependency",
                    "isdependency",
                    "upgradepackage"
                };
                foreach (var package in packagesExpected)
                {
                    var packageDir = Path.Combine(Scenario.get_top_level(), "lib", package);
                    Directory.Exists(packageDir).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_4_out_of_5_packages_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("5/6")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_that_upgradepackage_with_an_expected_specified_version_was_installed()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Info).or_empty_list_if_null())
                {
                    if (message.Contains("upgradepackage v1.0.0")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result_for_all_but_expected_missing_package()
            {
                foreach (var packageResult in Results)
                {
                    if (packageResult.Value.Name.is_equal_to("missingpackage")) continue;

                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_a_successful_package_result_for_missing_package()
            {
                foreach (var packageResult in Results)
                {
                    if (!packageResult.Value.Name.is_equal_to("missingpackage")) continue;

                    packageResult.Value.Success.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_specify_config_file_is_being_used_in_message()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Info).or_empty_list_if_null())
                {
                    if (message.Contains("Installing from config file:")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_print_out_package_from_config_file_in_message()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Info).or_empty_list_if_null())
                {
                    if (message.Contains("installpackage")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_an_already_installed_package : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_still_have_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_still_have_the_expected_version_of_the_package_installed()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_was_unable_to_install_any_packages()
            {
                bool installWarning = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) installWarning = true;
                }

                installWarning.ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_about_force_to_reinstall()
            {
                bool installWarning = false;
                foreach (var messageType in MockLogger.Messages.or_empty_list_if_null())
                {
                    foreach (var message in messageType.Value)
                    {
                        if (message.Contains("Use --force to reinstall")) installWarning = true;
                    }
                }

                installWarning.ShouldBeTrue();
            }

            [Fact]
            public void should_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeTrue();
            }

            [Fact]
            public void should_ave_warning_package_result()
            {
                packageResult.Warning.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_force_installing_an_already_installed_package : ScenariosBase
        {
            private PackageResult packageResult;
            private readonly string modifiedText = "bob";

            public override void Context()
            {
                base.Context();
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                var fileToModify = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                File.WriteAllText(fileToModify, modifiedText);

                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_remove_and_re_add_the_package_files_in_the_lib_directory()
            {
                var modifiedFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                File.ReadAllText(modifiedFile).ShouldNotEqual(modifiedText);
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_force_installing_an_already_installed_package_that_errors : ScenariosBase
        {
            private PackageResult packageResult;
            private readonly string modifiedText = "bob";

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "badpackage";
                Configuration.SkipPackageInstallProvider = true;
                Scenario.install_package(Configuration, "badpackage", "1.0");
                Configuration.SkipPackageInstallProvider = false;
                Configuration.Force = true;

                var fileToModify = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                File.WriteAllText(fileToModify, modifiedText);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_restore_the_backup_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_restore_the_original_files_in_the_package_lib_folder()
            {
                var modifiedFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                File.ReadAllText(modifiedFile).ShouldEqual(modifiedText);
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_message_that_it_was_unsuccessful()
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
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_force_installing_an_already_installed_package_with_a_read_and_delete_share_locked_file : ScenariosBase
        {
            private PackageResult packageResult;
            private FileStream fileStream;

            public override void Context()
            {
                base.Context();
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Configuration.Force = true;
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
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_reinstall_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_reinstall_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_not_be_able_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_message_that_it_installed_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
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
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_force_installing_an_already_installed_package_with_with_an_exclusively_locked_file : ScenariosBase
        {
            private PackageResult packageResult;
            private FileStream fileStream;

            public override void Context()
            {
                base.Context();
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Configuration.Force = true;
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
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_have_a_package_installed_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_still_have_the_package_installed_with_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            [Pending("Force install with file locked leaves inconsistent state - GH-114")]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_message_that_it_was_unable_to_reinstall_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            [Pending("Force install with file locked leaves inconsistent state - GH-114")]
            public void should_not_have_a_successful_package_result()
            {
                packageResult.Success.ShouldBeFalse();
            }

            [Fact]
            public void should_have_inconclusive_package_result()
            {
                packageResult.Inconclusive.ShouldBeTrue();
            }

            [Fact]
            [Pending("Force install with file locked leaves inconsistent state - GH-114")]
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_that_exists_but_a_version_that_does_not_exist : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Version = "1.0.1";
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_did_not_install_successfully()
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
                        if (message.Message.Contains("The package was not found")) errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_one()
            {
                packageResult.Version.ShouldEqual("1.0.1");
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_that_does_not_exist : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonexisting";
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
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
                        if (message.Message.Contains("The package was not found")) errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_that_errors : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "badpackage";
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_put_a_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bad", Configuration.PackageNames);

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
                        if (message.Message.Contains("chocolateyInstall.ps1")) errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_that_has_nonterminating_errors : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonterminatingerror";
                Configuration.Features.FailOnStandardError = false; //the default

                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + Constants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.Input);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.Input, Configuration.Input + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_contain_a_message_that_it_installed_successfully()
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
                packageResult.Name.ShouldEqual(Configuration.Input);
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0");
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_that_has_nonterminating_errors_with_fail_on_stderr : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonterminatingerror";
                Configuration.Features.FailOnStandardError = true;

                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + Constants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_put_a_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bad", Configuration.PackageNames);

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
                        if (message.Message.Contains("chocolateyInstall.ps1")) errorFound = true;
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_side_by_side_package : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.AllowMultipleVersions = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames) + ".1.0.0";

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_switching_a_normal_package_to_a_side_by_side_package : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Configuration.AllowMultipleVersions = true;
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames) + ".1.0.0";

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_switching_a_side_by_side_package_to_a_normal_package : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.AllowMultipleVersions = true;
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Configuration.AllowMultipleVersions = false;
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_with_dependencies_happy : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_expected_version_of_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_contain_a_message_that_everything_installed_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("3/3")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Version.ShouldEqual("1.0.0");
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_force_installing_an_already_installed_package_with_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "isdependency*" + Constants.PackageExtension);
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_still_have_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_not_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_contain_a_message_that_it_installed_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Version.ShouldEqual("1.0.0");
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_force_installing_an_already_installed_package_forcing_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "isdependency*" + Constants.PackageExtension);
                Configuration.Force = true;
                Configuration.ForceDependencies = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_reinstall_the_floating_dependency_with_the_latest_version_that_satisfies_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.1.0.0");
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_exact_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("3/3")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_force_installing_an_already_installed_package_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "isdependency*" + Constants.PackageExtension);
                Configuration.Force = true;
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_not_touch_the_floating_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_not_touch_the_exact_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_force_installing_an_already_installed_package_forcing_and_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "isdependency*" + Constants.PackageExtension);
                Configuration.Force = true;
                Configuration.ForceDependencies = true;
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_remove_the_floating_dependency()
            {
                var dependency = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");
                Directory.Exists(dependency).ShouldBeFalse();
            }

            [Fact]
            public void should_remove_the_exact_dependency()
            {
                var dependency = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency");
                Directory.Exists(dependency).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_with_dependencies_and_dependency_cannot_be_found : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency*" + Constants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_not_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_was_unable_to_install_any_packages()
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
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_have_an_error_package_result()
            {
                bool errorFound = false;

                foreach (var packageResult in Results)
                {
                    foreach (var message in packageResult.Value.Messages)
                    {
                        if (message.MessageType == ResultType.Error)
                        {
                            errorFound = true;
                        }
                    }
                }

                errorFound.ShouldBeTrue();
            }

            [Fact]
            public void should_have_expected_error_in_package_result()
            {
                bool errorFound = false;

                foreach (var packageResult in Results)
                {
                    foreach (var message in packageResult.Value.Messages)
                    {
                        if (message.MessageType == ResultType.Error)
                        {
                            if (message.Message.Contains("Unable to resolve dependency 'isdependency")) errorFound = true;
                        }
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_ignoring_dependencies_that_cannot_be_found : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency*" + Constants.PackageExtension);
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("2.1.0.0");
            }

            [Fact]
            public void should_not_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_that_depends_on_a_newer_version_of_an_installed_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.6.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.1.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "isdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.6.0.0");
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.1.0.0");
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
            {
                bool installedSuccessfully = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("3/3")) installedSuccessfully = true;
                }

                installedSuccessfully.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_that_depends_on_an_unavailable_newer_version_of_an_installed_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.6.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "isdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_not_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_message_that_is_was_unable_to_install_any_packages()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("0/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_not_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_that_depends_on_an_unavailable_newer_version_of_an_installed_dependency_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.6.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "isdependency", "1.0.0");
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.6.0.0");
            }

            [Fact]
            public void should_contain_a_message_that_it_installed_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("1/1")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_force_installing_a_package_that_depends_on_an_unavailable_newer_version_of_an_installed_dependency_forcing_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.6.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "isdependency", "1.0.0");
                Configuration.Force = true;
                Configuration.ForceDependencies = true;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            public void should_not_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_was_unable_to_install_any_packages()
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
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_have_an_error_package_result()
            {
                bool errorFound = false;

                foreach (var packageResult in Results)
                {
                    foreach (var message in packageResult.Value.Messages)
                    {
                        if (message.MessageType == ResultType.Error)
                        {
                            errorFound = true;
                        }
                    }
                }

                errorFound.ShouldBeTrue();
            }

            [Fact]
            public void should_have_expected_error_in_package_result()
            {
                bool errorFound = false;

                foreach (var packageResult in Results)
                {
                    foreach (var message in packageResult.Value.Messages)
                    {
                        if (message.MessageType == ResultType.Error)
                        {
                            if (message.Message.Contains("Unable to resolve dependency 'isdependency")) errorFound = true;
                        }
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_with_dependencies_on_a_newer_version_of_a_package_than_an_existing_package_has_with_that_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "conflictingdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "conflictingdependency.1.0.1*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "isdependency", "1.0.0");
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.1.0");
            }

            [Fact]
            public void should_contain_a_message_that_it_installed_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("installed 2/2")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            public void should_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_with_dependencies_on_a_newer_version_of_a_package_than_are_allowed_by_an_existing_package_with_that_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "conflictingdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "conflictingdependency.2.1.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "isdependency", "1.0.0");
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_install_the_conflicting_package()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_install_the_conflicting_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_upgrade_the_minimum_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_upgrade_the_exact_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_contain_a_message_that_it_was_unable_to_install_any_packages()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("installed 0/3")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeFalse();
                }
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_have_an_error_package_result()
            {
                bool errorFound = false;

                foreach (var packageResult in Results)
                {
                    foreach (var message in packageResult.Value.Messages)
                    {
                        if (message.MessageType == ResultType.Error)
                        {
                            errorFound = true;
                        }
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_with_dependencies_on_an_older_version_of_a_package_than_is_already_installed : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "conflictingdependency.2.1.0*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "conflictingdependency", "2.1.0");
            }

            /*
             Setup should have the following installed:
             * conflictingdependency 2.1.0
             * isexactversiondependency 2.0.0
             * isdependency at least 2.0.0
             */

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_install_the_conflicting_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeFalse();
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_upgrade_the_exact_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_contain_a_message_that_it_was_unable_to_install_any_packages()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("installed 0/3")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeFalse();
                }
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            [Pending("NuGet does not deal with version conflicts - GH-116")]
            public void should_have_an_error_package_result()
            {
                bool errorFound = false;

                foreach (var packageResult in Results)
                {
                    foreach (var message in packageResult.Value.Messages)
                    {
                        if (message.MessageType == ResultType.Error)
                        {
                            errorFound = true;
                        }
                    }
                }

                errorFound.ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_with_a_dependent_package_that_also_depends_on_a_less_constrained_but_still_valid_dependency_of_the_same_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "toplevelhasexactversiondependency";
                Scenario.add_packages_to_source_location(Configuration, "toplevelhasexactversiondependency*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "childdependencywithlooserversiondependency*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + Constants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            /*
             Because should result in the following installed:
             * toplevelhasexactversiondependency 1.0.0
             * childdependencywithlooserversiondependency 1.0.0
             * isexactversiondependency 1.0.0
             */

            [Fact]
            [Pending("NuGet does not handle version conflicts with highestversion dependency resolution - GH-507")]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    Directory.Exists(packageResult.Value.InstallLocation).ShouldBeTrue();
                }
            }

            [Fact]
            [Pending("NuGet does not handle version conflicts with highestversion dependency resolution - GH-507")]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            [Pending("NuGet does not handle version conflicts with highestversion dependency resolution - GH-507")]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "childdependencywithlooserversiondependency");

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            [Pending("NuGet does not handle version conflicts with highestversion dependency resolution - GH-507")]
            public void should_install_the_expected_version_of_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "childdependencywithlooserversiondependency", "childdependencywithlooserversiondependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            [Pending("NuGet does not handle version conflicts with highestversion dependency resolution - GH-507")]
            public void should_install_the_expected_version_of_the_constrained_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            [Pending("NuGet does not handle version conflicts with highestversion dependency resolution - GH-507")]
            public void should_contain_a_message_that_everything_installed_successfully()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("3/3")) expectedMessage = true;
                }

                expectedMessage.ShouldBeTrue();
            }

            [Fact]
            [Pending("NuGet does not handle version conflicts with highestversion dependency resolution - GH-507")]
            public void should_have_a_successful_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }

            [Fact]
            [Pending("NuGet does not handle version conflicts with highestversion dependency resolution - GH-507")]
            public void should_not_have_inconclusive_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            [Pending("NuGet does not handle version conflicts with highestversion dependency resolution - GH-507")]
            public void should_not_have_warning_package_result()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_from_a_nupkg_file : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "{0}\\installpackage.1.0.0.nupkg".format_with(Configuration.Sources);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                Directory.Exists(packageResult.InstallLocation).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "installpackage");

                Directory.Exists(packageDir).ShouldBeTrue();
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "installpackage", "installpackage" + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                File.Exists(shimfile).ShouldBeTrue();
            }

            [Fact]
            public void should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                File.Exists(shimfile).ShouldBeTrue();
            }

            [Fact]
            public void should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "not.installed.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "casemismatch.exe");

                File.Exists(shimfile).ShouldBeFalse();
            }

            [Fact]
            public void should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.get_top_level(), "extensions", "installpackage");

                Directory.Exists(extensionsDirectory).ShouldBeFalse();
            }

            [Fact]
            public void should_have_a_console_shim_that_is_set_for_non_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");
                CommandExecutor.execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                var messageFound = false;

                foreach (var message in messages.or_empty_list_if_null())
                {
                    if (string.IsNullOrWhiteSpace(message)) continue;
                    if (message.Contains("is gui? False")) messageFound = true;
                }

                messageFound.ShouldBeTrue("GUI false message not found");
            }

            [Fact]
            public void should_have_a_graphical_shim_that_is_set_for_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");
                CommandExecutor.execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                var messageFound = false;

                foreach (var message in messages.or_empty_list_if_null())
                {
                    if (string.IsNullOrWhiteSpace(message)) continue;
                    if (message.Contains("is gui? True")) messageFound = true;
                }

                messageFound.ShouldBeTrue("GUI true message not found");
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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
                packageResult.Name.ShouldEqual("installpackage");
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_with_config_transforms : ScenariosBase
        {
            private PackageResult packageResult;
            private string _xmlFilePath = string.Empty;
            private XPathNavigator _xPathNavigator;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Scenario.add_packages_to_source_location(Configuration, "upgradepackage.1.0.0*" + Constants.PackageExtension);

                _xmlFilePath = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "console.exe.config");
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
                var xmlDocument = new XPathDocument(_xmlFilePath);
                _xPathNavigator = xmlDocument.CreateNavigator();
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + Constants.PackageExtension);
                var package = new OptimizedZipPackage(packageFile);
                package.Version.Version.to_string().ShouldEqual("1.0.0.0");
            }

            [Fact]
            public void should_contain_a_warning_message_that_it_installed_successfully()
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
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }

            [Fact]
            public void should_not_change_the_test_value_in_the_config_due_to_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='test']/@value").TypedValue.to_string().ShouldEqual("default 1.0.0");
            }

            [Fact]
            public void should_change_the_testReplace_value_in_the_config_due_to_XDT_Replace()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='testReplace']/@value").TypedValue.to_string().ShouldEqual("1.0.0");
            }

            [Fact]
            public void should_add_the_insert_value_in_the_config_due_to_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='insert']/@value").TypedValue.to_string().ShouldEqual("1.0.0");
            }
        }

        [Concern(typeof(ChocolateyInstallCommand))]
        public class when_installing_a_package_with_no_sources_enabled : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = null;
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_have_no_sources_enabled_result()
            {
                MockLogger.contains_message("Installation was NOT successful. There are no sources enabled for", LogLevel.Error).ShouldBeTrue();
            }

            [Fact]
            public void should_not_install_any_packages()
            {
                Results.Count().ShouldEqual(0);
            }
        }
    }
}
