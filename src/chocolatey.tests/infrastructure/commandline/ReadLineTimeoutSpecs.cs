using System;
using chocolatey.infrastructure.commandline;
using FluentAssertions;

namespace chocolatey.tests.infrastructure.commandline
{
    public class ReadLineTimeoutSpecs
    {
        public abstract class ReadLineTimeoutSpecsBase : TinySpec
        {
            protected string Result;

            public override void Context()
            {
                // no-op
            }
        }

        public class When_waiting_for_line_and_timeout_occurs : ReadLineTimeoutSpecsBase
        {
            public override void Because()
            {
                Result = ReadLineTimeout.Read(300); // 300ms
            }

            [Fact]
            public void Should_return_null_when_input_times_out()
            {
                Result.Should().BeNull();
            }
        }
    }
}
