// Copyright © 2023-Present Chocolatey Software, Inc
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
    using Should;

    public class ListScenarios
    {
        [ConcernFor("list")]
        public abstract class ScenariosBase : TinySpec
        {
            protected IList<PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected IChocolateyPackageService Service;

            public override void Context()
            {
                Configuration = Scenario.list();
                Scenario.reset(Configuration);
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "installpackage*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Scenario.install_package(Configuration, "upgradepackage", "1.0.0");

                Service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
                Configuration.ListCommand.LocalOnly = true;
            }
        }

        public class When_listing_local_packages : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.0.0").ShouldBeTrue(userMessage: "Warnings: " + string.Join("\n", MockLogger.Messages["Info"]));
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.0.0").ShouldBeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages installed").ShouldBeTrue();
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

        public class When_listing_local_packages_with_id_only : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.ListCommand.IdOnly = true;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_package_name()
            {
                MockLogger.ContainsMessage("upgradepackage").ShouldBeTrue();
            }

            [Fact]
            public void Should_not_contain_any_version_number()
            {
                MockLogger.ContainsMessage(".0").ShouldBeFalse();
            }
        }

        public class When_listing_local_packages_limiting_output : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.RegularOutput = false;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.0.0").ShouldBeTrue();
            }

            [Fact]
            public void Should_only_have_messages_related_to_package_information()
            {
                var count = MockLogger.MessagesFor(LogLevel.Info).OrEmpty().Count();
                count.ShouldEqual(2);
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.0.0").ShouldBeFalse();
            }

            [Fact]
            public void Should_not_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages installed").ShouldBeFalse();
            }

            [Fact]
            public void Should_not_contain_debugging_messages()
            {
                MockLogger.ContainsMessage("Searching for package information", LogLevel.Debug).ShouldBeFalse();
                MockLogger.ContainsMessage("Running list with the following filter", LogLevel.Debug).ShouldBeFalse();
                MockLogger.ContainsMessage("Start of List", LogLevel.Debug).ShouldBeFalse();
                MockLogger.ContainsMessage("End of List", LogLevel.Debug).ShouldBeFalse();
            }
        }

        public class When_listing_local_packages_limiting_output_with_id_only : ScenariosBase
        {
            public override void Context()
            {
                base.Context();

                Configuration.ListCommand.IdOnly = true;
                Configuration.RegularOutput = false;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_id()
            {
                MockLogger.ContainsMessage("upgradepackage").ShouldBeTrue();
            }

            [Fact]
            public void Should_not_contain_any_version_number()
            {
                MockLogger.ContainsMessage(".0").ShouldBeFalse();
            }

            [Fact]
            public void Should_not_contain_pipe()
            {
                MockLogger.ContainsMessage("|").ShouldBeFalse();
            }
        }

        public class When_listing_local_packages_with_uppercase_id_package_installed : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Scenario.add_packages_to_source_location(Configuration, "UpperCase" + "*" + NuGetConstants.PackageExtension);
                Scenario.install_package(Configuration, "UpperCase", "1.1.0");
            }

            public override void Because()
            {
                MockLogger.Reset();
                Results = Service.List(Configuration).ToList();
            }

            [Fact]
            public void Should_contain_packages_and_versions_with_a_space_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage 1.0.0").ShouldBeTrue();
            }

            [Fact]
            public void Should_contain_uppercase_id_package()
            {
                MockLogger.ContainsMessage("UpperCase 1.1.0").ShouldBeTrue();
            }

            [Fact]
            public void Should_not_contain_packages_and_versions_with_a_pipe_between_them()
            {
                MockLogger.ContainsMessage("upgradepackage|1.0.0").ShouldBeFalse();
            }

            [Fact]
            public void Should_contain_a_summary()
            {
                MockLogger.ContainsMessage("packages installed").ShouldBeTrue();
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
    }
}
