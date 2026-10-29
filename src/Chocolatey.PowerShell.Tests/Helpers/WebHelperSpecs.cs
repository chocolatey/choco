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
using System.Collections.Specialized;
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

    public class WebHelperSpecs
    {
        public abstract class WebHelperSpecsBase : EnvironmentSpecsBase
        {
            protected Mock<ICommandRuntime> CommandRuntime;
            protected WebRequestOptions Options;
            protected TestWebHelper Helper;

            public override void Context()
            {
                base.Context();

                CommandRuntime = new Mock<ICommandRuntime>();
                Options = new WebRequestOptions { Uri = new Uri("http://localhost/file.zip") };
                Helper = new TestWebHelper(new TestCmdlet { CommandRuntime = CommandRuntime.Object }, Options);
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

        public abstract class CreateWebRequestSpecsBase : WebHelperSpecsBase
        {
            protected HttpWebRequest Request;

            protected override string[] ManagedVariables { get; } =
            {
                Package.ChocolateyRequestTimeout,
                Package.ChocolateyResponseTimeout,
            };

            public override void Because()
            {
                Request = Helper.CreateRequest();
            }
        }

        public abstract class SetProxyConfigurationSpecsBase : WebHelperSpecsBase
        {
            protected HttpWebRequest Request;

            protected WebProxy AppliedProxy
            {
                get { return (WebProxy)Request.Proxy; }
            }

            protected override string[] ManagedVariables { get; } =
            {
                Package.ChocolateyProxyLocation,
                Package.ChocolateyProxyBypassOnLocal,
                Package.ChocolateyProxyBypassList,
                Package.ChocolateyProxyUser,
                Package.ChocolateyProxyPassword,
            };

            public override void Context()
            {
                base.Context();
                Request = (HttpWebRequest)WebRequest.Create("http://remote.example.com/file.zip");
            }

            public override void Because()
            {
                Helper.ApplyProxyConfiguration(Request);
            }
        }

        public class When_creating_a_web_request_with_only_a_uri : CreateWebRequestSpecsBase
        {
            [Fact]
            public void Should_target_the_requested_uri()
            {
                Request.RequestUri.Should().Be(Options.Uri);
            }

            [Fact]
            public void Should_accept_any_content_type()
            {
                Request.Accept.Should().Be("*/*");
            }

            [Fact]
            public void Should_follow_up_to_twenty_redirects()
            {
                Request.AllowAutoRedirect.Should().BeTrue();
                Request.MaximumAutomaticRedirections.Should().Be(20);
            }

            [Fact]
            public void Should_decompress_gzip_and_deflate_responses()
            {
                Request.AutomaticDecompression.Should().Be(DecompressionMethods.GZip | DecompressionMethods.Deflate);
            }

            [Fact]
            public void Should_use_the_default_credentials()
            {
                Request.Credentials.Should().BeSameAs(CredentialCache.DefaultCredentials);
            }

            [Fact]
            public void Should_have_a_cookie_container()
            {
                Request.CookieContainer.Should().NotBeNull();
            }

            [Fact]
            public void Should_time_out_after_thirty_seconds()
            {
                Request.Timeout.Should().Be(30000);
            }

            [Fact]
            public void Should_leave_the_read_write_timeout_at_its_default()
            {
                Request.ReadWriteTimeout.Should().Be(((HttpWebRequest)WebRequest.Create(Options.Uri)).ReadWriteTimeout);
            }

            [Fact]
            public void Should_not_write_any_messages()
            {
                CommandRuntime.Verify(r => r.WriteDebug(It.IsAny<string>()), Times.Never);
                CommandRuntime.Verify(r => r.WriteVerbose(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_the_request_timeout_variable_is_set : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyRequestTimeout, "60000");
            }

            [Fact]
            public void Should_use_the_configured_timeout()
            {
                Request.Timeout.Should().Be(60000);
            }

            [Fact]
            public void Should_report_the_timeout_being_set()
            {
                CommandRuntime.Verify(r => r.WriteDebug("Setting request timeout to 60000"), Times.Once);
            }
        }

        public class When_the_request_timeout_variable_is_empty : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyRequestTimeout, string.Empty);
            }

            [Fact]
            public void Should_use_the_default_timeout()
            {
                Request.Timeout.Should().Be(30000);
            }
        }

        public class When_the_response_timeout_variable_is_set : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyResponseTimeout, "45000");
            }

            [Fact]
            public void Should_use_the_configured_read_write_timeout()
            {
                Request.ReadWriteTimeout.Should().Be(45000);
            }

            [Fact]
            public void Should_report_the_timeout_being_set()
            {
                CommandRuntime.Verify(r => r.WriteDebug("Setting read/write timeout to 45000"), Times.Once);
            }

            [Fact]
            public void Should_not_write_the_timeout_to_the_host()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_a_user_agent_is_specified : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.UserAgent = "Test-Agent/1.0";
            }

            [Fact]
            public void Should_set_the_user_agent()
            {
                Request.UserAgent.Should().Be("Test-Agent/1.0");
            }

            [Fact]
            public void Should_report_the_user_agent_being_set()
            {
                CommandRuntime.Verify(r => r.WriteDebug("Setting the UserAgent to 'Test-Agent/1.0'"), Times.Once);
            }
        }

        public class When_custom_headers_are_specified : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Headers = new Hashtable
                {
                    { "Accept", "text/plain" },
                    { "Referer", "http://referrer.example.com/" },
                    { "Cookie", "session=abc123" },
                    { "Authorization", "Bearer token" },
                    { "Accept-Language", "en-GB" },
                    { "Cache-Control", "no-cache" },
                };
            }

            [Fact]
            public void Should_replace_the_default_accept_value()
            {
                Request.Accept.Should().Be("text/plain");
            }

            [Fact]
            public void Should_set_the_referer()
            {
                Request.Referer.Should().Be("http://referrer.example.com/");
            }

            [Fact]
            public void Should_add_the_cookie_to_the_cookie_container()
            {
                Request.CookieContainer.GetCookies(Options.Uri)["session"].Value.Should().Be("abc123");
            }

            [Fact]
            public void Should_include_the_accept_and_referer_values_in_the_header_collection()
            {
                Request.Headers[HttpRequestHeader.Accept].Should().Be("text/plain");
                Request.Headers[HttpRequestHeader.Referer].Should().Be("http://referrer.example.com/");
            }

            [Fact]
            public void Should_add_the_authorization_header_to_the_header_collection()
            {
                Request.Headers[HttpRequestHeader.Authorization].Should().Be("Bearer token");
            }

            [Fact]
            public void Should_add_the_accept_language_header_to_the_header_collection()
            {
                Request.Headers[HttpRequestHeader.AcceptLanguage].Should().Be("en-GB");
            }

            [Fact]
            public void Should_add_the_cache_control_header_to_the_header_collection()
            {
                Request.Headers[HttpRequestHeader.CacheControl].Should().Be("no-cache");
            }

            [Fact]
            public void Should_report_that_custom_headers_are_being_set()
            {
                CommandRuntime.Verify(r => r.WriteDebug("Setting custom headers"), Times.Once);
            }
        }

        public class When_a_user_agent_is_given_as_both_an_option_and_a_header : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.UserAgent = "Option-Agent/1.0";
                Options.Headers = new Hashtable { { "User-Agent", "Header-Agent/2.0" } };
            }

            [Fact]
            public void Should_prefer_the_header()
            {
                Request.UserAgent.Should().Be("Header-Agent/2.0");
            }

            [Fact]
            public void Should_include_the_header_value_in_the_header_collection()
            {
                Request.Headers[HttpRequestHeader.UserAgent].Should().Be("Header-Agent/2.0");
            }
        }

        public class When_header_names_are_not_in_their_usual_case : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Headers = new Hashtable
                {
                    { "accept", "text/plain" },
                    { "REFERER", "http://referrer.example.com/" },
                    { "cOOkie", "session=abc123" },
                    { "user-AGENT", "Mixed-Agent/1.0" },
                };
            }

            [Fact]
            public void Should_set_the_accept_value()
            {
                Request.Accept.Should().Be("text/plain");
            }

            [Fact]
            public void Should_set_the_referer()
            {
                Request.Referer.Should().Be("http://referrer.example.com/");
            }

            [Fact]
            public void Should_add_the_cookie_to_the_cookie_container()
            {
                Request.CookieContainer.GetCookies(Options.Uri)["session"].Value.Should().Be("abc123");
            }

            [Fact]
            public void Should_set_the_user_agent()
            {
                Request.UserAgent.Should().Be("Mixed-Agent/1.0");
            }
        }

        public class When_a_header_name_is_not_a_known_request_header : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Headers = new Hashtable { { "X-Custom-Header", "value" } };
            }

            [Fact]
            public void Should_add_the_header_under_the_given_name()
            {
                Request.Headers["X-Custom-Header"].Should().Be("value");
            }
        }

        public class When_headers_are_given_as_a_dictionary_other_than_a_hashtable : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Headers = new OrderedDictionary
                {
                    { "accept", "text/plain" },
                    { "X-Custom-Header", "value" },
                };
            }

            [Fact]
            public void Should_apply_the_dedicated_headers()
            {
                Request.Accept.Should().Be("text/plain");
            }

            [Fact]
            public void Should_apply_the_other_headers()
            {
                Request.Headers["X-Custom-Header"].Should().Be("value");
            }
        }

        public class When_header_keys_are_request_header_values : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Headers = new Hashtable
                {
                    { HttpRequestHeader.Accept, "text/plain" },
                    { HttpRequestHeader.Referer, "http://referrer.example.com/" },
                    { HttpRequestHeader.Cookie, "session=abc123" },
                    { HttpRequestHeader.UserAgent, "Enum-Agent/1.0" },
                    { HttpRequestHeader.Authorization, "Bearer token" },
                };
            }

            [Fact]
            public void Should_set_the_accept_value()
            {
                Request.Accept.Should().Be("text/plain");
            }

            [Fact]
            public void Should_set_the_referer()
            {
                Request.Referer.Should().Be("http://referrer.example.com/");
            }

            [Fact]
            public void Should_add_the_cookie_to_the_cookie_container()
            {
                Request.CookieContainer.GetCookies(Options.Uri)["session"].Value.Should().Be("abc123");
            }

            [Fact]
            public void Should_set_the_user_agent()
            {
                Request.UserAgent.Should().Be("Enum-Agent/1.0");
            }

            [Fact]
            public void Should_add_other_headers_to_the_header_collection()
            {
                Request.Headers[HttpRequestHeader.Authorization].Should().Be("Bearer token");
            }
        }

        public class When_header_keys_are_integers : CreateWebRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Headers = new Hashtable
                {
                    { (int)HttpRequestHeader.Accept, "text/plain" },
                    { (int)HttpRequestHeader.Authorization, "Bearer token" },
                };
            }

            [Fact]
            public void Should_treat_the_key_as_the_request_header_value_for_dedicated_headers()
            {
                Request.Accept.Should().Be("text/plain");
            }

            [Fact]
            public void Should_treat_the_key_as_the_request_header_value_for_other_headers()
            {
                Request.Headers[HttpRequestHeader.Authorization].Should().Be("Bearer token");
            }
        }

        public class When_an_explicit_proxy_is_configured : SetProxyConfigurationSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
            }

            [Fact]
            public void Should_use_the_configured_address()
            {
                AppliedProxy.Address.Should().Be(new Uri("http://proxy.example.com:8080"));
            }

            [Fact]
            public void Should_not_set_credentials()
            {
                AppliedProxy.Credentials.Should().BeNull();
            }

            [Fact]
            public void Should_not_bypass_on_local()
            {
                AppliedProxy.BypassProxyOnLocal.Should().BeFalse();
            }

            [Fact]
            public void Should_have_an_empty_bypass_list()
            {
                AppliedProxy.BypassList.Should().BeEmpty();
            }

            [Fact]
            public void Should_report_the_proxy_being_used()
            {
                CommandRuntime.Verify(
                    r => r.WriteVerbose(It.Is<string>(m => m.StartsWith("Using explicit proxy server") && m.Contains("proxy.example.com:8080"))),
                    Times.Once);
            }
        }

        public class When_the_explicit_proxy_has_a_username_and_password : SetProxyConfigurationSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyUser, "someone");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyPassword, "hunter2");
            }

            [Fact]
            public void Should_use_the_credentials()
            {
                var credentials = AppliedProxy.Credentials.Should().BeOfType<NetworkCredential>().Subject;

                credentials.UserName.Should().Be("someone");
                credentials.Password.Should().Be("hunter2");
            }
        }

        public class When_the_explicit_proxy_has_a_username_but_no_password : SetProxyConfigurationSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyUser, "someone");
            }

            [Fact]
            public void Should_not_set_credentials()
            {
                AppliedProxy.Credentials.Should().BeNull();
            }
        }

        public class When_the_explicit_proxy_bypasses_local_addresses : SetProxyConfigurationSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyBypassOnLocal, "true");
            }

            [Fact]
            public void Should_bypass_on_local()
            {
                AppliedProxy.BypassProxyOnLocal.Should().BeTrue();
            }
        }

        public class When_the_explicit_proxy_has_a_bypass_list : SetProxyConfigurationSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyBypassList, @"example\.com,internal\.local");
            }

            [Fact]
            public void Should_use_the_bypass_list()
            {
                AppliedProxy.BypassList.Should().Equal(@"example\.com", @"internal\.local");
            }
        }

        public class When_no_explicit_proxy_is_configured_and_the_request_has_a_system_proxy : SetProxyConfigurationSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Request.Proxy = new WebProxy("http://system-proxy.example.com:3128");
            }

            [Fact]
            public void Should_use_the_system_proxy_address()
            {
                AppliedProxy.Address.Should().Be(new Uri("http://system-proxy.example.com:3128"));
            }

            [Fact]
            public void Should_use_the_default_credentials()
            {
                AppliedProxy.Credentials.Should().BeSameAs(CredentialCache.DefaultCredentials);
            }

            [Fact]
            public void Should_bypass_on_local()
            {
                AppliedProxy.BypassProxyOnLocal.Should().BeTrue();
            }

            [Fact]
            public void Should_report_the_proxy_being_used()
            {
                CommandRuntime.Verify(r => r.WriteVerbose("Using system proxy server 'system-proxy.example.com:3128'."), Times.Once);
            }
        }

        public class When_the_system_proxy_is_bypassed_for_the_request : SetProxyConfigurationSpecsBase
        {
            private IWebProxy _systemProxy;

            public override void Context()
            {
                base.Context();
                _systemProxy = new WebProxy("http://system-proxy.example.com:3128", false, new[] { @"remote\.example\.com" });
                Request.Proxy = _systemProxy;
            }

            [Fact]
            public void Should_leave_the_proxy_unchanged()
            {
                Request.Proxy.Should().BeSameAs(_systemProxy);
            }

            [Fact]
            public void Should_not_write_any_messages()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_no_explicit_proxy_is_configured_and_the_request_has_no_proxy : SetProxyConfigurationSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Request.Proxy = null;
            }

            [Fact]
            public void Should_leave_the_request_without_a_proxy()
            {
                Request.Proxy.Should().BeNull();
            }

            [Fact]
            public void Should_not_write_any_messages()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.IsAny<string>()), Times.Never);
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
