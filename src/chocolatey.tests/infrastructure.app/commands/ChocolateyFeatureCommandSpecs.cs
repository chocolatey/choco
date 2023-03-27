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

    public class ChocolateyFeatureCommandSpecs
    {
        [ConcernFor("feature")]
        public abstract class ChocolateyFeatureCommandSpecsBase : TinySpec
        {
            protected ChocolateyFeatureCommand command;
            protected Mock<IChocolateyConfigSettingsService> configSettingsService = new Mock<IChocolateyConfigSettingsService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                configuration.Sources = "https://localhost/somewhere/out/there";
                command = new ChocolateyFeatureCommand(configSettingsService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateyFeatureCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_feature()
            {
                results.ShouldContain("feature");
            }

            [Fact]
            public void Should_implement_features()
            {
                results.ShouldContain("features");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyFeatureCommandSpecsBase
        {
            private OptionSet optionSet;

            public override void Context()
            {
                base.Context();
                optionSet = new OptionSet();
                configuration.Sources = "https://localhost/somewhere/out/there";
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
            public void Should_add_short_version_of_name_to_the_option_set()
            {
                optionSet.Contains("n").ShouldBeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateyFeatureCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private Action because;

            public override void Because()
            {
                because = () => command.ParseAdditionalArguments(unparsedArgs, configuration);
            }

            public void Reset()
            {
                unparsedArgs.Clear();
                configSettingsService.ResetCalls();
            }

            [Fact]
            public void Should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("list");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.List);
            }

            [Fact]
            public void Should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                Reset();
                unparsedArgs.Add("wtf");
                unparsedArgs.Add("bbq");
                var errored = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
                error.Message.ShouldContain("A single features command must be listed");
            }

            [Fact]
            public void Should_accept_enable_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("enable");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.Enable);
            }

            [Fact]
            public void Should_accept_disable_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("disable");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.Disable);
            }

            [Fact]
            public void Should_set_unrecognized_values_to_list_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("wtf");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.List);
            }

            [Fact]
            public void Should_default_to_list_as_the_subcommand()
            {
                Reset();
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.List);
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string()
            {
                Reset();
                unparsedArgs.Add(" ");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.List);
            }
        }

        public class When_validating : ChocolateyFeatureCommandSpecsBase
        {
            private Action because;

            public override void Because()
            {
                because = () => command.Validate(configuration);
            }

            [Fact]
            public void Should_throw_when_command_is_not_list_and_name_is_not_set()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.Unknown;
                configuration.FeatureCommand.Name = "";
                var errored = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
                error.Message.ShouldEqual("When specifying the subcommand '{0}', you must also specify --name.".FormatWith(configuration.FeatureCommand.Command.ToStringSafe().ToLower()));
            }

            [Fact]
            public void Should_continue_when_command_is_list_and_name_is_not_set()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.List;
                configuration.SourceCommand.Name = "";
                because();
            }

            [Fact]
            public void Should_continue_when_command_is_not_list_and_name_is_set()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.List;
                configuration.SourceCommand.Name = "bob";
                because();
            }
        }

        public class When_noop_is_called : ChocolateyFeatureCommandSpecsBase
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

        public class When_run_is_called : ChocolateyFeatureCommandSpecsBase
        {
            private Action _because;

            public override void Because()
            {
                _because = () => command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_source_list_when_command_is_list()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.List;
                _because();
                configSettingsService.Verify(c => c.ListFeatures(configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_source_disable_when_command_is_disable()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.Disable;
                _because();
                configSettingsService.Verify(c => c.DisableFeature(configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_source_enable_when_command_is_enable()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.Enable;
                _because();
                configSettingsService.Verify(c => c.EnableFeature(configuration), Times.Once);
            }
        }
    }
}
