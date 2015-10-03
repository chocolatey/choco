// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using NuGet;
    using Should;
    using bdddoc.core;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.results;

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
                Configuration.PackageNames = Configuration.Input = "upgradepackage";
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "installpackage*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "badpackage*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Scenario.install_package(Configuration, "upgradepackage", "1.0.0");
                Configuration.SkipPackageInstallProvider = true;
                Scenario.install_package(Configuration, "badpackage", "1.0");
                Configuration.SkipPackageInstallProvider = false;

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }

            public bool has_expected_message(string expectedMessage)
            {
                bool messageFound = false;
                foreach (var messageLevel in MockLogger.Messages)
                {
                    foreach (var message in messageLevel.Value.or_empty_list_if_null())
                    {
                        if (message.Contains(expectedMessage)) messageFound = true;
                    }
                }

                return messageFound;
            }

            public bool has_expected_message(string expectedMessage, LogLevel level)
            {
                bool messageFound = false;
                foreach (var message in MockLogger.MessagesFor(level).or_empty_list_if_null())
                {
                    if (message.Contains(expectedMessage)) messageFound = true;
                }

                return messageFound;
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_listing_local_packages_happy_path : ScenariosBase
        {
            public override void Because()
            {
                MockLogger.reset();
                Results = Service.list_run(Configuration).ToList();
            }

            [Fact]
            public void should_contain_packages_and_versions_with_a_space_between_them()
            {
                has_expected_message("upgradepackage 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                has_expected_message("upgradepackage|1.1.0").ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_summary()
            {
                has_expected_message("packages installed").ShouldBeTrue();
            }

            [Fact]
            public void should_contain_debugging_messages()
            {
                has_expected_message("Searching for package information", LogLevel.Debug).ShouldBeTrue();
                has_expected_message("Running list with the following filter", LogLevel.Debug).ShouldBeTrue();
                has_expected_message("Start of List", LogLevel.Debug).ShouldBeTrue();
                has_expected_message("End of List", LogLevel.Debug).ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyListCommand))]
        public class when_listing_local_packages_limiting_output : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
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
                has_expected_message("upgradepackage|1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void should_only_have_messages_related_to_package_information()
            {
                var count = MockLogger.Messages.SelectMany(messageLevel => messageLevel.Value.or_empty_list_if_null()).Count();
                count.ShouldEqual(1);
            }

            [Fact]
            public void should_not_contain_packages_and_versions_with_a_space_between_them()
            {
                has_expected_message("upgradepackage 1.1.0").ShouldBeFalse();
            }

            [Fact]
            public void should_not_contain_a_summary()
            {
                has_expected_message("packages installed").ShouldBeFalse();
            }

            [Fact]
            public void should_not_contain_debugging_messages()
            {
                has_expected_message("Searching for package information", LogLevel.Debug).ShouldBeFalse();
                has_expected_message("Running list with the following filter", LogLevel.Debug).ShouldBeFalse();
                has_expected_message("Start of List", LogLevel.Debug).ShouldBeFalse();
                has_expected_message("End of List", LogLevel.Debug).ShouldBeFalse();
            }
        }
    }
}
