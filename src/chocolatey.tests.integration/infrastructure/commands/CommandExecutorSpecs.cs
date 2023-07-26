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

namespace chocolatey.tests.integration.infrastructure.commands
{
    using System;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using NUnit.Framework;
    using FluentAssertions;

    public class CommandExecutorSpecs
    {
        public abstract class CommandExecutorSpecsBase : TinySpec
        {
            protected readonly IFileSystem FileSystem = new DotNetFileSystem();
            protected CommandExecutor CommandExecutor;

            public override void Context()
            {
                CommandExecutor = new CommandExecutor(FileSystem);
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_CommandExecutor_errors : CommandExecutorSpecsBase
        {
            private int _result;
            private string _errorOutput;

            public override void Context()
            {
                base.Context();
            }

            public override void Because()
            {
                _result = CommandExecutor.Execute(
                    "cmd.exe",
                    "/c bob123123",
                    ApplicationParameters.DefaultWaitForExitInSeconds,
                    FileSystem.GetCurrentDirectory(),
                    null,
                    (s, e) => { _errorOutput += e.Data; },
                    updateProcessPath: false,
                    allowUseWindow: false);
            }

            [Fact]
            public void Should_not_return_an_exit_code_of_zero()
            {
                _result.Should().NotBe(0);
            }

            [Fact]
            public void Should_contain_error_output()
            {
                _errorOutput.Should().NotBeNull();
            }

            [Fact]
            public void Should_message_the_error()
            {
                _errorOutput.Should().Be("'bob123123' is not recognized as an internal or external command,operable program or batch file.");
            }
        }

        [WindowsOnly]
        [Platform(Exclude = "Mono")]
        public class When_CommandExecutor_is_given_a_nonexisting_process : CommandExecutorSpecsBase
        {
            private string _result;
            private string _errorOutput;

            public override void Because()
            {
                try
                {
                    CommandExecutor.Execute("noprocess.exe", "/c bob123123", ApplicationParameters.DefaultWaitForExitInSeconds, null, (s, e) => { _errorOutput += e.Data; });
                }
                catch (Exception e)
                {
                    _result = e.Message;
                }
            }

            [Fact]
            public void Should_have_an_error_message()
            {
                _result.Should().NotBeNull();
            }
        }
    }
}
