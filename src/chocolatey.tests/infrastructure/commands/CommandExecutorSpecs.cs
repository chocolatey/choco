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

namespace chocolatey.tests.infrastructure.commands
{
    using System;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using Should;

    public class CommandExecutorSpecs
    {
        public abstract class CommandExecutorSpecsBase : TinySpec
        {
            protected Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            protected Mock<IProcess> process = new Mock<IProcess>();
            protected CommandExecutor commandExecutor;

            public override void Context()
            {
                commandExecutor = new CommandExecutor(fileSystem.Object);
                CommandExecutor.initialize_with(new Lazy<IFileSystem>(() => fileSystem.Object), () => process.Object);
            }
        }

        public class when_CommandExecutor_is_executed_normally : CommandExecutorSpecsBase
        {
            private int result;

            public override void Context()
            {
                base.Context();
                process.Setup(p => p.ExitCode).Returns(0);
                process.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(true);
            }

            public override void Because()
            {
                result = commandExecutor.execute("bob", "args", ApplicationParameters.DefaultWaitForExitInSeconds);
            }

            [Fact]
            public void should_call_Start()
            {
                process.Verify(p => p.Start(), Times.Once);
            }

            [Fact]
            public void should_have_EnableRaisingEvents_set_to_true()
            {
                process.VerifySet(p => p.EnableRaisingEvents = true);
            }

            [Fact]
            public void should_call_WaitForExit()
            {
                process.Verify(p => p.WaitForExit(It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void should_call_BeginErrorReadLine()
            {
                process.Verify(p => p.BeginErrorReadLine(), Times.Once);
            }

            [Fact]
            public void should_call_BeginOutputReadLine()
            {
                process.Verify(p => p.BeginOutputReadLine(), Times.Once);
            }

            [Fact]
            public void should_call_ExitCode()
            {
                process.Verify(p => p.ExitCode, Times.Once);
            }

            [Fact]
            public void should_return_an_exit_code_of_zero_when_finished()
            {
                result.ShouldEqual(0);
            }
        }

        public class when_CommandExecutor_has_a_long_running_process_that_takes_longer_than_wait_time : CommandExecutorSpecsBase
        {
            private int result;

            public override void Context()
            {
                base.Context();
                process.Setup(p => p.WaitForExit(It.IsAny<int>())).Returns(false);
                process.Setup(p => p.ExitCode).Returns(0);
            }

            public override void Because()
            {
                result = commandExecutor.execute("bob", "args", ApplicationParameters.DefaultWaitForExitInSeconds);
            }

            [Fact]
            public void should_call_WaitForExit()
            {
                process.Verify(p => p.WaitForExit(It.IsAny<int>()), Times.Once);
            }

            [Fact]
            public void should_return_an_exit_code_of_negative_one_since_it_timed_out()
            {
                result.ShouldEqual(-1);
            }

            [Fact]
            public void should_not_call_ExitCode()
            {
                process.Verify(p => p.ExitCode, Times.Never);
            }
        }

        public class when_CommandExecutor_does_not_wait_for_exit : CommandExecutorSpecsBase
        {
            private int result;

            public override void Because()
            {
                result = commandExecutor.execute("bob", "args", waitForExitInSeconds: 0, workingDirectory: null, stdOutAction: null, stdErrAction: null, updateProcessPath: false, allowUseWindow: true, waitForExit: false);
            }

            [Fact]
            public void should_have_an_exit_code_of_negative_one_as_it_didnt_wait_for_finish()
            {
                result.ShouldEqual(-1);
            }

            [Fact]
            public void should_not_call_WaitForExit()
            {
                process.Verify(p => p.WaitForExit(It.IsAny<int>()), Times.Never);
            }
        } 
    }
}
