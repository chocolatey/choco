// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using chocolatey.infrastructure.app.attributes;
using chocolatey.infrastructure.app.commands;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace chocolatey.tests.infrastructure.app.commands
{
    public class ChocolateyLicenseCommandSpecs
    {
        [ConcernFor("license")]
        public abstract class ChocolateyLicenseCommandSpecsBase : TinySpec
        {
            protected ChocolateyLicenseCommand Command;
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateyLicenseCommand();
            }
        }

        public class When_Implementing_Command_For : ChocolateyLicenseCommandSpecsBase
        {
            private List<CommandForAttribute> _results;

            public override void Because()
            {
                _results = Command.GetType().GetCustomAttributes<CommandForAttribute>().ToList();
            }

            [Fact]
            public void Should_Have_Expected_Number_Of_Commands()
            {
                _results.Should().HaveCount(1);
            }

            [InlineData("license")]
            public void Should_Implement_Expected_Command(string name)
            {
                _results.Should().ContainSingle(r => r.CommandName == name);
            }

            [Fact]
            public void Should_Specify_Expected_Version_For_All_Commands()
            {
                _results.Should().AllSatisfy(r => r.Version.Should().Be("2.5.0"));
            }
        }

        public class When_parsing_additional_arguments_ : ChocolateyLicenseCommandSpecsBase
        {
            private readonly IList<string> _unparsedArgs = new List<string>();
            private Action _because;

            public override void Because()
            {
                _because = () => Command.ParseAdditionalArguments(_unparsedArgs, Configuration);
            }

            public new void Reset()
            {
                _unparsedArgs.Clear();
            }

            [Fact]
            public void Should_use_the_first_unparsed_arg_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("info");
                _because();

                Configuration.LicenseCommand.Command.Should().Be(LicenseCommandType.Info);
            }

            [Fact]
            public void Should_throw_when_more_than_one_unparsed_arg_is_passed()
            {
                Reset();
                _unparsedArgs.Add("abc");
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
                error.Message.Should().Contain("A single license command must be listed");
            }

            [Fact]
            public void Should_accept_info_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("info");
                _because();

                Configuration.LicenseCommand.Command.Should().Be(LicenseCommandType.Info);
            }

            [Fact]
            public void Should_accept_uppercase_info_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("INFO");
                _because();

                Configuration.LicenseCommand.Command.Should().Be(LicenseCommandType.Info);
            }

            [Fact]
            public void Should_set_unrecognized_values_to_info_as_the_subcommand()
            {
                Reset();
                _unparsedArgs.Add("abc");
                _because();

                Configuration.LicenseCommand.Command.Should().Be(LicenseCommandType.Info);
            }

            [Fact]
            public void Should_default_to_list_as_the_subcommand()
            {
                Reset();
                _because();

                Configuration.LicenseCommand.Command.Should().Be(LicenseCommandType.Info);
            }

            [Fact]
            public void Should_handle_passing_in_an_empty_string()
            {
                Reset();
                _unparsedArgs.Add(" ");
                _because();

                Configuration.LicenseCommand.Command.Should().Be(LicenseCommandType.Info);
            }
        }

        public class When_Help_Is_Called : ChocolateyLicenseCommandSpecsBase
        {
            public override void Because()
            {
                Command.HelpMessage(Configuration);
            }

            [Fact]
            public void Should_log_a_message()
            {
                MockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.AtLeastOnce);
            }

            [Fact]
            public void Should_log_the_message_we_expect()
            {
                var messages = MockLogger.MessagesFor(LogLevel.Info);
                messages.Should().HaveCount(19);
                messages[0].Should().Contain("License Command");
                messages[2].Should().Contain("Show information about the current Chocolatey CLI license.");
            }
        }

        public class When_DryRun_Is_Called : ChocolateyLicenseCommandSpecsBase
        {
            public override void Because()
            {
                Configuration.LicenseCommand.Command = LicenseCommandType.Info;
                Command.DryRun(Configuration);
            }

            [Fact]
            public void Should_log_a_message()
            {
                MockLogger.Verify(l => l.Warn(It.IsAny<string>()), Times.AtLeastOnce);
            }

            [Fact]
            public void Should_log_the_message_we_expect()
            {
                var messages = MockLogger.MessagesFor(LogLevel.Warn);
                messages.Should().ContainSingle();
                messages[0].Should().Contain("No Chocolatey license found.");
            }
        }

        public class When_Run_Is_Called : ChocolateyLicenseCommandSpecsBase
        {
            public override void Because()
            {
                Configuration.LicenseCommand.Command = LicenseCommandType.Info;
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_log_a_message()
            {
                MockLogger.Verify(l => l.Warn(It.IsAny<string>()), Times.AtLeastOnce);
            }

            [Fact]
            public void Should_log_the_message_we_expect()
            {
                var messages = MockLogger.MessagesFor(LogLevel.Warn);
                messages.Should().ContainSingle();
                messages[0].Should().Contain("No Chocolatey license found.");
            }
        }

        public class When_Run_Is_Called_With_Limit_Output : ChocolateyLicenseCommandSpecsBase
        {
            public override void Because()
            {
                Configuration.LicenseCommand.Command = LicenseCommandType.Info;
                Configuration.RegularOutput = false;
                Command.Run(Configuration);
            }

            [Fact]
            public void Should_log_a_message()
            {
                MockLogger.Verify(l => l.Warn(It.IsAny<string>()), Times.AtLeastOnce);
            }

            [Fact]
            public void Should_log_the_message_we_expect()
            {
                var messages = MockLogger.MessagesFor(LogLevel.Warn);
                messages.Should().ContainSingle();
                messages[0].Should().Contain("No Chocolatey license found.");
            }
        }
    }
}