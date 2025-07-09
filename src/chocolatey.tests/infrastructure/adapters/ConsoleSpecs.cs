using System;
using System.IO;
using System.Reflection;
using chocolatey.infrastructure.adapters;
using chocolatey.infrastructure.app;
using FluentAssertions;
using Console = chocolatey.infrastructure.adapters.Console;
using Environment = System.Environment;

namespace chocolatey.tests.infrastructure.adapters
{
    public class ConsoleSpecs
    {
        public abstract class ConsoleSpecsBase : TinySpec
        {
            protected Console Console;
            private FieldInfo _allowPromptsField;
            private bool _originalAllowPrompts;

            public override void Context()
            {
                Console = new Console();
                _allowPromptsField = typeof(ApplicationParameters)
                    .GetField(nameof(ApplicationParameters.AllowPrompts), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                _originalAllowPrompts = (bool)_allowPromptsField.GetValue(null);
                _allowPromptsField.SetValue(null, false);
            }

            public override void AfterObservations()
            {
                _allowPromptsField.SetValue(null, _originalAllowPrompts);
            }
        }

        public class When_readline_is_disabled : ConsoleSpecsBase
        {
            private string _result;

            public override void Because()
            {
                _result = Console.ReadLine();
            }

            [Fact]
            public void Should_return_empty_string()
            {
                _result.Should().BeEmpty();
            }
        }

        public class When_readkey_is_disabled : ConsoleSpecsBase
        {
            private ConsoleKeyInfo _result;

            public override void Because()
            {
                _result = Console.ReadKey(intercept: true);
            }

            [Fact]
            public void Should_return_enter_key_with_null_char()
            {
                _result.Key.Should().Be(ConsoleKey.Enter);
                _result.KeyChar.Should().Be('\0');
                _result.Modifiers.Should().Be(0);
            }
        }

        public class When_writing_object_to_console_out : ConsoleSpecsBase
        {
            private StringWriter _output;

            public override void Context()
            {
                base.Context();
                _output = new StringWriter();
                System.Console.SetOut(_output);
            }

            public override void Because()
            {
                Console.Write(42);
            }

            [Fact]
            public void Should_write_value_as_string()
            {
                _output.ToString().Should().Be("42");
            }
        }

        public class When_writing_line_to_console_out : ConsoleSpecsBase
        {
            private StringWriter _output;

            public override void Context()
            {
                base.Context();
                _output = new StringWriter();
                System.Console.SetOut(_output);
            }

            public override void Because()
            {
                Console.WriteLine("test line");
            }

            [Fact]
            public void Should_write_line_with_newline()
            {
                _output.ToString().Should().Be("test line" + Environment.NewLine);
            }
        }

        public class When_writing_empty_line : ConsoleSpecsBase
        {
            private StringWriter _output;

            public override void Context()
            {
                base.Context();
                _output = new StringWriter();
                System.Console.SetOut(_output);
            }

            public override void Because()
            {
                Console.WriteLine();
            }

            [Fact]
            public void Should_write_just_newline()
            {
                _output.ToString().Should().Be(Environment.NewLine);
            }
        }

        public class When_readline_with_timeout_is_disabled : ConsoleSpecsBase
        {
            private string _result;

            public override void Because()
            {
                _result = Console.ReadLine(200);
            }

            [Fact]
            public void Should_return_empty_string()
            {
                _result.Should().BeEmpty();
            }
        }

        public class When_readkey_with_timeout_is_disabled : ConsoleSpecsBase
        {
            private ConsoleKeyInfo _result;

            public override void Because()
            {
                _result = Console.ReadKey(200);
            }

            [Fact]
            public void Should_return_enter_key_with_null_char()
            {
                _result.Key.Should().Be(ConsoleKey.Enter);
                _result.KeyChar.Should().Be('\0');
                _result.Modifiers.Should().Be(0);
            }
        }

        public class When_accessing_console_streams : ConsoleSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_return_non_null_output_writer()
            {
                Console.Out.Should().NotBeNull();
            }

            [Fact]
            public void Should_return_non_null_error_writer()
            {
                Console.Error.Should().NotBeNull();
            }
        }

    }
}
