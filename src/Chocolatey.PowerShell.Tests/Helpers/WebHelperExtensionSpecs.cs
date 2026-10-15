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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public partial class WebHelperSpecs
    {
        public abstract class RequestUriSpecsBase : DownloadSpecsBase
        {
            protected static readonly Uri OriginalUri = new Uri("http://localhost/file.zip");
            protected static readonly Uri CachedUri = new Uri("http://cache.example.com/cached/file.zip");

            protected List<Uri> Requested = new List<Uri>();

            protected virtual HttpStatusCode StatusFor(Uri uri)
            {
                return HttpStatusCode.OK;
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requested.Add(request.RequestUri);

                return Task.FromResult(new HttpResponseMessage(StatusFor(request.RequestUri))
                {
                    Content = new StringContent("content from " + request.RequestUri.Host)
                });
            }
        }

        public abstract class CachedThenOriginalSpecsBase : RequestUriSpecsBase
        {
            public override void Context()
            {
                base.Context();

                var attempts = 0;
                Helper.RequestUriChooser = isDownload => attempts++ == 0 ? CachedUri : OriginalUri;
            }
        }

        public class When_a_different_address_is_chosen_for_a_download : RequestUriSpecsBase
        {
            private bool? _chosenForDownload;

            public override void Context()
            {
                base.Context();
                Helper.RequestUriChooser = isDownload =>
                {
                    _chosenForDownload = isDownload;

                    return CachedUri;
                };
            }

            [Fact]
            public void Should_send_the_request_to_the_chosen_address()
            {
                Requested.Should().Equal(CachedUri);
            }

            [Fact]
            public void Should_say_that_the_request_is_for_a_download()
            {
                _chosenForDownload.Should().BeTrue();
            }

            [Fact]
            public void Should_write_what_the_chosen_address_responded_with()
            {
                CommandRuntime.Verify(r => r.WriteObject("content from cache.example.com", true), Times.Once);
            }

            [Fact]
            public void Should_not_offer_recovery()
            {
                Helper.RecoveryErrors.Should().BeEmpty();
            }
        }

        public class When_the_chosen_address_fails_and_recovery_is_requested : CachedThenOriginalSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Helper.Recovery = (error, isDownload) => true;
            }

            protected override HttpStatusCode StatusFor(Uri uri)
            {
                return uri == CachedUri ? HttpStatusCode.NotFound : HttpStatusCode.OK;
            }

            [Fact]
            public void Should_not_fail()
            {
                Error.Should().BeNull();
            }

            [Fact]
            public void Should_send_the_request_to_the_chosen_address_and_then_to_the_original()
            {
                Requested.Should().Equal(CachedUri, OriginalUri);
            }

            [Fact]
            public void Should_write_what_the_original_address_responded_with()
            {
                CommandRuntime.Verify(r => r.WriteObject("content from localhost", true), Times.Once);
            }

            [Fact]
            public void Should_offer_recovery_once_with_the_error()
            {
                Helper.RecoveryErrors.Should().ContainSingle().Which.Should().BeOfType<WebException>().Which.Message.Should().Contain("(404)");
            }

            [Fact]
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }

            [Fact]
            public void Should_ask_for_an_address_for_each_request()
            {
                Helper.ChosenUris.Should().Equal(CachedUri, OriginalUri);
            }
        }

        public class When_the_request_fails_again_after_recovery : CachedThenOriginalSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Helper.Recovery = (error, isDownload) => true;
            }

            protected override HttpStatusCode StatusFor(Uri uri)
            {
                return HttpStatusCode.NotFound;
            }

            [Fact]
            public void Should_throw_a_web_exception()
            {
                Error.Should().BeOfType<WebException>();
            }

            [Fact]
            public void Should_name_the_address_of_the_last_attempt()
            {
                Error.Message.Should().Contain("for url 'http://localhost/file.zip'");
            }

            [Fact]
            public void Should_only_send_the_request_twice()
            {
                Requested.Should().Equal(CachedUri, OriginalUri);
            }

            [Fact]
            public void Should_only_offer_recovery_once()
            {
                Helper.RecoveryErrors.Should().HaveCount(1);
            }

            [Fact]
            public void Should_report_the_exit_code()
            {
                Helper.ReportedExitCode.Should().Be(404);
            }
        }

        public class When_recovery_is_not_requested : RequestUriSpecsBase
        {
            private bool? _offeredForDownload;

            public override void Context()
            {
                base.Context();
                Helper.Recovery = (error, isDownload) =>
                {
                    _offeredForDownload = isDownload;

                    return false;
                };
            }

            protected override HttpStatusCode StatusFor(Uri uri)
            {
                return HttpStatusCode.NotFound;
            }

            [Fact]
            public void Should_send_the_request_once_to_the_address_in_the_options()
            {
                Requested.Should().Equal(OriginalUri);
            }

            [Fact]
            public void Should_offer_recovery_once_for_a_download()
            {
                Helper.RecoveryErrors.Should().HaveCount(1);
                _offeredForDownload.Should().BeTrue();
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

        public class When_the_request_is_cancelled_and_recovery_would_be_requested : DownloadSpecsBase
        {
            protected override bool UseCancellationToken
            {
                get { return true; }
            }

            public override void Context()
            {
                base.Context();
                TokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));
                Helper.Recovery = (error, isDownload) => true;
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
            public void Should_not_offer_recovery()
            {
                Helper.RecoveryErrors.Should().BeEmpty();
            }
        }

        public class When_a_different_address_is_chosen_for_a_file_name : FileNameSpecsBase
        {
            private readonly List<Uri> _requested = new List<Uri>();
            private bool? _chosenForDownload;

            public override void Context()
            {
                base.Context();
                Helper.RequestUriChooser = isDownload =>
                {
                    _chosenForDownload = isDownload;

                    return new Uri("http://cache.example.com/cached/cached-name.zip");
                };
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _requested.Add(request.RequestUri);

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[0]) });
            }

            [Fact]
            public void Should_send_the_request_to_the_chosen_address()
            {
                _requested.Should().Equal(new Uri("http://cache.example.com/cached/cached-name.zip"));
            }

            [Fact]
            public void Should_say_that_the_request_is_not_for_a_download()
            {
                _chosenForDownload.Should().BeFalse();
            }

            [Fact]
            public void Should_use_the_name_from_the_chosen_address()
            {
                FileName.Should().Be("cached-name.zip");
            }
        }

        public class When_a_different_address_is_chosen_for_headers : WebHeadersSpecsBase
        {
            private readonly List<Uri> _requested = new List<Uri>();
            private bool? _chosenForDownload;

            public override void Context()
            {
                base.Context();
                Helper.RequestUriChooser = isDownload =>
                {
                    _chosenForDownload = isDownload;

                    return new Uri("http://cache.example.com/cached/file.zip");
                };
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _requested.Add(request.RequestUri);

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[0]) });
            }

            [Fact]
            public void Should_send_the_request_to_the_chosen_address()
            {
                _requested.Should().Equal(new Uri("http://cache.example.com/cached/file.zip"));
            }

            [Fact]
            public void Should_say_that_the_request_is_not_for_a_download()
            {
                _chosenForDownload.Should().BeFalse();
            }
        }

        public class When_the_request_for_headers_fails_and_recovery_is_requested : WebHeadersSpecsBase
        {
            private readonly List<Uri> _requested = new List<Uri>();

            public override void Context()
            {
                base.Context();

                var attempts = 0;
                Helper.RequestUriChooser = isDownload => attempts++ == 0 ? new Uri("http://cache.example.com/file.zip") : new Uri("http://localhost/file.zip");
                Helper.Recovery = (error, isDownload) => true;
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _requested.Add(request.RequestUri);
                if (request.RequestUri.Host == "cache.example.com")
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(new byte[0]) };
                response.Headers.TryAddWithoutValidation("X-Origin", "original");

                return Task.FromResult(response);
            }

            [Fact]
            public void Should_send_the_request_again_to_the_original_address()
            {
                _requested.Select(uri => uri.Host).Should().Equal("cache.example.com", "localhost");
            }

            [Fact]
            public void Should_return_the_headers_from_the_second_response()
            {
                Headers["X-Origin"].Should().Be("original");
            }

            [Fact]
            public void Should_offer_recovery_for_something_that_is_not_a_download()
            {
                Helper.RecoveryErrors.Should().HaveCount(1);
            }

            [Fact]
            public void Should_not_report_an_exit_code()
            {
                Helper.ReportedExitCode.Should().BeNull();
            }
        }

        public class When_the_download_cache_hint_is_turned_off : DownloadFailureSpecsBase
        {
            protected override HttpStatusCode Status
            {
                get { return HttpStatusCode.NotFound; }
            }

            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.DownloadCacheAvailable, "true");
                Helper.DownloadCacheHint = false;
            }

            [Fact]
            public void Should_not_add_the_private_cdn_hint()
            {
                Error.Message.Should().NotContain("private-cdn");
            }

            [Fact]
            public void Should_not_add_anything_after_the_failure()
            {
                Error.Message.Should().EndWith("(404) Not Found.");
            }
        }

        public class When_the_download_cache_hint_is_turned_on : DownloadFailureSpecsBase
        {
            protected override HttpStatusCode Status
            {
                get { return HttpStatusCode.NotFound; }
            }

            public override void Context()
            {
                base.Context();
                Helper.DownloadCacheHint = true;
            }

            [Fact]
            public void Should_add_the_private_cdn_hint_on_a_new_line()
            {
                Error.Message.Should().Contain("(404) Not Found.\nThis package is likely not broken for licensed users").And.Contain("private-cdn");
            }
        }

        public class When_the_request_for_headers_fails_with_the_download_cache_hint_turned_on : WebHeadersSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Helper.DownloadCacheHint = true;
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            [Fact]
            public void Should_not_add_the_private_cdn_hint_as_it_is_not_a_download()
            {
                Error.Message.Should().NotContain("private-cdn");
            }
        }
        public abstract class BufferSizeSpecsBase : DownloadSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Helper.RecordReads = true;
            }

            protected override Task<HttpResponseMessage> Respond(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.ASCII.GetBytes("0123456789"))
                });
            }
        }

        public class When_the_buffer_size_is_not_changed : BufferSizeSpecsBase
        {
            [Fact]
            public void Should_read_one_megabyte_at_a_time()
            {
                Helper.ReadSizes.Should().NotBeEmpty().And.OnlyContain(size => size == 1048576);
            }
        }

        public class When_the_buffer_size_is_changed : BufferSizeSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Helper.BufferSize = 4;
            }

            [Fact]
            public void Should_read_that_many_bytes_at_a_time()
            {
                Helper.ReadSizes.Should().NotBeEmpty().And.OnlyContain(size => size == 4);
            }

            [Fact]
            public void Should_read_until_the_end_of_the_response()
            {
                // 10 bytes in reads of 4 is three reads with data, then the read that finds the end.
                Helper.ReadSizes.Should().HaveCount(4);
            }

            [Fact]
            public void Should_still_write_all_of_the_content()
            {
                CommandRuntime.Verify(r => r.WriteObject("0123456789", true), Times.Once);
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
