// Copyright © 2017 - 2026 Chocolatey Software, Inc
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

using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Chocolatey.PowerShell.Helpers;
using FluentAssertions;
using Moq;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public partial class WebHelperSpecs
    {
        public abstract class WebHelperSpecsBase : EnvironmentSpecsBase
        {
            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestCmdlet Cmdlet;
            protected WebRequestOptions Options;
            protected TestWebHelper Helper;

            public override void Context()
            {
                base.Context();

                CommandRuntime = new Mock<ICommandRuntime>();
                Cmdlet = new TestCmdlet { CommandRuntime = CommandRuntime.Object };
                Options = new WebRequestOptions { Uri = new Uri("http://localhost/file.zip") };
                Helper = new TestWebHelper(Cmdlet, Options);
            }

            public override void AfterObservations()
            {
                Helper.Dispose();
                base.AfterObservations();
            }
        }

        public abstract class ReadBufferSpecsBase : TinySpec
        {
            protected CancellationTokenSource TokenSource;
            protected TestWebHelper Helper;
            protected byte[] Target = new byte[10];
            protected int Count;
            protected Exception Error;

            protected virtual bool UseCancellationToken
            {
                get { return true; }
            }

            public override void Context()
            {
                TokenSource = new CancellationTokenSource();
                Helper = UseCancellationToken
                    ? new TestWebHelper(new TestCmdlet(), new WebRequestOptions(), TokenSource.Token)
                    : new TestWebHelper(new TestCmdlet(), new WebRequestOptions());
            }

            public override void AfterObservations()
            {
                Helper.Dispose();
                TokenSource.Dispose();
            }
        }

        public class When_reading_without_a_cancellation_token : ReadBufferSpecsBase
        {
            protected override bool UseCancellationToken
            {
                get { return false; }
            }

            public override void Because()
            {
                Count = Helper.Read(new MemoryStream(new byte[] { 1, 2, 3 }), Target);
            }

            [Fact]
            public void Should_return_the_number_of_bytes_read()
            {
                Count.Should().Be(3);
            }

            [Fact]
            public void Should_fill_the_buffer()
            {
                Target.Take(3).Should().Equal(1, 2, 3);
            }
        }

        public class When_reading_with_a_token_that_has_not_been_cancelled : ReadBufferSpecsBase
        {
            public override void Because()
            {
                Count = Helper.Read(new MemoryStream(new byte[] { 1, 2, 3 }), Target);
            }

            [Fact]
            public void Should_return_the_number_of_bytes_read()
            {
                Count.Should().Be(3);
            }

            [Fact]
            public void Should_fill_the_buffer()
            {
                Target.Take(3).Should().Equal(1, 2, 3);
            }
        }

        public class When_reading_at_the_end_of_the_stream : ReadBufferSpecsBase
        {
            public override void Because()
            {
                Count = Helper.Read(new MemoryStream(), Target);
            }

            [Fact]
            public void Should_return_zero()
            {
                Count.Should().Be(0);
            }
        }

        public class When_the_read_fails : ReadBufferSpecsBase
        {
            public override void Because()
            {
                try
                {
                    Helper.Read(new FaultingStream(), Target);
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }

            [Fact]
            public void Should_throw_the_original_exception_rather_than_an_aggregate()
            {
                Error.Should().BeOfType<IOException>().Which.Message.Should().Be("The read failed.");
            }
        }

        public class When_the_token_is_already_cancelled : ReadBufferSpecsBase
        {
            public override void Context()
            {
                base.Context();
                TokenSource.Cancel();
            }

            public override void Because()
            {
                try
                {
                    Helper.Read(new MemoryStream(new byte[] { 1, 2, 3 }), Target);
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }

            [Fact]
            public void Should_throw_an_operation_cancelled_exception()
            {
                Error.Should().BeAssignableTo<OperationCanceledException>();
            }
        }

        public class When_the_token_is_cancelled_while_the_read_is_pending : ReadBufferSpecsBase
        {
            private bool _completed;

            public override void Because()
            {
                TokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

                var read = Task.Run(() => Helper.Read(new HangingStream(), Target));
                _completed = ((IAsyncResult)read).AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));

                Error = read.Exception?.InnerException;
            }

            [Fact]
            public void Should_stop_waiting_for_the_read()
            {
                _completed.Should().BeTrue();
            }

            [Fact]
            public void Should_throw_an_operation_cancelled_exception()
            {
                Error.Should().BeAssignableTo<OperationCanceledException>();
            }
        }

        public class When_a_read_does_not_complete_within_the_read_timeout : ReadBufferSpecsBase
        {
            private bool _completed;

            protected override bool UseCancellationToken
            {
                get { return false; }
            }

            public override void Context()
            {
                base.Context();
                Helper.SetReadTimeout(TimeSpan.FromMilliseconds(100));
            }

            public override void Because()
            {
                var read = Task.Run(() => Helper.Read(new HangingStream(), Target));
                _completed = ((IAsyncResult)read).AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));

                Error = read.Exception?.InnerException;
            }

            [Fact]
            public void Should_stop_waiting_for_the_read()
            {
                _completed.Should().BeTrue();
            }

            [Fact]
            public void Should_throw_a_web_exception_with_a_timeout_status()
            {
                Error.Should().BeOfType<WebException>().Which.Status.Should().Be(WebExceptionStatus.Timeout);
            }
        }

        public class When_a_read_completes_within_the_read_timeout : ReadBufferSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Helper.SetReadTimeout(TimeSpan.FromSeconds(30));
            }

            public override void Because()
            {
                Count = Helper.Read(new MemoryStream(new byte[] { 1, 2, 3 }), Target);
            }

            [Fact]
            public void Should_return_the_number_of_bytes_read()
            {
                Count.Should().Be(3);
            }
        }

        public class When_the_token_is_cancelled_while_a_read_timeout_is_configured : ReadBufferSpecsBase
        {
            private bool _completed;

            public override void Context()
            {
                base.Context();
                Helper.SetReadTimeout(TimeSpan.FromSeconds(30));
            }

            public override void Because()
            {
                TokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

                var read = Task.Run(() => Helper.Read(new HangingStream(), Target));
                _completed = ((IAsyncResult)read).AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10));

                Error = read.Exception?.InnerException;
            }

            [Fact]
            public void Should_stop_waiting_for_the_read()
            {
                _completed.Should().BeTrue();
            }

            [Fact]
            public void Should_report_a_cancellation_rather_than_a_timeout()
            {
                Error.Should().BeAssignableTo<OperationCanceledException>();
                Error.Should().NotBeOfType<WebException>();
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
