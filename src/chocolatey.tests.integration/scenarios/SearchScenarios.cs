// Copyright © 2017 - Present Chocolatey Software, Inc
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
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.results;
    using NuGet.Configuration;
    using NUnit.Framework;
    using FluentAssertions;
    using System.IO;

    public class SearchScenarios
    {
        [ConcernFor("search")]
        public abstract class ScenariosBase : TinySpec
        {
            protected IList<PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;

            public override void Context()
            {
                Configuration = Scenario.Search();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.0.0");

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }
        }

        [Categories.SourcePriority]
        public class When_searching_packages_with_source_priority : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = "upgradepackage";
                Configuration.AllVersions = false;

                Configuration.Sources = string.Join(";", new[]
                {
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, name: "NormalPriority"),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1)
                });

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_only_pick_up_package_from_highest_priority()
            {
                Results.Should().ContainSingle();
                Results[0].Name.Should().Be("upgradepackage");
                Results[0].Version.Should().Be("1.0.0");
            }
        }

        [Categories.SourcePriority]
        public class When_searching_packages_with_source_priority_and_pre_release : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = "upgradepackage";
                Configuration.AllVersions = false;
                Configuration.Prerelease = true;

                Configuration.Sources = string.Join(";", new[]
                {
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, name: "NormalPriority"),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1)
                });

                Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.1.1-beta2" + NuGetConstants.PackageExtension, name: "NormalPriority");
                Scenario.AddPackagesToPrioritySourceLocation(Configuration, "upgradepackage.1.1.1-beta" + NuGetConstants.PackageExtension, priority: 1);

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_only_pick_up_package_from_highest_priority()
            {
                Results.Should().HaveCount(2);
                Results[0].Name.Should().Be("upgradepackage");
                Results[0].Version.Should().Be("1.1.1-beta");
                Results[1].Name.Should().Be("upgradepackage");
                Results[1].Version.Should().Be("1.0.0");
            }
        }

        [Categories.SourcePriority]
        public class When_searching_packages_with_source_priority_and_different_package_in_different_feed : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PackageNames = "dependency";
                Configuration.AllVersions = false;

                Configuration.Sources = string.Join(";", new[]
                {
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension, name: "NormalPriority"),
                    Scenario.AddPackagesToPrioritySourceLocation(Configuration, "isdependency.*" + NuGetConstants.PackageExtension, priority: 1)
                });

                Scenario.AddPackagesToPrioritySourceLocation(Configuration, "isexactdependency.1.1.0" + NuGetConstants.PackageExtension, name: "NormalPriority");
                Scenario.AddPackagesToPrioritySourceLocation(Configuration, "isexactdependency.2.0.0" + NuGetConstants.PackageExtension, priority: 1);
                Scenario.AddPackagesToPrioritySourceLocation(Configuration, "conflictingdependency.2.0.0" + NuGetConstants.PackageExtension, priority: 1);


                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_pick_up_packages_from_all_feeds_except_those_with_same_name()
            {
                Results.Should().HaveCount(4);
                Results[0].Name.Should().Be("conflictingdependency");
                Results[0].Version.Should().Be("2.0.0");
                Results[1].Name.Should().Be("hasdependency");
                Results[1].Version.Should().Be("2.1.0");
                Results[2].Name.Should().Be("isdependency");
                Results[2].Version.Should().Be("2.1.0");
                Results[3].Name.Should().Be("isexactdependency");
                Results[3].Version.Should().Be("2.0.0");
            }
        }

        public class When_searching_packages_with_no_filter_happy_path : ScenariosBase
        {
            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_list_available_packages_only_once()
            {
                MockLogger.ContainsMessageCount("upgradepackage").Should().Be(1);
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.1.0").Should().BeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.1.0").Should().BeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Searching for package information"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Running list with the following filter"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Start of List"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("End of List"));
            }
        }

        public class When_searching_for_a_particular_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Input = Configuration.PackageNames = "upgradepackage";
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.1.0").Should().BeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_that_do_not_match()
            {
                MockLogger.ContainsMessage("installpackage").Should().BeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Searching for package information"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Running list with the following filter"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Start of List"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("End of List"));
            }
        }

        public class When_searching_all_available_packages : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.AllVersions = true;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_list_available_packages_as_many_times_as_they_show_on_the_feed()
            {
                MockLogger.ContainsMessageCount("upgradepackage").Should().NotBe(0);
                MockLogger.ContainsMessageCount("upgradepackage").Should().NotBe(1);
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.1.0").Should().BeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.1.0").Should().BeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Searching for package information"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Running list with the following filter"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Start of List"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("End of List"));
            }
        }

        public class When_searching_packages_with_verbose : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Verbose = true;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.1.0").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_description()
            {
                MockLogger.ContainsMessage("Description: ").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_tags()
            {
                MockLogger.ContainsMessage("Tags: ").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_download_counts()
            {
                MockLogger.ContainsMessage("Number of Downloads: ").Should().BeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.1.0").Should().BeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Searching for package information"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Running list with the following filter"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Start of List"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("End of List"));
            }
        }

        public class When_listing_packages_with_no_sources_enabled : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = null;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_have_no_sources_enabled_result()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Error.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Unable to search for packages when there are no sources enabled for"));
            }

            [Fact]
            public void Should_not_list_any_packages()
            {
                Results.Should().BeEmpty();
            }
        }

        public class When_searching_for_an_exact_package : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.Search();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, "exactpackage*" + NuGetConstants.PackageExtension);
                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                Configuration.ListCommand.Exact = true;
                Configuration.Input = Configuration.PackageNames = "exactpackage";
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_not_error()
            {
                // nothing necessary here
            }

            [Fact]
            public void Should_find_exactly_one_result()
            {
                Results.Should().ContainSingle();
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("exactpackage 1.0.0").Should().BeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_that_do_not_match()
            {
                MockLogger.ContainsMessage("exactpackage.dontfind").Should().BeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Searching for package information"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Running list with the following filter"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Start of List"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("End of List"));
            }
        }

        public class When_searching_for_an_exact_package_with_zero_results : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.Search();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, "exactpackage*" + NuGetConstants.PackageExtension);
                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                Configuration.ListCommand.Exact = true;
                Configuration.Input = Configuration.PackageNames = "exactpackage123";
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }


            [Fact]
            public void Should_not_error()
            {
                // nothing necessary here
            }


            [Fact]
            public void Should_not_have_any_results()
            {
                Results.Should().BeEmpty();
            }

            [Fact]
            public void Should_not_contain_packages_that_do_not_match()
            {
                MockLogger.ContainsMessage("exactpackage.dontfind").Should().BeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").Should().BeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Searching for package information"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Running list with the following filter"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("Start of List"));
                MockLogger.Messages.Should().ContainKey(LogLevel.Debug.ToStringSafe())
                    .WhoseValue.Should().Contain(m => m.Contains("End of List"));
            }
        }

        public class When_searching_for_all_packages_with_exact_id : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.Search();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, "exactpackage*" + NuGetConstants.PackageExtension);
                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                Configuration.ListCommand.Exact = true;
                Configuration.AllVersions = true;
                Configuration.Input = Configuration.PackageNames = "exactpackage";
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_not_error()
            {
                // nothing necessary here
            }

            [Fact]
            public void Should_find_two_results()
            {
                Results.Should().HaveCount(2);
            }

            [Fact]
            public void Should_find_only_packages_with_exact_id()
            {
                Results[0].PackageMetadata.Id.Should().Be("exactpackage");
                Results[1].PackageMetadata.Id.Should().Be("exactpackage");
            }

            [Fact]
            public void Should_find_all_non_prerelease_versions_in_descending_order()
            {
                Results[0].PackageMetadata.Version.ToNormalizedString().Should().Be("1.0.0");
                Results[1].PackageMetadata.Version.ToNormalizedString().Should().Be("0.9.0");
            }
        }

        public class WhenSearchingForAPackageWithPageSizeAndMultipleSources : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.Search();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, "upgradepackage*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "isexactversiondependency*" + NuGetConstants.PackageExtension);
                var secondSource = Path.Combine(Scenario.GetTopLevel(), "infrastructure");
                Configuration.Sources = Configuration.Sources + ";" + secondSource;
                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                Configuration.ListCommand.PageSize = 2;
                Configuration.ListCommand.ExplicitPageSize = true;
                Configuration.Input = Configuration.PackageNames = string.Empty;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void ShouldOutputWarningAboutThresholdBeingReached()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Warn.ToString())
                    .WhoseValue.Should().ContainSingle(m => m == "The threshold of 2 packages per source has been met. Please refine your search, or specify a page to find any more results.");
            }

            [Fact]
            public void ShouldListExpectedPackagesFoundOnSource()
            {
                MockLogger.Messages.Should()
                    .ContainKey(LogLevel.Info.ToString())
                    .WhoseValue.Should()
                        .ContainInOrder(
                            "installpackage 1.0.0",
                            "isexactversiondependency 2.0.0")
                        .And.NotContain(new[]
                        {
                            "installpackage 0.9.9",
                            "isexactversiondependency 1.0.0",
                            "upgradepackage 1.1.0",
                        });
            }
        }

        public class When_searching_for_all_packages_including_prerelease_with_exact_id : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.Search();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, "exactpackage*" + NuGetConstants.PackageExtension);
                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                Configuration.ListCommand.Exact = true;
                Configuration.AllVersions = true;
                Configuration.Prerelease = true;
                Configuration.Input = Configuration.PackageNames = "exactpackage";
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_not_error()
            {
                // nothing necessary here
            }

            [Fact]
            public void Should_find_three_results()
            {
                Results.Should().HaveCount(3);
            }

            [Fact]
            public void Should_find_only_packages_with_exact_id()
            {
                Results[0].PackageMetadata.Id.Should().Be("exactpackage");
                Results[1].PackageMetadata.Id.Should().Be("exactpackage");
                Results[2].PackageMetadata.Id.Should().Be("exactpackage");
            }

            [Fact]
            public void Should_find_all_versions_in_descending_order()
            {
                Results[0].PackageMetadata.Version.ToNormalizedString().Should().Be("1.0.0");
                Results[1].PackageMetadata.Version.ToNormalizedString().Should().Be("1.0.0-beta1");
                Results[2].PackageMetadata.Version.ToNormalizedString().Should().Be("0.9.0");
            }
        }
    }
}
