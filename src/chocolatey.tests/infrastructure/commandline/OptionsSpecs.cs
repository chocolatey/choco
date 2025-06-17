using chocolatey.infrastructure.commandline;
using FluentAssertions;
using NuGet.Packaging;
using System;
using System.Collections.Generic;

namespace chocolatey.tests.infrastructure.commandline
{
    public class OptionSetSpecs
    {
        public abstract class OptionSetSpecsBase : TinySpec
        {
            protected OptionSet Options;
            protected List<string> RemainingArgs;

            public override void Context()
            {
                Options = new OptionSet();
                RemainingArgs = new List<string>();
            }
        }

        public class When_parsing_single_named_option_with_value : OptionSetSpecsBase
        {
            private string _nameValue;

            public override void Context()
            {
                base.Context();
                Options.Add("name=", v => _nameValue = v);
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "--name=choco" });
            }

            [Fact]
            public void Should_set_the_value_correctly()
            {
                _nameValue.Should().Be("choco");
            }

            [Fact]
            public void Should_have_no_remaining_arguments()
            {
                RemainingArgs.Should().BeEmpty();
            }
        }

        public class When_parsing_option_without_required_value : OptionSetSpecsBase
        {
            private Exception _caught;

            public override void Context()
            {
                base.Context();
                Options.Add("key=", v => { });
            }

            public override void Because()
            {
                try
                {
                    Options.Parse(new[] { "--key" });
                }
                catch (Exception ex)
                {
                    _caught = ex;
                }
            }

            [Fact]
            public void Should_throw_an_option_exception()
            {
                _caught.Should().BeOfType<OptionException>();
            }
        }

        public class When_parsing_bundled_short_options : OptionSetSpecsBase
        {
            private int _count;

            public override void Context()
            {
                base.Context();
                Options
                    .Add("a", v => _count++)
                    .Add("b", v => _count++)
                    .Add("c", v => _count++);
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "-abc" });
            }

            [Fact]
            public void Should_invoke_all_three_options()
            {
                _count.Should().Be(3);
            }

            [Fact]
            public void Should_have_no_remaining_arguments()
            {
                RemainingArgs.Should().BeEmpty();
            }
        }

        public class When_parsing_unknown_option : OptionSetSpecsBase
        {
            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "--unknown", "arg1" });
            }

            [Fact]
            public void Should_place_unknown_argument_in_remaining()
            {
                RemainingArgs.Should().Contain("--unknown");
            }

            [Fact]
            public void Should_preserve_additional_args()
            {
                RemainingArgs.Should().Contain("arg1");
            }
        }

        public class When_parsing_option_with_optional_value : OptionSetSpecsBase
        {
            private string _opt;

            public override void Context()
            {
                base.Context();
                Options.Add("flag:", v => _opt = v);
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "--flag" });
            }

            [Fact]
            public void Should_allow_missing_value()
            {
                _opt.Should().BeNull();
            }

            [Fact]
            public void Should_have_no_remaining_arguments()
            {
                RemainingArgs.Should().BeEmpty();
            }
        }

        public class When_parsing_with_default_option_handler : OptionSetSpecsBase
        {
            private List<string> _defaults;

            public override void Context()
            {
                base.Context();
                _defaults = new List<string>();
                Options.Add("<>", v => _defaults.Add(v));
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "foo", "--bar=1", "baz" });
            }

            [Fact]
            public void Should_capture_positional_arguments()
            {
                _defaults.Should().Contain("foo").And.Contain("baz");
            }

            [Fact]
            public void Should_have_no_remaining_arguments()
            {
                RemainingArgs.Should().BeEmpty();
            }

            [Fact]
            public void Should_capture_arguments_in_correct_order()
            {
                _defaults.Should().ContainInOrder("foo", "baz");
            }
        }

        public class When_parsing_integer_option : OptionSetSpecsBase
        {
            private int _value;

            public override void Context()
            {
                base.Context();
                Options.Add<int>("port=", v => _value = v);
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "--port=8080" });
            }

            [Fact]
            public void Should_convert_string_to_integer()
            {
                _value.Should().Be(8080);
            }

            [Fact]
            public void Should_have_no_remaining_arguments()
            {
                RemainingArgs.Should().BeEmpty();
            }
        }

        public class When_parsing_invalid_bundled_option : OptionSetSpecsBase
        {
            private List<string> _unparsed;

            public override void Context()
            {
                base.Context();
                Options.Add("a", v => { });
            }

            public override void Because()
            {
                _unparsed = Options.Parse(new[] { "-az" });
            }

            [Fact]
            public void Should_return_argument_as_unparsed()
            {
                _unparsed.Should().Contain("-az");
            }
        }

        public class When_parsing_invalid_integer_value : OptionSetSpecsBase
        {
            private Exception _error;

            public override void Context()
            {
                base.Context();
                Options.Add<int>("port=", v => { });
            }

            public override void Because()
            {
                try
                {
                    Options.Parse(new[] { "--port=notanumber" });
                }
                catch (Exception ex)
                {
                    _error = ex;
                }
            }

            [Fact]
            public void Should_throw_option_exception()
            {
                _error.Should().BeOfType<OptionException>();
            }
        }

        public class When_parsing_option_with_optional_value_supplied : OptionSetSpecsBase
        {
            private string _opt;

            public override void Context()
            {
                base.Context();
                Options.Add("flag:", v => _opt = v);
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "--flag=someval" });
            }

            [Fact]
            public void Should_assign_provided_value()
            {
                _opt.Should().Be("someval");
            }

            [Fact]
            public void Should_have_no_remaining_arguments()
            {
                RemainingArgs.Should().BeEmpty();
            }
        }

        public class When_parsing_optional_value_with_colon_separator : OptionSetSpecsBase
        {
            private string _value;

            public override void Context()
            {
                base.Context();
                Options.Add("opt:", v => _value = v);
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "--opt", "value" });
            }

            [Fact]
            public void Should_leave_value_unset()
            {
                _value.Should().BeNull("because no inline value was passed");
            }

            [Fact]
            public void Should_leave_value_argument_unparsed()
            {
                RemainingArgs.Should().ContainSingle().Which.Should().Be("value");
            }
        }

        public class When_parsing_multiple_values_with_custom_option : OptionSetSpecsBase
        {
            private string _first;
            private string _second;

            public override void Context()
            {
                base.Context();

                var multi = new MultiValueOption("coords=:{,}", "specifies coordinates", 2,
                    values =>
                    {
                        _first = values[0];
                        _second = values[1];
                    });

                Options.Add(multi);
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "--coords=10,20" });
            }

            [Fact]
            public void Should_parse_both_values()
            {
                _first.Should().Be("10");
                _second.Should().Be("20");
            }

            [Fact]
            public void Should_have_no_remaining_arguments()
            {
                RemainingArgs.Should().BeEmpty();
            }

            private class MultiValueOption : Option
            {
                private readonly Action<OptionValueCollection> _callback;

                public MultiValueOption(string prototype, string description, int count, Action<OptionValueCollection> callback)
                    : base(prototype, description, count)
                {
                    _callback = callback;
                }

                protected override void OnParseComplete(OptionContext c)
                {
                    _callback(c.OptionValues);
                }
            }
        }

        public class When_parsing_multiple_values_with_semicolon_separator : OptionSetSpecsBase
        {
            private string _x;
            private string _y;

            public override void Context()
            {
                base.Context();

                var option = new MultiValueOption("coords=:{;}", "x and y coordinate pair", 2,
                    values =>
                    {
                        _x = values[0];
                        _y = values[1];
                    });

                Options.Add(option);
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "--coords=10;20" });
            }

            [Fact]
            public void Should_parse_first_coordinate()
            {
                _x.Should().Be("10");
            }

            [Fact]
            public void Should_parse_second_coordinate()
            {
                _y.Should().Be("20");
            }

            [Fact]
            public void Should_have_no_remaining_arguments()
            {
                RemainingArgs.Should().BeEmpty();
            }

            private class MultiValueOption : Option
            {
                private readonly Action<OptionValueCollection> _callback;

                public MultiValueOption(string prototype, string description, int count, Action<OptionValueCollection> callback)
                    : base(prototype, description, count)
                {
                    _callback = callback;
                }

                protected override void OnParseComplete(OptionContext c)
                {
                    _callback(c.OptionValues);
                }
            }
        }


        public class When_parsing_alias_option : OptionSetSpecsBase
        {
            private string _name;

            public override void Context()
            {
                base.Context();
                Options.Add("n|name=", v => _name = v);
            }

            public override void Because()
            {
                RemainingArgs = Options.Parse(new[] { "-n", "choco" });
            }

            [Fact]
            public void Should_parse_using_alias()
            {
                _name.Should().Be("choco");
            }

            [Fact]
            public void Should_have_no_remaining_arguments()
            {
                RemainingArgs.Should().BeEmpty();
            }
        }
    }
}
