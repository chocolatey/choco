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

namespace chocolatey.tests.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using Should;

    public class ChocolateyConfigCommandSpecs
    {
        [ConcernFor("config")]
        public abstract class ChocolateyConfigCommandSpecsBase : TinySpec
        {
            protected ChocolateyConfigCommand command;
            protected Mock<IChocolateyConfigSettingsService> configSettingsService = new Mock<IChocolateyConfigSettingsService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                command = new ChocolateyConfigCommand(configSettingsService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyConfigCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_config()
            {
                results.ShouldContain("config");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyConfigCommandSpecsBase
        {
            private OptionSet optionSet;

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
            }

            public override void Because()
            {
                command.ConfigureArgumentParser(optionSet, configuration);
            }

            [Fact]
            public void Should_add_name_to_the_option_set()
            {
                optionSet.Contains("name").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_value_to_the_option_set()
            {
                optionSet.Contains("value").ShouldBeTrue();
            }
        }

        public class When_noop_is_called : ChocolateyConfigCommandSpecsBase
        {
            public override void Because()
            {
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_call_service_noop()
            {
                configSettingsService.Verify(c => c.DryRun(configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyConfigCommandSpecsBase
        {
            private Action because;

            public override void Because()
            {
                because = () => command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_source_list_when_command_is_list()
            {
                configuration.ConfigCommand.Command = ConfigCommandType.List;
                because();
                configSettingsService.Verify(c => c.ListConfig(configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_source_disable_when_command_is_disable()
            {
                configuration.ConfigCommand.Command = ConfigCommandType.Get;
                because();
                configSettingsService.Verify(c => c.GetConfig(configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_source_enable_when_command_is_enable()
            {
                configuration.ConfigCommand.Command = ConfigCommandType.Set;
                because();
                configSettingsService.Verify(c => c.SetConfig(configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_source_unset_when_command_is_unset()
            {
                configuration.ConfigCommand.Command = ConfigCommandType.Unset;
                because();
                configSettingsService.Verify(c => c.UnsetConfig(configuration), Times.Once);
            }
        }
    }
}
