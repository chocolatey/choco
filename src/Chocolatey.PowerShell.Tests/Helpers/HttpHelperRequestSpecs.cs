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
using System.Linq;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Moq;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public partial class HttpHelperSpecs
    {
        public abstract class CreateRequestSpecsBase : HttpHelperSpecsBase
        {
            protected HttpClientHandler Handler;
            protected HttpRequestMessage Message;
            protected TimeSpan RequestTimeoutValue;
            protected TimeSpan? ReadTimeoutValue;

            protected override string[] ManagedVariables { get; } =
            {
                Package.ChocolateyRequestTimeout,
                Package.ChocolateyResponseTimeout,
            };

            protected virtual IWebProxy HandlerProxy
            {
                get { return null; }
            }

            protected string[] HeaderValues(string name)
            {
                return Message.Headers
                    .Where(header => string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(header => header.Value)
                    .ToArray();
            }

            public override void Because()
            {
                Handler = Helper.CreateRequestHandler(HandlerProxy);
                Message = Helper.CreateMessage();
                RequestTimeoutValue = Helper.RequestTimeout();
                ReadTimeoutValue = Helper.ResponseTimeout();
            }

            public override void AfterObservations()
            {
                Message?.Dispose();
                Handler?.Dispose();
                base.AfterObservations();
            }
        }

        public abstract class ResolveProxySpecsBase : HttpHelperSpecsBase
        {
            protected IWebProxy Proxy;

            protected WebProxy AppliedProxy
            {
                get { return (WebProxy)Proxy; }
            }

            protected override string[] ManagedVariables { get; } =
            {
                Package.ChocolateyProxyLocation,
                Package.ChocolateyProxyBypassOnLocal,
                Package.ChocolateyProxyBypassList,
                Package.ChocolateyProxyUser,
                Package.ChocolateyProxyPassword,
            };

            public override void Because()
            {
                Proxy = Helper.ProxyFor(new Uri("http://remote.example.com/file.zip"));
            }
        }

        public class When_creating_a_request_with_only_a_uri : CreateRequestSpecsBase
        {
            [Fact]
            public void Should_target_the_requested_uri()
            {
                Message.RequestUri.Should().Be(Options.Uri);
            }

            [Fact]
            public void Should_use_the_get_method()
            {
                Message.Method.Should().Be(HttpMethod.Get);
            }

            [Fact]
            public void Should_accept_any_content_type()
            {
                HeaderValues("Accept").Should().Equal("*/*");
            }

            [Fact]
            public void Should_use_the_default_user_agent()
            {
                string.Join(" ", HeaderValues("User-Agent")).Should().Be("chocolatey command line");
            }

            [Fact]
            public void Should_follow_up_to_twenty_redirects()
            {
                Handler.AllowAutoRedirect.Should().BeTrue();
                Handler.MaxAutomaticRedirections.Should().Be(20);
            }

            [Fact]
            public void Should_decompress_gzip_and_deflate_responses()
            {
                Handler.AutomaticDecompression.Should().Be(DecompressionMethods.GZip | DecompressionMethods.Deflate);
            }

            [Fact]
            public void Should_use_the_default_credentials()
            {
                Handler.Credentials.Should().BeSameAs(CredentialCache.DefaultCredentials);
            }

            [Fact]
            public void Should_use_the_shared_cookie_container()
            {
                Handler.UseCookies.Should().BeTrue();
                Handler.CookieContainer.Should().BeSameAs(Helper.CookieJar);
            }

            [Fact]
            public void Should_wait_thirty_seconds_for_the_response()
            {
                RequestTimeoutValue.Should().Be(TimeSpan.FromSeconds(30));
            }

            [Fact]
            public void Should_leave_the_proxy_to_the_default()
            {
                Handler.Proxy.Should().BeNull();
            }

            [Fact]
            public void Should_not_apply_a_read_timeout()
            {
                ReadTimeoutValue.Should().BeNull();
            }

            [Fact]
            public void Should_only_report_the_default_user_agent_being_set()
            {
                CommandRuntime.Verify(
                    r => r.WriteDebug(It.Is<string>(m => m != "Setting the UserAgent to 'chocolatey command line'")),
                    Times.Never);
                CommandRuntime.Verify(r => r.WriteDebug("Setting the UserAgent to 'chocolatey command line'"), Times.Once);
            }

            [Fact]
            public void Should_not_write_to_the_host()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_creating_a_handler_for_a_proxy : CreateRequestSpecsBase
        {
            private readonly WebProxy _proxy = new WebProxy("http://proxy.example.com:8080");

            protected override IWebProxy HandlerProxy
            {
                get { return _proxy; }
            }

            [Fact]
            public void Should_send_requests_through_that_proxy()
            {
                Handler.Proxy.Should().BeSameAs(_proxy);
            }
        }

        public class When_the_request_timeout_variable_is_set : CreateRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyRequestTimeout, "60000");
            }

            [Fact]
            public void Should_use_the_configured_timeout()
            {
                RequestTimeoutValue.Should().Be(TimeSpan.FromSeconds(60));
            }

            [Fact]
            public void Should_report_the_timeout_being_set()
            {
                CommandRuntime.Verify(r => r.WriteDebug("Setting request timeout to 60000"), Times.Once);
            }
        }

        public class When_the_request_timeout_variable_is_not_a_positive_number : CreateRequestSpecsBase
        {
            [InlineData("0")]
            [InlineData("-1")]
            [InlineData("abc")]
            public void Should_use_the_default_timeout(string value)
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyRequestTimeout, value);

                Helper.RequestTimeout().Should().Be(TimeSpan.FromSeconds(30));
            }

            [InlineData("0")]
            [InlineData("-1")]
            [InlineData("abc")]
            public void Should_not_report_a_timeout_being_set(string value)
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyRequestTimeout, value);

                Helper.RequestTimeout();

                CommandRuntime.Verify(r => r.WriteDebug(It.Is<string>(m => m.StartsWith("Setting request timeout"))), Times.Never);
            }
        }

        public class When_the_response_timeout_variable_is_not_a_positive_number : CreateRequestSpecsBase
        {
            [InlineData("0")]
            [InlineData("-1")]
            [InlineData("abc")]
            public void Should_not_apply_a_read_timeout(string value)
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyResponseTimeout, value);

                Helper.ResponseTimeout().Should().BeNull();
            }

            [InlineData("0")]
            [InlineData("-1")]
            [InlineData("abc")]
            public void Should_not_report_a_timeout_being_set(string value)
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyResponseTimeout, value);

                Helper.ResponseTimeout();

                CommandRuntime.Verify(r => r.WriteDebug(It.Is<string>(m => m.StartsWith("Setting read/write timeout"))), Times.Never);
            }
        }

        public class When_the_request_timeout_variable_is_empty : CreateRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyRequestTimeout, string.Empty);
            }

            [Fact]
            public void Should_use_the_default_timeout()
            {
                RequestTimeoutValue.Should().Be(TimeSpan.FromSeconds(30));
            }
        }

        public class When_the_response_timeout_variable_is_set : CreateRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyResponseTimeout, "45000");
            }

            [Fact]
            public void Should_use_the_configured_read_timeout()
            {
                ReadTimeoutValue.Should().Be(TimeSpan.FromSeconds(45));
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

        public class When_a_user_agent_is_specified : CreateRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.UserAgent = "Test-Agent/1.0";
            }

            [Fact]
            public void Should_set_the_user_agent()
            {
                HeaderValues("User-Agent").Should().Equal("Test-Agent/1.0");
            }

            [Fact]
            public void Should_report_the_user_agent_being_set()
            {
                CommandRuntime.Verify(r => r.WriteDebug("Setting the UserAgent to 'Test-Agent/1.0'"), Times.Once);
            }
        }

        public class When_custom_headers_are_specified : CreateRequestSpecsBase
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
                HeaderValues("Accept").Should().Equal("text/plain");
            }

            [Fact]
            public void Should_set_the_referer()
            {
                HeaderValues("Referer").Should().Equal("http://referrer.example.com/");
            }

            [Fact]
            public void Should_add_the_cookie_to_the_cookie_container()
            {
                Helper.CookieJar.GetCookies(Options.Uri)["session"].Value.Should().Be("abc123");
            }

            [Fact]
            public void Should_not_send_the_cookie_as_a_header_value()
            {
                Message.Headers.Contains("Cookie").Should().BeFalse();
            }

            [Fact]
            public void Should_add_the_authorization_header()
            {
                HeaderValues("Authorization").Should().Equal("Bearer token");
            }

            [Fact]
            public void Should_add_the_accept_language_header()
            {
                HeaderValues("Accept-Language").Should().Equal("en-GB");
            }

            [Fact]
            public void Should_add_the_cache_control_header()
            {
                HeaderValues("Cache-Control").Should().Equal("no-cache");
            }

            [Fact]
            public void Should_report_that_custom_headers_are_being_set()
            {
                CommandRuntime.Verify(r => r.WriteDebug("Setting custom headers"), Times.Once);
            }
        }

        public class When_a_user_agent_is_given_as_both_an_option_and_a_header : CreateRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.UserAgent = "Option-Agent/1.0";
                Options.Headers = new Hashtable { { "User-Agent", "Header-Agent/2.0" } };
            }

            [Fact]
            public void Should_use_only_the_header()
            {
                HeaderValues("User-Agent").Should().Equal("Header-Agent/2.0");
            }
        }

        public class When_header_names_are_not_in_their_usual_case : CreateRequestSpecsBase
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
            public void Should_replace_the_accept_value()
            {
                HeaderValues("Accept").Should().Equal("text/plain");
            }

            [Fact]
            public void Should_set_the_referer()
            {
                HeaderValues("Referer").Should().Equal("http://referrer.example.com/");
            }

            [Fact]
            public void Should_add_the_cookie_to_the_cookie_container()
            {
                Helper.CookieJar.GetCookies(Options.Uri)["session"].Value.Should().Be("abc123");
            }

            [Fact]
            public void Should_set_the_user_agent()
            {
                HeaderValues("User-Agent").Should().Equal("Mixed-Agent/1.0");
            }
        }

        public class When_a_header_name_is_not_a_known_request_header : CreateRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Headers = new Hashtable { { "X-Custom-Header", "value" } };
            }

            [Fact]
            public void Should_add_the_header_under_the_given_name()
            {
                HeaderValues("X-Custom-Header").Should().Equal("value");
            }
        }

        public class When_a_header_value_is_not_valid_for_its_header : CreateRequestSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Options.Headers = new Hashtable
                {
                    { "User-Agent", "chocolatey/1.0 (unclosed" },
                    { "Referer", "not a uri" },
                };
            }

            [Fact]
            public void Should_keep_the_user_agent_as_given()
            {
                HeaderValues("User-Agent").Should().Equal("chocolatey/1.0 (unclosed");
            }

            [Fact]
            public void Should_send_the_referer_percent_encoded()
            {
                HeaderValues("Referer").Should().Equal("not%20a%20uri");
            }
        }

        public class When_a_header_cannot_be_set_on_a_request : CreateRequestSpecsBase
        {
            private Exception _error;

            public override void Context()
            {
                base.Context();
                Options.Headers = new Hashtable { { "Content-Type", "text/plain" } };
            }

            public override void Because()
            {
                try
                {
                    base.Because();
                }
                catch (Exception error)
                {
                    _error = error;
                }
            }

            [Fact]
            public void Should_fail_to_create_the_request()
            {
                _error.Should().BeOfType<ArgumentException>();
            }
        }

        public class When_headers_are_given_as_a_dictionary_other_than_a_hashtable : CreateRequestSpecsBase
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
                HeaderValues("Accept").Should().Equal("text/plain");
            }

            [Fact]
            public void Should_apply_the_other_headers()
            {
                HeaderValues("X-Custom-Header").Should().Equal("value");
            }
        }

        public class When_header_keys_are_request_header_values : CreateRequestSpecsBase
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
                    { HttpRequestHeader.CacheControl, "no-cache" },
                };
            }

            [Fact]
            public void Should_replace_the_accept_value()
            {
                HeaderValues("Accept").Should().Equal("text/plain");
            }

            [Fact]
            public void Should_set_the_referer()
            {
                HeaderValues("Referer").Should().Equal("http://referrer.example.com/");
            }

            [Fact]
            public void Should_add_the_cookie_to_the_cookie_container()
            {
                Helper.CookieJar.GetCookies(Options.Uri)["session"].Value.Should().Be("abc123");
            }

            [Fact]
            public void Should_set_the_user_agent()
            {
                HeaderValues("User-Agent").Should().Equal("Enum-Agent/1.0");
            }

            [Fact]
            public void Should_add_other_headers_under_their_header_names()
            {
                HeaderValues("Authorization").Should().Equal("Bearer token");
                HeaderValues("Cache-Control").Should().Equal("no-cache");
            }
        }

        public class When_header_keys_are_integers : CreateRequestSpecsBase
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
                HeaderValues("Accept").Should().Equal("text/plain");
            }

            [Fact]
            public void Should_treat_the_key_as_the_request_header_value_for_other_headers()
            {
                HeaderValues("Authorization").Should().Equal("Bearer token");
            }
        }

        public class When_an_explicit_proxy_is_configured : ResolveProxySpecsBase
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

        public class When_the_explicit_proxy_has_a_username_and_password : ResolveProxySpecsBase
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

        public class When_the_explicit_proxy_has_a_username_but_no_password : ResolveProxySpecsBase
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

        public class When_the_explicit_proxy_bypasses_local_addresses : ResolveProxySpecsBase
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

        public class When_the_explicit_proxy_has_a_bypass_list : ResolveProxySpecsBase
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

        public class When_a_proxy_is_resolved_for_two_downloads : ResolveProxySpecsBase
        {
            public override void Context()
            {
                base.Context();
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
            }

            public override void Because()
            {
                base.Because();
                Helper.ProxyFor(new Uri("http://another.example.com/file.zip"));
            }

            [Fact]
            public void Should_report_the_proxy_being_used_each_time()
            {
                CommandRuntime.Verify(
                    r => r.WriteVerbose(It.Is<string>(m => m.StartsWith("Using explicit proxy server") && m.Contains("proxy.example.com:8080"))),
                    Times.Exactly(2));
            }
        }

        public class When_no_explicit_proxy_is_configured_and_the_system_has_a_proxy : ResolveProxySpecsBase
        {
            public override void Context()
            {
                base.Context();
                Helper.SystemProxy = new WebProxy("http://system-proxy.example.com:3128");
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

        public class When_the_system_proxy_is_bypassed_for_the_request : ResolveProxySpecsBase
        {
            public override void Context()
            {
                base.Context();
                Helper.SystemProxy = new WebProxy("http://system-proxy.example.com:3128", false, new[] { @"remote\.example\.com" });
            }

            [Fact]
            public void Should_leave_the_choice_to_the_default()
            {
                Proxy.Should().BeNull();
            }

            [Fact]
            public void Should_not_write_any_messages()
            {
                CommandRuntime.Verify(r => r.WriteVerbose(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_no_explicit_proxy_is_configured_and_the_system_has_no_proxy : ResolveProxySpecsBase
        {
            public override void Context()
            {
                base.Context();
                Helper.SystemProxy = null;
            }

            [Fact]
            public void Should_leave_the_choice_to_the_default()
            {
                Proxy.Should().BeNull();
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
