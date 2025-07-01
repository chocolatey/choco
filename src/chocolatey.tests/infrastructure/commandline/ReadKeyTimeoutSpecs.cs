using System;
using chocolatey.infrastructure.commandline;
using FluentAssertions;

namespace chocolatey.tests.infrastructure.commandline
{
    public class ReadKeyTimeoutSpecs
    {
        // Tests only timeout behavior — interactive input paths are deliberately not covered to avoid flakiness.

        public abstract class ReadKeyTimeoutSpecsBase : TinySpec
        {
            protected ConsoleKeyInfo Result;

            public override void Context()
            {
            }
        }

        [WindowsOnly]
        public class When_waiting_for_key_and_timeout_occurs : ReadKeyTimeoutSpecsBase
        {
            public override void Because()
            {
                // Deliberately expect timeout (no input will be sent)
                // Note: No character is printed because Console.ReadKey uses intercept: true internally

                Result = ReadKeyTimeout.ReadKey(300); // 300ms
            }

            [Fact]
            public void Should_return_default_enter_key_with_null_char()
            {
                Result.Key.Should().Be(ConsoleKey.Enter);
                Result.KeyChar.Should().Be('\0');
            }

            [Fact]
            public void Should_return_non_shifted_non_alt_non_ctrl()
            {
                Result.Modifiers.Should().Be(0);
            }
        }
    }
}
