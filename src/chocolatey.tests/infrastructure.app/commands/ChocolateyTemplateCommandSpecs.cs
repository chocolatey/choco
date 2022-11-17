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
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using Should;

    public class ChocolateyTemplateCommandSpecs
    {
        [ConcernFor("template")]
        public abstract class ChocolateyTemplateCommandSpecsBase : TinySpec
        {
            protected ChocolateyTemplateCommand command;
            protected Mock<ITemplateService> templateService = new Mock<ITemplateService>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                command = new ChocolateyTemplateCommand(templateService.Object);
            }

            public void reset()
            {
                templateService.ResetCalls();
            }
        }

        public class when_implementing_command_for : ChocolateyTemplateCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void should_implement_help()
            {
                results.ShouldContain("template");
                results.ShouldContain("templates");
            }
        }

        public class when_configurating_the_argument_parser : ChocolateyTemplateCommandSpecsBase
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


        public class when_handling_additional_argument_parsing : ChocolateyTemplateCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private Action because;

            public override void Because()
            {
                because = () => command.handle_additional_argument_parsing(unparsedArgs, configuration);
            }

            public new void reset()
            {
                configuration.TemplateCommand.Name = string.Empty;
                configuration.TemplateCommand.Command = TemplateCommandType.unknown;
                unparsedArgs.Clear();
                base.reset();
            }

            [Fact]
            public void should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("list");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.list);
            }

            [Fact]
            public void should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                reset();
                unparsedArgs.Add("badcommand");
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
                error.Message.ShouldContain("A single template command must be listed");
            }

            [Fact]
            public void should_accept_list_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("list");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.list);
            }

            [Fact]
            public void should_accept_uppercase_list_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("LIST");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.list);
            }

            [Fact]
            public void should_accept_info_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("info");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.info);
            }

            [Fact]
            public void should_accept_uppercase_info_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("INFO");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.info);
            }

            [Fact]
            public void should_set_unrecognized_values_to_list_as_the_subcommand()
            {
                reset();
                unparsedArgs.Add("badcommand");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.list);
            }

            [Fact]
            public void should_default_to_list_as_the_subcommand()
            {
                reset();
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.list);
            }

            [Fact]
            public void should_handle_passing_in_an_empty_string()
            {
                reset();
                unparsedArgs.Add(" ");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.list);
            }
        }

        public class when_handling_validation : ChocolateyTemplateCommandSpecsBase
        {
            private Action because;

            public override void Because()
            {
                because = () => command.handle_validation(configuration);
            }

            [Fact]
            public void should_continue_when_command_is_list_and_name_is_set()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.list;
                configuration.TemplateCommand.Name = "bob";
                because();
            }

            [Fact]
            public void should_continue_when_command_is_list_and_name_is_not_set()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.list;
                configuration.TemplateCommand.Name = "";
                because();
            }

            [Fact]
            public void should_throw_when_command_is_info_and_name_is_not_set()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.info;
                configuration.TemplateCommand.Name = "";
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
                error.Message.ShouldEqual("When specifying the subcommand '{0}', you must also specify --name.".format_with(configuration.TemplateCommand.Command.to_string()));
            }

            [Fact]
            public void should_continue_when_command_info_and_name_is_set()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.info;
                configuration.TemplateCommand.Name = "bob";
                because();
            }
        }

        public class when_noop_is_called : ChocolateyTemplateCommandSpecsBase
        {
            public override void Because()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.list;
                command.noop(configuration);
            }

            [Fact]
            public void should_call_service_list_noop()
            {
                templateService.Verify(c => c.list_noop(configuration), Times.Once);
            }
        }

        public class when_run_is_called : ChocolateyTemplateCommandSpecsBase
        {
            public override void Because()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.list;
                command.run(configuration);
            }

            [Fact]
            public void should_call_service_list()
            {
                templateService.Verify(c => c.list(configuration), Times.Once);
            }
        }
    }
}
