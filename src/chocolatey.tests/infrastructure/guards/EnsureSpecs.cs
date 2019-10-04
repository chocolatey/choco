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

namespace chocolatey.tests.infrastructure.guards
{
    using System;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.guards;
    using Moq;
    using Should;

    public class EnsureSpecs
    {
        public abstract class EnsureSpecsBase : TinySpec
        {
            public override void Context()
            {
            }
        }

        public class when_Ensure_is_being_set_to_a_type : EnsureSpecsBase
        {
            private object result;
            private readonly string bob = "something";

            public override void Because()
            {
                result = Ensure.that(() => bob);
            }

            [Fact]
            public void should_return_a_type_of_string_for_ensuring()
            {
                result.ShouldBeType<Ensure<string>>();
            }

            [Fact]
            public void should_have_the_value_specified()
            {
                var bobEnsure = result as Ensure<string>;
                bobEnsure.Value.ShouldEqual(bob);
            }
        }

        public class when_using_Ensure : EnsureSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void when_testing_a_string_against_is_not_null_should_pass()
            {
                string test = "value";
                Ensure.that(() => test).is_not_null();
            }

            [Fact]
            public void when_testing_a_null_string_against_is_not_null_should_throw_an_Argument_exception()
            {
                string test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.that(() => test).is_not_null();
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.ShouldBeType<ArgumentNullException>();
                exceptionMessage.ShouldContain("cannot be null.");
            }

            [Fact]
            public void when_testing_a_Func_against_is_not_null_should_pass()
            {
                Func<string> test = () => "value";
                Ensure.that(() => test).is_not_null();
            }

            [Fact]
            public void when_testing_a_null_Func_against_is_not_null_should_throw_an_Argument_exception()
            {
                Func<string> test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.that(() => test).is_not_null();
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.ShouldBeType<ArgumentNullException>();
                exceptionMessage.ShouldContain("cannot be null.");
            }

            [Fact]
            public void when_testing_a_class_against_is_not_null_should_pass()
            {
                var test = new ChocolateyConfiguration();
                Ensure.that(() => test).is_not_null();
            }

            [Fact]
            public void when_testing_an_uninstantiated_class_against_is_not_null_should_throw_an_Argument_exception()
            {
                ChocolateyConfiguration test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.that(() => test).is_not_null();
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.ShouldBeType<ArgumentNullException>();
                exceptionMessage.ShouldContain("cannot be null.");
            }

            [Fact]
            public void when_testing_meets_with_null_ensureFunction_against_string_value_should_throw_ArgumentNullException_on_ensureFunction()
            {
                string test = "bob";
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.that(() => test).meets(
                        null,
                        (name, value) => { throw new ApplicationException("this is what we throw."); });
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.ShouldBeType<ArgumentNullException>();
                exceptionMessage.ShouldContain("Value for ensureFunction cannot be null.");
            }

            [Fact]
            public void when_testing_meets_with_null_exceptionAction_against_string_value_that_passes_should_throw_ArgumentNullException_on_exceptionAction()
            {
                string test = "bob";
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.that(() => test).meets(
                        s => s == s.ToLower(),
                        null);
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.ShouldBeType<ArgumentNullException>();
                exceptionMessage.ShouldContain("exceptionAction");
                exceptionMessage.ShouldContain("cannot be null.");
            }

            [Fact]
            public void when_testing_meets_with_null_exceptionAction_against_string_value_that_fails_should_throw_ArgumentNullException_on_exceptionAction()
            {
                string test = "bob";
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.that(() => test).meets(
                        s => s == s.ToUpper(),
                        null);
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.ShouldBeType<ArgumentNullException>();
                exceptionMessage.ShouldContain("exceptionAction");
                exceptionMessage.ShouldContain("cannot be null.");
            }

            [Fact]
            public void when_testing_meets_with_null_ensureFunction_against_null_value_should_throw_ArgumentNullException_on_ensureFunction()
            {
                string test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.that(() => test).meets(
                        null,
                        (name, value) => { throw new ApplicationException("this is what we throw."); });
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.ShouldBeType<ArgumentNullException>();
                exceptionMessage.ShouldContain("ensureFunction");
                exceptionMessage.ShouldContain("cannot be null.");
            }

            [Fact]
            public void when_testing_meets_with_null_exceptionAction_against_null_value_should_throw_ArgumentNullException_on_exceptionAction()
            {
                string test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.that(() => test).meets(
                        s => s == s.ToLower(),
                        null);
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.ShouldBeType<ArgumentNullException>();
                exceptionMessage.ShouldContain("exceptionAction");
                exceptionMessage.ShouldContain("cannot be null.");
            }

            [Fact]
            public void when_testing_meets_with_null_everything_should_throw_ArgumentNullException_on_ensureFunction()
            {
                string test = null;
                object exceptionType = null;
                var exceptionMessage = string.Empty;
                try
                {
                    Ensure.that(() => test).meets(
                        null,
                        null);
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }

                exceptionType.ShouldBeType<ArgumentNullException>();
                exceptionMessage.ShouldContain("ensureFunction");
                exceptionMessage.ShouldContain("cannot be null.");
            }
        }

        public class when_testing_Ensure_meets_against_a_string_value_that_passes : EnsureSpecsBase
        {
            private object exceptionType;
            private string exceptionMessage = string.Empty;
            private bool exceptionActionInvoked;

            public override void Because()
            {
                string test = "bob";

                try
                {
                    Ensure.that(() => test).meets(
                        s => s == s.ToLower(),
                        (name, value) =>
                        {
                            exceptionActionInvoked = true;
                            throw new ApplicationException("this is what we throw.");
                        });
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }
            }

            [Fact]
            public void should_not_invoke_the_exceptionAction()
            {
                exceptionActionInvoked.ShouldBeFalse();
            }

            [Fact]
            public void should_not_return_a_specified_exception_since_there_was_no_failure()
            {
                exceptionType.ShouldBeNull();
            }

            [Fact]
            public void should_not_return_the_specified_error_message()
            {
                exceptionMessage.ShouldNotContain("this is what we throw.");
            }

            [Fact]
            public void should_not_log_an_error()
            {
                MockLogger.Verify(l => l.Error(It.IsAny<string>()), Times.Never);
            }
        }

        public class when_testing_Ensure_meets_against_a_string_value_that_fails : EnsureSpecsBase
        {
            private object exceptionType;
            private string exceptionMessage = string.Empty;
            private bool exceptionActionInvoked;

            public override void Because()
            {
                string test = "BOB";

                try
                {
                    Ensure.that(() => test).meets(
                        s => s == s.ToLower(),
                        (name, value) =>
                        {
                            exceptionActionInvoked = true;
                            throw new ApplicationException("this is what we throw.");
                        });
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }
            }

            [Fact]
            public void should_invoke_the_exceptionAction()
            {
                exceptionActionInvoked.ShouldBeTrue();
            }

            [Fact]
            public void should_return_the_specified_exception_of_type_ApplicationException()
            {
                exceptionType.ShouldBeType<ApplicationException>();
            }

            [Fact]
            public void should_return_the_specified_error_message()
            {
                exceptionMessage.ShouldContain("this is what we throw.");
            }

            [Fact]
            public void should_not_log_an_error()
            {
                MockLogger.Verify(l => l.Error(It.IsAny<string>()), Times.Never);
            }
        }

        public class when_testing_Ensure_meets_against_a_null_value_without_guarding_the_value : EnsureSpecsBase
        {
            private object exceptionType;
            private string exceptionMessage = string.Empty;
            private bool exceptionActionInvoked;

            public override void Because()
            {
                string test = null;

                try
                {
                    Ensure.that(() => test).meets(
                        s => s == s.ToLower(),
                        (name, value) =>
                        {
                            exceptionActionInvoked = true;
                            throw new ApplicationException("this is what we throw.");
                        });
                }
                catch (Exception ex)
                {
                    exceptionType = ex;
                    exceptionMessage = ex.Message;
                }
            }

            [Fact]
            public void should_not_invoke_the_exceptionAction()
            {
                exceptionActionInvoked.ShouldBeFalse();
            }

            [Fact]
            public void should_throw_an_error()
            {
                exceptionType.ShouldNotBeNull();
            }

            [Fact]
            public void should_not_return_the_specified_exception_of_type_ApplicationException()
            {
                exceptionType.ShouldNotBeType<ApplicationException>();
            }

            [Fact]
            public void should_not_return_the_specified_error_message()
            {
                exceptionMessage.ShouldNotContain("this is what we throw.");
            }

            //[Fact]
            //public void should_log_an_error()
            //{
            //    MockLogger.Verify(l => l.Error(It.IsAny<string>()), Times.Once);
            //}

            //    [Fact]
            //    public void should_log_the_error_we_expect()
            //    {
            //       var messages = MockLogger.MessagesFor(LogLevel.Error);
            //        messages.ShouldNotBeEmpty();
            //        messages.Count.ShouldEqual(1);
            //        messages[0].ShouldContain("Trying to call ensureFunction on");
            //    }
        }
    }
}
