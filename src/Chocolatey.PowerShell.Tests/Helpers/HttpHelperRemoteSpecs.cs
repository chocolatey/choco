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
using System.Collections;
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
        public abstract class FileNameSpecsBase : DownloadSpecsBase
        {
            protected const string DefaultName = "default.bin";

            protected string FileName;

            protected StubMessageHandler Handler
            {
                get { return (StubMessageHandler)Helper.MessageHandler; }
            }

            public override void Because()
            {
                try
                {
                    FileName = Helper.GetRemoteFileName(DefaultName);
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }
        }

        public abstract class FileNameResponseSpecsBase : FileNameSpecsBase
        {
            protected virtual string ContentDispositionValue
            {
                get { return null; }
            }

            protected virtual string LocationValue
            {
                get { return null; }
            }

            protected virtual Uri FinalUri
            {
                get { return null; }
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[0]) };
                if (ContentDispositionValue != null)
                {
                    response.Content.Headers.TryAddWithoutValidation("Content-Disposition", ContentDispositionValue);
                }

                if (LocationValue != null)
                {
                    response.Headers.TryAddWithoutValidation("Location", LocationValue);
                }

                if (FinalUri != null)
                {
                    response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, FinalUri);
                }

                return Task.FromResult(response);
            }
        }

        public class When_the_server_names_the_file_in_a_header : FileNameResponseSpecsBase
        {
            protected override string ContentDispositionValue
            {
                get { return "attachment; filename=\"installer.msi\""; }
            }

            [Fact]
            public void Should_use_the_name_from_the_header()
            {
                FileName.Should().Be("installer.msi");
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }

            [Fact]
            public void Should_request_the_address_from_the_options()
            {
                Handler.LastRequest.RequestUri.Should().Be(Options.Uri);
            }
        }

        public class When_the_server_redirected_to_a_named_file : FileNameResponseSpecsBase
        {
            protected override Uri FinalUri
            {
                get { return new Uri("http://cdn.example.com/files/setup.exe"); }
            }

            [Fact]
            public void Should_use_the_name_from_the_address_that_answered()
            {
                FileName.Should().Be("setup.exe");
            }
        }

        public class When_the_server_gives_no_name_but_the_address_has_one : FileNameResponseSpecsBase
        {
            [Fact]
            public void Should_use_the_name_in_the_address()
            {
                FileName.Should().Be("file.zip");
            }
        }

        public class When_the_server_gives_no_name_and_the_address_has_none : FileNameResponseSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Uri = new Uri("http://localhost/");
            }

            [Fact]
            public void Should_use_the_default_name()
            {
                FileName.Should().Be(DefaultName);
            }
        }

        public class When_the_server_gives_no_name_and_the_address_ends_in_a_segment_without_an_extension : FileNameResponseSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Uri = new Uri("http://localhost/download");
            }

            [Fact]
            public void Should_use_that_segment_as_the_name()
            {
                FileName.Should().Be("download");
            }
        }

        public class When_the_address_for_a_file_name_is_empty : FileNameResponseSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Uri = null;
            }

            [Fact]
            public void Should_use_the_default_name()
            {
                FileName.Should().Be(DefaultName);
            }

            [Fact]
            public void Should_not_make_a_request()
            {
                Handler.LastRequest.Should().BeNull();
            }
        }

        public class When_the_address_for_a_file_name_is_a_local_file : FileNameResponseSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Uri = new Uri("file:///C:/downloads/installer.msi");
            }

            [Fact]
            [NUnit.Framework.Platform("Win")]
            public void Should_use_the_name_of_the_file()
            {
                FileName.Should().Be("installer.msi");
            }

            [Fact]
            public void Should_not_make_a_request()
            {
                Handler.LastRequest.Should().BeNull();
            }
        }

        public class When_the_address_for_a_file_name_is_a_local_file_with_a_forward_slash_path : FileNameResponseSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Uri = new Uri("file:///tmp/downloads/installer.msi");
            }

            [Fact]
            public void Should_use_the_name_of_the_file()
            {
                FileName.Should().Be("installer.msi");
            }

            [Fact]
            public void Should_not_make_a_request()
            {
                Handler.LastRequest.Should().BeNull();
            }
        }

        public class When_the_address_for_a_file_name_is_an_ftp_address : FileNameResponseSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Uri = new Uri("ftp://example.com/files/tool.zip");
            }

            [Fact]
            public void Should_use_the_default_name()
            {
                FileName.Should().Be(DefaultName);
            }

            [Fact]
            public void Should_not_make_a_request()
            {
                Handler.LastRequest.Should().BeNull();
            }
        }

        public class When_getting_a_file_name_through_an_explicit_proxy : FileNameResponseSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
            }

            [Fact]
            public void Should_report_the_proxy_to_the_debug_stream()
            {
                CommandRuntime.Verify(
                    r => r.WriteDebug(It.Is<string>(m => m.StartsWith("Using explicit proxy server") && m.Contains("proxy.example.com:8080"))),
                    Times.Once);
            }

            [Fact]
            public void Should_not_report_the_proxy_to_the_host()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.Is<string>(m => m.StartsWith("Using explicit proxy server"))), Times.Never);
            }
        }

        public class When_the_server_responds_with_an_error_for_a_file_name : FileNameSpecsBase
        {
            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            [Fact]
            public void Should_use_the_default_name()
            {
                FileName.Should().Be(DefaultName);
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }

            [Fact]
            public void Should_say_why_the_default_name_was_used()
            {
                CommandRuntime.Verify(
                    r => r.WriteDebug(It.Is<string>(m => m.StartsWith("Url request/response failed - file name will be 'default.bin':") && m.Contains("(404)"))),
                    Times.Once);
            }
        }

        public class When_the_connection_fails_for_a_file_name : FileNameSpecsBase
        {
            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var cause = new WebException("Unable to connect to the remote server", WebExceptionStatus.ConnectFailure);

                return Task.FromException<HttpResponseMessage>(new HttpRequestException("An error occurred while sending the request.", cause));
            }

            [Fact]
            public void Should_use_the_default_name()
            {
                FileName.Should().Be(DefaultName);
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }
        }

        public class When_the_request_for_a_file_name_times_out : FileNameSpecsBase
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
            public void Should_use_the_default_name()
            {
                FileName.Should().Be(DefaultName);
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }
        }

        public class When_the_request_for_a_file_name_is_cancelled : FileNameSpecsBase
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
            public void Should_report_a_cancellation_rather_than_the_default_name()
            {
                Error.Should().BeAssignableTo<OperationCanceledException>();
            }
        }

        public abstract class WebHeadersSpecsBase : DownloadSpecsBase
        {
            protected Hashtable Headers;

            protected StubMessageHandler Handler
            {
                get { return (StubMessageHandler)Helper.MessageHandler; }
            }

            public override void Because()
            {
                try
                {
                    Headers = Helper.GetHeaders();
                }
                catch (Exception error)
                {
                    Error = error;
                }
            }
        }

        public class When_the_server_responds_with_headers : WebHeadersSpecsBase
        {
            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var content = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
                content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                content.Headers.ContentLength = 1234;

                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
                response.Headers.TryAddWithoutValidation("X-Custom", "value");
                response.Headers.TryAddWithoutValidation("X-Empty", string.Empty);
                response.Headers.TryAddWithoutValidation("Server", "Test-Server/1.0 (Windows)");

                return Task.FromResult(response);
            }

            [Fact]
            public void Should_return_a_hashtable()
            {
                Headers.GetType().Should().Be(typeof(Hashtable));
            }

            [Fact]
            public void Should_include_the_response_headers()
            {
                Headers["X-Custom"].Should().Be("value");
                Headers["Content-Type"].Should().Be("text/plain");
            }

            [Fact]
            public void Should_include_the_content_length_that_callers_look_for()
            {
                Headers.ContainsKey("Content-Length").Should().BeTrue();
                Headers["Content-Length"].Should().Be("1234");
            }

            [Fact]
            public void Should_look_headers_up_regardless_of_case()
            {
                Headers.ContainsKey("content-type").Should().BeTrue();
            }

            [Fact]
            public void Should_leave_out_headers_without_a_value()
            {
                Headers.ContainsKey("X-Empty").Should().BeFalse();
            }

            [Fact]
            public void Should_report_the_request_headers()
            {
                CommandRuntime.Verify(r => r.WriteDebug("Request Headers:"), Times.Once);
                CommandRuntime.Verify(r => r.WriteDebug("  'Accept':'*/*'"), Times.Once);
            }

            [Fact]
            public void Should_report_the_user_agent_with_its_parts_separated_by_spaces()
            {
                CommandRuntime.Verify(r => r.WriteDebug("  'User-Agent':'chocolatey command line'"), Times.Once);
            }

            [Fact]
            public void Should_report_the_response_headers()
            {
                CommandRuntime.Verify(r => r.WriteDebug("Response Headers:"), Times.Once);
                CommandRuntime.Verify(r => r.WriteDebug("  'X-Custom':'value'"), Times.Once);
            }

            [Fact]
            public void Should_report_the_server_with_its_parts_separated_by_spaces()
            {
                CommandRuntime.Verify(r => r.WriteDebug("  'Server':'Test-Server/1.0 (Windows)'"), Times.Once);
            }

            [Fact]
            public void Should_not_report_the_headers_without_a_value_as_response_headers()
            {
                CommandRuntime.Verify(r => r.WriteDebug(It.Is<string>(m => m.StartsWith("  'X-Empty'"))), Times.Never);
            }

            [Fact]
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }
        }

        public class When_getting_headers_through_an_explicit_proxy : WebHeadersSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            [Fact]
            public void Should_report_the_proxy_to_the_host()
            {
                CommandRuntime.Verify(
                    r => r.WriteVerbose(It.Is<string>(m => m.StartsWith("Using explicit proxy server") && m.Contains("proxy.example.com:8080"))),
                    Times.Once);
            }
        }

        public class When_the_address_for_headers_is_empty : WebHeadersSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Uri = null;
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            [Fact]
            public void Should_return_an_empty_hashtable()
            {
                Headers.Count.Should().Be(0);
            }

            [Fact]
            public void Should_not_make_a_request()
            {
                Handler.LastRequest.Should().BeNull();
            }
        }

        public class When_the_server_responds_with_an_error_for_headers : WebHeadersSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.DownloadCacheAvailable, "true");
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
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
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }
        }

        public class When_the_request_for_headers_times_out : WebHeadersSpecsBase
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

        public class When_the_request_for_headers_is_cancelled : WebHeadersSpecsBase
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
        }
    }

    // ReSharper restore InconsistentNaming
}
