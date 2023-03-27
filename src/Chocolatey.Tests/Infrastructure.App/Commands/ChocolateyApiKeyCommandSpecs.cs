﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace Chocolatey.Tests.Infrastructure.App.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Chocolatey.Infrastructure.App.Attributes;
    using Chocolatey.Infrastructure.App.Commands;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Domain;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.CommandLine;
    using Moq;
    using Should;

    public class ChocolateyApiKeyCommandSpecs
    {
        [ConcernFor("apikey")]
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

        public class When_implementing_command_for : ChocolateyApiKeyCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_apikey()
            {
                results.ShouldContain("apikey");
            }

            [Fact]
            public void Should_implement_setapikey()
            {
                results.ShouldContain("setapikey");
            }
        }

        public class When_configuring_the_argument_parser : ChocolateyApiKeyCommandSpecsBase
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
            public void Should_clear_previously_set_Source()
            {
                configuration.Sources.ShouldBeNull();
            }

            [Fact]
            public void Should_add_source_to_the_option_set()
            {
                optionSet.Contains("source").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_source_to_the_option_set()
            {
                optionSet.Contains("s").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_apikey_to_the_option_set()
            {
                optionSet.Contains("apikey").ShouldBeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_apikey_to_the_option_set()
            {
                optionSet.Contains("k").ShouldBeTrue();
            }
        }

        public class When_handling_validation : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_throw_when_key_is_set_without_a_source()
            {
                configuration.ApiKeyCommand.Key = "bob";
                configuration.Sources = "";
                var errored = false;
                Exception error = null;

                try
                {
                    command.Validate(configuration);
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
            }

            [Fact]
            public void Should_continue_when_source_is_set_but_no_key_set()
            {
                configuration.ApiKeyCommand.Key = "";
                configuration.Sources = "bob";
                command.Validate(configuration);
            }

            [Fact]
            public void Should_continue_when_both_source_and_key_are_set()
            {
                configuration.ApiKeyCommand.Key = "bob";
                configuration.Sources = "bob";
                command.Validate(configuration);
            }

            [Fact]
            public void Should_throw_when_key_is_removed_without_a_source()
            {
                configuration.ApiKeyCommand.Command = ApiKeyCommandType.Remove;
                configuration.Sources = "";
                var errored = false;
                Exception error = null;

                try
                {
                    command.Validate(configuration);
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
            }

            [Fact]
            public void Should_continue_when_removing_and_source_is_set()
            {
                configuration.ApiKeyCommand.Command = ApiKeyCommandType.Remove;
                configuration.Sources = "bob";
                command.Validate(configuration);
            }
        }

        public class When_noop_is_called : ChocolateyApiKeyCommandSpecsBase
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

        public class When_run_is_called_without_key_set : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                configuration.Sources = "bob";
                configuration.ApiKeyCommand.Key = "";
            }

            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_get_api_key()
            {
                configSettingsService.Verify(c => c.GetApiKey(configuration, It.IsAny<Action<ConfigFileApiKeySetting>>()), Times.Once);
            }
        }

        public class When_run_is_called_with_key_set : ChocolateyApiKeyCommandSpecsBase
        {
            public override void Context()
            {
                base.Context();
                configuration.Sources = "bob";
                configuration.ApiKeyCommand.Key = "bob";
                configuration.ApiKeyCommand.Command = ApiKeyCommandType.Add;
            }

            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_set_api_key()
            {
                configSettingsService.Verify(c => c.SetApiKey(configuration), Times.Once);
            }
        }
    }
}
