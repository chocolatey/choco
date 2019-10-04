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
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commandline;
    using Moq;
    using Should;

    public class ChocolateyFeatureCommandSpecs
    {
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

        public class when_implementing_command_for : ChocolateyFeatureCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_feature()
            {
                results.ShouldContain("feature");
            }

            [Fact]
            public void should_implement_features()
            {
                results.ShouldContain("features");
            }
        }

        public class when_configurating_the_argument_parser : ChocolateyFeatureCommandSpecsBase
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
                command.configure_argument_parser(optionSet, configuration);
            }

            [Fact]
            public void should_add_name_to_the_option_set()
            {
                optionSet.Contains("name").ShouldBeTrue();
            }

            [Fact]
            public void should_add_short_version_of_name_to_the_option_set()
            {
                optionSet.Contains("n").ShouldBeTrue();
            }
        }

        public class when_handling_additional_argument_parsing : ChocolateyFeatureCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private Action because;

            public override void Because()
            {
                because = () => command.handle_additional_argument_parsing(unparsedArgs, configuration);
            }

            public void reset()
            {
                unparsedArgs.Clear();
                configSettingsService.ResetCalls();
            }

            [Fact]
            public void should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("list");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.list);
            }

            [Fact]
            public void should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                reset();
                unparsedArgs.Add("wtf");
                unparsedArgs.Add("bbq");
                var errorred = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
                error.Message.ShouldContain("A single features command must be listed");
            }

            [Fact]
            public void should_accept_enable_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("enable");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.enable);
            }

            [Fact]
            public void should_accept_disable_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("disable");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.disable);
            }

            [Fact]
            public void should_set_unrecognized_values_to_list_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("wtf");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.list);
            }

            [Fact]
            public void should_default_to_list_as_the_subcommand()
            {
                reset();
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.list);
            }

            [Fact]
            public void should_handle_passing_in_an_empty_string()
            {
                reset();
                unparsedArgs.Add(" ");
                because();

                configuration.FeatureCommand.Command.ShouldEqual(FeatureCommandType.list);
            }
        }

        public class when_handling_validation : ChocolateyFeatureCommandSpecsBase
        {
            private Action because;

            public override void Because()
            {
                because = () => command.handle_validation(configuration);
            }

            [Fact]
            public void should_throw_when_command_is_not_list_and_name_is_not_set()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.unknown;
                configuration.FeatureCommand.Name = "";
                var errorred = false;
                Exception error = null;

                try
                {
                    because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.ShouldBeTrue();
                error.ShouldNotBeNull();
                error.ShouldBeType<ApplicationException>();
                error.Message.ShouldEqual("When specifying the subcommand '{0}', you must also specify --name.".format_with(configuration.FeatureCommand.Command.to_string()));
            }

            [Fact]
            public void should_continue_when_command_is_list_and_name_is_not_set()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.list;
                configuration.SourceCommand.Name = "";
                because();
            }

            [Fact]
            public void should_continue_when_command_is_not_list_and_name_is_set()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.list;
                configuration.SourceCommand.Name = "bob";
                because();
            }
        }

        public class when_noop_is_called : ChocolateyFeatureCommandSpecsBase
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

        public class when_run_is_called : ChocolateyFeatureCommandSpecsBase
        {
            private Action because;

            public override void Because()
            {
                because = () => command.run(configuration);
            }

            [Fact]
            public void should_call_service_source_list_when_command_is_list()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.list;
                because();
                configSettingsService.Verify(c => c.feature_list(configuration), Times.Once);
            }

            [Fact]
            public void should_call_service_source_disable_when_command_is_disable()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.disable;
                because();
                configSettingsService.Verify(c => c.feature_disable(configuration), Times.Once);
            }

            [Fact]
            public void should_call_service_source_enable_when_command_is_enable()
            {
                configuration.FeatureCommand.Command = FeatureCommandType.enable;
                because();
                configSettingsService.Verify(c => c.feature_enable(configuration), Times.Once);
            }
        }
    }
}
