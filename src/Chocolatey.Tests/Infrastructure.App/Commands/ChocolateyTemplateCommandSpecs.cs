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
    using Chocolatey.Infrastructure.Filesystem;
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

            public void Reset()
            {
                templateService.ResetCalls();
            }
        }

        public class When_implementing_command_for : ChocolateyTemplateCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_help()
            {
                results.ShouldContain("template");
                results.ShouldContain("templates");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyTemplateCommandSpecsBase
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
            public void Should_add_short_version_of_name_to_the_option_set()
            {
                optionSet.Contains("n").ShouldBeTrue();
            }
        }


        public class When_handling_additional_argument_parsing : ChocolateyTemplateCommandSpecsBase
        {
            private readonly IList<string> unparsedArgs = new List<string>();
            private Action because;

            public override void Because()
            {
                because = () => command.ParseAdditionalArguments(unparsedArgs, configuration);
            }

            public new void Reset()
            {
                configuration.TemplateCommand.Name = string.Empty;
                configuration.TemplateCommand.Command = TemplateCommandType.Unknown;
                unparsedArgs.Clear();
                base.Reset();
            }

            [Fact]
            public void Should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("list");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.List);
            }

            [Fact]
            public void Should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                Reset();
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
            public void Should_accept_list_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("list");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.List);
            }

            [Fact]
            public void Should_accept_uppercase_list_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("LIST");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.List);
            }

            [Fact]
            public void Should_accept_info_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("info");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.Info);
            }

            [Fact]
            public void Should_accept_uppercase_info_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("INFO");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.Info);
            }

            [Fact]
            public void Should_set_unrecognized_values_to_list_as_the_subcommand()
            {
                Reset();
                unparsedArgs.Add("badcommand");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.List);
            }

            [Fact]
            public void Should_default_to_list_as_the_subcommand()
            {
                Reset();
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.List);
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string()
            {
                Reset();
                unparsedArgs.Add(" ");
                because();

                configuration.TemplateCommand.Command.ShouldEqual(TemplateCommandType.List);
            }
        }

        public class When_handling_validation : ChocolateyTemplateCommandSpecsBase
        {
            private Action because;

            public override void Because()
            {
                because = () => command.Validate(configuration);
            }

            [Fact]
            public void Should_continue_when_command_is_list_and_name_is_set()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.List;
                configuration.TemplateCommand.Name = "bob";
                because();
            }

            [Fact]
            public void Should_continue_when_command_is_list_and_name_is_not_set()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.List;
                configuration.TemplateCommand.Name = "";
                because();
            }

            [Fact]
            public void Should_throw_when_command_is_info_and_name_is_not_set()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.Info;
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
                error.Message.ShouldEqual("When specifying the subcommand '{0}', you must also specify --name.".FormatWith(configuration.TemplateCommand.Command.ToStringChecked()));
            }

            [Fact]
            public void Should_continue_when_command_info_and_name_is_set()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.Info;
                configuration.TemplateCommand.Name = "bob";
                because();
            }
        }

        public class When_noop_is_called : ChocolateyTemplateCommandSpecsBase
        {
            public override void Because()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.List;
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_call_service_list_noop()
            {
                templateService.Verify(c => c.ListDryRun(configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyTemplateCommandSpecsBase
        {
            public override void Because()
            {
                configuration.TemplateCommand.Command = TemplateCommandType.List;
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_service_list()
            {
                templateService.Verify(c => c.List(configuration), Times.Once);
            }
        }
    }
}
