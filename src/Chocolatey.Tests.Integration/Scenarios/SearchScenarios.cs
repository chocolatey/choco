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

namespace Chocolatey.Tests.Integration.Scenarios
{
    using System.Collections.Generic;
    using System.Linq;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.Results;
    using global::NuGet.Configuration;
    using NUnit.Framework;
    using Should;

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
                Configuration = Scenario.search();
                Scenario.reset(Configuration);
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "installpackage*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Scenario.install_package(Configuration, "upgradepackage", "1.0.0");

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
                    Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, name: "NormalPriority"),
                    Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1)
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
                Results.Count.ShouldEqual(1);
                Results[0].Name.ShouldEqual("upgradepackage");
                Results[0].Version.ShouldEqual("1.0.0");
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
                    Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.1.0" + NuGetConstants.PackageExtension, name: "NormalPriority"),
                    Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.0.0" + NuGetConstants.PackageExtension, priority: 1)
                });

                Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.1.1-beta2" + NuGetConstants.PackageExtension, name: "NormalPriority");
                Scenario.add_packages_to_priority_source_location(Configuration, "upgradepackage.1.1.1-beta" + NuGetConstants.PackageExtension, priority: 1);

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
                Results.Count.ShouldEqual(2);
                Results[0].Name.ShouldEqual("upgradepackage");
                Results[0].Version.ShouldEqual("1.1.1-beta");
                Results[1].Name.ShouldEqual("upgradepackage");
                Results[1].Version.ShouldEqual("1.0.0");
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
                    Scenario.add_packages_to_priority_source_location(Configuration, "hasdependency.*" + NuGetConstants.PackageExtension, name: "NormalPriority"),
                    Scenario.add_packages_to_priority_source_location(Configuration, "isdependency.*" + NuGetConstants.PackageExtension, priority: 1)
                });

                Scenario.add_packages_to_priority_source_location(Configuration, "isexactdependency.1.1.0" + NuGetConstants.PackageExtension, name: "NormalPriority");
                Scenario.add_packages_to_priority_source_location(Configuration, "isexactdependency.2.0.0" + NuGetConstants.PackageExtension, priority: 1);
                Scenario.add_packages_to_priority_source_location(Configuration, "conflictingdependency.2.0.0" + NuGetConstants.PackageExtension, priority: 1);


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
                Results.Count.ShouldEqual(4);
                Results[0].Name.ShouldEqual("conflictingdependency");
                Results[0].Version.ShouldEqual("2.0.0");
                Results[1].Name.ShouldEqual("hasdependency");
                Results[1].Version.ShouldEqual("2.1.0");
                Results[2].Name.ShouldEqual("isdependency");
                Results[2].Version.ShouldEqual("2.1.0");
                Results[3].Name.ShouldEqual("isexactdependency");
                Results[3].Version.ShouldEqual("2.0.0");
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
                MockLogger.ContainsMessageCount("upgradepackage").ShouldEqual(1);
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.1.0").ShouldBeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.ContainsMessage("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("End of List", LogLevel.Debug).ShouldBeTrue();
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
                MockLogger.ContainsMessage("upgradepackage 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_that_do_not_match()
            {
                MockLogger.ContainsMessage("installpackage").ShouldBeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.ContainsMessage("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("End of List", LogLevel.Debug).ShouldBeTrue();
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
                MockLogger.ContainsMessageCount("upgradepackage").ShouldNotEqual(0);
                MockLogger.ContainsMessageCount("upgradepackage").ShouldNotEqual(1);
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.1.0").ShouldBeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.ContainsMessage("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("End of List", LogLevel.Debug).ShouldBeTrue();
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
                MockLogger.ContainsMessage("upgradepackage 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_description()
            {
                MockLogger.ContainsMessage("Description: ").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_tags()
            {
                MockLogger.ContainsMessage("Tags: ").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_download_counts()
            {
                MockLogger.ContainsMessage("Number of Downloads: ").ShouldBeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.1.0").ShouldBeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.ContainsMessage("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("End of List", LogLevel.Debug).ShouldBeTrue();
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
                MockLogger.ContainsMessage("Unable to search for packages when there are no sources enabled for", LogLevel.Error).ShouldBeTrue();
            }

            [Fact]
            public void Should_not_list_any_packages()
            {
                Results.Count().ShouldEqual(0);
            }
        }

        public class When_searching_for_an_exact_package : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.search();
                Scenario.reset(Configuration);
                Scenario.add_packages_to_source_location(Configuration, "exactpackage*" + NuGetConstants.PackageExtension);
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
                Results.Count.ShouldEqual(1);
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("exactpackage 1.0.0").ShouldBeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_that_do_not_match()
            {
                MockLogger.ContainsMessage("exactpackage.dontfind").ShouldBeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.ContainsMessage("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("End of List", LogLevel.Debug).ShouldBeTrue();
            }
        }

        public class When_searching_for_an_exact_package_with_zero_results : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.search();
                Scenario.reset(Configuration);
                Scenario.add_packages_to_source_location(Configuration, "exactpackage*" + NuGetConstants.PackageExtension);
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
                Results.Count.ShouldEqual(0);
            }

            [Fact]
            public void Should_not_contain_packages_that_do_not_match()
            {
                MockLogger.ContainsMessage("exactpackage.dontfind").ShouldBeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages found").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_debugging_messages()
            {
                MockLogger.ContainsMessage("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.ContainsMessage("End of List", LogLevel.Debug).ShouldBeTrue();
            }
        }

        public class When_searching_for_all_packages_with_exact_id : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.search();
                Scenario.reset(Configuration);
                Scenario.add_packages_to_source_location(Configuration, "exactpackage*" + NuGetConstants.PackageExtension);
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
                Results.Count.ShouldEqual(2);
            }

            [Fact]
            public void Should_find_only_packages_with_exact_id()
            {
                Results[0].PackageMetadata.Id.ShouldEqual("exactpackage");
                Results[1].PackageMetadata.Id.ShouldEqual("exactpackage");
            }

            [Fact]
            public void Should_find_all_non_prerelease_versions_in_descending_order()
            {
                Results[0].PackageMetadata.Version.ToNormalizedString().ShouldEqual("1.0.0");
                Results[1].PackageMetadata.Version.ToNormalizedString().ShouldEqual("0.9.0");
            }
        }

        public class When_searching_for_all_packages_including_prerelease_with_exact_id : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.search();
                Scenario.reset(Configuration);
                Scenario.add_packages_to_source_location(Configuration, "exactpackage*" + NuGetConstants.PackageExtension);
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
                Results.Count.ShouldEqual(3);
            }

            [Fact]
            public void Should_find_only_packages_with_exact_id()
            {
                Results[0].PackageMetadata.Id.ShouldEqual("exactpackage");
                Results[1].PackageMetadata.Id.ShouldEqual("exactpackage");
                Results[2].PackageMetadata.Id.ShouldEqual("exactpackage");
            }

            [Fact]
            public void Should_find_all_versions_in_descending_order()
            {
                Results[0].PackageMetadata.Version.ToNormalizedString().ShouldEqual("1.0.0");
                Results[1].PackageMetadata.Version.ToNormalizedString().ShouldEqual("1.0.0-beta1");
                Results[2].PackageMetadata.Version.ToNormalizedString().ShouldEqual("0.9.0");
            }
        }
    }
}
