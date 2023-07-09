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

namespace chocolatey.tests.infrastructure.guards
{
    using System;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.guards;
    using Moq;
    using FluentAssertions;

    public class EnsureSpecs
    {
        public abstract class EnsureSpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public class When_Ensure_is_being_set_to_a_type : EnsureSpecsBase
        {
            private object _result;
            private readonly object _bob = "something";

            public override void Because()
            {
                _result = Ensure.That(() => _bob);
            }

            [Fact]
            public void Should_return_a_type_of_object_for_ensuring()
            {
                _result.Should().BeOfType<Ensure<object>>();
            }

            [Fact]
            public void Should_have_the_value_specified()
            {
                var bobEnsure = _result as Ensure<object>;
                bobEnsure.Value.Should().Be(_bob);
            }
        }

        public class When_Ensure_is_a_string_type : EnsureSpecsBase
        {
            private object _result;
            private readonly string _bob = "something";

            public override void Because()
            {
                _result = Ensure.That(() => _bob);
            }

            [Fact]
            public void Should_return_a_ensure_string_type()
            {
                _result.Should().BeOfType<EnsureString>();
            }

            [Fact]
            public void Should_have_the_value_specified()
            {
                var bobEnsure = _result as EnsureString;
                bobEnsure.Value.Should().Be(_bob);
            }
        }

        public class When_using_EnsureString : EnsureSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void When_testing_a_string_against_null_value_should_fail()
            {
                string test = null;

                Action a = () => Ensure.That(() => test).NotNullOrWhitespace();

                a.Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void When_testing_a_string_against_an_empty_value_should_fail()
            {
                Action a = () => Ensure.That(() => string.Empty).NotNullOrWhitespace();

                a.Should().Throw<ArgumentException>();
            }

            [Fact]
            public void When_testing_a_string_against_a_whitespace_value_should_fail()
            {
                var test = "      ";

                Action a = () => Ensure.That(() => test).NotNullOrWhitespace();

                a.Should().Throw<ArgumentException>();
            }

            [Fact]
            public void When_testing_a_string_against_a_non_empty_value_should_pass()
            {
                var test = "some value";

                Ensure.That(() => test).NotNullOrWhitespace();
            }

            [Fact]
            public void When_testing_a_string_without_expected_extension_should_fail()
            {
                var test = "some-file.png";

                Action a = () => Ensure.That(() => test).HasExtension(".jpg", ".bmp", ".gif");

                a.Should().Throw<ArgumentException>();
            }

            [Fact]
            public void When_testing_a_string_with_expected_extension_should_pass()
            {
                var test = "some-file.png";

                Ensure.That(() => test).HasExtension(".jpg", ".bmp", ".gif", ".png");
            }
        }

        public class When_using_Ensure : EnsureSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void When_testing_a_string_against_is_not_null_should_pass()
            {
                string test = "value";
                Ensure.That(() => test).NotNull();
            }

            [Fact]
            public void When_testing_a_null_string_against_is_not_null_should_throw_an_Argument_exception()
            {
                string test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.That(() => test).NotNull();
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.Should().BeOfType<ArgumentNullException>();
                exceptionMessage.Should().Contain("cannot be null.");
            }

            [Fact]
            public void When_testing_a_Func_against_is_not_null_should_pass()
            {
                Func<string> test = () => "value";
                Ensure.That(() => test).NotNull();
            }

            [Fact]
            public void When_testing_a_null_Func_against_is_not_null_should_throw_an_Argument_exception()
            {
                Func<string> test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.That(() => test).NotNull();
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.Should().BeOfType<ArgumentNullException>();
                exceptionMessage.Should().Contain("cannot be null.");
            }

            [Fact]
            public void When_testing_a_class_against_is_not_null_should_pass()
            {
                var test = new ChocolateyConfiguration();
                Ensure.That(() => test).NotNull();
            }

            [Fact]
            public void When_testing_an_uninstantiated_class_against_is_not_null_should_throw_an_Argument_exception()
            {
                ChocolateyConfiguration test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.That(() => test).NotNull();
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.Should().BeOfType<ArgumentNullException>();
                exceptionMessage.Should().Contain("cannot be null.");
            }

            [Fact]
            public void When_testing_meets_with_null_ensureFunction_against_string_value_should_throw_ArgumentNullException_on_ensureFunction()
            {
                string test = "bob";
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.That(() => test).Meets(
                        null,
                        (name, value) => { throw new ApplicationException("this is what we throw."); });
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.Should().BeOfType<ArgumentNullException>();
                exceptionMessage.Should().Contain("Value for ensureFunction cannot be null.");
            }

            [Fact]
            public void When_testing_meets_with_null_exceptionAction_against_string_value_that_passes_should_throw_ArgumentNullException_on_exceptionAction()
            {
                string test = "bob";
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.That(() => test).Meets(
                        s => s == s.ToLower(),
                        null);
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.Should().BeOfType<ArgumentNullException>();
                exceptionMessage.Should().Contain("exceptionAction");
                exceptionMessage.Should().Contain("cannot be null.");
            }

            [Fact]
            public void When_testing_meets_with_null_exceptionAction_against_string_value_that_fails_should_throw_ArgumentNullException_on_exceptionAction()
            {
                string test = "bob";
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.That(() => test).Meets(
                        s => s == s.ToUpper(),
                        null);
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.Should().BeOfType<ArgumentNullException>();
                exceptionMessage.Should().Contain("exceptionAction");
                exceptionMessage.Should().Contain("cannot be null.");
            }

            [Fact]
            public void When_testing_meets_with_null_ensureFunction_against_null_value_should_throw_ArgumentNullException_on_ensureFunction()
            {
                string test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.That(() => test).Meets(
                        null,
                        (name, value) => { throw new ApplicationException("this is what we throw."); });
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.Should().BeOfType<ArgumentNullException>();
                exceptionMessage.Should().Contain("ensureFunction");
                exceptionMessage.Should().Contain("cannot be null.");
            }

            [Fact]
            public void When_testing_meets_with_null_exceptionAction_against_null_value_should_throw_ArgumentNullException_on_exceptionAction()
            {
                string test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.That(() => test).Meets(
                        s => s == s.ToLower(),
                        null);
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.Should().BeOfType<ArgumentNullException>();
                exceptionMessage.Should().Contain("exceptionAction");
                exceptionMessage.Should().Contain("cannot be null.");
            }

            [Fact]
            public void When_testing_meets_with_null_everything_should_throw_ArgumentNullException_on_ensureFunction()
            {
                string test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.That(() => test).Meets(
                        null,
                        null);
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.Should().BeOfType<ArgumentNullException>();
                exceptionMessage.Should().Contain("ensureFunction");
                exceptionMessage.Should().Contain("cannot be null.");
            }
        }

        public class When_testing_Ensure_meets_against_a_string_value_that_passes : EnsureSpecsBase
        {
            private object _exceptionType;
            private string _exceptionMessage = string.Empty;
            private bool _exceptionActionInvoked;

            public override void Because()
            {
                string test = "bob";

                try
                {
                    Ensure.That(() => test).Meets(
                        s => s == s.ToLower(),
                        (name, value) =>
                        {
                            _exceptionActionInvoked = true;
                            throw new ApplicationException("this is what we throw.");
                        });
                }
                catch (Exception ex)
                {
                    _exceptionType = ex;
                    _exceptionMessage = ex.Message;
                }
            }

            [Fact]
            public void Should_not_invoke_the_exceptionAction()
            {
                _exceptionActionInvoked.Should().BeFalse();
            }

            [Fact]
            public void Should_not_return_a_specified_exception_since_there_was_no_failure()
            {
                _exceptionType.Should().BeNull();
            }

            [Fact]
            public void Should_not_return_the_specified_error_message()
            {
                _exceptionMessage.Should().NotContain("this is what we throw.");
            }

            [Fact]
            public void Should_not_log_an_error()
            {
                MockLogger.Verify(l => l.Error(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_testing_Ensure_meets_against_a_string_value_that_fails : EnsureSpecsBase
        {
            private object _exceptionType;
            private string _exceptionMessage = string.Empty;
            private bool _exceptionActionInvoked;

            public override void Because()
            {
                string test = "BOB";

                try
                {
                    Ensure.That(() => test).Meets(
                        s => s == s.ToLower(),
                        (name, value) =>
                        {
                            _exceptionActionInvoked = true;
                            throw new ApplicationException("this is what we throw.");
                        });
                }
                catch (Exception ex)
                {
                    _exceptionType = ex;
                    _exceptionMessage = ex.Message;
                }
            }

            [Fact]
            public void Should_invoke_the_exceptionAction()
            {
                _exceptionActionInvoked.Should().BeTrue();
            }

            [Fact]
            public void Should_return_the_specified_exception_of_type_ApplicationException()
            {
                _exceptionType.Should().BeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_return_the_specified_error_message()
            {
                _exceptionMessage.Should().Contain("this is what we throw.");
            }

            [Fact]
            public void Should_not_log_an_error()
            {
                MockLogger.Verify(l => l.Error(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_testing_Ensure_meets_against_a_null_value_without_guarding_the_value : EnsureSpecsBase
        {
            private object _exceptionType;
            private string _exceptionMessage = string.Empty;
            private bool _exceptionActionInvoked;

            public override void Because()
            {
                string test = null;

                try
                {
                    Ensure.That(() => test).Meets(
                        s => s == s.ToLower(),
                        (name, value) =>
                        {
                            _exceptionActionInvoked = true;
                            throw new ApplicationException("this is what we throw.");
                        });
                }
                catch (Exception ex)
                {
                    _exceptionType = ex;
                    _exceptionMessage = ex.Message;
                }
            }

            [Fact]
            public void Should_not_invoke_the_exceptionAction()
            {
                _exceptionActionInvoked.Should().BeFalse();
            }

            [Fact]
            public void Should_throw_an_error()
            {
                _exceptionType.Should().NotBeNull();
            }

            [Fact]
            public void Should_not_return_the_specified_exception_of_type_ApplicationException()
            {
                _exceptionType.Should().NotBeOfType<ApplicationException>();
            }

            [Fact]
            public void Should_not_return_the_specified_error_message()
            {
                _exceptionMessage.Should().NotContain("this is what we throw.");
            }

            //[Fact]
            //public void Should_log_an_error()
            //{
            //    MockLogger.Verify(l => l.Error(It.IsAny<string>()), Times.Once);
            //}

            //    [Fact]
            //    public void Should_log_the_error_we_expect()
            //    {
            //       var messages = MockLogger.MessagesFor(LogLevel.Error);
            //        messages.Should().NotBeEmpty();
            //        messages.Should().ContainSingle();
            //        messages[0].Should().Contain("Trying to call ensureFunction on");
            //    }
        }
    }
}
