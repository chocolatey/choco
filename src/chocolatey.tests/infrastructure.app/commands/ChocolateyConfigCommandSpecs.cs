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
    using FluentAssertions;

    public class ChocolateyConfigCommandSpecs
    {
        [ConcernFor("config")]
        public abstract class ChocolateyConfigCommandSpecsBase : TinySpec
        {
            protected ChocolateyConfigCommand Command;
            protected Mock<IChocolateyConfigSettingsService> ConfigSettingsService = new Mock<IChocolateyConfigSettingsService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateyConfigCommand(ConfigSettingsService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyConfigCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_config()
            {
                _results.Should().Contain("config");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyConfigCommandSpecsBase
        {
            private OptionSet _optionSet;

            public override void Context()
            {
                base.Context();
                _optionSet = new OptionSet();
            }

            public override void Because()
            {
                Command.ConfigureArgumentParser(_optionSet, Configuration);
            }

            [Fact]
            public void Should_add_name_to_the_option_set()
            {
                _optionSet.Contains("name").Should().BeTrue();
            }

            [Fact]
            public void Should_add_value_to_the_option_set()
            {
                _optionSet.Contains("value").Should().BeTrue();
            }
        }

        public class When_noop_is_called : ChocolateyConfigCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_call_service_noop()
            {
                ConfigSettingsService.Verify(c => c.DryRun(Configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyConfigCommandSpecsBase
        {
            private Action _because;

            public override void Because()
            {
                _because = () => Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_config_list_when_command_is_list()
            {
                Configuration.ConfigCommand.Command = ConfigCommandType.List;
                _because();
                ConfigSettingsService.Verify(c => c.ListConfig(Configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_config_get_when_command_is_get()
            {
                Configuration.ConfigCommand.Command = ConfigCommandType.Get;
                _because();
                ConfigSettingsService.Verify(c => c.GetConfig(Configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_config_set_when_command_is_set()
            {
                Configuration.ConfigCommand.Command = ConfigCommandType.Set;
                _because();
                ConfigSettingsService.Verify(c => c.SetConfig(Configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_config_unset_when_command_is_unset()
            {
                Configuration.ConfigCommand.Command = ConfigCommandType.Unset;
                _because();
                ConfigSettingsService.Verify(c => c.UnsetConfig(Configuration), Times.Once);
            }
        }
    }
}
