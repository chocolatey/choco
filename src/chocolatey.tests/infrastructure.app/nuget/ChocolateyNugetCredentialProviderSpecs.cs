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

            public override void Because()
            {
                With().Wait();
            }

            public virtual async Task With()
            {
                var result = await Provider.GetAsync(TargetSourceUri, proxy: null, CredentialRequestType.Unauthorized, message: string.Empty, isRetry: false, nonInteractive: true, CancellationToken);
                Result = result.Credentials as NetworkCredential;
            }
        }

        public class When_Using_Explicit_Credentials_And_Source_Param : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = Configuration.ExplicitSources = TargetSourceUrl;
                Configuration.SourceCommand.Username = "user";
                Configuration.SourceCommand.Password = "totally_secure_password!!!";
            }

            [Fact]
            public void Should_Create_Credentials()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Should_Provide_The_Correct_Username()
            {
                Result.UserName.Should().Be("user");
            }

            [Fact]
            public void Should_Provide_The_Correct_Password()
            {
                Result.Password.Should().Be("totally_secure_password!!!");
            }
        }

        public class When_A_Source_Name_Is_Provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = TargetSourceUrl;
                Configuration.ExplicitSources = TargetSourceName;
            }

            [Fact]
            public void Should_Find_The_Saved_Source_And_Returns_The_Credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Should_Provide_The_Correct_Username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Should_Provide_The_Correct_Password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class When_A_Source_Url_Matching_A_Saved_Source_Is_Provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = Configuration.ExplicitSources = TargetSourceUrl;
            }

            [Fact]
            public void Should_Find_The_Saved_Source_And_Return_The_Credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Should_Provide_The_Correct_Username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Should_Provide_The_Correct_Password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class Looks_Up_Source_Url_When_Name_And_Credentials_Is_Provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = TargetSourceUrl;
                Configuration.ExplicitSources = TargetSourceName;

                Configuration.SourceCommand.Username = "user";
                Configuration.SourceCommand.Password = "totally_secure_password!!!";
            }

            [Fact]
            public void Should_Create_And_Return_The_Credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Should_Provide_The_Correct_Username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Should_Provide_The_Correct_Password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class When_No_Matching_Source_Is_Found_By_Url : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = Configuration.ExplicitSources = "https://unknownurl.com/api/v2/";
            }

            public override async Task With()
            {
                var result = await Provider.GetAsync(new Uri("https://unknownurl.com/api/v2/"), proxy: null, CredentialRequestType.Unauthorized, message: string.Empty, isRetry: false, nonInteractive: true, CancellationToken);
                Result = result.Credentials as NetworkCredential;
            }

            [Fact]
            public void Should_Return_The_Default_Network_Credential()
            {
                Result.Should().Be(CredentialCache.DefaultNetworkCredentials);
            }
        }

        public class When_Multiple_Named_Sources_Are_Provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = Configuration.MachineSources.Select(s => s.Key).Join(";");
                Configuration.ExplicitSources = Configuration.MachineSources.Select(s => s.Name).Join(";");
            }

            [Fact]
            public void Should_Find_The_Correct_Saved_Source_For_The_Target_Uri_And_Return_The_Credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Should_Provide_The_Correct_Username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Should_Provide_The_Correct_Password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class When_Multiple_Source_Urls_Are_Provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = Configuration.ExplicitSources = $"https://unknownurl.com/api/v2/;{TargetSourceUrl}";
            }

            [Fact]
            public void Should_Find_The_Saved_Source_And_Return_The_Credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Should_Provide_The_Correct_Username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Should_Provide_The_Correct_Password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        public class When_A_Mix_Of_Named_And_Url_Sources_Are_Provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            public override void Context()
            {
                base.Context();
                Configuration.Sources = $"https://unknownurl.com/api/v2/;{TargetSourceUrl}";
                Configuration.ExplicitSources = $"https://unknownurl.com/api/v2/;{TargetSourceName}";
            }

            [Fact]
            public void Should_Find_The_Saved_Source_And_Return_The_Credential()
            {
                Result.Should().NotBeNull();
            }

            [Fact]
            public void Should_Provide_The_Correct_Username()
            {
                Result.UserName.Should().Be(Username);
            }

            [Fact]
            public void Should_Provide_The_Correct_Password()
            {
                Result.Password.Should().Be(Password);
            }
        }

        // This is a regression test for issue #3565
        public class When_A_Url_Matching_The_Hostname_Only_Of_A_Saved_Source_Is_Provided : ChocolateyNugetCredentialProviderSpecsBase
        {
            private Uri _otherRepoUri;
            public override void Context()
            {
                base.Context();
                _otherRepoUri = new Uri(TargetSourceUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped) + "/other_path/repository/");
                Configuration.Sources = Configuration.ExplicitSources = _otherRepoUri.AbsoluteUri;
            }

            public override async Task With()
            {
                var result = await Provider.GetAsync(new Uri("https://unknownurl.com/api/v2/"), proxy: null, CredentialRequestType.Unauthorized, message: string.Empty, isRetry: false, nonInteractive: true, CancellationToken);
                Result = result.Credentials as NetworkCredential;
            }

            [Fact]
            public void Should_Return_The_Default_Network_Credential()
            {
                Result.Should().Be(CredentialCache.DefaultNetworkCredentials);
            }
        }
    }
}
