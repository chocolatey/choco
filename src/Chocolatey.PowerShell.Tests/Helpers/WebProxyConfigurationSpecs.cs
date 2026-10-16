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
using System.Net;
using Chocolatey.PowerShell.Helpers;
using FluentAssertions;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Tests.Helpers
{
    // ReSharper disable InconsistentNaming

    public class WebProxyConfigurationSpecs
    {
        public abstract class WebProxyConfigurationSpecsBase : EnvironmentSpecsBase
        {
            protected override string[] ManagedVariables { get; } =
            {
                Package.ChocolateyProxyLocation,
                Package.ChocolateyProxyBypassOnLocal,
                Package.ChocolateyProxyBypassList,
                Package.ChocolateyProxyUser,
                Package.ChocolateyProxyPassword,
            };
        }

        public class When_no_proxy_variables_are_set : WebProxyConfigurationSpecsBase
        {
            public override void Because()
            {
            }

            [Fact]
            public void Should_not_be_enabled()
            {
                WebProxyConfiguration.IsEnabled.Should().BeFalse();
            }

            [Fact]
            public void Should_not_bypass_on_local()
            {
                WebProxyConfiguration.BypassOnLocal.Should().BeFalse();
            }

            [Fact]
            public void Should_have_an_empty_bypass_list()
            {
                WebProxyConfiguration.BypassList.Should().NotBeNull().And.BeEmpty();
            }
        }

        public class When_the_proxy_location_is_empty : WebProxyConfigurationSpecsBase
        {
            public override void Because()
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, string.Empty);
            }

            [Fact]
            public void Should_not_be_enabled()
            {
                WebProxyConfiguration.IsEnabled.Should().BeFalse();
            }
        }

        public class When_the_proxy_location_is_set : WebProxyConfigurationSpecsBase
        {
            public override void Because()
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
            }

            [Fact]
            public void Should_be_enabled()
            {
                WebProxyConfiguration.IsEnabled.Should().BeTrue();
            }

            [Fact]
            public void Should_return_the_configured_url()
            {
                WebProxyConfiguration.Url.Should().Be("http://proxy.example.com:8080");
            }
        }

        public class When_the_proxy_credentials_are_set : WebProxyConfigurationSpecsBase
        {
            public override void Because()
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyUser, "someone");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyPassword, "hunter2");
            }

            [Fact]
            public void Should_return_the_configured_username()
            {
                WebProxyConfiguration.Username.Should().Be("someone");
            }

            [Fact]
            public void Should_return_the_configured_password()
            {
                WebProxyConfiguration.Password.Should().Be("hunter2");
            }
        }

        public class When_bypass_on_local_is_the_literal_string_true : WebProxyConfigurationSpecsBase
        {
            public override void Because()
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyBypassOnLocal, "true");
            }

            [Fact]
            public void Should_bypass_on_local()
            {
                WebProxyConfiguration.BypassOnLocal.Should().BeTrue();
            }
        }

        public class When_bypass_on_local_is_any_other_value : WebProxyConfigurationSpecsBase
        {
            public override void Because()
            {
            }

            [InlineData("false")]
            [InlineData("")]
            [InlineData("yes")]
            [InlineData("1")]
            public void Should_not_bypass_on_local(string value)
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyBypassOnLocal, value);

                WebProxyConfiguration.BypassOnLocal.Should().BeFalse();
            }
        }

        public class When_the_bypass_list_has_a_single_value : WebProxyConfigurationSpecsBase
        {
            public override void Because()
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyBypassList, "*.example.com");
            }

            [Fact]
            public void Should_return_a_single_value_in_an_array()
            {
                WebProxyConfiguration.BypassList.Should().Equal("*.example.com");
            }
        }

        public class When_the_bypass_list_has_comma_separated_values : WebProxyConfigurationSpecsBase
        {
            public override void Because()
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyBypassList, "*.example.com,internal.local");
            }

            [Fact]
            public void Should_split_on_the_commas()
            {
                WebProxyConfiguration.BypassList.Should().Equal("*.example.com", "internal.local");
            }
        }

        public class When_the_bypass_list_contains_empty_entries : WebProxyConfigurationSpecsBase
        {
            public override void Because()
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyBypassList, ",a.example.com,,b.example.com,");
            }

            [Fact]
            public void Should_discard_the_empty_entries()
            {
                WebProxyConfiguration.BypassList.Should().Equal("a.example.com", "b.example.com");
            }
        }

        public class When_creating_the_proxy_from_the_configuration : WebProxyConfigurationSpecsBase
        {
            private WebProxy _proxy;

            public override void Because()
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyBypassOnLocal, "true");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyBypassList, "example\\.com,internal\\.local");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyUser, "someone");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyPassword, "hunter2");

                _proxy = WebProxyConfiguration.CreateProxy();
            }

            [Fact]
            public void Should_use_the_location_as_the_address()
            {
                _proxy.Address.Should().Be(new Uri("http://proxy.example.com:8080"));
            }

            [Fact]
            public void Should_bypass_the_proxy_on_local_addresses()
            {
                _proxy.BypassProxyOnLocal.Should().BeTrue();
            }

            [Fact]
            public void Should_use_the_bypass_list()
            {
                _proxy.BypassList.Should().Equal(@"example\.com", @"internal\.local");
            }

            [Fact]
            public void Should_use_the_user_and_password_as_the_credentials()
            {
                var credential = _proxy.Credentials.Should().BeOfType<NetworkCredential>().Subject;
                credential.UserName.Should().Be("someone");
                credential.Password.Should().Be("hunter2");
            }
        }

        public class When_creating_the_proxy_without_a_password : WebProxyConfigurationSpecsBase
        {
            private WebProxy _proxy;

            public override void Because()
            {
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyLocation, "http://proxy.example.com:8080");
                Environment.SetEnvironmentVariable(Package.ChocolateyProxyUser, "someone");

                _proxy = WebProxyConfiguration.CreateProxy();
            }

            [Fact]
            public void Should_not_set_credentials()
            {
                _proxy.Credentials.Should().BeNull();
            }

            [Fact]
            public void Should_not_bypass_the_proxy_on_local_addresses()
            {
                _proxy.BypassProxyOnLocal.Should().BeFalse();
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
