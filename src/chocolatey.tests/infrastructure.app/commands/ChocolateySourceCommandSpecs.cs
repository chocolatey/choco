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

    public class ChocolateySourceCommandSpecs
    {
        [ConcernFor("source")]
        public abstract class ChocolateySourceCommandSpecsBase : TinySpec
        {
            protected ChocolateySourceCommand Command;
            protected Mock<IChocolateyConfigSettingsService> ConfigSettingsService = new Mock<IChocolateyConfigSettingsService>();
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Configuration.Sources = "https://localhost/somewhere/out/there";
                Command = new ChocolateySourceCommand(ConfigSettingsService.Object);
            }
        }

        public class When_implementing_command_for : ChocolateySourceCommandSpecsBase
        {
            private List<string> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_source()
            {
                _results.Should().Contain("source");
            }

            [Fact]
            public void Should_implement_sources()
            {
                _results.Should().Contain("sources");
            }
        }

        public class When_configurating_the_argument_parser : ChocolateySourceCommandSpecsBase
        {
            private OptionSet _optionSet;

            public override void Context()
            {
                base.Context();
                _optionSet = new OptionSet();
                Configuration.Sources = "https://localhost/somewhere/out/there";
            }

            public override void Because()
            {
                Command.ConfigureArgumentParser(_optionSet, Configuration);
            }

            [Fact]
            public void Should_clear_previously_set_Source()
            {
                Configuration.Sources.Should().BeEmpty();
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
            public void Should_add_user_to_the_option_set()
            {
                _optionSet.Contains("user").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_user_to_the_option_set()
            {
                _optionSet.Contains("u").Should().BeTrue();
            }

            [Fact]
            public void Should_add_password_to_the_option_set()
            {
                _optionSet.Contains("password").Should().BeTrue();
            }

            [Fact]
            public void Should_add_short_version_of_password_to_the_option_set()
            {
                _optionSet.Contains("p").Should().BeTrue();
            }
        }

        public class When_handling_additional_argument_parsing : ChocolateySourceCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private Action _because;

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            public void Reset()
            {
                _unparsedArgs.Clear();
                ConfigSettingsService.ResetCalls();
            }

            [Fact]
            public void Should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("list");
                _because();

                Configuration.SourceCommand.Command.Should().Be(SourceCommandType.List);
            }

            [Fact]
            public void Should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                Reset();
                _unparsedArgs.Add("wtf");
                _unparsedArgs.Add("bbq");
                var errored = false;
                Exception error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                error.Message.Should().Contain("A single sources command must be listed");
            }

            [Fact]
            public void Should_accept_add_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("add");
                _because();

                Configuration.SourceCommand.Command.Should().Be(SourceCommandType.Add);
            }

            [Fact]
            public void Should_accept_uppercase_add_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("ADD");
                _because();

                Configuration.SourceCommand.Command.Should().Be(SourceCommandType.Add);
            }

            [Fact]
            public void Should_remove_add_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("remove");
                _because();

                Configuration.SourceCommand.Command.Should().Be(SourceCommandType.Remove);
            }

            [Fact]
            public void Should_accept_enable_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("enable");
                _because();

                Configuration.SourceCommand.Command.Should().Be(SourceCommandType.Enable);
            }

            [Fact]
            public void Should_accept_disable_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("disable");
                _because();

                Configuration.SourceCommand.Command.Should().Be(SourceCommandType.Disable);
            }

            [Fact]
            public void Should_set_unrecognized_values_to_list_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("wtf");
                _because();

                Configuration.SourceCommand.Command.Should().Be(SourceCommandType.List);
            }

            [Fact]
            public void Should_default_to_list_as_the_subcommand()
            {
                Reset();
                _because();

                Configuration.SourceCommand.Command.Should().Be(SourceCommandType.List);
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string()
            {
                Reset();
                _unparsedArgs.Add(" ");
                _because();

                Configuration.SourceCommand.Command.Should().Be(SourceCommandType.List);
            }
        }

        public class When_validating : ChocolateySourceCommandSpecsBase
        {
            private Action _because;

            public override void Because()
            {
                _because = () => Command.Validate(Configuration);
            }

            [Fact]
            public void Should_throw_when_command_is_not_list_and_name_is_not_set()
            {
                Configuration.SourceCommand.Name = "";
                const string expectedMessage = "When specifying the subcommand '{0}', you must also specify --name.";
                VerifyExceptionThrownOnCommand(expectedMessage);
            }

            [Fact]
            public void Should_throw_when_command_is_add_and_source_is_not_set()
            {
                Configuration.SourceCommand.Name = "irrelevant";
                Configuration.Sources = string.Empty;
                const string expectedMessage = "When specifying the subcommand '{0}', you must also specify --source.";
                VerifyExceptionThrownOnCommand(expectedMessage);
            }

            private void VerifyExceptionThrownOnCommand(string expectedMessage)
            {
                Configuration.SourceCommand.Command = SourceCommandType.Add;

                var errored = false;
                Exception error = null;

                try
                {
                    _because();
                }
                catch (Exception ex)
                {
                    errored = true;
                    error = ex;
                }

                errored.Should().BeTrue();
                error.Should().NotBeNull();
                error.Should().BeOfType<ApplicationException>();
                var commandName = Configuration.SourceCommand.Command.ToStringSafe().ToLower();
                error.Message.Should().Be(expectedMessage.FormatWith(commandName));
            }

            [Fact]
            public void Should_continue_when_command_is_list_and_name_is_not_set()
            {
                Configuration.SourceCommand.Command = SourceCommandType.List;
                Configuration.SourceCommand.Name = "";
                _because();
            }

            [Fact]
            public void Should_continue_when_command_is_not_list_and_name_is_set()
            {
                Configuration.SourceCommand.Command = SourceCommandType.List;
                Configuration.SourceCommand.Name = "bob";
                _because();
            }
        }

        public class When_noop_is_called : ChocolateySourceCommandSpecsBase
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

        public class When_run_is_called : ChocolateySourceCommandSpecsBase
        {
            private Action _because;

            public override void Because()
            {
                _because = () => Command.Run(Configuration);
            }

            [Fact]
            public void Should_call_service_source_list_when_command_is_list()
            {
                Configuration.SourceCommand.Command = SourceCommandType.List;
                _because();
                ConfigSettingsService.Verify(c => c.ListSources(Configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_source_add_when_command_is_add()
            {
                Configuration.SourceCommand.Command = SourceCommandType.Add;
                _because();
                ConfigSettingsService.Verify(c => c.AddSource(Configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_source_remove_when_command_is_remove()
            {
                Configuration.SourceCommand.Command = SourceCommandType.Remove;
                _because();
                ConfigSettingsService.Verify(c => c.RemoveSource(Configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_source_disable_when_command_is_disable()
            {
                Configuration.SourceCommand.Command = SourceCommandType.Disable;
                _because();
                ConfigSettingsService.Verify(c => c.DisableSource(Configuration), Times.Once);
            }

            [Fact]
            public void Should_call_service_source_enable_when_command_is_enable()
            {
                Configuration.SourceCommand.Command = SourceCommandType.Enable;
                _because();
                ConfigSettingsService.Verify(c => c.EnableSource(Configuration), Times.Once);
            }
        }

        public class When_list_is_called : ChocolateySourceCommandSpecsBase
        {
            private Action _because;

            public override void Because()
            {
                _because = () => Command.List(Configuration);
            }

            [Fact]
            public void Should_call_service_source_list_when_command_is_list()
            {
                Configuration.SourceCommand.Command = SourceCommandType.List;
                _because();
                ConfigSettingsService.Verify(c => c.ListSources(Configuration), Times.Once);
            }
        }
    }
}
