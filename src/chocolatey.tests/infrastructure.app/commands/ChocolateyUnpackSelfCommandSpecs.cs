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
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app.attributes;
    using chocolatey.infrastructure.app.commands;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using FluentAssertions;

    public class ChocolateyUnpackSelfCommandSpecs
    {
        [ConcernFor("unpackself")]
        public abstract class ChocolateyUnpackSelfCommandSpecsBase : TinySpec
        {
            protected ChocolateyUnpackSelfCommand command;
            protected Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            protected Mock<IAssembly> assembly = new Mock<IAssembly>();
            protected ChocolateyConfiguration configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                command = new ChocolateyUnpackSelfCommand(fileSystem.Object);
                command.InitializeWith(new Lazy<IAssembly>(() => assembly.Object));
            }
        }

        public class When_implementing_command_for : ChocolateyUnpackSelfCommandSpecsBase
        {
            private List<string> results;

            public override void Because()
            {
                results = command.GetType().GetCustomAttributes(typeof(CommandForAttribute), false).Cast<CommandForAttribute>().Select(a => a.CommandName).ToList();
            }

            [Fact]
            public void Should_implement_unpackself()
            {
                results.Should().Contain("unpackself");
            }
        }

        public class When_noop_is_called : ChocolateyUnpackSelfCommandSpecsBase
        {
            public override void Because()
            {
                command.DryRun(configuration);
            }

            [Fact]
            public void Should_log_a_message()
            {
                MockLogger.Verify(l => l.Info(It.IsAny<string>()), Times.Once);
            }

            [Fact]
            public void Should_log_one_message()
            {
                MockLogger.Messages.Should().HaveCount(1);
            }

            [Fact]
            public void Should_log_a_message_about_what_it_would_have_done()
            {
                MockLogger.MessagesFor(LogLevel.Info).FirstOrDefault().Should().Contain("This would have unpacked");
            }
        }

        public class When_run_is_called : ChocolateyUnpackSelfCommandSpecsBase
        {
            public override void Because()
            {
                command.Run(configuration);
            }

            [Fact]
            public void Should_call_assembly_file_extractor()
            {
                assembly.Verify(a => a.GetManifestResourceNames(), Times.Once);
            }
        }
    }
}
