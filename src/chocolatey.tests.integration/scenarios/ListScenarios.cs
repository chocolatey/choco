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
    using System.Collections.Generic;
    using System.Linq;
    using bdddoc.core;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.results;
    using NuGet;
    using Should;

    public class ListScenarios
    {
        public abstract class ScenariosBase : TinySpec
        {
            protected IList<PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;

            public override void Context()
            {
                Configuration = Scenario.list();
                Scenario.reset(Configuration);
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "installpackage*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Scenario.install_package(Configuration, "upgradepackage", "1.0.0");

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_searching_packages_with_no_filter_happy_path : ScenariosBase
        {
            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_list_available_packages_only_once()
            {
                MockLogger.contains_message_count("upgradepackage").ShouldEqual(1);
            }

            [Fact]
            public void should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.contains_message("upgradepackage 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.contains_message("upgradepackage|1.1.0").ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_summary()
            {
                MockLogger.contains_message("packages found").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_debugging_messages()
            {
                MockLogger.contains_message("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("End of List", LogLevel.Debug).ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_searching_for_a_particular_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Input = Configuration.PackageNames = "upgradepackage";
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.contains_message("upgradepackage 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_packages_that_do_not_match()
            {
                MockLogger.contains_message("installpackage").ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_summary()
            {
                MockLogger.contains_message("packages found").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_debugging_messages()
            {
                MockLogger.contains_message("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("End of List", LogLevel.Debug).ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_searching_all_available_packages : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.AllVersions = true;
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_list_available_packages_as_many_times_as_they_show_on_the_feed()
            {
                MockLogger.contains_message_count("upgradepackage").ShouldNotEqual(0);
                MockLogger.contains_message_count("upgradepackage").ShouldNotEqual(1);
            }

            [Fact]
            public void should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.contains_message("upgradepackage 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.contains_message("upgradepackage|1.1.0").ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_summary()
            {
                MockLogger.contains_message("packages found").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_debugging_messages()
            {
                MockLogger.contains_message("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("End of List", LogLevel.Debug).ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_searching_packages_with_verbose : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Verbose = true;
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.contains_message("upgradepackage 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_description()
            {
                MockLogger.contains_message("Description: ").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_tags()
            {
                MockLogger.contains_message("Tags: ").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_download_counts()
            {
                MockLogger.contains_message("Number of Downloads: ").ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.contains_message("upgradepackage|1.1.0").ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_summary()
            {
                MockLogger.contains_message("packages found").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_debugging_messages()
            {
                MockLogger.contains_message("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("End of List", LogLevel.Debug).ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_listing_local_packages : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.ListCommand.LocalOnly = true;
                Configuration.Sources = ApplicationParameters.PackagesLocation;
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.contains_message("upgradepackage 1.0.0").ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.contains_message("upgradepackage|1.0.0").ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_summary()
            {
                MockLogger.contains_message("packages installed").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_debugging_messages()
            {
                MockLogger.contains_message("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("End of List", LogLevel.Debug).ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_listing_local_packages_with_id_only : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.ListCommand.LocalOnly = true;
                Configuration.ListCommand.IdOnly = true;
                Configuration.Sources = ApplicationParameters.PackagesLocation;
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_contain_package_name()
            {
                MockLogger.contains_message("upgradepackage").ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_any_version_number()
            {
                MockLogger.contains_message(".0").ShouldBeFalse();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_listing_local_packages_limiting_output : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.ListCommand.LocalOnly = true;
                Configuration.Sources = ApplicationParameters.PackagesLocation;
                Configuration.RegularOutput = false;
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.contains_message("upgradepackage|1.0.0").ShouldBeTrue();
            }

            [Fact]
            public void should_only_have_messages_related_to_package_information()
            {
                var count = MockLogger.Messages.SelectMany(messageLevel => messageLevel.Value.or_empty_list_if_null()).Count();
                count.ShouldEqual(2);
            }

            [Fact]
            public void should_not_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.contains_message("upgradepackage 1.0.0").ShouldBeFalse();
            }

            [Fact]
            public void should_not_contain_a_summary()
            {
                MockLogger.contains_message("packages installed").ShouldBeFalse();
            }

            [Fact]
            public void should_not_contain_debugging_messages()
            {
                MockLogger.contains_message("Searching for package information", LogLevel.Debug).ShouldBeFalse();
                MockLogger.contains_message("Running list with the following filter", LogLevel.Debug).ShouldBeFalse();
                MockLogger.contains_message("Start of List", LogLevel.Debug).ShouldBeFalse();
                MockLogger.contains_message("End of List", LogLevel.Debug).ShouldBeFalse();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_listing_local_packages_limiting_output_with_id_only : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.ListCommand.LocalOnly = true;
                Configuration.ListCommand.IdOnly = true;
                Configuration.Sources = ApplicationParameters.PackagesLocation;
                Configuration.RegularOutput = false;
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_contain_packages_id()
            {
                MockLogger.contains_message("upgradepackage").ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_any_version_number()
            {
                MockLogger.contains_message(".0").ShouldBeFalse();
            }

            [Fact]
            public void should_not_contain_pipe()
            {
                MockLogger.contains_message("|").ShouldBeFalse();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_listing_packages_with_no_sources_enabled : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = null;
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_have_no_sources_enabled_result()
            {
                MockLogger.contains_message("Unable to search for packages when there are no sources enabled for", LogLevel.Error).ShouldBeTrue();
            }

            [Fact]
            public void should_not_list_any_packages()
            {
                Results.Count().ShouldEqual(0);
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_searching_for_an_exact_package : ScenariosBase
        {
            public override void Context()
            {
                Configuration = Scenario.list();
                Scenario.reset(Configuration);
                Scenario.add_packages_to_source_location(Configuration, "exactpackage*" + Constants.PackageExtension);
                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();

                Configuration.ListCommand.Exact = true;
                Configuration.Input = Configuration.PackageNames = "exactpackage";
            }

            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.contains_message("exactpackage 1.0.0").ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_packages_that_do_not_match()
            {
                MockLogger.contains_message("exactpackage.dontfind").ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_summary()
            {
                MockLogger.contains_message("packages found").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_debugging_messages()
            {
                MockLogger.contains_message("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("Start of List", LogLevel.Debug).ShouldBeTrue();
                MockLogger.contains_message("End of List", LogLevel.Debug).ShouldBeTrue();
            }
        }
    }
}
