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
    using System.Collections.Generic;
    using System.Linq;
    using bdddoc.core;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.results;
    using NuGet;
    using Should;

    public class PinScenarios
    {
        public abstract class ScenariosBase : TinySpec
        {
            protected IList<PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected ChocolateyPinCommand Service;

            public override void Context()
            {
                Configuration = Scenario.pin();
                Scenario.reset(Configuration);
                Scenario.add_packages_to_source_location(Configuration, Configuration.Input + "*" + Constants.PackageExtension);
                Scenario.add_packages_to_source_location(Configuration, "installpackage*" + Constants.PackageExtension);
                Scenario.install_package(Configuration, "installpackage", "1.0.0");
                Scenario.install_package(Configuration, "upgradepackage", "1.0.0");
                Scenario.install_package(Configuration, "hasdependency", "1.0.0");

                var commands = NUnitSetup.Container.GetAllInstances<ICommand>();
                Service = commands.Where(
                    (c) =>
                    {
                        var attributes = c.GetType().GetCustomAttributes(typeof(CommandForAttribute), false);
                        return attributes.Cast<CommandForAttribute>().Any(attribute => attribute.CommandName.is_equal_to(Configuration.CommandName));
                    }).FirstOrDefault() as ChocolateyPinCommand;

                Configuration.Sources = ApplicationParameters.PackagesLocation;
                Configuration.ListCommand.LocalOnly = true;
                Configuration.AllVersions = true;
                Configuration.Prerelease = true;
            }
        }

        [Concern(typeof(ChocolateyPinCommand))]
        public class when_listing_pins_with_no_pins : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.list;
            }

            public override void Because()
            {
                MockLogger.reset();
                Service.run(Configuration);
            }

            [Fact]
            public void should_not_contain_list_results()
            {
                MockLogger.contains_message("upgradepackage 1.0.0", LogLevel.Info).ShouldBeFalse();
                MockLogger.contains_message("upgradepackage 1.0.0", LogLevel.Warn).ShouldBeFalse();
                MockLogger.contains_message("upgradepackage 1.0.0", LogLevel.Error).ShouldBeFalse();
            }

            [Fact]
            public void should_not_contain_any_pins_by_default()
            {
                MockLogger.contains_message("upgradepackage|1.0.0").ShouldBeFalse();
            }
        }

        [Concern(typeof(ChocolateyPinCommand))]
        public class when_listing_pins_with_an_existing_pin : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.add;
                Configuration.PinCommand.Name = "upgradepackage";
                Service.run(Configuration);
                Configuration.PinCommand.Command = PinCommandType.list;
            }

            public override void Because()
            {
                MockLogger.reset();
                Service.run(Configuration);
            }

            [Fact]
            public void should_not_contain_list_results()
            {
                MockLogger.contains_message("upgradepackage 1.0.0", LogLevel.Info).ShouldBeFalse();
                MockLogger.contains_message("upgradepackage 1.0.0", LogLevel.Warn).ShouldBeFalse();
                MockLogger.contains_message("upgradepackage 1.0.0", LogLevel.Error).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_existing_pin_messages()
            {
                MockLogger.contains_message("upgradepackage|1.0.0").ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyPinCommand))]
        public class when_listing_pins_with_existing_pins : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.add;
                Configuration.PinCommand.Name = "upgradepackage";
                Service.run(Configuration);
                Configuration.PinCommand.Name = "installpackage";
                Service.run(Configuration);
                Configuration.PinCommand.Command = PinCommandType.list;
            }

            public override void Because()
            {
                MockLogger.reset();
                Service.run(Configuration);
            }

            [Fact]
            public void should_not_contain_list_results()
            {
                MockLogger.contains_message("upgradepackage 1.0.0", LogLevel.Info).ShouldBeFalse();
                MockLogger.contains_message("upgradepackage 1.0.0", LogLevel.Warn).ShouldBeFalse();
                MockLogger.contains_message("upgradepackage 1.0.0", LogLevel.Error).ShouldBeFalse();
            }

            [Fact]
            public void should_contain_a_pin_message_for_each_existing_pin()
            {
                MockLogger.contains_message("installpackage|1.0.0").ShouldBeTrue();
                MockLogger.contains_message("upgradepackage|1.0.0").ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyPinCommand))]
        public class when_setting_a_pin_for_an_installed_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.add;
                Configuration.PinCommand.Name = "upgradepackage";
            }

            public override void Because()
            {
                MockLogger.reset();
                Service.run(Configuration);
            }

            [Fact]
            public void should_contain_success_message()
            {
                MockLogger.contains_message("Successfully added a pin for upgradepackage").ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyPinCommand))]
        public class when_setting_a_pin_for_an_already_pinned_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.add;
                Configuration.PinCommand.Name = "upgradepackage";
                Service.run(Configuration);
            }

            public override void Because()
            {
                MockLogger.reset();
                Service.run(Configuration);
            }

            [Fact]
            public void should_contain_nothing_to_do_message()
            {
                MockLogger.contains_message("Nothing to change. Pin already set or removed.").ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyPinCommand))]
        public class when_setting_a_pin_for_a_non_installed_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.add;
                Configuration.PinCommand.Name = "whatisthis";
            }

            public override void Because()
            {
                MockLogger.reset();
            }

            [ExpectedException(typeof(ApplicationException), ExpectedMessage = "Unable to find package named 'whatisthis' to pin. Please check to ensure it is installed.")]
            [Fact]
            public void should_throw_an_error_about_not_finding_the_package()
            {
                Service.run(Configuration);
            }
        }

        [Concern(typeof(ChocolateyPinCommand))]
        public class when_removing_a_pin_for_a_pinned_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.add;
                Configuration.PinCommand.Name = "upgradepackage";
                Service.run(Configuration);

                Configuration.PinCommand.Command = PinCommandType.remove;
            }

            public override void Because()
            {
                MockLogger.reset();
                Service.run(Configuration);
            }

            [Fact]
            public void should_contain_success_message()
            {
                MockLogger.contains_message("Successfully removed a pin for upgradepackage").ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyPinCommand))]
        public class when_removing_a_pin_for_an_unpinned_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.remove;
                Configuration.PinCommand.Name = "upgradepackage";
            }

            public override void Because()
            {
                MockLogger.reset();
                Service.run(Configuration);
            }

            [Fact]
            public void should_contain_nothing_to_do_message()
            {
                MockLogger.contains_message("Nothing to change. Pin already set or removed.").ShouldBeTrue();
            }
        }

        [Concern(typeof(ChocolateyPinCommand))]
        public class when_removing_a_pin_for_a_non_installed_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.remove;
                Configuration.PinCommand.Name = "whatisthis";
            }

            public override void Because()
            {
                MockLogger.reset();
            }

            [ExpectedException(typeof(ApplicationException), ExpectedMessage = "Unable to find package named 'whatisthis' to pin. Please check to ensure it is installed.")]
            [Fact]
            public void should_throw_an_error_about_not_finding_the_package()
            {
                Service.run(Configuration);
            }
        }
    }
}
