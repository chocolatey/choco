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
using System.Management.Automation;
using System.Net;
using System.Text;
using System.Threading;
using FluentAssertions;
using Moq;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class FtpHelperSpecs
    {
        public abstract class FtpSpecsBase : EnvironmentSpecsBase
        {
            protected static readonly byte[] Content = Encoding.ASCII.GetBytes("0123456789");

            protected Mock<ICommandRuntime> CommandRuntime;
            protected TestCmdlet Cmdlet;
            protected TestFtpHelper Helper;
            protected CancellationTokenSource TokenSource;
            protected Exception Error;

            protected override string[] ManagedVariables { get; } =
            {
                Package.ChocolateyRequestTimeout,
                Package.ChocolateyResponseTimeout,
                Package.DownloadCacheAvailable,
                Package.ChocolateyProxyLocation,
                Package.ChocolateyProxyBypassOnLocal,
                Package.ChocolateyProxyBypassList,
                Package.ChocolateyProxyUser,
                Package.ChocolateyProxyPassword,
            };

            protected virtual string Address
            {
                get { return "ftp://example.com/files/file.zip"; }
            }

            protected virtual bool Quiet
            {
                get { return false; }
            }

            protected virtual bool UseCancellationToken
            {
                get { return false; }
            }

            protected virtual WebResponse Respond()
            {
                return new FakeFtpResponse(Content);
            }

            public override void Context()
            {
                base.Context();

                CommandRuntime = new Mock<ICommandRuntime>();
                Cmdlet = new TestCmdlet { CommandRuntime = CommandRuntime.Object };
                TokenSource = new CancellationTokenSource();
                Helper = UseCancellationToken
                    ? new TestFtpHelper(Cmdlet, new Uri(Address), "someone", "hunter2", TokenSource.Token)
                    : new TestFtpHelper(Cmdlet, new Uri(Address), "someone", "hunter2");
                Helper.Request = new FakeFtpRequest(Respond);
            }

            public override void Because()
            {
                try
                {
                    Helper.DownloadFile("file.zip", Quiet);
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }

            public override void AfterObservations()
            {
                Helper.Dispose();
                TokenSource.Dispose();
                base.AfterObservations();
            }
        }

        public class When_the_file_is_downloaded : FtpSpecsBase
        {
            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_write_the_content_to_the_target()
            {
                Helper.Written.ToArray().Should().Equal(Content);
            }

            [Fact]
            public void Should_ask_to_download_the_file()
            {
                Helper.Request.Method.Should().Be(WebRequestMethods.Ftp.DownloadFile);
            }

            [Fact]
            public void Should_connect_with_the_credentials()
            {
                var credential = Helper.Request.Credentials.Should().BeOfType<NetworkCredential>().Subject;
                credential.UserName.Should().Be("someone");
                credential.Password.Should().Be("hunter2");
            }

            [Fact]
            public void Should_not_set_a_proxy()
            {
                Helper.Request.Proxy.Should().BeNull();
            }

            [Fact]
            public void Should_not_set_a_timeout()
            {
                Helper.Request.TimeoutWasSet.Should().BeFalse();
            }

            [Fact]
            public void Should_write_the_progress_of_the_download()
            {
                CommandRuntime.Verify(
                    r => r.WriteProgress(It.Is<ProgressRecord>(p => p.PercentComplete == 100 && p.StatusDescription == "Saving 10 B of 10 B (10/10)")),
                    Times.AtLeastOnce);
            }

            [Fact]
            public void Should_complete_the_progress()
            {
                CommandRuntime.Verify(
                    r => r.WriteProgress(It.Is<ProgressRecord>(p => p.RecordType == ProgressRecordType.Completed && p.Activity == "Completed download of ftp://example.com/files/file.zip.")),
                    Times.Once);
            }

            [Fact]
            public void Should_say_that_the_download_completed()
            {
                CommandRuntime.Verify(r => r.WriteVerbose("Download of 'file.zip' (10 B) completed."), Times.Once);
            }

            [Fact]
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }
        }

        public class When_the_download_is_quiet : FtpSpecsBase
        {
            protected override bool Quiet
            {
                get { return true; }
            }

            [Fact]
            public void Should_write_the_content_to_the_target()
            {
                Helper.Written.ToArray().Should().Equal(Content);
            }

            [Fact]
            public void Should_not_write_any_progress()
            {
                CommandRuntime.Verify(r => r.WriteProgress(It.IsAny<ProgressRecord>()), Times.Never);
            }

            [Fact]
            public void Should_still_say_that_the_download_completed()
            {
                CommandRuntime.Verify(r => r.WriteVerbose("Download of 'file.zip' (10 B) completed."), Times.Once);
            }
        }

        public class When_the_length_of_the_file_is_not_known : FtpSpecsBase
        {
            protected override WebResponse Respond()
            {
                return new FakeFtpResponse(Content, -1);
            }

            [Fact]
            public void Should_write_the_content_to_the_target()
            {
                Helper.Written.ToArray().Should().Equal(Content);
            }

            [Fact]
            public void Should_write_the_number_of_bytes_saved_as_the_progress()
            {
                CommandRuntime.Verify(
                    r => r.WriteProgress(It.Is<ProgressRecord>(p => p.StatusDescription == "Saving 10 bytes..." && p.RecordType == ProgressRecordType.Completed)),
                    Times.AtLeastOnce);
            }
        }

        public class When_an_explicit_proxy_is_configured : FtpSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyUser, "proxyuser");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyPassword, "proxypassword");
            }

            [Fact]
            public void Should_use_the_proxy()
            {
                var proxy = Helper.Request.Proxy.Should().BeOfType<WebProxy>().Subject;
                proxy.Address.Should().Be(new Uri("http://proxy.example.com:8080"));
                proxy.Credentials.Should().BeOfType<NetworkCredential>().Which.UserName.Should().Be("proxyuser");
            }

            [Fact]
            public void Should_say_which_proxy_is_used()
            {
                CommandRuntime.Verify(r => r.WriteVerbose("Using explicit proxy server 'http://proxy.example.com:8080'."), Times.Once);
            }
        }

        public class When_the_request_timeout_is_configured : FtpSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyRequestTimeout, "5000");
            }

            [Fact]
            public void Should_use_it_for_the_request()
            {
                Helper.Request.Timeout.Should().Be(5000);
            }
        }

        public class When_the_request_timeout_is_not_a_positive_number : FtpSpecsBase
        {
            [InlineData("0")]
            [InlineData("-1")]
            [InlineData("abc")]
            public void Should_not_set_a_timeout(string value)
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyRequestTimeout, value);
                var request = new FakeFtpRequest(Respond);
                var helper = new TestFtpHelper(Cmdlet, new Uri(Address), "someone", "hunter2") { Request = request };

                helper.DownloadFile("file.zip", quiet: true);

                request.TimeoutWasSet.Should().BeFalse();
            }
        }

        public class When_the_server_responds_with_an_error : FtpSpecsBase
        {
            protected override WebResponse Respond()
            {
                throw new WebException("The remote server returned an error: (550) File unavailable.");
            }

            [Fact]
            public void Should_throw_a_web_exception()
            {
                Error.Should().BeOfType<WebException>();
            }

            [Fact]
            public void Should_name_the_url_and_the_error()
            {
                Error.Message.Should().Be("The remote file either doesn't exist, is unauthorized, or is forbidden for url 'ftp://example.com/files/file.zip'. The remote server returned an error: (550) File unavailable.");
            }

            [Fact]
            public void Should_keep_the_original_error_as_the_cause()
            {
                Error.InnerException.Should().BeOfType<WebException>();
            }

            [Fact]
            public void Should_abort_the_request()
            {
                Helper.Request.Aborted.Should().BeTrue();
            }

            [Fact]
            public void Should_report_the_exit_code()
            {
                Helper.ReportedExitCode.Should().Be(404);
            }

            [Fact]
            public void Should_not_write_anything_to_the_target()
            {
                Helper.Written.ToArray().Should().BeEmpty();
            }
        }

        public class When_the_download_cache_is_available_and_the_server_fails : FtpSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.DownloadCacheAvailable, "true");
            }

            protected override WebResponse Respond()
            {
                throw new WebException("The remote server returned an error: (550) File unavailable.");
            }

            [Fact]
            public void Should_not_add_the_private_cdn_hint_as_there_is_no_cache_for_ftp_downloads()
            {
                Error.Message.Should().Be("The remote file either doesn't exist, is unauthorized, or is forbidden for url 'ftp://example.com/files/file.zip'. The remote server returned an error: (550) File unavailable.");
            }
        }

        public class When_the_connection_fails_while_reading : FtpSpecsBase
        {
            protected override WebResponse Respond()
            {
                return new FakeFtpResponse(new FaultingReadStream(), 10);
            }

            [Fact]
            public void Should_throw_a_web_exception()
            {
                Error.Should().BeOfType<WebException>();
            }

            [Fact]
            public void Should_report_the_exit_code()
            {
                Helper.ReportedExitCode.Should().Be(404);
            }
        }

        public class When_the_download_is_cancelled : FtpSpecsBase
        {
            protected override bool UseCancellationToken
            {
                get { return true; }
            }

            protected override WebResponse Respond()
            {
                TokenSource.Cancel();

                // Aborting a request makes it fail in some other way than a cancellation.
                throw new WebException("The request was aborted.", WebExceptionStatus.RequestCanceled);
            }

            [Fact]
            public void Should_report_a_cancellation()
            {
                Error.Should().BeAssignableTo<OperationCanceledException>();
            }

            [Fact]
            public void Should_abort_the_request()
            {
                Helper.Request.Aborted.Should().BeTrue();
            }

            [Fact]
            public void Should_not_report_a_missing_remote_file()
            {
                Error.Should().NotBeOfType<WebException>();
            }

            [Fact]
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }
        }

        public class When_the_download_is_cancelled_before_the_request_is_sent : FtpSpecsBase
        {
            private int _requestsSent;

            protected override bool UseCancellationToken
            {
                get { return true; }
            }

            public override void Context()
            {
                base.Context();
                TokenSource.Cancel();
            }

            protected override WebResponse Respond()
            {
                _requestsSent++;

                return base.Respond();
            }

            [Fact]
            public void Should_report_a_cancellation()
            {
                Error.Should().BeAssignableTo<OperationCanceledException>();
            }

            [Fact]
            public void Should_not_send_the_request()
            {
                _requestsSent.Should().Be(0);
            }

            [Fact]
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }
        }

        public class When_the_helper_is_disposed : FtpSpecsBase
        {
            private bool _abortedBeforeDisposal;

            public override void Because()
            {
                base.Because();
                _abortedBeforeDisposal = Helper.Request.Aborted;

                Helper.Dispose();
            }

            [Fact]
            public void Should_not_have_aborted_a_request_that_succeeded()
            {
                _abortedBeforeDisposal.Should().BeFalse();
            }

            [Fact]
            public void Should_abort_the_request()
            {
                Helper.Request.Aborted.Should().BeTrue();
            }
        }

        public class When_the_address_is_not_an_ftp_address : FtpSpecsBase
        {
            protected override string Address
            {
                get { return "http://example.com/files/file.zip"; }
            }

            public override void Context()
            {
                base.Context();
                Helper.Request = null;
            }

            [Fact]
            public void Should_throw_an_argument_exception()
            {
                Error.Should().BeOfType<ArgumentException>().Which.Message.Should().Contain("The address 'http://example.com/files/file.zip' is not an FTP address.");
            }

            [Fact]
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }
        }
    }

    // ReSharper restore InconsistentNaming
}