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

namespace chocolatey.tests.infrastructure.tolerance
{
    using System;
    using chocolatey.infrastructure.tolerance;
    using NUnit.Framework;
    using FluentAssertions;

    public class FaultToleranceSpecs
    {
        public abstract class FaultToleranceSpecsBase : TinySpec
        {
            public override void Context()
            {
            }

            protected void Reset()
            {
                MockLogger.Reset();
            }
        }

        public class When_retrying_an_action : FaultToleranceSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_not_allow_the_number_of_retries_to_be_zero()
            {
                Reset();

                Action m = () => FaultTolerance.Retry(
                    0,
                    () =>
                    {
                    });

                m.Should().Throw<ApplicationException>();
            }

            [Fact]
            public void Should_throw_an_error_if_retries_are_reached()
            {
                Reset();

                Action m = () => FaultTolerance.Retry(2, () => { throw new Exception("YIKES"); }, waitDurationMilliseconds: 0);

                m.Should().Throw<Exception>();
            }

            [Fact]
            public void Should_log_warning_each_time()
            {
                Reset();

                try
                {
                    FaultTolerance.Retry(3, () => { throw new Exception("YIKES"); }, waitDurationMilliseconds: 0);
                }
                catch
                {
                    // don't care
                }

                MockLogger.MessagesFor(LogLevel.Warn).Should().HaveCount(2);
            }

            [Fact]
            public void Should_retry_the_number_of_times_specified()
            {
                Reset();

                var i = 0;
                try
                {
                    FaultTolerance.Retry(
                        10,
                        () =>
                        {
                            i += 1;
                            throw new Exception("YIKES");
                        },
                        waitDurationMilliseconds: 0);
                }
                catch
                {
                    // don't care
                }

                i.Should().Be(10);
            }

            [Fact]
            public void Should_return_immediately_when_successful()
            {
                Reset();

                var i = 0;
                FaultTolerance.Retry(3, () => { i += 1; }, waitDurationMilliseconds: 0);

                i.Should().Be(1);

                MockLogger.MessagesFor(LogLevel.Warn).Should().BeEmpty();
            }
        }

        public class When_wrapping_a_function_with_try_catch : FaultToleranceSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_log_an_error_message()
            {
                Reset();

                FaultTolerance.TryCatchWithLoggingException(
                    () => { throw new Exception("This is the message"); },
                    "You have an error"
                );

                MockLogger.MessagesFor(LogLevel.Error).Should().ContainSingle();
            }

            [Fact]
            public void Should_log_the_expected_error_message()
            {
                Reset();

                FaultTolerance.TryCatchWithLoggingException(
                    () => { throw new Exception("This is the message"); },
                    "You have an error"
                );

                MockLogger.MessagesFor(LogLevel.Error)[0].Should().Be("You have an error:{0} This is the message".FormatWith(Environment.NewLine));
            }

            [Fact]
            public void Should_log_a_warning_message_when_set_to_warn()
            {
                Reset();

                FaultTolerance.TryCatchWithLoggingException(
                    () => { throw new Exception("This is the message"); },
                    "You have an error",
                    logWarningInsteadOfError: true
                );

                MockLogger.MessagesFor(LogLevel.Warn).Should().ContainSingle();
            }

            [Fact]
            public void Should_throw_an_error_if_throwError_set_to_true()
            {
                Reset();

                Action m = () => FaultTolerance.TryCatchWithLoggingException(
                    () => { throw new Exception("This is the message"); },
                    "You have an error",
                    throwError: true
                );

                m.Should().Throw<Exception>();
            }

            [Fact]
            public void Should_still_throw_an_error_when_warn_is_set_if_throwError_set_to_true()
            {
                Reset();

                Action m = () => FaultTolerance.TryCatchWithLoggingException(
                    () => { throw new Exception("This is the message"); },
                    "You have an error",
                    logWarningInsteadOfError: true,
                    throwError: true
                );

                m.Should().Throw<Exception>();
            }
        }
    }
}
