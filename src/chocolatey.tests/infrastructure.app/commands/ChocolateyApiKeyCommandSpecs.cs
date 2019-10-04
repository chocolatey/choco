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

namespace chocolatey.tests.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using Should;

    public class ChocolateyApiKeyCommandSpecs
    {
        public abstract class ChocolateyApiKeyCommandSpecsBase : TinySpec
        {
            protected ChocolateyApiKeyCommand command;
            protected Mock<IChocolateyConfigSettingsService> configSettingsService = new Mock<IChocolateyConfigSettingsService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Sources = "bob";
                command = new ChocolateyApiKeyCommand(configSettingsService.Object);
            }
        }

        public class when_implementing_command_for : ChocolateyApiKeyCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_apikey()
            {
                results.ShouldContain("apikey");
            }

            [Fact]
            public void should_implement_setapikey()
            {
                results.ShouldContain("setapikey");
            }
        }

        public class when_configuring_the_argument_parser : ChocolateyApiKeyCommandSpecsBase
        {
            private OptionSet optionSet;

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
            }

            public override void Because()
            {
                command.configure_argument_parser(optionSet, configuration);
            }

            [Fact]
            public void should_clear_previously_set_Source()
            {
                configuration.Sources.ShouldBeNull();
            }

            [Fact]
            public void should_add_source_to_the_option_set()
            {
                optionSet.Contains("source").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_source_to_the_option_set()
            {
                optionSet.Contains("s").ShouldBeTrue();
            }

            [Fact]
            public void should_add_apikey_to_the_option_set()
            {
                optionSet.Contains("apikey").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_apikey_to_the_option_set()
            {
                optionSet.Contains("k").ShouldBeTrue();
            }

            [Fact]
            public void should_add_remove_to_the_option_set()
            {
                optionSet.Contains("remove").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_remove_to_the_option_set()
            {
                optionSet.Contains("rem").ShouldBeTrue();
            }
        }

        public class when_handling_validation : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void should_throw_when_key_is_set_without_a_source()
            {
                configuration.ApiKeyCommand.Key = "bob";
                configuration.Sources = "";
                var errorred = false;
                Exception error = null;

                try
                {
                    command.handle_validation(configuration);
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
            }

            [Fact]
            public void should_continue_when_source_is_set_but_no_key_set()
            {
                configuration.ApiKeyCommand.Key = "";
                configuration.Sources = "bob";
                command.handle_validation(configuration);
            }

            [Fact]
            public void should_continue_when_both_source_and_key_are_set()
            {
                configuration.ApiKeyCommand.Key = "bob";
                configuration.Sources = "bob";
                command.handle_validation(configuration);
            }

            [Fact]
            public void should_throw_when_key_is_removed_without_a_source()
            {
                configuration.ApiKeyCommand.Remove = true;
                configuration.Sources = "";
                var errorred = false;
                Exception error = null;

                try
                {
                    command.handle_validation(configuration);
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
            }

            [Fact]
            public void should_continue_when_removing_and_source_is_set()
            {
                configuration.ApiKeyCommand.Remove = true;
                configuration.Sources = "bob";
                command.handle_validation(configuration);
            }
        }

        public class when_noop_is_called : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Because()
            {
                command.noop(configuration);
            }

            [Fact]
            public void should_call_service_noop()
            {
                configSettingsService.Verify(c => c.noop(configuration), Times.Once);
            }
        }

        public class when_run_is_called_without_key_set : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                configuration.Sources = "bob";
                configuration.ApiKeyCommand.Key = "";
            }

            public override void Because()
            {
                command.run(configuration);
            }

            [Fact]
            public void should_call_service_get_api_key()
            {
                configSettingsService.Verify(c => c.get_api_key(configuration, It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Once);
            }
        }

        public class when_run_is_called_with_key_set : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                configuration.Sources = "bob";
                configuration.ApiKeyCommand.Key = "bob";
            }

            public override void Because()
            {
                command.run(configuration);
            }

            [Fact]
            public void should_call_service_set_api_key()
            {
                configSettingsService.Verify(c => c.set_api_key(configuration), Times.Once);
            }
        }
    }
}
