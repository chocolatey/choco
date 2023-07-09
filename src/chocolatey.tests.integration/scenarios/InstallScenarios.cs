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
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml.XPath;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.results;
    using NuGet.Configuration;
    using NuGet.Packaging;
    using NUnit.Framework;
    using FluentAssertions;
    using FluentAssertions.Execution;
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
                Configuration = Scenario.Install();
                Scenario.Reset(Configuration);
                Configuration.PackageNames = Configuration.Input = "installpackage";
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "badpackage.1*" + NuGetConstants.PackageExtension);

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                CommandExecutor = new CommandExecutor(NUnitSetup.Container.GetInstance<IFileSystem>());
            }
        }

        public class When_noop_installing_a_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Noop = true;
            }

            public override void Because()
            {
                Service.InstallDryRun(Configuration);
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_would_have_used_Nuget_to_install_a_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().Contain(m => m.Contains("would have used NuGet to install packages"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_contain_a_message_that_it_would_have_run_a_powershell_script()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("chocolateyinstall.ps1"));
            }

            [Fact]
            public void Should_not_contain_a_message_that_it_would_have_run_powershell_modification_script()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("chocolateyBeforeModify.ps1"));
            }
        }

        public class When_noop_installing_a_package_that_does_not_exist : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "somethingnonexisting";
                Configuration.Noop = true;
            }

            public override void Because()
            {
                Service.InstallDryRun(Configuration);
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_would_have_used_Nuget_to_install_a_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().Contain(m => m.Contains("would have used NuGet to install packages"));

            }

            [Fact]
            public void Should_contain_a_message_that_it_was_unable_to_find_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToString())
                    .WhoseValue.Should().Contain(m => m.Contains("somethingnonexisting not installed. The package was not found with the source(s) listed"));
            }
        }

        public class When_installing_a_package_happy_path : ScenariosBase
        {
            private PackageResult _packageResult;

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
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be(TestVersion());
                }
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            public void Should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "not.installed.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "casemismatch.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.GetTopLevel(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void Should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_a_console_shim_that_is_set_for_non_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");
                CommandExecutor.Execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                messages.Should()
                    .NotBeNullOrEmpty()
                    .And.Contain(m => m.Contains("is gui? False"), "GUI false message not found");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_a_graphical_shim_that_is_set_for_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");
                CommandExecutor.Execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                messages.Should()
                    .NotBeNullOrEmpty()
                    .And.Contain(m => m.Contains("is gui? True"), "GUI true message not found");
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Warn.ToString())
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
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be(TestVersion());
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script()
            {
                var message = "installpackage v{0} has been installed".FormatWith(TestVersion());

                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should().Contain(m => m.Contains(message));
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
        public class When_installing_a_package_with_semver_2_0_meta_data : When_installing_a_package_happy_path
        {
            protected override string TestSemVersion => "0.9.9+build.543";
        }

        [Categories.SemVer20]
        public class When_installing_a_package_with_semver_2_0_pre_release_tag : When_installing_a_package_happy_path
        {
            protected override string TestSemVersion => "1.0.0-alpha.34";
        }

        public class When_installing_packages_with_packages_config : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                var packagesConfig = "{0}{1}context{1}testing.packages.config".FormatWith(Scenario.GetTopLevel(), Path.DirectorySeparatorChar);
                Configuration.PackageNames = Configuration.Input = packagesConfig;
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "upgradepackage*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    if (packageResult.Value.Name.IsEqualTo("missingpackage")) continue;

                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_expected_packages_in_the_lib_directory()
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
                    var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", package);
                    DirectoryAssert.Exists(packageDir);
                }
            }

            [Fact]
            public void Should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_4_out_of_5_packages_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("5/6"));
            }

            [Fact]
            public void Should_contain_a_message_that_upgradepackage_with_an_expected_specified_version_was_installed()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage v1.0.0"));
            }

            [Fact]
            public void Should_have_a_successful_package_result_for_all_but_expected_missing_package()
            {
                Results.Where(r => !r.Value.Name.IsEqualTo("missingpackage"))
                    .Should().AllSatisfy(p => p.Value.Success.Should().BeTrue());
            }

            [Fact]
            public void Should_not_have_a_successful_package_result_for_missing_package()
            {
                Results.Should().Contain(r => r.Value.Name.IsEqualTo("missingpackage"))
                    .Which.Value.Success.Should().BeFalse();
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

            [Fact]
            public void Should_specify_config_file_is_being_used_in_message()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Installing from config file:"));
            }

            [Fact]
            public void Should_print_out_package_from_config_file_in_message()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage"));
            }
        }

        public class When_installing_an_already_installed_package : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_still_have_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_still_have_the_expected_version_of_the_package_installed()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_was_unable_to_install_any_packages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("0/1"));
            }

            [Fact]
            public void Should_contain_a_message_about_force_to_reinstall()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Use --force to reinstall"));
            }

            [Fact]
            public void Should_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeTrue();
            }

            [Fact]
            public void Should_ave_warning_package_result()
            {
                _packageResult.Warning.Should().BeTrue();
            }
        }

        public class When_force_installing_an_already_installed_package : ScenariosBase
        {
            private PackageResult _packageResult;
            private readonly string _modifiedText = "bob";

            public override void Context()
            {
                base.Context();
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");
                var fileToModify = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");
                File.WriteAllText(fileToModify, _modifiedText);

                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_remove_and_re_add_the_package_files_in_the_lib_directory()
            {
                var modifiedFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");
                File.ReadAllText(modifiedFile).Should().NotBe(_modifiedText);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
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
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_force_installing_an_already_installed_package_that_errors : ScenariosBase
        {
            private PackageResult _packageResult;
            private readonly string _modifiedText = "bob";

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "badpackage";
                Configuration.SkipPackageInstallProvider = true;
                Scenario.InstallPackage(Configuration, "badpackage", "1.0");
                Configuration.SkipPackageInstallProvider = false;
                Configuration.Force = true;

                var fileToModify = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                File.WriteAllText(fileToModify, _modifiedText);
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_restore_the_backup_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedString().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_restore_the_original_files_in_the_package_lib_folder()
            {
                var modifiedFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyInstall.ps1");
                File.ReadAllText(modifiedFile).Should().Be(_modifiedText);
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_was_unsuccessful()
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
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_force_installing_an_already_installed_package_with_a_read_and_delete_share_locked_file : ScenariosBase
        {
            private PackageResult _packageResult;
            private FileStream _fileStream;

            public override void Context()
            {
                base.Context();
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");
                Configuration.Force = true;
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
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_reinstall_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_reinstall_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            [Pending("Does not work under .Net 4.8, See issue #2690")]
            [Broken]
            public void Should_not_be_able_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_installed_successfully()
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
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_force_installing_an_already_installed_package_with_with_an_exclusively_locked_file : ScenariosBase
        {
            private PackageResult _packageResult;
            private FileStream _fileStream;

            public override void Context()
            {
                base.Context();
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");
                Configuration.Force = true;
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
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_have_a_package_installed_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_still_have_the_package_installed_with_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_it_was_unable_to_reinstall_successfully()
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
            public void Should_have_inconclusive_package_result()
            {
                _packageResult.Inconclusive.Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeFalse();
            }
        }

        public class When_installing_a_package_that_exists_but_a_version_that_does_not_exist : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Version = "1.0.1";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_did_not_install_successfully()
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
                        m.Message.Contains("The package was not found")));
            }

            [Fact]
            public void Should_have_a_version_of_one_dot_zero_dot_one()
            {
                _packageResult.Version.Should().Be("1.0.1");
            }
        }

        public class When_installing_a_package_that_does_not_exist : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonexisting";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

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
                        m.Message.Contains("The package was not found")));
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_installing_a_package_that_errors : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "badpackage";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_put_a_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bad", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
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
                        m.Message.Contains("chocolateyInstall.ps1")));
            }
        }

        public class When_installing_a_package_that_has_nonterminating_errors : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonterminatingerror";
                Configuration.Features.FailOnStandardError = false; //the default

                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.Input);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.Input, Configuration.Input + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedString().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_it_installed_successfully()
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
                _packageResult.Name.Should().Be(Configuration.Input);
            }

            [Fact]
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_installing_a_package_that_has_nonterminating_errors_with_fail_on_stderr : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonterminatingerror";
                Configuration.Features.FailOnStandardError = true;

                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_put_a_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bad", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
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
                        m.Message.Contains("chocolateyInstall.ps1")));
            }
        }

        public class When_installing_a_package_with_dependencies_happy : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_everything_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("3/3"));
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

            [Fact]
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                Results.Should().AllSatisfy(r => r.Value.Version.Should().Be("1.0.0"));
            }
        }

        public class When_force_installing_an_already_installed_package_with_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency*" + NuGetConstants.PackageExtension);
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_still_have_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_not_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_it_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
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

            [Fact]
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                Results.Should().AllSatisfy(r => r.Value.Version.Should().Be("1.0.0"));
            }
        }

        public class When_force_installing_an_already_installed_package_forcing_dependencies : ScenariosBase
        {
            private IEnumerable<string> _installedPackagePaths;
            public override void Context()
            {
                base.Context();

                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency*" + NuGetConstants.PackageExtension);
                _installedPackagePaths = Scenario.GetInstalledPackagePaths().ToList();

                Configuration.Force = true;
                Configuration.ForceDependencies = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_not_remove_any_existing_packages_in_the_lib_directory()
            {
                foreach (var packagePath in _installedPackagePaths)
                {
                    FileAssert.Exists(packagePath);
                }
            }

            [Fact]
            public void Should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_reinstall_the_floating_dependency_with_the_latest_version_that_satisfies_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_reinstall_the_exact_same_version_of_the_exact_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("3/3"));
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

        public class When_force_installing_an_already_installed_package_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency*" + NuGetConstants.PackageExtension);
                Configuration.Force = true;
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_not_touch_the_floating_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_not_touch_the_exact_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
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

        public class When_force_installing_an_already_installed_package_forcing_and_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency*" + NuGetConstants.PackageExtension);
                Configuration.Force = true;
                Configuration.ForceDependencies = true;
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_reinstall_the_exact_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_remove_the_floating_dependency()
            {
                var dependency = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency");
                DirectoryAssert.DoesNotExist(dependency);
            }

            [Fact]
            public void Should_remove_the_exact_dependency()
            {
                var dependency = Path.Combine(Scenario.GetTopLevel(), "lib", "isexactversiondependency");
                DirectoryAssert.DoesNotExist(dependency);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
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

        public class When_installing_a_package_with_dependencies_and_dependency_cannot_be_found : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_was_unable_to_install_any_packages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("0/1"));
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeFalse());
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

            [Fact]
            public void Should_have_an_error_package_result()
            {
                Results.Should().AllSatisfy(r =>
                    r.Value.Messages.Should().Contain(m => m.MessageType == ResultType.Error));
            }

            [Fact]
            public void Should_have_expected_error_in_package_result()
            {
                Results.Should().AllSatisfy(r =>
                    r.Value.Messages.Should().Contain(m =>
                        m.MessageType == ResultType.Error &&
                        m.Message.Contains("Unable to resolve dependency 'isdependency")));
            }
        }

        public class When_installing_a_package_ignoring_dependencies_that_cannot_be_found : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency*" + NuGetConstants.PackageExtension);
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.1.0");
                }
            }

            [Fact]
            public void Should_not_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
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

        public class When_installing_a_package_that_depends_on_a_newer_version_of_an_installed_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.6.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.1.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.6.0");
                }
            }

            [Fact]
            public void Should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("3/3"));
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

        public class When_installing_a_package_that_depends_on_an_unavailable_newer_version_of_an_installed_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.6.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_that_is_was_unable_to_install_any_packages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("0/1"));
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeFalse());
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

        public class When_installing_a_package_that_depends_on_an_unavailable_newer_version_of_an_installed_dependency_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.6.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.6.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_it_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
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

        public class When_force_installing_a_package_that_depends_on_an_unavailable_newer_version_of_an_installed_dependency_forcing_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.6.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Configuration.Force = true;
                Configuration.ForceDependencies = true;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_was_unable_to_install_any_packages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("0/1"));
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeFalse());
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

            [Fact]
            public void Should_have_an_error_package_result()
            {
                Results.Should().AllSatisfy(r =>
                    r.Value.Messages.Should().Contain(m => m.MessageType == ResultType.Error));
            }

            [Fact]
            public void Should_have_expected_error_in_package_result()
            {
                Results.Should().AllSatisfy(r =>
                    r.Value.Messages.Should().Contain(m =>
                        m.MessageType == ResultType.Error &&
                        m.Message.Contains("Unable to resolve dependency 'isdependency")));
            }
        }

        public class When_installing_a_package_with_dependencies_on_a_newer_version_of_a_package_than_an_existing_package_has_with_that_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "conflictingdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "conflictingdependency.1.0.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.0.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_upgrade_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.1");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_it_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installed 2/2"));
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

        public class When_installing_a_package_with_dependencies_on_a_newer_version_of_a_package_than_are_allowed_by_an_existing_package_with_that_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "conflictingdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "conflictingdependency.2.1.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_not_install_the_conflicting_package()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.DoesNotExist(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_not_install_the_conflicting_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_upgrade_the_minimum_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_not_upgrade_the_exact_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_it_was_unable_to_install_any_packages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installed 0/1"));
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeFalse());
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

            [Fact]
            public void Should_have_an_error_package_result()
            {
                Results.Should().AllSatisfy(r =>
                    r.Value.Messages.Should().Contain(m => m.MessageType == ResultType.Error));
            }
        }

        public class When_installing_a_package_with_dependencies_on_an_older_version_of_a_package_than_is_already_installed : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.1.0.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "conflictingdependency.2.1.0*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "conflictingdependency", "2.1.0");
            }

            /*
             Setup should have the following installed:
             * conflictingdependency 2.1.0
             * isexactversiondependency 2.0.0
             * isdependency at least 2.0.0
             */

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_not_install_the_conflicting_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_downgrade_the_exact_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_it_was_unable_to_install_any_packages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installed 0/1"));
            }

            [Fact]
            public void Should_not_have_a_successful_package_result()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeFalse());
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

            [Fact]
            public void Should_have_an_error_package_result()
            {
                Results.Should().AllSatisfy(r =>
                    r.Value.Messages.Should().Contain(m => m.MessageType == ResultType.Error));
            }
        }

        public class When_installing_a_package_with_a_dependent_package_that_also_depends_on_a_less_constrained_but_still_valid_dependency_of_the_same_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "toplevelhasexactversiondependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "toplevelhasexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "childdependencywithlooserversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            /*
             Because should result in the following installed:
             * toplevelhasexactversiondependency 1.0.0
             * childdependencywithlooserversiondependency 1.0.0
             * isexactversiondependency 1.0.0
             */

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_dependency_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "childdependencywithlooserversiondependency");

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "childdependencywithlooserversiondependency", "childdependencywithlooserversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_constrained_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_everything_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("3/3"));
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

        public class When_installing_a_package_from_a_nupkg_file : ScenariosBase
        {
            private Exception _exception;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "{0}{1}installpackage.1.0.0.nupkg".FormatWith(Configuration.Sources, Path.DirectorySeparatorChar);
            }

            public override void Because()
            {
                try
                {
                    Results = Service.Install(Configuration);
                }
                catch (Exception ex)
                {
                    _exception = ex;
                }
            }

            [Fact]
            public void Should_have_thrown_exception_when_installing()
            {
                _exception.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_have_outputted_expected_exception_message()
            {
                // We use a string builder here to ensure that the same line endings are used.
                var expectedMessage = new StringBuilder("Package name cannot be a path to a file on a remote, or local file system.")
                    .AppendLine()
                    .AppendLine()
                    .AppendLine("To install a local, or remote file, you may use:")
                    .AppendLine("  choco install installpackage --version=\"1.0.0\" --source=\"{0}\"".FormatWith(Configuration.Sources))
                    .ToString();

                _exception.Message.Should().Be(expectedMessage);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bad", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_backup_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }
        }

        public class When_installing_a_package_from_a_prerelease_nupkg_file : ScenariosBase
        {
            private Exception _exception;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "{0}{1}installpackage.0.56-alpha-0544.nupkg".FormatWith(Configuration.Sources, Path.DirectorySeparatorChar);
            }

            public override void Because()
            {
                try
                {
                    Results = Service.Install(Configuration);
                }
                catch (Exception ex)
                {
                    _exception = ex;
                }
            }

            [Fact]
            public void Should_have_thrown_exception_when_installing()
            {
                _exception.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_have_outputted_expected_exception_message()
            {
                // We use a string builder here to ensure that the same line endings are used.
                var expectedMessage = new StringBuilder("Package name cannot be a path to a file on a remote, or local file system.")
                    .AppendLine()
                    .AppendLine()
                    .AppendLine("To install a local, or remote file, you may use:")
                    .AppendLine("  choco install installpackage --version=\"0.56.0-alpha-0544\" --prerelease --source=\"{0}\"".FormatWith(Configuration.Sources))
                    .ToString();

                _exception.Message.Should().Be(expectedMessage);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bad", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_backup_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }
        }

        [Categories.Unc]
        public class When_installing_a_package_from_a_nupkg_file_and_unc_path : ScenariosBase
        {
            private Exception _exception;

            public override void Context()
            {
                base.Context();
                Configuration.Sources = UNCHelper.ConvertLocalFolderPathToIpBasedUncPath(Configuration.Sources);

                Configuration.PackageNames = Configuration.Input = "{0}{1}installpackage.1.0.0.nupkg".FormatWith(Configuration.Sources, Path.DirectorySeparatorChar);
            }

            public override void Because()
            {
                try
                {
                    Results = Service.Install(Configuration);
                }
                catch (Exception ex)
                {
                    _exception = ex;
                }
            }

            [Fact]
            public void Should_have_thrown_exception_when_installing()
            {
                _exception.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_have_outputted_expected_exception_message()
            {
                // We use a string builder here to ensure that the same line endings are used.
                var expectedMessage = new StringBuilder("Package name cannot be a path to a file on a UNC location.")
                    .AppendLine()
                    .AppendLine()
                    .AppendLine("To install a file in a UNC location, you may use:")
                    .AppendLine("  choco install installpackage --version=\"1.0.0\" --source=\"{0}\"".FormatWith(Configuration.Sources))
                    .ToString();

                _exception.Message.Should().Be(expectedMessage);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bad", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_backup_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }
        }

        public class When_installing_a_package_from_a_remote_nupkg_file : ScenariosBase
        {
            private Exception _exception;

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "https://testing.com/raw/installpackage.1.0.0.nupkg";
            }

            public override void Because()
            {
                try
                {
                    Results = Service.Install(Configuration);
                }
                catch (Exception ex)
                {
                    _exception = ex;
                }
            }

            [Fact]
            public void Should_have_thrown_exception_when_installing()
            {
                _exception.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_have_outputted_expected_exception_message()
            {
                _exception.Message.Should().Be("Package name cannot point directly to a local, or remote file. Please use the --source argument and point it to a local file directory, UNC directory path or a NuGet feed instead.");
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bad", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_backup_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }
        }

        public class When_installing_a_package_from_a_manifest_file : ScenariosBase
        {
            private Exception _exception;

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = Configuration.Input = "{0}{1}installpackage.nuspec".FormatWith(Configuration.Sources, Path.DirectorySeparatorChar);
            }

            public override void Because()
            {
                try
                {
                    Results = Service.Install(Configuration);
                }
                catch (Exception ex)
                {
                    _exception = ex;
                }
            }

            [Fact]
            public void Should_have_thrown_exception_when_installing()
            {
                _exception.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_have_outputted_expected_exception_message()
            {
                _exception.Message.Should().Be("Package name cannot point directly to a package manifest file. Please create a package by running 'choco pack' on the .nuspec file first.");
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bad", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_install_the_package_in_the_lib_backup_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", "installpackage");

                DirectoryAssert.DoesNotExist(packageDir);
            }
        }

        public class When_installing_a_package_with_config_transforms : ScenariosBase
        {
            private PackageResult _packageResult;
            private string _xmlFilePath = string.Empty;
            private XPathNavigator _xPathNavigator;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Scenario.AddPackagesToSourceLocation(Configuration, "upgradepackage.1.0.0*" + NuGetConstants.PackageExtension);

                _xmlFilePath = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe.config");
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
                var xmlDocument = new XPathDocument(_xmlFilePath);
                _xPathNavigator = xmlDocument.CreateNavigator();
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
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
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }

            [Fact]
            public void Should_not_change_the_test_value_in_the_config_due_to_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='test']/@value").TypedValue.ToStringSafe().Should().Be("default 1.0.0");
            }

            [Fact]
            public void Should_change_the_testReplace_value_in_the_config_due_to_XDT_Replace()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='testReplace']/@value").TypedValue.ToStringSafe().Should().Be("1.0.0");
            }

            [Fact]
            public void Should_add_the_insert_value_in_the_config_due_to_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='insert']/@value").TypedValue.ToStringSafe().Should().Be("1.0.0");
            }
        }

        public class When_installing_a_package_with_no_sources_enabled : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = null;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_have_no_sources_enabled_result()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Installation was NOT successful. There are no sources enabled for"));
            }

            [Fact]
            public void Should_not_install_any_packages()
            {
                Results.Should().BeEmpty();
            }
        }

        public class When_installing_a_hook_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "scriptpackage.hook";
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + ".1.0.0" + NuGetConstants.PackageExtension);
            }

            private PackageResult _packageResult;

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.GetTopLevel(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void Should_create_a_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames.Replace(".hook", string.Empty));

                DirectoryAssert.Exists(hooksDirectory);
            }

            [Fact]
            public void Should_install_hook_scripts_to_folder()
            {
                var hookScripts = new List<string> { "pre-install-all.ps1", "post-install-all.ps1", "pre-upgrade-all.ps1", "post-upgrade-all.ps1", "pre-uninstall-all.ps1", "post-uninstall-all.ps1" };
                foreach (string scriptName in hookScripts)
                {
                    var hookScriptPath = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames.Replace(".hook", string.Empty), scriptName);
                    File.ReadAllText(hookScriptPath).Should().Contain("Write-Output");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
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
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }

        }

        public class When_installing_a_package_happy_path_with_hook_scripts : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "scriptpackage.hook" + "*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "scriptpackage.hook", "1.0.0");
                Configuration.PackageNames = Configuration.Input = "installpackage";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            public void Should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "not.installed.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "casemismatch.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.GetTopLevel(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void Should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_a_console_shim_that_is_set_for_non_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");
                CommandExecutor.Execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                messages.Should()
                    .NotBeNullOrEmpty()
                    .And.Contain(m => m.Contains("is gui? False"), "GUI false message not found");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_a_graphical_shim_that_is_set_for_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");
                CommandExecutor.Execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                messages.Should()
                    .NotBeNullOrEmpty()
                    .And.Contain(m => m.Contains("is gui? True"), "GUI true message not found");
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
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
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage v1.0.0 has been installed"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_all_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-install-all.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_all_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-install-all.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_installpackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-install-installpackage.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_installpackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-install-installpackage.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_uninstall_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("post-uninstall-all.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_upgradepackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("pre-install-upgradepackage.ps1 hook ran for installpackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_beforemodify_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("pre-beforemodify-all.ps1 hook ran for installpackage 1.0.0"));
            }
        }

        public class When_installing_a_portable_package_happy_path_with_hook_scripts : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "scriptpackage.hook" + ".1.0.0" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "scriptpackage.hook", "1.0.0");
                Configuration.PackageNames = Configuration.Input = "portablepackage";
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            public void Should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "not.installed.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "casemismatch.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.GetTopLevel(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void Should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_a_console_shim_that_is_set_for_non_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");
                CommandExecutor.Execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                messages.Should()
                    .NotBeNullOrEmpty()
                    .And.Contain(m => m.Contains("is gui? False"), "GUI false message not found");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_a_graphical_shim_that_is_set_for_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");
                CommandExecutor.Execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                messages.Should()
                    .NotBeNullOrEmpty()
                    .And.Contain(m => m.Contains("is gui? True"), "GUI true message not found");
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
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
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_chocolateyInstall_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("portablepackage v1.0.0 has been installed"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_all_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-install-all.ps1 hook ran for portablepackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_all_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-install-all.ps1 hook ran for portablepackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_uninstall_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("post-uninstall-all.ps1 hook ran for portablepackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_upgradepackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("pre-install-upgradepackage.ps1 hook ran for portablepackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_beforemodify_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("pre-beforemodify-all.ps1 hook ran for portablepackage 1.0.0"));
            }
        }

        [Categories.SourcePriority]
        public class When_installing_package_from_lower_priority_source_with_version_specified : ScenariosBase
        {
            private PackageResult _packageResult;
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Configuration.Version = "2.0.0";
                Configuration.Sources = string.Join(",",
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "isdependency.1.1.0" + NuGetConstants.PackageExtension, priority: 1),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "isdependency.2.0.0" + NuGetConstants.PackageExtension, name: "No-Priority"));
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.Install(Configuration);
                _packageResult = Results.Select(r => r.Value).FirstOrDefault();
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageDirectory = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                using (var reader = new PackageFolderReader(packageDirectory))
                {
                    reader.NuspecReader.GetVersion().ToNormalizedString().Should().Be("2.0.0");
                }
            }

            [Fact]
            public void Should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.GetTopLevel(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void Should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
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
            public void Should_have_a_version_of_two_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("2.0.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_reported_package_installed()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("isdependency 2.0.0 Installed"));
            }
        }

        [Categories.SourcePriority]
        public class When_installing_non_existing_package_from_priority_source : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "non-existing";
                Scenario.AddMachineSource(Configuration, "Priority-Source", priority: 1);
                Configuration.Sources = "Priority-Source";
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_not_report_success()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeFalse());
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_have_inconclusive_package_result()
            {
                Results.Should().AllSatisfy(r => r.Value.Inconclusive.Should().BeFalse());
            }

            [Fact]
            public void Should_not_have_warning_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Warning.Should().BeFalse());
            }

            [Fact]
            public void Should_report_package_not_found()
            {
                Results.Should().AllSatisfy(r => r.Value.Messages.First().MessageType.Should().Be(ResultType.Error))
                    .And.AllSatisfy(p =>
                        p.Value.Messages.First().Message.Should()
                            .StartWith("non-existing not installed. The package was not found with the source(s) listed."));
            }
        }

        [Categories.SourcePriority]
        public class When_installing_new_package_from_priority_source_with_repository_optimization : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Configuration.Features.UsePackageRepositoryOptimizations = true;
                Scenario.AddMachineSource(Configuration, "chocolatey", path: "https://community.chocolatey.org/api/v2/", createDirectory: false);

                Configuration.Sources = string.Join(";", new[]
                {
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1)
                });
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_lower_version_of_package()
            {
                Results.Should().AllSatisfy(r => r.Value.Version.Should().Be("1.0.0"));
            }

            [Fact]
            public void Should_have_installed_expected_version_in_lib_directory()
            {
                var installedPath = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                var packageFolder = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                using (var reader = new PackageFolderReader(packageFolder))
                {
                    reader.NuspecReader.GetVersion().ToNormalizedString().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_not_have_inconclusive_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Inconclusive.Should().BeFalse());
            }

            [Fact]
            public void Should_not_have_warning_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Warning.Should().BeFalse());
            }

            [Fact]
            public void Should_have_success_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeTrue());
            }
        }

        [Categories.SourcePriority]
        public class When_installing_new_package_from_priority_source : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "upgradepackage";

                Configuration.Sources = string.Join(";", new[]
                {
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1)
                });
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [Fact]
            public void Should_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);
                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_have_installed_expected_version_in_lib_directory()
            {
                var packageFolder = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);

                using (var reader = new PackageFolderReader(packageFolder))
                {
                    reader.NuspecReader.GetVersion().ToNormalizedString().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_install_lower_version_of_package()
            {
                Results.Should().AllSatisfy(r => r.Value.Version.Should().Be("1.0.0"));
            }

            [Fact]
            public void Should_not_have_inconclusive_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Inconclusive.Should().BeFalse());
            }

            [Fact]
            public void Should_not_have_warning_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Warning.Should().BeFalse());
            }

            [Fact]
            public void Should_have_success_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeTrue());
            }
        }

        [Categories.SourcePriority]
        public class When_installing_package_with_dependencies_on_different_priority_sources : ScenariosBase
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
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "hasdependency.1.6.0" + NuGetConstants.PackageExtension, priority: 1),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension, priority: 2),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "isexactversiondependency.1.1.0" + NuGetConstants.PackageExtension)
                });
                Scenario.AddPackagesToPrioritySourceLocation(Configuration, "isexactversiondependency.2.0.0" + NuGetConstants.PackageExtension, priority: 1);
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                foreach (var packageResult in Results)
                {
                    DirectoryAssert.Exists(packageResult.Value.InstallLocation);
                }
            }

            [TestCaseSource(nameof(ExpectedInstallations))]
            public void Should_install_hasdependency_package_to_lib_directory(string name)
            {
                var expectedPath = Path.Combine(Scenario.GetTopLevel(), "lib", name);
                DirectoryAssert.Exists(expectedPath);
            }

            [TestCaseSource(nameof(ExpectedPackageVersions))]
            public void Should_instal_expected_package_version(string name, string version)
            {
                var path = Path.Combine(Scenario.GetTopLevel(), "lib", name);

                using (var reader = new PackageFolderReader(path))
                {
                    reader.NuspecReader.GetVersion().ToNormalizedString().Should().Be(version);
                }
            }

            [TestCaseSource(nameof(ExpectedPackageVersions))]
            public void Should_report_installed_version_of_package(string name, string version)
            {
                var package = Results.First(r => r.Key == name);
                package.Value.Version.Should().Be(version);
            }

            [Fact]
            public void Should_not_have_inconclusive_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Inconclusive.Should().BeFalse());
            }

            [Fact]
            public void Should_not_have_warning_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Warning.Should().BeFalse());
            }

            [Fact]
            public void Should_have_success_package_results()
            {
                Results.Should().AllSatisfy(r => r.Value.Success.Should().BeTrue());
            }
        }

        public class When_installing_a_package_with_an_uppercase_id : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "UpperCase.1.0.0" + NuGetConstants.PackageExtension);
                Configuration.PackageNames = Configuration.Input = "UpperCase";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_have_the_correct_casing_for_the_nuspec()
            {
                var nuspecFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.ManifestExtension);
                FileAssert.Exists(nuspecFile);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.GetTopLevel(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void Should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
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
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("UpperCase 1.0.0 Installed"));
            }
        }

        public class When_installing_a_package_with_unsupported_metadata_elements : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "unsupportedelements" + ".1.0.0" + NuGetConstants.PackageExtension);
                Configuration.PackageNames = Configuration.Input = "unsupportedelements";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.GetTopLevel(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void Should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_about_unsupported_elements()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Issues found with nuspec elements"));
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
            public void Should_have_warning_package_result()
            {
                _packageResult.Warning.Should().BeTrue();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            public void Should_have_a_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("unsupportedelements 1.0.0 Installed"));
            }
        }

        public class When_installing_a_package_with_non_normalized_version : ScenariosBase
        {
            private PackageResult _packageResult;

            protected virtual string NonNormalizedVersion => "2.02.0.0";
            protected virtual string NormalizedVersion => "2.2.0";

            public override void Context()
            {
                base.Context();
                Scenario.AddChangedVersionPackageToSourceLocation(Configuration, "installpackage.1.0.0" + NuGetConstants.PackageExtension, NonNormalizedVersion);
                Configuration.PackageNames = Configuration.Input = "installpackage";
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_install_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_install_the_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_install_the_expected_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToStringSafe().Should().Be(NonNormalizedVersion);
                }
            }

            [Fact]
            public void Should_not_create_an_extensions_folder_for_the_package()
            {
                var extensionsDirectory = Path.Combine(Scenario.GetTopLevel(), "extensions", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(extensionsDirectory);
            }

            [Fact]
            public void Should_not_create_an_hooks_folder_for_the_package()
            {
                var hooksDirectory = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(hooksDirectory);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_installed_successfully()
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
            public void Result_should_have_the_correct_version()
            {
                _packageResult.Version.Should().Be(NormalizedVersion);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script()
            {
                var message = "installpackage v{0} has been installed".FormatWith(NormalizedVersion);

                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains(message));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_create_a_shim_for_console_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_create_a_shim_for_graphical_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");

                FileAssert.Exists(shimfile);
            }

            [Fact]
            public void Should_not_create_a_shim_for_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "not.installed.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            public void Should_not_create_a_shim_for_mismatched_case_ignored_executable_in_the_bin_directory()
            {
                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "casemismatch.exe");

                FileAssert.DoesNotExist(shimfile);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_a_console_shim_that_is_set_for_non_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "console.exe");
                CommandExecutor.Execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                messages.Should()
                    .NotBeNullOrEmpty()
                    .And.Contain(m => m.Contains("is gui? False"), "GUI false message not found");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_a_graphical_shim_that_is_set_for_gui_access()
            {
                var messages = new List<string>();

                var shimfile = Path.Combine(Scenario.GetTopLevel(), "bin", "graphical.exe");
                CommandExecutor.Execute(
                    shimfile,
                    "--shimgen-noop",
                    10,
                    stdOutAction: (s, e) => messages.Add(e.Data),
                    stdErrAction: (s, e) => messages.Add(e.Data)
                );

                messages.Should()
                    .NotBeNullOrEmpty()
                    .And.Contain(m => m.Contains("is gui? True"), "GUI true message not found");
            }
        }

        public class When_installing_a_package_specifying_normalized_version : When_installing_a_package_with_non_normalized_version
        {
            protected override string NormalizedVersion => "2.2.0";
            protected override string NonNormalizedVersion => "2.02.0.0";

            public override void Context()
            {
                base.Context();
                Configuration.Version = NormalizedVersion;
            }
        }

        public class When_installing_a_package_specifying_non_normalized_version : When_installing_a_package_with_non_normalized_version
        {
            protected override string NormalizedVersion => "2.2.0";
            protected override string NonNormalizedVersion => "2.02.0.0";

            public override void Context()
            {
                base.Context();
                Configuration.Version = NonNormalizedVersion;
            }
        }

        public class When_installing_a_package_with_multiple_leading_zeros : When_installing_a_package_with_non_normalized_version
        {
            protected override string NormalizedVersion => "4.4.5.1";
            protected override string NonNormalizedVersion => "0004.0004.00005.01";
        }

        public class When_installing_a_package_with_multiple_leading_zeros_specifying_normalized_version : When_installing_a_package_with_non_normalized_version
        {
            protected override string NormalizedVersion => "4.4.5.1"  ;
            protected override string NonNormalizedVersion => "0004.0004.00005.01";

            public override void Context()
            {
                base.Context();
                Configuration.Version = NormalizedVersion;
            }
        }

        public class When_installing_a_package_with_multiple_leading_zeros_specifying_non_normalized_version : When_installing_a_package_with_non_normalized_version
        {
            protected override string NormalizedVersion => "4.4.5.1";
            protected override string NonNormalizedVersion => "0004.0004.00005.01";

            public override void Context()
            {
                base.Context();
                Configuration.Version = NonNormalizedVersion;
            }
        }

        public class When_installing_a_package_that_requires_updating_a_dependency : ScenariosBase
        {
            private const string TargetPackageName = "hasdependencywithbeforemodify";
            private const string DependencyName = "isdependencywithbeforemodify";

            public override void Context()
            {
                base.Context();

                Scenario.AddPackagesToSourceLocation(Configuration, "{0}.*".FormatWith(TargetPackageName) + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "{0}.*".FormatWith(DependencyName) + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, DependencyName, "1.0.0");

                Configuration.PackageNames = Configuration.Input = TargetPackageName;
            }

            public override void Because()
            {
                Results = Service.Install(Configuration);
            }

            [Fact]
            public void Should_install_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", TargetPackageName, "{0}.nupkg".FormatWith(TargetPackageName));
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.0.0");
                }
            }

            [Fact]
            public void Should_upgrade_the_minimum_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", DependencyName, "{0}.nupkg".FormatWith(DependencyName));
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_everything_installed_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installed 2/2"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_run_target_package_beforeModify_for_upgraded_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("Ran BeforeModify: {0} {1}".FormatWith(TargetPackageName, "2.0.0")));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_already_installed_dependency_package_beforeModify()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Ran BeforeModify: {0} {1}".FormatWith(DependencyName, "1.0.0")));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_run_dependency_package_beforeModify_for_upgraded_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("Ran BeforeModify: {0} {1}".FormatWith(DependencyName, "2.0.0")));
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
