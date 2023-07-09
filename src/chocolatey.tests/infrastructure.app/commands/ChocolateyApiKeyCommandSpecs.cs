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

    public class ChocolateyApiKeyCommandSpecs
    {
        [ConcernFor("apikey")]
        public abstract class ChocolateyApiKeyCommandSpecsBase : TinySpec
        {
            protected ChocolateyApiKeyCommand Command;
            protected Mock<IChocolateyConfigSettingsService> ConfigSettingsService = new Mock<IChocolateyConfigSettingsService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Configuration.Sources = "bob";
                Command = new ChocolateyApiKeyCommand(ConfigSettingsService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyApiKeyCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_apikey()
            {
                _results.Should().Contain("apikey");
            }

            [Fact]
            public void Should_implement_setapikey()
            {
                _results.Should().Contain("setapikey");
            }
        }

        public class When_configuring_the_argument_parser : ChocolateyApiKeyCommandSpecsBase
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
            public void Should_clear_previously_set_Source()
            {
                Configuration.Sources.Should().BeNull();
            }

            [Fact]
            public void Should_add_source_to_the_option_set()
            {
                _optionSet.Contains("source").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                _optionSet.Contains("s").Should().BeTrue();
            }

            [Fact]
            public void Should_add_apikey_to_the_option_set()
            {
                _optionSet.Contains("apikey").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_apikey_to_the_option_set()
            {
                _optionSet.Contains("k").Should().BeTrue();
            }
        }

        public class When_validating : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_throw_when_key_is_set_without_a_source()
            {
                Configuration.ApiKeyCommand.Key = "bob";
                Configuration.Sources = "";
                var errored = false;
                Exception error = null;

                try
                {
                    Command.Validate(Configuration);
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_continue_when_source_is_set_but_no_key_set()
            {
                Configuration.ApiKeyCommand.Key = "";
                Configuration.Sources = "bob";
                Command.Validate(Configuration);
            }

            [Fact]
            public void Should_continue_when_both_source_and_key_are_set()
            {
                Configuration.ApiKeyCommand.Key = "bob";
                Configuration.Sources = "bob";
                Command.Validate(Configuration);
            }

            [Fact]
            public void Should_throw_when_key_is_removed_without_a_source()
            {
                Configuration.ApiKeyCommand.Command = ApiKeyCommandType.Remove;
                Configuration.Sources = "";
                var errored = false;
                Exception error = null;

                try
                {
                    Command.Validate(Configuration);
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_continue_when_removing_and_source_is_set()
            {
                Configuration.ApiKeyCommand.Command = ApiKeyCommandType.Remove;
                Configuration.Sources = "bob";
                Command.Validate(Configuration);
            }
        }

        public class When_noop_is_called : ChocolateyApiKeyCommandSpecsBase
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

        public class When_run_is_called_without_key_set : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = "bob";
                Configuration.ApiKeyCommand.Key = "";
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_get_api_key()
            {
                ConfigSettingsService.Verify(c => c.GetApiKey(Configuration, It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Once);
            }
        }

        public class When_run_is_called_with_key_set : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = "bob";
                Configuration.ApiKeyCommand.Key = "bob";
                Configuration.ApiKeyCommand.Command = ApiKeyCommandType.Add;
            }

            public override void Because()
            {
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_set_api_key()
            {
                ConfigSettingsService.Verify(c => c.SetApiKey(Configuration), Times.Once);
            }
        }
    }
}
