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
using FluentAssertions;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace chocolatey.tests.infrastructure.app.commands
{
    public class ChocolateySupportCommandSpecs
    {
        [ConcernFor("support")]
        public abstract class ChocolateySupportCommandSpecsBase : TinySpec
        {
            protected ChocolateySupportCommand Command;
            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                Command = new ChocolateySupportCommand();
            }
        }

        public class When_Implementing_Command_For : ChocolateySupportCommandSpecsBase
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

            [InlineData("support")]
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

        public class When_Noop_Is_Called : ChocolateySupportCommandSpecsBase
        {
            public override void Because()
            {
                Command.DryRun(Configuration);
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
                messages.Should().ContainSingle();
                messages[0].Should().Contain("Unfortunately, we are unable to provide private support");
            }
        }

        public class When_Run_Is_Called : ChocolateySupportCommandSpecsBase
        {
            public override void Because()
            {
                Command.Run(Configuration);
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
                messages.Should().ContainSingle();
                messages[0].Should().Contain("Unfortunately, we are unable to provide private support");
            }
        }

        public class When_Help_Is_Called : ChocolateySupportCommandSpecsBase
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
                messages.Should().HaveCount(2);
                messages[0].Should().Contain("Support Command");
                messages[1].Should().Contain("As a user of Chocolatey CLI open-source, we are unable to");
            }
        }
    }
}
