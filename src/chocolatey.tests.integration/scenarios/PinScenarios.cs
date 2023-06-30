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
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.results;

    using NUnit.Framework;

    using NuGet.Configuration;
    using FluentAssertions;
    using Moq;

    public class PinScenarios
    {
        [ConcernFor("pin")]
        public abstract class ScenariosBase : TinySpec
        {
            protected IList<PackageResult> Results;
            protected ChocolateyConfiguration Configuration;
            protected ChocolateyPinCommand Service;

            public override void Context()
            {
                Configuration = Scenario.Pin();
                Scenario.Reset(Configuration);
                Scenario.AddPackagesToSourceLocation(Configuration, Configuration.Input + "*" + NuGetConstants.PackageExtension);
                Scenario.AddPackagesToSourceLocation(Configuration, "installpackage*" + NuGetConstants.PackageExtension);
                Scenario.InstallPackage(Configuration, "installpackage", "1.0.0");
                Scenario.InstallPackage(Configuration, "upgradepackage", "1.0.0");
                Scenario.InstallPackage(Configuration, "hasdependency", "1.0.0");

                var commands = NUnitSetup.Container.GetAllInstances<ICommand>();
                Service = commands.Where(
                    (c) =>
                    {
                        var attributes = c.GetType().GetCustomAttributes(typeof(CommandForAttribute), false);
                        return attributes.Cast<CommandForAttribute>().Any(attribute => attribute.CommandName.IsEqualTo(Configuration.CommandName));
                    }).FirstOrDefault() as ChocolateyPinCommand;

                Configuration.Sources = ApplicationParameters.PackagesLocation;
                Configuration.ListCommand.LocalOnly = true;
                Configuration.AllVersions = true;
                Configuration.Prerelease = true;
            }
        }

        public class When_listing_pins_with_no_pins : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.List;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Service.Run(Configuration);
            }

            [Fact]
            public void Should_not_contain_list_results()
            {
                MockLogger.Messages.Should()
                    .NotContainKeys(new string[]
                        { LogLevel.Info.ToStringSafe(), LogLevel.Warn.ToStringSafe(), LogLevel.Error.ToStringSafe() });
            }

            [Fact]
            public void Should_not_contain_any_pins_by_default()
            {
                MockLogger.ContainsMessage("upgradepackage|1.0.0").Should().BeFalse();
            }
        }

        public class When_listing_pins_with_an_existing_pin : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.Add;
                Configuration.PinCommand.Name = "upgradepackage";
                Service.Run(Configuration);
                Configuration.PinCommand.Command = PinCommandType.List;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Service.Run(Configuration);
            }

            [Fact]
            public void Should_not_contain_list_results()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.0.0"));
                MockLogger.Messages.Should()
                    .NotContainKeys(new string[]
                        { LogLevel.Warn.ToStringSafe(), LogLevel.Error.ToStringSafe() });
            }

            [Fact]
            public void Should_contain_existing_pin_messages()
            {
                MockLogger.ContainsMessage("upgradepackage|1.0.0").Should().BeTrue();
            }
        }

        public class When_listing_pins_with_existing_pins : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.Add;
                Configuration.PinCommand.Name = "upgradepackage";
                Service.Run(Configuration);
                Configuration.PinCommand.Name = "installpackage";
                Service.Run(Configuration);
                Configuration.PinCommand.Command = PinCommandType.List;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Service.Run(Configuration);
            }

            [Fact]
            public void Should_not_contain_list_results()
            {
                MockLogger.Messages.Should().ContainKey(LogLevel.Info.ToStringSafe())
                    .WhoseValue.Should().NotContain(m => m.Contains("upgradepackage 1.0.0"));
                MockLogger.Messages.Should()
                    .NotContainKeys(new string[]
                        { LogLevel.Warn.ToStringSafe(), LogLevel.Error.ToStringSafe() });
            }

            [Fact]
            public void Should_contain_a_pin_message_for_each_existing_pin()
            {
                MockLogger.ContainsMessage("installpackage|1.0.0").Should().BeTrue();
                MockLogger.ContainsMessage("upgradepackage|1.0.0").Should().BeTrue();
            }
        }

        public class When_setting_a_pin_for_an_installed_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.Add;
                Configuration.PinCommand.Name = "upgradepackage";
            }

            public override void Because()
            {
                MockLogger.Reset();
                Service.Run(Configuration);
            }

            [Fact]
            public void Should_contain_success_message()
            {
                MockLogger.ContainsMessage("Successfully added a pin for upgradepackage").Should().BeTrue();
            }
        }

        public class When_setting_a_pin_for_an_already_pinned_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.Add;
                Configuration.PinCommand.Name = "upgradepackage";
                Service.Run(Configuration);
            }

            public override void Because()
            {
                MockLogger.Reset();
                Service.Run(Configuration);
            }

            [Fact]
            public void Should_contain_nothing_to_do_message()
            {
                MockLogger.ContainsMessage("Nothing to change. Pin already set or removed.").Should().BeTrue();
            }
        }

        public class When_setting_a_pin_for_a_non_installed_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.Add;
                Configuration.PinCommand.Name = "whatisthis";
            }

            public override void Because()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_throw_an_error_about_not_finding_the_package()
            {
                Assert.That(() => Service.Run(Configuration),
                    Throws.TypeOf<ApplicationException>()
                    .And.Message.EqualTo("Unable to find package named 'whatisthis' to pin. Please check to ensure it is installed."));
            }
        }

        public class When_removing_a_pin_for_a_pinned_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.Add;
                Configuration.PinCommand.Name = "upgradepackage";
                Service.Run(Configuration);

                Configuration.PinCommand.Command = PinCommandType.Remove;
            }

            public override void Because()
            {
                MockLogger.Reset();
                Service.Run(Configuration);
            }

            [Fact]
            public void Should_contain_success_message()
            {
                MockLogger.ContainsMessage("Successfully removed a pin for upgradepackage").Should().BeTrue();
            }
        }

        public class When_removing_a_pin_for_an_unpinned_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.Remove;
                Configuration.PinCommand.Name = "upgradepackage";
            }

            public override void Because()
            {
                MockLogger.Reset();
                Service.Run(Configuration);
            }

            [Fact]
            public void Should_contain_nothing_to_do_message()
            {
                MockLogger.ContainsMessage("Nothing to change. Pin already set or removed.").Should().BeTrue();
            }
        }

        public class When_removing_a_pin_for_a_non_installed_package : ScenariosBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.PinCommand.Command = PinCommandType.Remove;
                Configuration.PinCommand.Name = "whatisthis";
            }

            public override void Because()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Should_throw_an_error_about_not_finding_the_package()
            {
                Assert.That(() => Service.Run(Configuration),
                    Throws.TypeOf<ApplicationException>()
                    .And.Message.EqualTo("Unable to find package named 'whatisthis' to pin. Please check to ensure it is installed."));
            }
        }
    }
}
