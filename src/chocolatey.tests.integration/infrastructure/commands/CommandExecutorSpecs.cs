namespace chocolatey.tests.integration.infrastructure.commands
{
    using System;
    using Should;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;

    public class CommandExecutorSpecs
    {
        public abstract class CommandExecutorSpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public class when_CommandExecutor_errors : CommandExecutorSpecsBase
        {
            private int result;
            private string errorOutput;
            private IFileSystem file_system = new DotNetFileSystem();

            public override void Context()
            {
                base.Context();
            }

            public override void Because()
            {
                result = CommandExecutor.execute("cmd.exe", "/c bob123123", true, file_system.get_current_directory(), null, (s, e) => { errorOutput += e.Data; }, updateProcessPath: false);
            }

            [Fact]
            public void should_not_return_an_exit_code_of_zero()
            {
                result.ShouldNotEqual(0);
            }

            [Fact]
            public void should_contain_error_output()
            {
                errorOutput.ShouldNotBeNull();
            }

            [Fact]
            public void should_message_the_error()
            {
                errorOutput.ShouldEqual("'bob123123' is not recognized as an internal or external command,operable program or batch file.");
            }
        }

        public class when_CommandExecutor_is_given_a_nonexisting_process : CommandExecutorSpecsBase
        {
            private string result;
            private string errorOutput;

            public override void Because()
            {
                try
                {
                   CommandExecutor.execute("noprocess.exe", "/c bob123123", true, null, (s, e) => { errorOutput += e.Data; });

                }
                catch (Exception e)
                {
                    result = e.Message;
                }
            }

            [Fact]
            public void should_have_an_error_message()
            {
                result.ShouldNotBeNull();
            }
        }
    }
}