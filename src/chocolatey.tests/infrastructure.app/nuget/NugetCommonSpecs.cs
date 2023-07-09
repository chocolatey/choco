// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace chocolatey.tests.infrastructure.app.nuget
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.nuget;
    using chocolatey.infrastructure.filesystem;
    using Moq;
    using NuGet.Common;
    using NuGet.Packaging;
    using NuGet.Protocol;
    using NuGet.Protocol.Core.Types;
    using FluentAssertions;

    public class NugetCommonSpecs
    {
        private class When_gets_remote_repository : TinySpec
        {
            private Action _because;
            private readonly Mock<ILogger> _nugetLogger = new Mock<ILogger>();
            private readonly Mock<IPackageDownloader> _packageDownloader = new Mock<IPackageDownloader>();
            private readonly Mock<IFileSystem> _filesystem = new Mock<IFileSystem>();
            private ChocolateyConfiguration _configuration;
            private IEnumerable<SourceRepository> _packageRepositories;

            public override void Context()
            {
                _configuration = new ChocolateyConfiguration();
                _nugetLogger.ResetCalls();
                _packageDownloader.ResetCalls();
                _filesystem.ResetCalls();

                _filesystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns((string a) =>
                {
                    return "C:\\packages\\" + a;
                });
            }

            public override void Because()
            {
                _because = () => _packageRepositories = NugetCommon.GetRemoteRepositories(_configuration, _nugetLogger.Object, _filesystem.Object);
            }

            [Fact]
            public void Should_create_repository_when_source_is_null()
            {
                Context();
                _configuration.Sources = null;

                _because();

                _packageRepositories.Should().BeEmpty();
            }

            [Fact]
            public void Should_parse_http_source()
            {
                Context();
                var source = "http://nexus.example.com:8081/repository/choco";
                _configuration.Sources = source;
                _configuration.CacheLocation = "C:\\temp";

                _because();

                _packageRepositories.First().PackageSource.TrySourceAsUri.Should().NotBeNull();
                _packageRepositories.First().PackageSource.SourceUri.ToStringSafe().Should().Be(source);
                _packageRepositories.First().PackageSource.IsHttp.Should().BeTrue();
            }

            [Fact]
            public void Should_parse_https_source()
            {
                Context();
                var source = "https://nexus.example.com/repository/choco";
                _configuration.Sources = source;
                _configuration.CacheLocation = "C:\\temp";

                _because();

                _packageRepositories.First().PackageSource.TrySourceAsUri.Should().NotBeNull();
                _packageRepositories.First().PackageSource.SourceUri.ToStringSafe().Should().Be(source);
                _packageRepositories.First().PackageSource.IsHttps.Should().BeTrue();
            }

            [Fact]
            public void Should_parse_absolute_path_source()
            {
                Context();
                var source = "C:\\packages";
                _configuration.Sources = source;

                _because();

                _packageRepositories.First().PackageSource.TrySourceAsUri.Should().NotBeNull();
                _packageRepositories.First().PackageSource.SourceUri.ToStringSafe()
                    .Should().Be(("file:///" + source).Replace("\\", "/"));
                _packageRepositories.First().PackageSource.IsLocal.Should().BeTrue();
            }

            [Fact]
            public void Should_parse_relative_path_source()
            {
                Context();
                var source = "choco";
                var fullsource = "C:\\packages\\choco";
                _configuration.Sources = source;

                _because();

                _packageRepositories.First().PackageSource.TrySourceAsUri.Should().NotBeNull();
                _packageRepositories.First().PackageSource.SourceUri.ToStringSafe()
                    .Should().Be(("file:///" + fullsource).Replace("\\", "/"));
                _packageRepositories.First().PackageSource.IsLocal.Should().BeTrue();
            }

            [Fact]
            public void Should_parse_dot_relative_path_source()
            {
                Context();
                var source = ".";
                var fullsource = "C:\\packages";
                _configuration.Sources = source;

                _because();

                _packageRepositories.First().PackageSource.TrySourceAsUri.Should().NotBeNull();
                _packageRepositories.First().PackageSource.SourceUri.ToStringSafe()
                    .Should().Be(("file:///" + fullsource + "/").Replace("\\", "/"));
                _packageRepositories.First().PackageSource.IsLocal.Should().BeTrue();
            }

            [Fact]
            public void Should_parse_unc_source()
            {
                Context();
                var source = "\\\\samba-server\\choco-share";
                _configuration.Sources = source;

                _because();

                _packageRepositories.First().PackageSource.TrySourceAsUri.Should().NotBeNull();
                _packageRepositories.First().PackageSource.SourceUri.ToStringSafe()
                    .Should().Be(("file:" + source).Replace("\\", "/"));
                _packageRepositories.First().PackageSource.IsLocal.Should().BeTrue();
                _packageRepositories.First().PackageSource.SourceUri.IsUnc.Should().BeTrue();
            }

            [Fact]
            public void Should_set_user_agent_string()
            {
                Context();
                var source = "https://community.chocolatey.org/api/v2/";
                _configuration.Sources = source;
                _configuration.Information.ChocolateyProductVersion = "vNext";

                _because();

                // Change this when the NuGet version is updated.
                string nugetClientVersion = "6.4.1";
                string expectedUserAgentString = "{0}/{1} via NuGet Client/{2}".FormatWith(ApplicationParameters.UserAgent, _configuration.Information.ChocolateyProductVersion, nugetClientVersion);
                UserAgent.UserAgentString.Should().StartWith(expectedUserAgentString);
            }
        }
    }
}
