namespace chocolatey.tests.infrastructure.commands
{
    using System;
    using Moq;
    using Should;
    using chocolatey.infrastructure.adapters;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;

    public class CommandExecutorSpecs
    {
        public abstract class CommandExecutorSpecsBase : TinySpec
        {
            protected Mock<IFileSystem> file_system = new Mock<IFileSystem>();
            protected Mock<IProcess> process = new Mock<IProcess>();

            public override void Context()
            {
                CommandExecutor.initialize_with(new Lazy<IFileSystem>(() => file_system.Object), () => process.Object);
            }
        }

        public class when_CommandExecutor_is_executed_normally : CommandExecutorSpecsBase
        {
            private int result;

            public override void Context()
            {
                base.Context();
                process.Setup(p => p.ExitCode).Returns(0);
            }

            public override void Because()
            {
                result = CommandExecutor.execute("bob", "args", waitForExit: true);
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
                process.Verify(p => p.WaitForExit(), Times.Once);
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
            public void should_return_an_exit_code_of_zero_when_finished()
            {
                result.ShouldEqual(0);
            }
        }

        public class when_CommandExecutor_does_not_wait_for_exit : CommandExecutorSpecsBase
        {
            private int result;

            public override void Because()
            {
                result = CommandExecutor.execute("bob", "args", waitForExit: false);
            }

            [Fact]
            public void should_have_an_exit_code_of_negative_one_as_it_didnt_wait_for_finish()
            {
                result.ShouldEqual(-1);
            }

            [Fact]
            public void should_not_call_WaitForExit()
            {
                process.Verify(p => p.WaitForExit(), Times.Never);
            }
        }
    }
}