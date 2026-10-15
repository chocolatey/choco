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
using Chocolatey.PowerShell.Helpers;
using FluentAssertions;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class WebRequestOptionsSpecs
    {
        public class When_creating_options_for_an_address : TinySpec
        {
            private WebRequestOptions _options;

            public override void Context()
            {
            }

            public override void Because()
            {
                _options = WebRequestOptions.ForUrl("http://example.com/file.zip", "Test-Agent/1.0");
            }

            [Fact]
            public void Should_use_the_address()
            {
                _options.Uri.Should().Be(new Uri("http://example.com/file.zip"));
            }

            [Fact]
            public void Should_use_the_user_agent()
            {
                _options.UserAgent.Should().Be("Test-Agent/1.0");
            }
        }

        public class When_creating_options_without_an_address : TinySpec
        {
            private WebRequestOptions _options;

            public override void Context()
            {
            }

            public override void Because()
            {
                _options = WebRequestOptions.ForUrl(string.Empty, WebRequestOptions.DefaultUserAgent);
            }

            [Fact]
            public void Should_have_no_address()
            {
                _options.Uri.Should().BeNull();
            }

            [Fact]
            public void Should_still_use_the_user_agent()
            {
                _options.UserAgent.Should().Be("chocolatey command line");
            }
        }

        public class When_creating_options_for_an_address_that_is_not_valid : TinySpec
        {
            private Exception _error;

            public override void Context()
            {
            }

            public override void Because()
            {
                try
                {
                    WebRequestOptions.ForUrl("not a url", WebRequestOptions.DefaultUserAgent);
                }
                catch (Exception error)
                {
                    _error = error;
                }
            }

            [Fact]
            public void Should_fail_with_a_uri_format_exception()
            {
                _error.Should().BeOfType<UriFormatException>();
            }
        }

        public class When_creating_options_for_a_different_address : TinySpec
        {
            private WebRequestOptions _original;
            private WebRequestOptions _copy;
            private readonly Hashtable _headers = new Hashtable { { "X-Custom", "value" } };

            public override void Context()
            {
                _original = new WebRequestOptions
                {
                    Uri = new Uri("http://example.com/file.zip"),
                    UserAgent = "Test-Agent/1.0",
                    Headers = _headers
                };
            }

            public override void Because()
            {
                _copy = _original.WithUri(new Uri("http://cache.example.com/file.zip"));
            }

            [Fact]
            public void Should_use_the_new_address()
            {
                _copy.Uri.Should().Be(new Uri("http://cache.example.com/file.zip"));
            }

            [Fact]
            public void Should_keep_the_user_agent_and_headers()
            {
                _copy.UserAgent.Should().Be("Test-Agent/1.0");
                _copy.Headers.Should().BeSameAs(_headers);
            }

            [Fact]
            public void Should_leave_the_original_options_alone()
            {
                _original.Uri.Should().Be(new Uri("http://example.com/file.zip"));
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
