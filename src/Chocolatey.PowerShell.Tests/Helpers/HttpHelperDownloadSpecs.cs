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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public partial class HttpHelperSpecs
    {
        public abstract class DownloadSpecsBase : HttpHelperSpecsBase
        {
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

            protected virtual bool UseCancellationToken
            {
                get { return false; }
            }

            protected abstract Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken);

            public override void Context()
            {
                base.Context();

                TokenSource = new CancellationTokenSource();
                if (UseCancellationToken)
                {
                    Helper.Dispose();
                    Helper = new TestHttpHelper(Cmdlet, Options, TokenSource.Token);
                }

                Helper.MessageHandler = new StubMessageHandler(Respond);
            }

            public override void Because()
            {
                try
                {
                    Helper.DownloadFile(null, true, true);
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }

            public override void AfterObservations()
            {
                TokenSource.Dispose();
                base.AfterObservations();
            }
        }

        public abstract class DownloadErrorSpecsBase : DownloadSpecsBase
        {
            [Fact]
            public void Should_report_the_exit_code()
            {
                Helper.ReportedExitCode.Should().Be(404);
            }
        }

        public abstract class DownloadFailureSpecsBase : DownloadErrorSpecsBase
        {
            protected abstract HttpStatusCode Status { get; }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent("an error page") });
            }
        }

        public class When_the_server_responds_with_not_found : DownloadFailureSpecsBase
        {
            protected override HttpStatusCode Status
            {
                get { return HttpStatusCode.NotFound; }
            }

            [Fact]
            public void Should_throw_a_web_exception()
            {
                Error.Should().BeOfType<WebException>();
            }

            [Fact]
            public void Should_name_the_url_and_the_status()
            {
                Error.Message.Should().Contain("for url 'http://localhost/file.zip'").And.Contain("(404)");
            }

            [Fact]
            public void Should_not_add_the_private_cdn_hint()
            {
                Error.Message.Should().NotContain("private-cdn");
            }

            [Fact]
            public void Should_not_write_any_output()
            {
                CommandRuntime.Verify(r => r.WriteObject(It.IsAny<object>(), It.IsAny<bool>()), Times.Never);
            }
        }

        public class When_the_server_responds_with_a_server_error : DownloadFailureSpecsBase
        {
            protected override HttpStatusCode Status
            {
                get { return HttpStatusCode.InternalServerError; }
            }

            [Fact]
            public void Should_throw_a_web_exception_naming_the_status()
            {
                Error.Should().BeOfType<WebException>().Which.Message.Should().Contain("(500)");
            }
        }

        public class When_the_download_cache_is_available_and_the_server_fails : DownloadFailureSpecsBase
        {
            protected override HttpStatusCode Status
            {
                get { return HttpStatusCode.NotFound; }
            }

            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.DownloadCacheAvailable, "true");
            }

            [Fact]
            public void Should_add_the_private_cdn_hint()
            {
                Error.Message.Should().Contain("private-cdn");
            }
        }

        public class When_the_request_times_out : DownloadErrorSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyRequestTimeout, "100");
            }

            protected override async Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(-1, cancellationToken);

                return null;
            }

            [Fact]
            public void Should_report_a_web_exception_rather_than_a_cancellation()
            {
                Error.Should().BeOfType<WebException>();
            }

            [Fact]
            public void Should_keep_the_timeout_as_the_cause()
            {
                Error.InnerException.Should().BeOfType<WebException>().Which.Status.Should().Be(WebExceptionStatus.Timeout);
            }
        }

        public abstract class SharedClientSpecsBase : HttpHelperSpecsBase
        {
            protected TestHttpHelper Other;
            protected HttpClient First;
            protected HttpClient Second;

            protected override string[] ManagedVariables { get; } = new string[0];

            protected abstract IWebProxy FirstProxy { get; }

            protected abstract IWebProxy SecondProxy { get; }

            public override void Because()
            {
                Other = new TestHttpHelper(Cmdlet, Options);

                First = Helper.SharedHttpClient(FirstProxy);
                Second = Other.SharedHttpClient(SecondProxy);
            }

            public override void AfterObservations()
            {
                Other.Dispose();
                base.AfterObservations();
            }
        }

        public class When_two_downloads_use_the_same_proxy_configuration : SharedClientSpecsBase
        {
            protected override IWebProxy FirstProxy
            {
                get { return new WebProxy("http://proxy.example.com:8080") { Credentials = new NetworkCredential("someone", "hunter2") }; }
            }

            protected override IWebProxy SecondProxy
            {
                get { return new WebProxy("http://proxy.example.com:8080") { Credentials = new NetworkCredential("someone", "hunter2") }; }
            }

            [Fact]
            public void Should_share_one_client()
            {
                First.Should().BeSameAs(Second);
            }

            [Fact]
            public void Should_leave_each_request_to_apply_its_own_timeout()
            {
                First.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
            }
        }

        public class When_the_proxy_configuration_changes_between_downloads : SharedClientSpecsBase
        {
            protected override IWebProxy FirstProxy
            {
                get { return new WebProxy("http://proxy.example.com:8080"); }
            }

            protected override IWebProxy SecondProxy
            {
                get { return new WebProxy("http://other-proxy.example.com:8080"); }
            }

            [Fact]
            public void Should_use_a_different_client()
            {
                First.Should().NotBeSameAs(Second);
            }
        }

        public class When_one_download_uses_the_default_proxy_and_the_next_uses_a_proxy : SharedClientSpecsBase
        {
            protected override IWebProxy FirstProxy
            {
                get { return null; }
            }

            protected override IWebProxy SecondProxy
            {
                get { return new WebProxy("http://proxy.example.com:8080"); }
            }

            [Fact]
            public void Should_use_a_different_client()
            {
                First.Should().NotBeSameAs(Second);
            }
        }

        public class When_a_proxy_requires_credentials : HttpHelperSpecsBase
        {
            private string _key;
            private string _keyForAnotherPassword;
            private string _keyForTheSameConfiguration;

            protected override string[] ManagedVariables { get; } = new string[0];

            private static WebProxy ProxyWithPassword(string password)
            {
                return new WebProxy("http://proxy.example.com:8080") { Credentials = new NetworkCredential("someone", password) };
            }

            public override void Because()
            {
                _key = Helper.ProxyKey(ProxyWithPassword("hunter2"));
                _keyForAnotherPassword = Helper.ProxyKey(ProxyWithPassword("different"));
                _keyForTheSameConfiguration = Helper.ProxyKey(ProxyWithPassword("hunter2"));
            }

            [Fact]
            public void Should_not_include_the_password_in_the_key()
            {
                _key.Should().NotContain("hunter2");
            }

            [Fact]
            public void Should_give_the_same_key_for_the_same_configuration()
            {
                _keyForTheSameConfiguration.Should().Be(_key);
            }

            [Fact]
            public void Should_give_a_different_key_when_the_password_changes()
            {
                _keyForAnotherPassword.Should().NotBe(_key);
            }
        }

        public class When_the_download_is_cancelled_while_waiting_for_the_response : DownloadSpecsBase
        {
            protected override bool UseCancellationToken
            {
                get { return true; }
            }

            public override void Context()
            {
                base.Context();
                TokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));
            }

            protected override async Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Delay(-1, cancellationToken);

                return null;
            }

            [Fact]
            public void Should_report_a_cancellation()
            {
                Error.Should().BeAssignableTo<OperationCanceledException>();
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

        public class When_a_failure_arrives_after_cancellation_was_requested : DownloadSpecsBase
        {
            protected override bool UseCancellationToken
            {
                get { return true; }
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                TokenSource.Cancel();

                return Task.FromException<HttpResponseMessage>(new IOException("The response was closed."));
            }

            [Fact]
            public void Should_report_a_cancellation()
            {
                Error.Should().BeAssignableTo<OperationCanceledException>();
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

        public class When_the_connection_fails : DownloadErrorSpecsBase
        {
            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var cause = new WebException("Unable to connect to the remote server", WebExceptionStatus.ConnectFailure);

                return Task.FromException<HttpResponseMessage>(new HttpRequestException("An error occurred while sending the request.", cause));
            }

            [Fact]
            public void Should_throw_a_web_exception()
            {
                Error.Should().BeOfType<WebException>();
            }

            [Fact]
            public void Should_keep_the_original_exception_as_the_cause()
            {
                Error.InnerException.Should().BeOfType<HttpRequestException>();
            }
        }

        public abstract class DownloadSuccessSpecsBase : DownloadSpecsBase
        {
            protected abstract HttpContent CreateContent();

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = CreateContent() });
            }

            [Fact]
            public void Should_complete_without_an_error()
            {
                Error.Should().BeNull();
            }
        }

        public class When_the_response_has_no_charset : DownloadSuccessSpecsBase
        {
            protected override HttpContent CreateContent()
            {
                var content = new ByteArrayContent(new byte[] { 0xE9, 0x74, 0xE9 });
                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                return content;
            }

            [Fact]
            public void Should_decode_the_content_as_iso_8859_1()
            {
                CommandRuntime.Verify(r => r.WriteObject("été", true), Times.Once);
            }
        }

        public class When_the_response_declares_a_charset : DownloadSuccessSpecsBase
        {
            protected override HttpContent CreateContent()
            {
                var content = new ByteArrayContent(new byte[] { 0xC3, 0xA9 });
                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "utf-8" };

                return content;
            }

            [Fact]
            public void Should_decode_the_content_with_that_charset()
            {
                CommandRuntime.Verify(r => r.WriteObject("é", true), Times.Once);
            }
        }

        public class When_the_response_length_is_known : DownloadSuccessSpecsBase
        {
            protected override HttpContent CreateContent()
            {
                var content = new ByteArrayContent(new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F });
                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                return content;
            }

            [Fact]
            public void Should_write_the_content()
            {
                CommandRuntime.Verify(r => r.WriteObject("hello", true), Times.Once);
            }

            [Fact]
            public void Should_report_the_size_when_the_download_completes()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.Is<string>(m => m.Contains("(5 B) completed."))), Times.Once);
            }
        }

        public class When_the_response_length_is_unknown : DownloadSuccessSpecsBase
        {
            protected override HttpContent CreateContent()
            {
                var content = new UnknownLengthContent(new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F });
                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                return content;
            }

            [Fact]
            public void Should_still_write_the_content()
            {
                CommandRuntime.Verify(r => r.WriteObject("hello", true), Times.Once);
            }

            [Fact]
            public void Should_report_an_unknown_size_when_the_download_completes()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.Is<string>(m => m.Contains("(-1 B) completed."))), Times.Once);
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
