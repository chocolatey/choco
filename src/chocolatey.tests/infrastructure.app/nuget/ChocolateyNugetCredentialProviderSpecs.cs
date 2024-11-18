using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.nuget;
using FluentAssertions;
using Moq;
using NuGet.Configuration;
using NUnit.Framework;

namespace chocolatey.tests.infrastructure.app.nuget
{
    public class ChocolateyNugetCredentialProviderSpecs
    {
        public abstract class ChocolateyNugetCredentialProviderSpecsBase : TinySpec
        {
            protected ChocolateyConfiguration Configuration;
            protected ChocolateyNugetCredentialProvider Provider;

            protected const string Username = "user";
            protected const string Password = "totally_secure_password!!!";
            protected const string TargetSourceName = "testsource";
            protected const string TargetSourceUrl = "https://testserver.org/repository/test-repository";
            protected Uri TargetSourceUri = new Uri(TargetSourceUrl);

            protected NetworkCredential Result;

            private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();
            protected CancellationToken CancellationToken
            {
                get
                {
                    return _tokenSource.Token;
                }
            }

            public override void Context()
            {
                Configuration = new ChocolateyConfiguration();
                Provider = new ChocolateyNugetCredentialProvider(Configuration);

                Configuration.Information.IsInteractive = false;
                Configuration.MachineSources = new List<MachineSourceConfiguration>
                {
                    new MachineSourceConfiguration
                    {
                        AllowSelfService = true,
                        VisibleToAdminsOnly = false,
                        EncryptedPassword = NugetEncryptionUtility.EncryptString("otherPassword"),
                        Username = "otherUserName",
                        Key = "https://someotherplace.com/repository/things/",
                        Name = "not-this-one",
                        Priority = 1,
                    },
                    new MachineSourceConfiguration
                    {
                        AllowSelfService = true,
                        VisibleToAdminsOnly = false,
                        EncryptedPassword = NugetEncryptionUtility.EncryptString(Password),
                        Username = Username,
                        Key = TargetSourceUrl,
                        Name = TargetSourceName,
                        Priority = 1,
                    },
                };
            }

            [OneTimeSetUp]
            public async Task OneTimeSetup()
            {
                await With();
            }

            public virtual async Task With()
            {
                var result = await Provider.GetAsync(TargetSourceUri, proxy: null, CredentialRequestType.Unauthorized, message: string.Empty, isRetry: false, nonInteractive: true, CancellationToken);
                Result = result.Credentials as NetworkCredential;
            }
        }

        public class When_using_explicit_credentials_and_source_param : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Because()
            {
                Configuration.Sources = Configuration.ExplicitSources = TargetSourceUrl;
                Configuration.SourceCommand.Username = "user";
                Configuration.SourceCommand.Password = "totally_secure_password!!!";
            }

            [Fact]
            public void Creates_a_valid_credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Provides_the_correct_username()
            {
                Result.UserName.Should().Be("user");
            }

            [Fact]
            public void Provides_the_correct_password()
            {
                Result.Password.Should().Be("totally_secure_password!!!");
            }
        }

        public class When_a_source_name_is_provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Because()
            {
                Configuration.Sources = TargetSourceUrl;
                Configuration.ExplicitSources = TargetSourceName;
            }

            [Fact]
            public void Finds_the_saved_source_and_returns_the_credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Provides_the_correct_username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Provides_the_correct_password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class When_a_source_url_matching_a_saved_source_is_provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Because()
            {
                Configuration.Sources = Configuration.ExplicitSources = TargetSourceUrl;
            }

            [Fact]
            public void Finds_the_saved_source_and_returns_the_credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Provides_the_correct_username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Provides_the_correct_password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class Looks_up_source_url_when_name_and_credentials_is_provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Because()
            {
                Configuration.Sources = TargetSourceUrl;
                Configuration.ExplicitSources = TargetSourceName;

                Configuration.SourceCommand.Username = "user";
                Configuration.SourceCommand.Password = "totally_secure_password!!!";
            }

            [Fact]
            public void Creates_and_returns_the_credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Provides_the_correct_username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Provides_the_correct_password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class When_no_matching_source_is_found_by_url : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Because()
            {
                Configuration.Sources = Configuration.ExplicitSources = "https://unknownurl.com/api/v2/";
            }

            public override async Task With()
            {
                var result = await Provider.GetAsync(new Uri("https://unknownurl.com/api/v2/"), proxy: null, CredentialRequestType.Unauthorized, message: string.Empty, isRetry: false, nonInteractive: true, CancellationToken);
                Result = result.Credentials as NetworkCredential;
            }

            [Fact]
            public void Returns_the_default_network_credential()
            {
                Result.Should().Be(CredentialCache.DefaultNetworkCredentials);
            }
        }

        public class When_multiple_named_sources_are_provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Because()
            {
                Configuration.Sources = Configuration.MachineSources.Select(s => s.Key).Join(";");
                Configuration.ExplicitSources = Configuration.MachineSources.Select(s => s.Name).Join(";");
            }

            [Fact]
            public void Finds_the_correct_saved_source_for_the_target_uri_and_returns_the_credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Provides_the_correct_username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Provides_the_correct_password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class When_multiple_source_urls_are_provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Because()
            {
                Configuration.Sources = Configuration.ExplicitSources = $"https://unknownurl.com/api/v2/;{TargetSourceUrl}";
            }

            [Fact]
            public void Finds_the_saved_source_and_returns_the_credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Provides_the_correct_username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Provides_the_correct_password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class When_a_mix_of_named_and_url_sources_are_provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Because()
            {
                Configuration.Sources = $"https://unknownurl.com/api/v2/;{TargetSourceUrl}";
                Configuration.ExplicitSources = $"https://unknownurl.com/api/v2/;{TargetSourceName}";
            }

            [Fact]
            public void Finds_the_saved_source_and_returns_the_credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Provides_the_correct_username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Provides_the_correct_password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        // This is a regression test for issue #3565
        public class When_a_url_matching_the_hostname_only_of_a_saved_source_is_provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            private Uri _otherRepoUri;
            public override void Because()
            {
                _otherRepoUri = new Uri(TargetSourceUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped) + "/other_path/repository/");
                Configuration.Sources = Configuration.ExplicitSources = _otherRepoUri.AbsoluteUri;
            }

            public override async Task With()
            {
                var result = await Provider.GetAsync(new Uri("https://unknownurl.com/api/v2/"), proxy: null, CredentialRequestType.Unauthorized, message: string.Empty, isRetry: false, nonInteractive: true, CancellationToken);
                Result = result.Credentials as NetworkCredential;
            }

            [Fact]
            public void Returns_the_default_network_credential()
            {
                Result.Should().Be(CredentialCache.DefaultNetworkCredentials);
            }
        }
    }
}
