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

namespace chocolatey.tests.infrastructure.commands
{
    using System;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using FluentAssertions;

    public class CommandExecutorSpecs
    {
        public abstract class CommandExecutorSpecsBase : TinySpec
        {
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Mock<IProcess> Process = new Mock<IProcess>();
            protected CommandExecutor CommandExecutor;

            public override void Context()
            {
                CommandExecutor = new CommandExecutor(FileSystem.Object);
                CommandExecutor.InitializeWith(new Lazy<IFileSystem>(() => FileSystem.Object), () => Process.Object);
            }
        }

        public class When_CommandExecutor_is_executed_normally : CommandExecutorSpecsBase
        {
            private int _result;

            public override void Context()
            {
                base.Context();
                Process.Setup(p => p.ExitCode).Returns(0);
                Process.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);
            }

            public override void Because()
            {
                _result = CommandExecutor.Execute("bob", "args", ApplicationParameters.DefaultWaitForExitInSeconds);
            }

            [Fact]
            public void Should_call_Start()
            {
                Process.Verify(p => p.Start(), Times.Once);
            }

            [Fact]
            public void Should_have_EnableRaisingEvents_set_to_true()
            {
                Process.VerifySet(p => p.EnableRaisingEvents = true);
            }

            [Fact]
            public void Should_call_WaitForExit()
            {
                Process.Verify(p => p.WaitForExit(It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void Should_call_BeginErrorReadLine()
            {
                Process.Verify(p => p.BeginErrorReadLine(), Times.Once);
            }

            [Fact]
            public void Should_call_BeginOutputReadLine()
            {
                Process.Verify(p => p.BeginOutputReadLine(), Times.Once);
            }

            [Fact]
            public void Should_call_ExitCode()
            {
                Process.Verify(p => p.ExitCode, Times.Once);
            }

            [Fact]
            public void Should_return_an_exit_code_of_zero_when_finished()
            {
                _result.Should().Be(0);
            }
        }

        public class When_CommandExecutor_has_a_long_running_process_that_takes_longer_than_wait_time : CommandExecutorSpecsBase
        {
            private int _result;

            public override void Context()
            {
                base.Context();
                Process.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(false);
                Process.Setup(p => p.ExitCode).Returns(0);
            }

            public override void Because()
            {
                _result = CommandExecutor.Execute("bob", "args", ApplicationParameters.DefaultWaitForExitInSeconds);
            }

            [Fact]
            public void Should_call_WaitForExit()
            {
                Process.Verify(p => p.WaitForExit(It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void Should_return_an_exit_code_of_negative_one_since_it_timed_out()
            {
                _result.Should().Be(-1);
            }

            [Fact]
            public void Should_not_call_ExitCode()
            {
                Process.Verify(p => p.ExitCode, Times.Never);
            }
        }

        public class When_CommandExecutor_does_not_wait_for_exit : CommandExecutorSpecsBase
        {
            private int _result;

            public override void Because()
            {
                _result = CommandExecutor.Execute("bob", "args", waitForExitInSeconds: 0, workingDirectory: null, stdOutAction: null, stdErrAction: null, updateProcessPath: false, allowUseWindow: true, waitForExit: false);
            }

            [Fact]
            public void Should_have_an_exit_code_of_negative_one_as_it_didnt_wait_for_finish()
            {
                _result.Should().Be(-1);
            }

            [Fact]
            public void Should_not_call_WaitForExit()
            {
                Process.Verify(p => p.WaitForExit(It.IsAny<int>()), Times.Never);
            }
        }
    }
}
