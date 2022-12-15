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
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.XPath;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.results;
    using NuGet.Configuration;
    using NuGet.Packaging;
    using NUnit.Framework;
    using Should;
    using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

    public class InstallScenarios
    {
        [ConcernFor("install")]
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
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "badpackage.1*" + NuGetConstants.PackageExtension);

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                CommandExecutor = new CommandExecutor(NUnitSetup.Container.GetInstance<IFileSystem>());
            }
        }

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

                DirectoryAssert.DoesNotExist(packageDir);
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
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
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

                DirectoryAssert.DoesNotExist(packageDir);
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

        public class when_installing_a_package_happy_path : ScenariosBase
        {
            private PackageResult packageResult;

            protected virtual string TestSemVersion => "1.0.0";

            public override void Context()
            {
                base.Context();

                if (TestSemVersion != "1.0.0")
                {
                    Configuration.Version = TestSemVersion;
                }
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual(TestVersion());
                }
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            public void should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "not.installed.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "casemismatch.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.get_top_level(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.get_top_level(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
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
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
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
                packageResult.Version.ShouldEqual(TestVersion());
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_have_executed_chocolateyInstall_script()
            {
                var message = "installpackage v{0} has been installed".format_with(TestVersion());

                MockLogger.contains_message(message, LogLevel.Info).ShouldBeTrue();
            }

            protected string TestVersion()
            {
                var index = TestSemVersion.IndexOf('+');

                if (index > 0)
                {
                    return TestSemVersion.Substring(0, index);
                }

                return TestSemVersion;
            }
        }

        [Categories.SemVer20]
        public class when_installing_a_package_with_semver_2_0_meta_data : when_installing_a_package_happy_path
        {
            protected override string TestSemVersion => "0.9.9+build.543";
        }

        [Categories.SemVer20]
        public class when_installing_a_package_with_semver_2_0_pre_release_tag : when_installing_a_package_happy_path
        {
            protected override string TestSemVersion => "1.0.0-alpha.34";
        }

        public class when_installing_packages_with_packages_config : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                var packagesConfig = "{0}{1}context{1}testing.packages.config".format_with(Scenario.get_top_level(), Path.DirectorySeparatorChar);
                Configuration.PackageNames = Configuration.Input = packagesConfig;
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "upgradepackage*" + NuGetConstants.PackageExtension);
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

                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
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
                    DirectoryAssert.Exists(packageDir);
                }
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
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

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_still_have_the_expected_version_of_the_package_installed()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
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

        public class when_force_installing_an_already_installed_package : ScenariosBase
        {
            private PackageResult packageResult;
            private readonly string modifiedText = "bob";

            public override void Context()
            {
                base.Context();
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                var fileToModify = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");
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
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_remove_and_re_add_the_package_files_in_the_lib_directory()
            {
                var modifiedFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");
                File.ReadAllText(modifiedFile).ShouldNotEqual(modifiedText);
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
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

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
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
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedString().ShouldEqual("1.0.0");
                }
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

                DirectoryAssert.DoesNotExist(packageDir);
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

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
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
                fileStream.Dispose();
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_reinstall_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_reinstall_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            [Pending("Does not work under .Net 4.8, See issue #2690")]
            [Broken]
            public void should_not_be_able_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
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

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
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
                fileStream.Dispose();
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

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_still_have_the_package_installed_with_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
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
            public void should_not_have_warning_package_result()
            {
                packageResult.Warning.ShouldBeFalse();
            }
        }

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

                DirectoryAssert.DoesNotExist(packageDir);
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

                DirectoryAssert.DoesNotExist(packageDir);
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

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
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

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void should_put_a_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bad", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
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

        public class when_installing_a_package_that_has_nonterminating_errors : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonterminatingerror";
                Configuration.Features.FailOnStandardError = false; //the default

                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.Input);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.Input, Configuration.Input + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedString().ShouldEqual("1.0.0");
                }
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

            [Fact, Pending("Current version of the NuGet client library changes this to 1.0"), Broken]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.0.0");
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class when_installing_a_package_that_has_nonterminating_errors_with_fail_on_stderr : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonterminatingerror";
                Configuration.Features.FailOnStandardError = true;

                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
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

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void should_put_a_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib-bad", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
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

        [Categories.SideBySide]
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
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames) + ".1.0.0";

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_put_version_in_nupkg_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.0.0"),
                    (Configuration.PackageNames + ".1.0.0" + NuGetConstants.PackageExtension));

                FileAssert.Exists(packageFile);
            }

            [Fact, Pending("Will be removed together with side by side removal"), Broken]
            public void should_put_version_in_nuspec_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.0.0"),
                    (Configuration.PackageNames + ".1.0.0" + NuGetConstants.ManifestExtension));

                FileAssert.Exists(packageFile);
            }

            [Fact]
            public void should_not_have_nupkg_without_version_in_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.0.0"),
                    (Configuration.PackageNames + NuGetConstants.PackageExtension));

                FileAssert.DoesNotExist(packageFile);
            }

            [Fact, Pending("Broken, will be removed together with side by side install"), Broken]
            public void should_not_have_nuspec_without_version_in_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.0.0"),
                    (Configuration.PackageNames + NuGetConstants.ManifestExtension));

                FileAssert.DoesNotExist(packageFile);
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
            public void should_contain_a_warning_message_that_installing_package_with_multiple_versions_being_deprecated()
            {
                const string expected = "Installing the same package with multiple versions is deprecated and will be removed in v2.0.0.";

                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains(expected))
                    {
                        return;
                    }
                }

                Assert.Fail("No warning message about side by side deprecation outputted");
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

        [Categories.SideBySide]
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
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames) + ".1.0.0";

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_put_version_in_nupkg_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.0.0"),
                    (Configuration.PackageNames + ".1.0.0" + NuGetConstants.PackageExtension));

                FileAssert.Exists(packageFile);
            }

            [Fact, Pending("Should be removed together with side by side installation"), Broken]
            public void should_put_version_in_nuspec_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.0.0"),
                    (Configuration.PackageNames + ".1.0.0" + NuGetConstants.ManifestExtension));

                FileAssert.Exists(packageFile);
            }

            [Fact]
            public void should_not_have_nupkg_without_version_in_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.0.0"),
                    (Configuration.PackageNames + NuGetConstants.PackageExtension));

                FileAssert.DoesNotExist(packageFile);
            }

            [Fact, Pending("Should be removed together with side by side installation"), Broken]
            public void should_not_have_nuspec_without_version_in_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.0.0"),
                    (Configuration.PackageNames + NuGetConstants.ManifestExtension));

                FileAssert.DoesNotExist(packageFile);
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

        [Categories.SideBySide]
        public class when_installing_an_older_version_side_by_side_with_a_newer_version : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "isdependency", "2.0.0");
                Configuration.AllowMultipleVersions = true;
                Configuration.Version = "1.1.0";
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames) + ".1.1.0";

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_put_version_in_nupkg_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.1.0"),
                    (Configuration.PackageNames + ".1.1.0" + NuGetConstants.PackageExtension));

                FileAssert.Exists(packageFile);
            }

            [Fact, Pending("Should be removed together with side by side installation"), Broken]
            public void should_put_version_in_nuspec_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.1.0"),
                    (Configuration.PackageNames + ".1.1.0" + NuGetConstants.ManifestExtension));

                FileAssert.Exists(packageFile);
            }

            [Fact]
            public void should_not_have_nupkg_without_version_in_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.1.0"),
                    (Configuration.PackageNames + NuGetConstants.PackageExtension));

                FileAssert.DoesNotExist(packageFile);
            }

            [Fact, Pending("Should be removed together with side by side installation"), Broken]
            public void should_not_have_nuspec_without_version_in_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    (Configuration.PackageNames + ".1.1.0"),
                    (Configuration.PackageNames + NuGetConstants.ManifestExtension));

                FileAssert.DoesNotExist(packageFile);
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
            public void should_have_a_version_of_one_dot_one_dot_zero()
            {
                packageResult.Version.ShouldEqual("1.1.0");
            }
        }

        [Categories.SideBySide]
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
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_remove_version_directory_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames + ".1.0.0");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void should_not_put_version_in_nupkg_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    Configuration.PackageNames,
                    (Configuration.PackageNames + NuGetConstants.PackageExtension));

                FileAssert.Exists(packageFile);
            }

            [Fact]
            public void should_not_put_version_in_nuspec_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    Configuration.PackageNames,
                    (Configuration.PackageNames + NuGetConstants.ManifestExtension));

                FileAssert.Exists(packageFile);
            }

            [Fact]
            public void should_not_have_nupkg_with_version_in_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    Configuration.PackageNames,
                    (Configuration.PackageNames + ".1.0.0" + NuGetConstants.PackageExtension));

                FileAssert.DoesNotExist(packageFile);
            }

            [Fact]
            public void should_not_have_nuspec_with_version_in_filename()
            {
                var packageFile = Path.Combine(
                    Scenario.get_top_level(), "lib",
                    Configuration.PackageNames,
                    (Configuration.PackageNames + ".1.0.0" + NuGetConstants.ManifestExtension));

                FileAssert.DoesNotExist(packageFile);
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

        public class when_installing_a_package_with_dependencies_happy : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
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
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
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

        public class when_force_installing_an_already_installed_package_with_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "isdependency*" + NuGetConstants.PackageExtension);
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
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_still_have_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_not_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
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

        public class when_force_installing_an_already_installed_package_forcing_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "isdependency*" + NuGetConstants.PackageExtension);
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
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_reinstall_the_floating_dependency_with_the_latest_version_that_satisfies_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_exact_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
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

        public class when_force_installing_an_already_installed_package_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "isdependency*" + NuGetConstants.PackageExtension);
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
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_not_touch_the_floating_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_not_touch_the_exact_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
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

        public class when_force_installing_an_already_installed_package_forcing_and_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
                Scenario.add_packages_to_source_location(Configuration, "isdependency*" + NuGetConstants.PackageExtension);
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
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_remove_the_floating_dependency()
            {
                var dependency = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");
                DirectoryAssert.DoesNotExist(dependency);
            }

            [Fact]
            public void should_remove_the_exact_dependency()
            {
                var dependency = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency");
                DirectoryAssert.DoesNotExist(dependency);
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

        public class when_installing_a_package_with_dependencies_and_dependency_cannot_be_found : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void should_not_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                DirectoryAssert.DoesNotExist(packageDir);
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

        public class when_installing_a_package_ignoring_dependencies_that_cannot_be_found : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency*" + NuGetConstants.PackageExtension);
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
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("2.1.0");
                }
            }

            [Fact]
            public void should_not_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                DirectoryAssert.DoesNotExist(packageDir);
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

        public class when_installing_a_package_that_depends_on_a_newer_version_of_an_installed_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.6.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.1.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
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
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.6.0");
                }
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.1.0");
                }
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

        public class when_installing_a_package_that_depends_on_an_unavailable_newer_version_of_an_installed_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.6.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
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

                DirectoryAssert.DoesNotExist(packageDir);
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

        public class when_installing_a_package_that_depends_on_an_unavailable_newer_version_of_an_installed_dependency_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.6.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
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
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.6.0");
                }
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

        public class when_force_installing_a_package_that_depends_on_an_unavailable_newer_version_of_an_installed_dependency_forcing_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.6.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
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

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void should_not_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
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

        public class when_installing_a_package_with_dependencies_on_a_newer_version_of_a_package_than_an_existing_package_has_with_that_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "conflictingdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "conflictingdependency.1.0.1*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.1.0.*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
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
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.1");
                }
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

        public class when_installing_a_package_with_dependencies_on_a_newer_version_of_a_package_than_are_allowed_by_an_existing_package_with_that_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "conflictingdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "conflictingdependency.2.1.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "isdependency", "1.0.0");
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_not_install_the_conflicting_package()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.DoesNotExist(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_not_install_the_conflicting_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void should_not_upgrade_the_minimum_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_not_upgrade_the_exact_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_contain_a_message_that_it_was_unable_to_install_any_packages()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("installed 0/1")) expectedMessage = true;
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
        }

        public class when_installing_a_package_with_dependencies_on_an_older_version_of_a_package_than_is_already_installed : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.add_packages_to_source_location(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "conflictingdependency.2.1.0*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
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
            public void should_not_install_the_conflicting_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void should_not_downgrade_the_exact_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("2.0.0");
                }
            }

            [Fact]
            public void should_contain_a_message_that_it_was_unable_to_install_any_packages()
            {
                bool expectedMessage = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("installed 0/1")) expectedMessage = true;
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
        }

        public class when_installing_a_package_with_a_dependent_package_that_also_depends_on_a_less_constrained_but_still_valid_dependency_of_the_same_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "toplevelhasexactversiondependency";
                Scenario.add_packages_to_source_location(Configuration, "toplevelhasexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "childdependencywithlooserversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
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
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "childdependencywithlooserversiondependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "childdependencywithlooserversiondependency", "childdependencywithlooserversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_install_the_expected_version_of_the_constrained_dependency()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
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
        }

        public class when_installing_a_package_from_a_nupkg_file : ScenariosBase
        {
            private PackageResult packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "{0}{1}installpackage.1.0.0.nupkg".format_with(Configuration.Sources, Path.DirectorySeparatorChar);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", "installpackage");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", "installpackage", "installpackage" + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            public void should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "not.installed.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "casemismatch.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.get_top_level(), "extensions", "installpackage");

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.get_top_level(), "hooks", "installpackage");

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
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
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
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

        public class when_installing_a_package_with_config_transforms : ScenariosBase
        {
            private PackageResult packageResult;
            private string _xmlFilePath = string.Empty;
            private XPathNavigator _xPathNavigator;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Scenario.add_packages_to_source_location(Configuration, "upgradepackage.1.0.0*" + NuGetConstants.PackageExtension);

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
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
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

        public class when_installing_a_hook_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "scriptpackage.hook";
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + ".1.0.0" + NuGetConstants.PackageExtension);
            }

            private PackageResult _packageResult;

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.get_top_level(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void should_create_a_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.get_top_level(), "hooks", Configuration.PackageNames.Replace(".hook", string.Empty));

                DirectoryAssert.Exists(hooksDirectory);
            }

            [Fact]
            public void should_install_hook_scripts_to_folder()
            {
                var hookScripts = new List<string> { "pre-install-all.ps1", "post-install-all.ps1", "pre-upgrade-all.ps1", "post-upgrade-all.ps1", "pre-uninstall-all.ps1", "post-uninstall-all.ps1" };
                foreach (string scriptName in hookScripts)
                {
                    var hookScriptPath = Path.Combine(Scenario.get_top_level(), "hooks", Configuration.PackageNames.Replace(".hook", string.Empty), scriptName);
                    File.ReadAllText(hookScriptPath).ShouldContain("Write-Output");
                }
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

            [Fact]
            public void config_should_match_package_result_name()
            {
                _packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.ShouldEqual("1.0.0");
            }

        }

        public class when_installing_a_package_happy_path_with_hook_scripts : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.add_packages_to_source_location(Configuration, "scriptpackage.hook" + "*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "scriptpackage.hook", "1.0.0");
                Configuration.PackageNames = Configuration.Input = "installpackage";
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            public void should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "not.installed.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "casemismatch.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.get_top_level(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.get_top_level(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
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
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
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

            [Fact]
            public void config_should_match_package_result_name()
            {
                _packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.ShouldEqual("1.0.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_have_executed_chocolateyInstall_script()
            {
                MockLogger.contains_message("installpackage v1.0.0 has been installed", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_have_executed_pre_all_hook_script()
            {
                MockLogger.contains_message("pre-install-all.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_have_executed_post_all_hook_script()
            {
                MockLogger.contains_message("post-install-all.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_have_executed_pre_installpackage_hook_script()
            {
                MockLogger.contains_message("pre-install-installpackage.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_have_executed_post_installpackage_hook_script()
            {
                MockLogger.contains_message("post-install-installpackage.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_not_have_executed_uninstall_hook_script()
            {
                MockLogger.contains_message("post-uninstall-all.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeFalse();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_not_have_executed_upgradepackage_hook_script()
            {
                MockLogger.contains_message("pre-install-upgradepackage.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeFalse();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_not_have_executed_beforemodify_hook_script()
            {
                MockLogger.contains_message("pre-beforemodify-all.ps1 hook ran for installpackage 1.0.0", LogLevel.Info).ShouldBeFalse();
            }
        }

        public class when_installing_a_portable_package_happy_path_with_hook_scripts : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.add_packages_to_source_location(Configuration, "scriptpackage.hook" + ".1.0.0" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "scriptpackage.hook", "1.0.0");
                Configuration.PackageNames = Configuration.Input = "portablepackage";
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.install_run(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().to_string().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "console.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "graphical.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            public void should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "not.installed.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.get_top_level(), "bin", "casemismatch.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.get_top_level(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.get_top_level(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
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
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
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

            [Fact]
            public void config_should_match_package_result_name()
            {
                _packageResult.Name.ShouldEqual(Configuration.PackageNames);
            }

            [Fact]
            public void should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.ShouldEqual("1.0.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_not_have_executed_chocolateyInstall_script()
            {
                MockLogger.contains_message("portablepackage v1.0.0 has been installed", LogLevel.Info).ShouldBeFalse();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_have_executed_pre_all_hook_script()
            {
                MockLogger.contains_message("pre-install-all.ps1 hook ran for portablepackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_have_executed_post_all_hook_script()
            {
                MockLogger.contains_message("post-install-all.ps1 hook ran for portablepackage 1.0.0", LogLevel.Info).ShouldBeTrue();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_not_have_executed_uninstall_hook_script()
            {
                MockLogger.contains_message("post-uninstall-all.ps1 hook ran for portablepackage 1.0.0", LogLevel.Info).ShouldBeFalse();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_not_have_executed_upgradepackage_hook_script()
            {
                MockLogger.contains_message("pre-install-upgradepackage.ps1 hook ran for portablepackage 1.0.0", LogLevel.Info).ShouldBeFalse();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_not_have_executed_beforemodify_hook_script()
            {
                MockLogger.contains_message("pre-beforemodify-all.ps1 hook ran for portablepackage 1.0.0", LogLevel.Info).ShouldBeFalse();
            }
        }

        [Categories.SourcePriority]
        public class when_installing_package_from_lower_priority_source_with_version_specified : ScenariosBase
        {
            private PackageResult packageResult;
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Configuration.Version = "2.0.0";
                Configuration.Sources = string.Join(",",
                    Scenario.add_packages_to_priority_source_location(Configuration, "isdependency.1.1.0" + NuGetConstants.PackageExtension, priority: 1),
                    Scenario.add_packages_to_priority_source_location(Configuration, "isdependency.2.0.0" + NuGetConstants.PackageExtension, name: "No-Priority"));
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.install_run(Configuration);
                packageResult = Results.Select(r => r.Value).FirstOrDefault();
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(packageResult.InstallLocation);
            }

            [Fact]
            public void should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_the_expected_version_of_the_package()
            {
                var packageDirectory = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                using (var reader = new PackageFolderReader(packageDirectory))
                {
                    reader.NuspecReader.GetVersion().ToNormalizedString().ShouldEqual("2.0.0");
                }
            }

            [Fact]
            public void should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.get_top_level(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.get_top_level(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
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
            public void should_have_a_version_of_two_dot_zero_dot_zero()
            {
                packageResult.Version.ShouldEqual("2.0.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void should_have_reported_package_installed()
            {
                MockLogger.contains_message("isdependency 2.0.0 Installed", LogLevel.Info).ShouldBeTrue();
            }
        }

        [Categories.SourcePriority]
        public class when_installing_non_existing_package_from_priority_source : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "non-existing";
                Scenario.add_machine_source(Configuration, "Priority-Source", priority: 1);
                Configuration.Sources = "Priority-Source";
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_not_report_success()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_install_a_pakcage_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
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
            public void should_not_have_warning_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_report_package_not_found()
            {
                foreach (var packageResult in Results)
                {
                    var message = packageResult.Value.Messages.First();
                    message.MessageType.ShouldEqual(ResultType.Error);
                    message.Message.ShouldStartWith("non-existing not installed. The package was not found with the source(s) listed.");
                }
            }
        }

        [Categories.SourcePriority]
        public class when_installing_new_package_from_priority_source_with_repository_optimization : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Configuration.Features.UsePackageRepositoryOptimizations = true;
                Scenario.add_machine_source(Configuration, "chocolatey", path: "https://community.chocolatey.org/api/v2/", createDirectory: false);

                Configuration.Sources = string.Join(";", new[]
                {
                    Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension),
                    Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1)
                });
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_install_lower_version_of_package()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Version.ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_have_installed_expected_version_in_lib_directory()
            {
                var installedPath = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                var packageFolder = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);

                using (var reader = new PackageFolderReader(packageFolder))
                {
                    reader.NuspecReader.GetVersion().ToNormalizedString().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_have_success_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }
        }

        [Categories.SourcePriority]
        public class when_installing_new_package_from_priority_source : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "upgradepackage";

                Configuration.Sources = string.Join(";", new[]
                {
                    Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension),
                    Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1)
                });
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames);
                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void should_have_installed_expected_version_in_lib_directory()
            {
                var packageFolder = Path.Combine(Scenario.get_top_level(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);

                using (var reader = new PackageFolderReader(packageFolder))
                {
                    reader.NuspecReader.GetVersion().ToNormalizedString().ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_install_lower_version_of_package()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Version.ShouldEqual("1.0.0");
                }
            }

            [Fact]
            public void should_not_have_inconclusive_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_have_success_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }
        }

        [Categories.SourcePriority]
        public class when_installing_package_with_dependencies_on_different_priority_sources : ScenariosBase
        {
            public static IEnumerable ExpectedInstallations
            {
                get
                {
                    yield return "hasdependency";
                    yield return "isdependency";
                    yield return "isexactversiondependency";
                }
            }

            public static IEnumerable ExpectedPackageVersions
            {
                get
                {
                    yield return new object[] { "hasdependency", "1.6.0" };
                    yield return new object[] { "isdependency", "2.1.0" };
                    yield return new object[] { "isexactversiondependency", "1.1.0" };
                }
            }

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";

                Configuration.Sources = string.Join(";", new[]
                {
                    Scenario.add_packages_to_priority_source_location(Configuration, "hasdependency.1.6.0" + NuGetConstants.PackageExtension, priority: 1),
                    Scenario.add_packages_to_priority_source_location(Configuration, "isdependency.*" + NuGetConstants.PackageExtension, priority: 2),
                    Scenario.add_packages_to_priority_source_location(Configuration, "isexactversiondependency.1.1.0" + NuGetConstants.PackageExtension)
                });
                Scenario.add_packages_to_priority_source_location(Configuration, "isexactversiondependency.2.0.0" + NuGetConstants.PackageExtension, priority: 1);
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.install_run(Configuration);
            }

            [Fact]
            public void should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [TestCaseSource(nameof(ExpectedInstallations))]
            public void should_install_hasdependency_package_to_lib_directory(string name)
            {
                var expectedPath = Path.Combine(Scenario.get_top_level(), "lib", name);
                DirectoryAssert.Exists(expectedPath);
            }

            [TestCaseSource(nameof(ExpectedPackageVersions))]
            public void should_instal_expected_package_version(string name, string version)
            {
                var path = Path.Combine(Scenario.get_top_level(), "lib", name);

                using (var reader = new PackageFolderReader(path))
                {
                    reader.NuspecReader.GetVersion().ToNormalizedString().ShouldEqual(version);
                }
            }

            [TestCaseSource(nameof(ExpectedPackageVersions))]
            public void should_report_installed_version_of_package(string name, string version)
            {
                var package = Results.First(r => r.Key == name);
                package.Value.Version.ShouldEqual(version);
            }

            [Fact]
            public void should_not_have_inconclusive_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Inconclusive.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_not_have_warning_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Warning.ShouldBeFalse();
                }
            }

            [Fact]
            public void should_have_success_package_results()
            {
                foreach (var packageResult in Results)
                {
                    packageResult.Value.Success.ShouldBeTrue();
                }
            }
        }
    }
}
