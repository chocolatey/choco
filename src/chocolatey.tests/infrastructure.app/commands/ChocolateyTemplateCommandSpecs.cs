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
    using FluentAssertions;

    public class ChocolateyTemplateCommandSpecs
    {
        [ConcernFor("template")]
        public abstract class ChocolateyTemplateCommandSpecsBase : TinySpec
        {
            protected ChocolateyTemplateCommand Command;
            protected Mock<ITemplateService> TemplateService = new Mock<ITemplateService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateyTemplateCommand(TemplateService.Object);
            }

            public void Reset()
            {
                TemplateService.ResetCalls();
            }
        }

        public class When_implementing_command_for : ChocolateyTemplateCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_help()
            {
                _results.Should().Contain("template");
                _results.Should().Contain("templates");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateyTemplateCommandSpecsBase
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
            public void Should_add_short_version_of_name_to_the_option_set()
            {
                _optionSet.Contains("n").Should().BeTrue();
            }
        }


        public class When_handling_additional_argument_parsing : ChocolateyTemplateCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private Action _because;

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            public new void Reset()
            {
                Configuration.TemplateCommand.Name = string.Empty;
                Configuration.TemplateCommand.Command = TemplateCommandType.Unknown;
                _unparsedArgs.Clear();
                base.Reset();
            }

            [Fact]
            public void Should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("list");
                _because();

                Configuration.TemplateCommand.Command.Should().Be(TemplateCommandType.List);
            }

            [Fact]
            public void Should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                Reset();
                _unparsedArgs.Add("badcommand");
                _unparsedArgs.Add("bbq");
                var errorred = false;
                Exception error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Contain("A single template command must be listed");
            }

            [Fact]
            public void Should_accept_list_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("list");
                _because();

                Configuration.TemplateCommand.Command.Should().Be(TemplateCommandType.List);
            }

            [Fact]
            public void Should_accept_uppercase_list_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("LIST");
                _because();

                Configuration.TemplateCommand.Command.Should().Be(TemplateCommandType.List);
            }

            [Fact]
            public void Should_accept_info_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("info");
                _because();

                Configuration.TemplateCommand.Command.Should().Be(TemplateCommandType.Info);
            }

            [Fact]
            public void Should_accept_uppercase_info_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("INFO");
                _because();

                Configuration.TemplateCommand.Command.Should().Be(TemplateCommandType.Info);
            }

            [Fact]
            public void Should_set_unrecognized_values_to_list_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("badcommand");
                _because();

                Configuration.TemplateCommand.Command.Should().Be(TemplateCommandType.List);
            }

            [Fact]
            public void Should_default_to_list_as_the_subcommand()
            {
                Reset();
                _because();

                Configuration.TemplateCommand.Command.Should().Be(TemplateCommandType.List);
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string()
            {
                Reset();
                _unparsedArgs.Add(" ");
                _because();

                Configuration.TemplateCommand.Command.Should().Be(TemplateCommandType.List);
            }
        }

        public class When_validating : ChocolateyTemplateCommandSpecsBase
        {
            private Action _because;

            public override void Because()
            {
                _because = () => Command.Validate(Configuration);
            }

            [Fact]
            public void Should_continue_when_command_is_list_and_name_is_set()
            {
                Configuration.TemplateCommand.Command = TemplateCommandType.List;
                Configuration.TemplateCommand.Name = "bob";
                _because();
            }

            [Fact]
            public void Should_continue_when_command_is_list_and_name_is_not_set()
            {
                Configuration.TemplateCommand.Command = TemplateCommandType.List;
                Configuration.TemplateCommand.Name = "";
                _because();
            }

            [Fact]
            public void Should_throw_when_command_is_info_and_name_is_not_set()
            {
                Configuration.TemplateCommand.Command = TemplateCommandType.Info;
                Configuration.TemplateCommand.Name = "";
                var errorred = false;
                Exception error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    errorred = true;
                    error = ex;
                }

                errorred.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Be("When specifying the subcommand '{0}', you must also specify --name.".FormatWith(Configuration.TemplateCommand.Command.ToStringSafe().ToLower()));
            }

            [Fact]
            public void Should_continue_when_command_info_and_name_is_set()
            {
                Configuration.TemplateCommand.Command = TemplateCommandType.Info;
                Configuration.TemplateCommand.Name = "bob";
                _because();
            }
        }

        public class When_noop_is_called : ChocolateyTemplateCommandSpecsBase
        {
            public override void Because()
            {
                Configuration.TemplateCommand.Command = TemplateCommandType.List;
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_call_service_list_noop()
            {
                TemplateService.Verify(c => c.ListDryRun(Configuration), Times.Once);
            }
        }

        public class When_run_is_called : ChocolateyTemplateCommandSpecsBase
        {
            public override void Because()
            {
                Configuration.TemplateCommand.Command = TemplateCommandType.List;
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_list()
            {
                TemplateService.Verify(c => c.List(Configuration), Times.Once);
            }
        }
    }
}
