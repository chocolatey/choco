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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.XPath;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.results;
    using NuGet.Configuration;
    using NuGet.Packaging;
    using NUnit.Framework;
    using FluentAssertions;

    public class UpgradeScenarios
    {
        [ConcernFor("upgrade")]
        public abstract class ScenariosBase : TinySpec
        {
            protected ConcurrentDictionary<string, PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;

            public override void Context()
            {
                Configuration = Scenario.Upgrade();
                Scenario.Reset(Configuration);
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "badpackage*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.0.0");
                Configuration.SkipPackageInstallProvider = true;
                Scenario.InstallPackage(Configuration, "badpackage", "1.0");
                Configuration.SkipPackageInstallProvider = false;

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }
        }

        public class When_noop_upgrading_a_package_that_has_available_upgrades : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Noop = true;
            }

            public override void Because()
            {
                Service.UpgradeDryRun(Configuration);
            }

            [Fact]
            public void Should_contain_older_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.0.0");
            }

            [Fact]
            public void Should_contain_a_message_that_a_new_version_is_available()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.0 is available based on your source(s)"));
            }

            [Fact]
            public void Should_contain_a_message_that_a_package_can_be_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("can upgrade 1/1"));
            }

            [Fact]
            public void Should_not_create_a_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }
        }

        public class When_noop_upgrading_a_package_that_does_not_have_available_upgrades : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Noop = true;
                Configuration.PackageNames = Configuration.Input = "installpackage";
            }

            public override void Because()
            {
                Service.UpgradeDryRun(Configuration);
            }

            [Fact]
            public void Should_contain_a_message_that_you_have_the_latest_version_available()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage v1.0.0 is the latest version available based on your source(s)"));
            }

            [Fact]
            public void Should_contain_a_message_that_no_packages_can_be_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("can upgrade 0/1"));
            }

            [Fact]
            public void Should_not_create_a_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }
        }

        public class When_noop_upgrading_a_package_that_does_not_exist : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Noop = true;
                Configuration.PackageNames = Configuration.Input = "nonexistentpackage";
            }

            public override void Because()
            {
                Service.UpgradeDryRun(Configuration);
            }

            [Fact]
            public void Should_contain_a_message_the_package_was_not_found()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("nonexistentpackage not installed. The package was not found with the source(s) listed"));
            }

            [Fact]
            public void Should_contain_a_message_that_no_packages_can_be_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("can upgrade 0/0"));
            }
        }

        public class When_upgrading_an_existing_package_happy_path : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.0");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.0 is available based on your source"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("upgradepackage 1.0.0 Before Modification"))
                    .Should().Contain(p => p.EndsWith("upgradepackage 1.1.0 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.0.0 Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.0 Installed"));
            }
        }

        [Categories.SourcePriority]
        public class When_upgrading_an_existing_package_with_higher_version_in_non_prioritised_source : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Sources = string.Join(",",
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, priority: 0));
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.Select(r => r.Value).FirstOrDefault();
            }

            [Fact]
            public void Should_contain_a_message_that_you_have_the_latest_version_available()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage v1.0.0 is the latest version available based on your source(s)"));
            }

            [Fact]
            public void Should_contain_a_message_that_no_packages_were_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 0/1 "));
            }

            [Fact]
            public void Should_not_create_a_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_be_the_same_version_of_the_package()
            {
                var packageFolder = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);

                using (var reader = new PackageFolderReader(packageFolder))
                {
                    reader.NuspecReader.GetVersion().ToNormalizedString().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
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

            [Fact]
            public void Should_match_the_original_package_version()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }
        }

        public class When_upgrading_an_existing_package_with_prerelease_available_without_prerelease_specified : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.1.0");
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_contain_a_message_that_you_have_the_latest_version_available()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage v1.1.0 is the latest version available based on your source(s)"));
            }

            [Fact]
            public void Should_contain_a_message_that_no_packages_were_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 0/1 "));
            }

            [Fact]
            public void Should_not_create_a_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_be_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
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

            [Fact]
            public void Should_match_the_original_package_version()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }
        }

        public class When_upgrading_an_existing_package_with_prerelease_available_and_prerelease_specified : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Prerelease = true;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.1-beta2");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    var version = packageReader.NuspecReader.GetVersion();
                    version.Version.ToStringSafe().Should().Be("1.1.1.0");
                    version.OriginalVersion.ToStringSafe().Should().Be("1.1.1-beta2");
                    version.ToString().Should().Be("1.1.1-beta2");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.1-beta2 is available based on your source"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_the_new_beta()
            {
                _packageResult.Version.Should().Be("1.1.1-beta2");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("upgradepackage 1.0.0 Before Modification"))
                    .Should().Contain(p => p.EndsWith("upgradepackage 1.1.1-beta2 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.0.0 Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.1-beta2 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.1-beta2 Installed"));
            }
        }

        [Categories.SemVer20]
        public class When_upgrading_an_existing_package_with_semver_2_0_prerelease_available_and_prerelease_specified : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Prerelease = true;
                Scenario.RemovePackagesFromDestinationLocation(Configuration, "upgradepackage.1.1.1-beta2.nupkg");
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.1-beta.1");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    var version = packageReader.NuspecReader.GetVersion();
                    version.Version.ToStringSafe().Should().Be("1.1.1.0");
                    version.OriginalVersion.ToStringSafe().Should().Be("1.1.1-beta.1");
                    version.ToString().Should().Be("1.1.1-beta.1");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.1-beta.1 is available based on your source"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_the_new_beta()
            {
                _packageResult.Version.Should().Be("1.1.1-beta.1");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("upgradepackage 1.0.0 Before Modification"))
                    .Should().Contain(p => p.EndsWith("upgradepackage 1.1.1-beta.1 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.0.0 Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.1-beta.1 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.1-beta.1 Installed"));
            }
        }

        public class When_upgrading_an_existing_prerelease_package_without_prerelease_specified : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Prerelease = true;
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.1.1-beta");
                Configuration.Prerelease = false;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.1-beta2");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    var version = packageReader.NuspecReader.GetVersion();

                    version.Version.ToStringSafe().Should().Be("1.1.1.0");
                    version.OriginalVersion.ToStringSafe().Should().Be("1.1.1-beta2");
                    version.ToNormalizedStringChecked().Should().Be("1.1.1-beta2");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.1.1-beta installed. Version 1.1.1-beta2 is available based on your source"));
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
            public void Should_match_the_upgrade_version_of_the_new_beta()
            {
                _packageResult.Version.Should().Be("1.1.1-beta2");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.1-beta Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("upgradepackage 1.1.1-beta Before Modification"))
                    .Should().Contain(p => p.EndsWith("upgradepackage 1.1.1-beta2 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.1-beta Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.1-beta2 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.1-beta2 Installed"));
            }
        }

        [Categories.SemVer20]
        public class When_upgrading_an_existing_prerelease_package_to_semver_2_0_without_prerelease_specified : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Prerelease = true;
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.1.1-beta");
                Scenario.RemovePackagesFromDestinationLocation(Configuration, "upgradepackage.1.1.1-beta2.nupkg");
                Configuration.Prerelease = false;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.1-beta.1");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    var version = packageReader.NuspecReader.GetVersion();

                    version.Version.ToStringSafe().Should().Be("1.1.1.0");
                    version.OriginalVersion.ToStringSafe().Should().Be("1.1.1-beta.1");
                    version.ToNormalizedStringChecked().Should().Be("1.1.1-beta.1");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.1.1-beta installed. Version 1.1.1-beta.1 is available based on your source"));
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
            public void Should_match_the_upgrade_version_of_the_new_beta()
            {
                _packageResult.Version.Should().Be("1.1.1-beta.1");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.1-beta Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("upgradepackage 1.1.1-beta Before Modification"))
                    .Should().Contain(p => p.EndsWith("upgradepackage 1.1.1-beta.1 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.1-beta Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.1-beta.1 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.1-beta.1 Installed"));
            }
        }

        [Categories.SemVer20]
        public class When_upgrading_an_existing_semver_2_0_prerelease_package_to_legacy_semver_without_prerelease_specified : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Prerelease = true;
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.1.1-beta.1");
                Configuration.Prerelease = false;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.1-beta2");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    var version = packageReader.NuspecReader.GetVersion();

                    version.Version.ToStringSafe().Should().Be("1.1.1.0");
                    version.OriginalVersion.ToStringSafe().Should().Be("1.1.1-beta2");
                    version.ToNormalizedStringChecked().Should().Be("1.1.1-beta2");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.1.1-beta.1 installed. Version 1.1.1-beta2 is available based on your source"));
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
            public void Should_match_the_upgrade_version_of_the_new_beta()
            {
                _packageResult.Version.Should().Be("1.1.1-beta2");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.1-beta.1 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("upgradepackage 1.1.1-beta.1 Before Modification"))
                    .Should().Contain(p => p.EndsWith("upgradepackage 1.1.1-beta2 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.1-beta.1 Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.1-beta2 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.1-beta2 Installed"));
            }
        }

        public class When_upgrading_an_existing_prerelease_package_with_prerelease_available_with_excludeprerelease_and_without_prerelease_specified : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Prerelease = true;
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.1.1-beta");
                Configuration.Prerelease = false;
                Configuration.UpgradeCommand.ExcludePrerelease = true;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_contain_a_message_that_you_have_the_latest_version_available()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage v1.1.1-beta is newer"));
            }

            [Fact]
            public void Should_contain_a_message_that_no_packages_were_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 0/1 "));
            }

            [Fact]
            public void Should_not_create_a_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_be_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    var version = packageReader.NuspecReader.GetVersion();
                    version.Version.ToStringSafe().Should().Be("1.1.1.0");
                    version.OriginalVersion.Should().Be("1.1.1-beta");
                    version.ToNormalizedStringChecked().Should().Be("1.1.1-beta");
                }
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
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

            [Fact]
            public void Should_only_find_the_last_stable_version()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }
        }

        public class When_upgrading_an_existing_prerelease_package_with_allow_downgrade_with_excludeprerelease_and_without_prerelease_specified : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Prerelease = true;
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.1.1-beta");
                Configuration.Prerelease = false;
                Configuration.UpgradeCommand.ExcludePrerelease = true;
                Configuration.AllowDowngrade = true;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_contain_a_message_that_you_have_the_latest_version_available()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage v1.1.1-beta is newer"));
            }

            [Fact]
            public void Should_contain_a_message_that_no_packages_were_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 0/1 "));
            }

            [Fact]
            public void Should_not_create_a_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_be_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    var version = packageReader.NuspecReader.GetVersion();
                    version.Version.ToStringSafe().Should().Be("1.1.1.0");
                    version.OriginalVersion.Should().Be("1.1.1-beta");
                    version.ToNormalizedStringChecked().Should().Be("1.1.1-beta");
                }
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
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

            [Fact]
            public void Should_only_find_the_last_stable_version()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }
        }

        public class When_force_upgrading_a_package : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Force = true;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.0");
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.0 is available based on your source"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }
        }

        public class When_upgrading_a_package_that_does_not_have_available_upgrades : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "installpackage";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_contain_a_message_that_you_have_the_latest_version_available()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage v1.0.0 is the latest version available based on your source(s)"));
            }

            [Fact]
            public void Should_contain_a_message_that_no_packages_were_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 0/1 "));
            }

            [Fact]
            public void Should_not_create_a_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_be_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_have_a_successful_package_result()
            {
                _packageResult.Success.Should().BeTrue();
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

            [Fact]
            public void Should_match_the_existing_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }
        }

        public class When_force_upgrading_a_package_that_does_not_have_available_upgrades : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.Force = true;
                Configuration.PackageNames = Configuration.Input = "installpackage";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_contain_a_message_that_you_have_the_latest_version_available()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("installpackage v1.0.0 is the latest version available based on your source(s)"));
            }

            [Fact]
            public void Should_contain_a_message_that_the_package_was_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_not_create_a_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_not_remove_the_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_be_the_same_version_of_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
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
            public void Should_match_the_existing_version_of_one_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("1.0.0");
            }
        }

        public class When_upgrading_packages_with_packages_config : ScenariosBase
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
                Action m = () => Service.Upgrade(Configuration);

                m.Should().Throw<ApplicationException>();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_upgrading_a_package_with_readonly_files : ScenariosBase
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
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.0");
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.0 is available based on your source"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_upgrading_a_package_with_a_read_and_delete_share_locked_file : ScenariosBase
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
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
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
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.0");
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.0 is available based on your source"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_upgrading_a_package_with_an_exclusively_locked_file : ScenariosBase
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
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_have_a_package_installed_in_the_lib_directory()
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
            public void Should_contain_old_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.0.0");
            }

            [Fact]
            public void Should_not_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_was_not_able_to_upgrade()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 0/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.0 is available based on your source"));
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

        public class When_upgrading_a_package_with_added_files : ScenariosBase
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
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_keep_the_added_file()
            {
                var fileAdded = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "dude.txt");

                FileAssert.Exists(fileAdded);
            }

            [Fact]
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.0");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }
        }

        public class When_upgrading_a_package_with_changed_files : ScenariosBase
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
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_update_the_changed_file()
            {
                var fileChanged = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "chocolateyinstall.ps1");

                File.ReadAllText(fileChanged).Should().NotBe("hellow");
            }

            [Fact]
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.0");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }
        }

        public class When_upgrading_a_package_that_does_not_exist : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "nonexistentpackage";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_message_the_package_was_not_found()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("nonexistentpackage not installed. The package was not found with the source(s) listed"));
            }

            [Fact]
            public void Should_contain_a_message_that_no_packages_were_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 0/1"));
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

        public class When_upgrading_a_package_that_is_not_installed : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "installpackage";
                Service.Uninstall(Configuration);
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
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
            public void Should_not_have_a_rollback_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
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
        public class When_upgrading_a_package_that_is_not_installed_and_failing_on_not_installed : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "installpackage";
                Service.Uninstall(Configuration);
                Configuration.UpgradeCommand.FailOnNotInstalled = true;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_install_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_was_unable_to_upgrade_a_package()
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
                        m.Message.Contains("Cannot upgrade a non-existent package")));
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_upgrading_a_package_that_errors : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "badpackage";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_not_remove_package_from_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_not_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_put_the_package_in_the_lib_bad_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bad", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_have_the_erroring_upgraded_package_in_the_lib_bad_directory()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib-bad", Configuration.PackageNames, "2.0.0", Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.0.0");
                }
            }

            [Fact]
            public void Should_delete_the_rollback()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib-bkp", Configuration.PackageNames);

                DirectoryAssert.DoesNotExist(packageDir);
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_was_unable_to_upgrade_a_package()
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

        public class When_upgrading_a_package_with_dependencies_happy : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.1.0");
                }
            }

            [Fact]
            public void Should_upgrade_the_minimum_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.1.0");
                }
            }

            [Fact]
            public void Should_upgrade_the_exact_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_everything_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 3/3"));
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

        public class When_upgrading_a_package_with_unavailable_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_not_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
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
            public void Should_contain_a_message_that_it_was_unable_to_upgrade_anything()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 0/1"));
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
                        m.Message.Contains("Unable to resolve dependency 'isexactversiondependency")));
            }
        }

        public class When_upgrading_a_package_with_unavailable_dependencies_ignoring_dependencies : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "hasdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency.1*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.1.0");
                }
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
            public void Should_contain_a_message_that_it_upgraded_only_the_package_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
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

        public class When_upgrading_a_dependency_happy : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_not_upgrade_the_parent_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
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
            public void Should_contain_a_message_the_dependency_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
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

        public class When_upgrading_a_dependency_with_parent_that_depends_on_a_range_less_than_upgrade_version : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.1.0");
                }
            }

            [Fact]
            public void Should_upgrade_the_parent_package_to_highest_version_that_meets_new_dependency_version()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.1.0");
                }
            }

            [Fact]
            public void Should_upgrade_the_exact_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_everything_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 3/3"));
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

        public class When_upgrading_a_dependency_with_parent_that_depends_on_a_range_less_than_upgrade_version_and_has_a_different_missing_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Configuration.IgnoreDependencies = true;
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Configuration.IgnoreDependencies = false;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.1.0");
                }
            }

            [Fact]
            public void Should_upgrade_the_parent_package_to_highest_version_that_meets_new_dependency_version()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.1.0");
                }
            }

            [Fact]
            public void Should_upgrade_the_exact_version_dependency()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isexactversiondependency", "isexactversiondependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_message_that_everything_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 3/3"));
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

        public class When_upgrading_a_dependency_with_parent_being_pinned_and_depends_on_a_range_less_than_upgrade_version : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Configuration.PinPackage = true;
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Configuration.PinPackage = false;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_upgrade_the_package_to_highest_version_in_range()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_not_upgrade_parent_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
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
            public void Should_contain_a_message_that_everything_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
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
            public void Should_have_outputted_conflicting_upgrade_message()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m =>
                        m.Contains("One or more unresolved package dependency constraints detected in the Chocolatey lib folder")
                        && m.Contains("hasdependency 1.0.0 constraint: isdependency (>= 1.0.0 && < 2.0.0)"));
            }
        }

        public class When_upgrading_a_dependency_with_parent_being_pinned_and_depends_on_a_range_less_than_upgrade_version_and_has_missing_dependency : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Configuration.PinPackage = true;
                Configuration.IgnoreDependencies = true;
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Configuration.PinPackage = false;
                Configuration.IgnoreDependencies = false;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_upgrade_the_package_to_highest_version_in_range()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_not_upgrade_parent_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
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
            public void Should_contain_a_message_that_everything_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 2/2"));
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
            public void Should_have_outputted_conflicting_upgrade_message()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m =>
                        m.Contains("One or more unresolved package dependency constraints detected in the Chocolatey lib folder")
                        && m.Contains("hasdependency 1.0.0 constraint: isexactversiondependency (= 1.0.0)")
                        && m.Contains("hasdependency 1.0.0 constraint: isdependency (>= 1.0.0 && < 2.0.0)"));
            }
        }

        public class When_upgrading_a_dependency_with_parent_has_different_pinned_dependency_and_depends_on_a_range_less_than_upgrade_version : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Configuration.PinPackage = true;
                Scenario.InstallPackage(Configuration, "isexactversiondependency", "1.0.0");
                Configuration.PinPackage = false;
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Scenario.RemovePackagesFromDestinationLocation(Configuration, "hasdependency.2.0.1" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_upgrade_the_package_to_highest_version_in_range()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_not_upgrade_parent_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
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
            public void Should_contain_a_message_that_everything_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
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
            public void Should_have_outputted_conflicting_upgrade_message()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m =>
                        m.Contains("One or more unresolved package dependency constraints detected in the Chocolatey lib folder")
                        && m.Contains("hasdependency 1.0.0 constraint: isdependency (>= 1.0.0 && < 2.0.0)"));
            }
        }

        public class When_upgrading_a_dependency_while_ignoring_dependencies_and_parent_package_is_pinned : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "isdependency";
                Scenario.AddPackagesToSourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Configuration.PinPackage = true;
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");
                Configuration.PinPackage = false;
                Configuration.IgnoreDependencies = true;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_not_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "isdependency", "isdependency.nupkg");
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.0.0");
                }
            }

            [Fact]
            public void Should_not_upgrade_parent_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "hasdependency", "hasdependency.nupkg");
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
            public void Should_contain_a_message_that_nothing_was_upgraded()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 0/1"));
            }

            [Fact]
            public void Should_have_an_error_package_result()
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
            public void Should_have_outputted_expected_error_message()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Unable to resolve dependency chain. This may be caused by a parent package depending on this package, try specifying a specific version to use or don't ignore any dependencies!"));
            }
        }

        public class When_upgrading_a_package_with_config_transforms : ScenariosBase
        {
            private PackageResult _packageResult;
            private string _xmlFilePath = string.Empty;
            private XPathNavigator _xPathNavigator;

            public override void Context()
            {
                base.Context();

                _xmlFilePath = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe.config");
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;

                var xmlDocument = new XPathDocument(_xmlFilePath);
                _xPathNavigator = xmlDocument.CreateNavigator();
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }

            // any file in a nuget package will overwrite an existing file
            // on the file system - we subvert that by pulling the backup
            // if we've determined that there is an xdt file

            [Fact]
            public void Should_not_change_the_test_value_in_the_config_from_original_one_dot_zero_dot_zero_due_to_upgrade_and_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='test']/@value").TypedValue.ToStringSafe().Should().Be("default 1.0.0");
            }

            [Fact]
            public void Should_change_the_testReplace_value_in_the_config_due_to_XDT_Replace()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='testReplace']/@value").TypedValue.ToStringSafe().Should().Be("1.1.0");
            }

            [Fact]
            public void Should_not_change_the_insert_value_in_the_config_due_to_upgrade_and_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='insert']/@value").TypedValue.ToStringSafe().Should().Be("1.0.0");
            }

            [Fact]
            public void Should_add_the_insertNew_value_in_the_config_due_to_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='insertNew']/@value").TypedValue.ToStringSafe().Should().Be("1.1.0");
            }
        }

        public class When_upgrading_a_package_with_config_transforms_when_config_was_edited : ScenariosBase
        {
            private PackageResult _packageResult;
            private string _xmlFilePath = string.Empty;
            private XPathNavigator _xPathNavigator;
            private const string CommentAdded = "<!-- dude -->";

            public override void Context()
            {
                base.Context();

                _xmlFilePath = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe.config");

                File.WriteAllText(_xmlFilePath, File.ReadAllText(_xmlFilePath) + CommentAdded);
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;

                var xmlDocument = new XPathDocument(_xmlFilePath);
                _xPathNavigator = xmlDocument.CreateNavigator();
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }

            // any file in a nuget package will overwrite an existing file
            // on the file system - we subvert that by pulling the backup
            // if we've determined that there is an xdt file

            [Fact]
            public void Should_not_change_the_test_value_in_the_config_from_original_one_dot_zero_dot_zero_due_to_upgrade_and_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='test']/@value").TypedValue.ToStringSafe().Should().Be("default 1.0.0");
            }

            [Fact]
            public void Should_change_the_testReplace_value_in_the_config_due_to_XDT_Replace()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='testReplace']/@value").TypedValue.ToStringSafe().Should().Be("1.1.0");
            }

            [Fact]
            public void Should_not_change_the_insert_value_in_the_config_due_to_upgrade_and_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='insert']/@value").TypedValue.ToStringSafe().Should().Be("1.0.0");
            }

            [Fact]
            public void Should_add_the_insertNew_value_in_the_config_due_to_XDT_InsertIfMissing()
            {
                _xPathNavigator.SelectSingleNode("//configuration/appSettings/add[@key='insertNew']/@value").TypedValue.ToStringSafe().Should().Be("1.1.0");
            }

            [Fact]
            public void Should_have_a_config_with_the_comment_from_the_original()
            {
                File.ReadAllText(_xmlFilePath).Should().Contain(CommentAdded);
            }
        }

        public class When_upgrading_a_package_with_no_sources_enabled : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = null;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_have_no_sources_enabled_result()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Upgrading was NOT successful. There are no sources enabled for"));
            }

            [Fact]
            public void Should_not_have_any_packages_upgraded()
            {
                Results.Should().BeEmpty();
            }
        }

        public class When_upgrading_all_packages_happy_path : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "all";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_report_for_all_installed_packages()
            {
                Results.Should().HaveCount(3);
            }

            [Fact]
            public void Should_upgrade_packages_with_upgrades()
            {
                var upgradePackageResult = Results.Where(x => x.Key == "upgradepackage").ToList();
                upgradePackageResult.Should().ContainSingle( "upgradepackage must be there once");
                upgradePackageResult.First().Value.Version.Should().Be("1.1.0");
            }

            [Fact]
            public void Should_skip_packages_without_upgrades()
            {
                var installPackageResult = Results.Where(x => x.Key == "installpackage").ToList();
                installPackageResult.Should().ContainSingle( "installpackage must be there once");
                installPackageResult.First().Value.Version.Should().Be("1.0.0");
            }
        }

        public class When_upgrading_all_packages_with_prereleases_installed : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Prerelease = true;
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.1.1-beta");
                Configuration.Prerelease = false;
                Configuration.PackageNames = Configuration.Input = "all";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_report_for_all_installed_packages()
            {
                Results.Should().HaveCount(3);
            }

            [Fact]
            public void Should_upgrade_packages_with_upgrades()
            {
                var upgradePackageResult = Results.Where(x => x.Key == "upgradepackage").ToList();
                upgradePackageResult.Should().ContainSingle( "upgradepackage must be there once");
                upgradePackageResult.First().Value.Version.Should().Be("1.1.1-beta2");
            }

            [Fact]
            public void Should_upgrade_upgradepackage()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "upgradepackage", "upgradepackage" + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    var version = packageReader.NuspecReader.GetVersion();
                    version.Version.ToStringSafe().Should().Be("1.1.1.0");
                    version.OriginalVersion.Should().Be("1.1.1-beta2");
                    version.ToNormalizedStringChecked().Should().Be("1.1.1-beta2");
                }
            }

            [Fact]
            public void Should_skip_packages_without_upgrades()
            {
                var installPackageResult = Results.Where(x => x.Key == "installpackage").ToList();
                installPackageResult.Should().ContainSingle( "installpackage must be there once");
                installPackageResult.First().Value.Version.Should().Be("1.0.0");
            }
        }

        public class When_upgrading_all_packages_with_prereleases_installed_with_excludeprerelease_specified : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Prerelease = true;
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.1.1-beta");
                Configuration.Prerelease = false;

                Configuration.PackageNames = Configuration.Input = "all";
                Configuration.UpgradeCommand.ExcludePrerelease = true;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_report_for_all_installed_packages()
            {
                Results.Should().HaveCount(3);
            }

            [Fact]
            public void Should_upgrade_packages_with_upgrades()
            {
                var upgradePackageResult = Results.Where(x => x.Key == "upgradepackage").ToList();
                upgradePackageResult.Should().ContainSingle( "upgradepackage must be there once");
                // available version will show as last stable
                upgradePackageResult.First().Value.Version.Should().Be("1.1.0");
            }

            [Fact]
            public void Should_not_upgrade_upgradepackage()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", "upgradepackage", "upgradepackage" + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    var version = packageReader.NuspecReader.GetVersion();
                    version.Version.ToStringSafe().Should().Be("1.1.1.0");
                    version.OriginalVersion.Should().Be("1.1.1-beta");
                    version.ToNormalizedStringChecked().Should().Be("1.1.1-beta");
                }
            }

            [Fact]
            public void Should_skip_packages_without_upgrades()
            {
                var installPackageResult = Results.Where(x => x.Key == "installpackage").ToList();
                installPackageResult.Should().ContainSingle( "installpackage must be there once");
                installPackageResult.First().Value.Version.Should().Be("1.0.0");
            }
        }

        [Categories.SemVer20]
        public class When_upgrading_package_to_an_explicit_semver_2_0_version : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Configuration.Version = "1.1.1-beta.1";
                Configuration.Prerelease = true;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.Select(r => r.Value).FirstOrDefault();
            }

            [Fact]
            public void Should_have_a_single_package_result()
            {
                Results.Should().ContainSingle( "The returned package results do not have a single value!");
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                FileAssert.Exists(shimFile);

                File.ReadAllText(shimFile).Should().Be("1.1.1-beta.1");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.1-beta.1");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.1-beta.1 is available based on your source"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.1-beta.1");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("upgradepackage 1.0.0 Before Modification"))
                    .Should().Contain(p => p.EndsWith("upgradepackage 1.1.1-beta.1 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.0.0 Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                const string expectedMessage = "upgradepackage 1.1.1-beta.1 Installed";

                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains(expectedMessage), "No log message containing the sentence '{0}' could be found!".FormatWith(expectedMessage));
            }
        }

        public class When_upgrading_all_packages_with_except : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = Configuration.Input = "all";
                Configuration.UpgradeCommand.PackageNamesToSkip = "upgradepackage,badpackage";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_report_for_all_non_skipped_packages()
            {
                Results.Should().HaveCount(1);
                Results.First().Key.Should().Be("installpackage");
            }

            [Fact]
            public void Should_skip_packages_in_except_list()
            {
                var upgradePackageResult = Results.Where(x => x.Key == "upgradepackage").ToList();
                upgradePackageResult.Should().BeEmpty("upgradepackage should not be in the results list");
            }
        }

        public class When_upgrading_all_packages_multiple_upgraded_packages : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "isdependency.1.*");
                Scenario.InstallPackage(Configuration, "isdependency", "1.0.0");
                Configuration.PackageNames = Configuration.Input = "all";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
            }

            [Fact]
            public void Should_report_start_of_list_for_upgraded_packages_only()
            {
                var startOfLists = MockLogger.Messages["Debug"].Where(m => m == "--- Start of List ---");
                startOfLists.Should().HaveCount(3);
            }

            [Fact]
            public void Should_upgrade_packages_with_upgrades()
            {
                var upgradePackageResult = Results.Where(x => (x.Key == "upgradepackage" || x.Key == "isdependency")).ToList();
                upgradePackageResult.Should().HaveCount(2);
                upgradePackageResult.ForEach(r =>
                {
                    r.Value.Version.Should().Be("1.1.0");
                });
            }

            [Fact]
            public void Should_skip_packages_without_upgrades()
            {
                var installPackageResult = Results.Where(x => x.Key == "installpackage").ToList();
                installPackageResult.Should().ContainSingle("installpackage must be there once");
                installPackageResult.First().Value.Version.Should().Be("1.0.0");
            }
        }

        public class When_upgrading_an_existing_hook_package : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "scriptpackage.hook" + ".1.0.0" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "scriptpackage.hook", "1.0.0");
                Configuration.PackageNames = Configuration.Input = "scriptpackage.hook";
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + ".2.0.0" + NuGetConstants.PackageExtension);
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("2.0.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have scriptpackage.hook v1.0.0 installed. Version 2.0.0 is available based on your source"));
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
            public void Should_match_the_upgrade_version_of_two_dot_zero_dot_zero()
            {
                _packageResult.Version.Should().Be("2.0.0");
            }

            [Fact]
            public void Should_have_a_hooks_folder_for_the_package()
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
            public void Should_remove_files_not_in_upgrade_version()
            {
                var hookScriptPath = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames.Replace(".hook", string.Empty), "pre-install-doesnotexist.ps1");
                FileAssert.DoesNotExist(hookScriptPath);
            }

            [Fact]
            public void Should_install_new_files_in_upgrade_version()
            {
                var hookScriptPath = Path.Combine(Scenario.GetTopLevel(), "hooks", Configuration.PackageNames.Replace(".hook", string.Empty), "post-install-doesnotexist.ps1");
                FileAssert.Exists(hookScriptPath);
            }
        }

        public class When_upgrading_an_existing_package_happy_path_with_hooks : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "scriptpackage.hook" + "*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "scriptpackage.hook", "1.0.0");
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_contain_newer_version_in_directory()
            {
                var shimFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, "tools", "console.exe");

                File.ReadAllText(shimFile).Should().Be("1.1.0");
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version 1.1.0 is available based on your source"));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("upgradepackage 1.0.0 Before Modification"))
                    .Should().Contain(p => p.EndsWith("upgradepackage 1.1.0 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.0.0 Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.1.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.1.0 Installed"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_all_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-install-all.ps1 hook ran for upgradepackage 1.1.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_all_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-install-all.ps1 hook ran for upgradepackage 1.1.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_pre_upgradepackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-install-upgradepackage.ps1 hook ran for upgradepackage 1.1.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_post_upgradepackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-install-upgradepackage.ps1 hook ran for upgradepackage 1.1.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_uninstall_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("post-uninstall-all.ps1 hook ran for upgradepackage 1.1.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_installpackage_hook_script()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("pre-install-installpackage.ps1 hook ran for upgradepackage 1.1.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_beforemodify_hook_script_for_previous_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-beforemodify-all.ps1 hook ran for upgradepackage 1.0.0"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_have_executed_beforemodify_hook_script_for_upgrade_version()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("pre-beforemodify-all.ps1 hook ran for upgradepackage 1.1.0"));
            }
        }
        public class When_upgrading_an_existing_package_with_uppercase_id : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "UpperCase" + "*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "UpperCase", "1.0.0");

                Configuration.PackageNames = Configuration.Input = "UpperCase";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_have_the_correct_casing_for_the_nuspec()
            {
                var nuspecFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.ManifestExtension);
                FileAssert.Exists(nuspecFile);
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have UpperCase v1.0.0 installed. Version 1.1.0 is available based on your source"));
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
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("UpperCase 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("UpperCase 1.0.0 Before Modification"))
                    .Should().Contain(p => p.EndsWith("UpperCase 1.1.0 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("UpperCase 1.0.0 Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("UpperCase 1.1.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("UpperCase 1.1.0 Installed"));
            }
        }

        public class When_upgrading_an_existing_package_with_unsupported_metadata_elements : ScenariosBase
        {
            private PackageResult _packageResult;

            public override void Context()
            {
                base.Context();
                Scenario.AddPackagesToSourceLocation(Configuration, "unsupportedelements" + "*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "unsupportedelements", "1.0.0");
                Configuration.PackageNames = Configuration.Input = "unsupportedelements";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
            {
                var packageDir = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames);

                DirectoryAssert.Exists(packageDir);
            }

            [Fact]
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToNormalizedStringChecked().Should().Be("1.1.0");
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have unsupportedelements v1.0.0 installed. Version 1.1.0 is available based on your source"));
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
            public void Should_match_the_upgrade_version_of_one_dot_one_dot_zero()
            {
                _packageResult.Version.Should().Be("1.1.0");
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("unsupportedelements 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("unsupportedelements 1.0.0 Before Modification"))
                    .Should().Contain(p => p.EndsWith("unsupportedelements 1.1.0 Installed"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("unsupportedelements 1.0.0 Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("unsupportedelements 1.1.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("unsupportedelements 1.1.0 Installed"));
            }
        }

        public class When_upgrading_an_existing_package_non_normalized_version : ScenariosBase
        {
            private PackageResult _packageResult;

            protected virtual string NonNormalizedVersion => "2.02.0.0";
            protected virtual string NormalizedVersion => "2.2.0";

            public override void Context()
            {
                base.Context();
                Scenario.AddChangedVersionPackageToSourceLocation(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, NonNormalizedVersion);
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
                _packageResult = Results.FirstOrDefault().Value;
            }

            [Fact]
            public void Should_upgrade_where_install_location_reports()
            {
                DirectoryAssert.Exists(_packageResult.InstallLocation);
            }

            [Fact]
            public void Should_upgrade_a_package_in_the_lib_directory()
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
            public void Should_upgrade_the_package()
            {
                var packageFile = Path.Combine(Scenario.GetTopLevel(), "lib", Configuration.PackageNames, Configuration.PackageNames + NuGetConstants.PackageExtension);
                using (var packageReader = new PackageArchiveReader(packageFile))
                {
                    packageReader.NuspecReader.GetVersion().ToStringSafe().Should().Be(NonNormalizedVersion);
                }
            }

            [Fact]
            public void Should_contain_a_warning_message_that_it_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 1/1"));
            }

            [Fact]
            public void Should_contain_a_warning_message_with_old_and_new_versions()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("You have upgradepackage v1.0.0 installed. Version {0} is available based on your source".FormatWith(NonNormalizedVersion)));
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
            public void Should_not_have_warning_package_result_other_than_before_modify_failures()
            {
                // For before modify scripts that fail, we add a warning message.
                // So we will ignore any such warnings.
                var messages = _packageResult.Messages.Where(m => m.MessageType == ResultType.Warn && !m.Message.ContainsSafe("chocolateyBeforeModify"));
                messages.Should().BeEmpty();
            }

            [Fact]
            public void Config_should_match_package_result_name()
            {
                _packageResult.Name.Should().Be(Configuration.PackageNames);
            }

            [Fact]
            public void Should_match_the_upgrade_version()
            {
                _packageResult.Version.Should().Be(NormalizedVersion);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage 1.0.0 Before Modification"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyBeforeModify_before_chocolateyInstall()
            {
                MockLogger.MessagesFor(LogLevel.Info).OrEmpty()
                    .SkipWhile(p => !p.Contains("upgradepackage 1.0.0 Before Modification"))
                    .Any(p => p.EndsWith("upgradepackage {0} Installed".FormatWith(NormalizedVersion)))
                    .Should().BeTrue();
            }

            [Fact]
            public void Should_not_have_executed_chocolateyUninstall_script_for_original_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.0.0 Uninstalled"));
            }

            [Fact]
            public void Should_not_have_executed_chocolateyBeforeModify_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage {0} Before Modification".FormatWith(NormalizedVersion)));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_have_executed_chocolateyInstall_script_for_new_package()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgradepackage {0} Installed".FormatWith(NormalizedVersion)));
            }
        }

        public class When_upgrading_an_existing_package_specifying_normalized_version : When_upgrading_an_existing_package_non_normalized_version
        {
            protected override string NormalizedVersion => "2.2.0";
            protected override string NonNormalizedVersion => "2.02.0.0";

            public override void Context()
            {
                base.Context();
                Configuration.Version = NormalizedVersion;
            }
        }

        public class When_upgrading_an_existing_package_specifying_non_normalized_version : When_upgrading_an_existing_package_non_normalized_version
        {
            protected override string NormalizedVersion => "2.2.0";
            protected override string NonNormalizedVersion => "2.02.0.0";

            public override void Context()
            {
                base.Context();
                Configuration.Version = NonNormalizedVersion;
            }
        }

        public class When_upgrading_an_existing_package_with_multiple_leading_zeros : When_upgrading_an_existing_package_non_normalized_version
        {
            protected override string NormalizedVersion => "4.4.5.1";
            protected override string NonNormalizedVersion => "0004.0004.00005.01";
        }

        public class When_upgrading_an_existing_package_with_multiple_leading_zeros_specifying_normalized_version : When_upgrading_an_existing_package_non_normalized_version
        {
            protected override string NormalizedVersion => "4.4.5.1";
            protected override string NonNormalizedVersion => "0004.0004.00005.01";

            public override void Context()
            {
                base.Context();
                Configuration.Version = NormalizedVersion;
            }
        }

        public class When_upgrading_an_existing_package_with_multiple_leading_zeros_specifying_non_normalized_version : When_upgrading_an_existing_package_non_normalized_version
        {
            protected override string NormalizedVersion => "4.4.5.1";
            protected override string NonNormalizedVersion => "0004.0004.00005.01";

            public override void Context()
            {
                base.Context();
                Configuration.Version = NonNormalizedVersion;
            }
        }


        public class When_upgrading_a_package_with_beforeModify_script_with_dependencies_with_beforeModify_scripts_and_hooks : ScenariosBase
        {
            private const string TargetPackageName = "hasdependencywithbeforemodify";
            private const string DependencyName = "isdependencywithbeforemodify";

            public override void Context()
            {
                base.Context();

                Scenario.AddPackagesToSourceLocation(Configuration, "{0}.*".FormatWith(TargetPackageName) + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "{0}.*".FormatWith(DependencyName) + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "scriptpackage.hook" + "*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, DependencyName, "1.0.0");
                Scenario.InstallPackage(Configuration, TargetPackageName, "1.0.0");
                Scenario.InstallPackage(Configuration, "scriptpackage.hook", "1.0.0");

                Configuration.PackageNames = Configuration.Input = TargetPackageName;
            }

            public override void Because()
            {
                Results = Service.Upgrade(Configuration);
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
            public void Should_contain_a_message_that_everything_upgraded_successfully()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Warn.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("upgraded 2/2"));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_beforemodify_hook_script_for_previous_version_of_target()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-beforemodify-all.ps1 hook ran for {0} {1}".FormatWith(TargetPackageName, "1.0.0")));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_already_installed_target_package_beforeModify()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Ran BeforeModify: {0} {1}".FormatWith(TargetPackageName, "1.0.0")));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_not_run_beforemodify_hook_script_for_upgrade_version_of_target()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("pre-beforemodify-all.ps1 hook ran for {0} {1}".FormatWith(TargetPackageName, "2.0.0")));
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
            public void Should_run_pre_all_hook_script_for_upgraded_version_of_target()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-install-all.ps1 hook ran for {0} {1}".FormatWith(TargetPackageName, "2.0.0")));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_post_all_hook_script_for_upgraded_version_of_target()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-install-all.ps1 hook ran for {0} {1}".FormatWith(TargetPackageName, "2.0.0")));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_beforemodify_hook_script_for_previous_version_of_dependency()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-beforemodify-all.ps1 hook ran for {0} {1}".FormatWith(DependencyName, "1.0.0")));
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
            public void Should_not_run_beforemodify_hook_script_for_upgrade_version_of_dependency()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("pre-beforemodify-all.ps1 hook ran for {0} {1}".FormatWith(DependencyName, "2.0.0")));
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
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_pre_all_hook_script_for_upgraded_version_of_dependency()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("pre-install-all.ps1 hook ran for {0} {1}".FormatWith(DependencyName, "2.0.0")));
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_run_post_all_hook_script_for_upgraded_version_of_dependency()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("post-install-all.ps1 hook ran for {0} {1}".FormatWith(DependencyName, "2.0.0")));
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
