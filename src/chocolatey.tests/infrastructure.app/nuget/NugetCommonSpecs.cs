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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using chocolatey.infrastructure.app;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.nuget;
using chocolatey.infrastructure.filesystem;
using Chocolatey.NuGet.Frameworks;
using FluentAssertions;
using Moq;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace chocolatey.tests.infrastructure.app.nuget
{
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

                _because();

                // Change this when the NuGet version is updated.
                const string nugetClientVersion = "6.4.1";
                var currentProcess = Process.GetCurrentProcess();
                var expectedUserAgentRegexString = @"^{0}\/[\d\.]+(-[A-za-z\d\.-]+)? {1}\/[\d\.]+(-[A-Za-z\d\.-]+)? (\([A-za-z\d\.-]+(, [A-Za-z\d\.-]+)?\) )?via NuGet Client\/{2}".FormatWith(
                    ApplicationParameters.UserAgent,
                    currentProcess.ProcessName,
                    Regex.Escape(nugetClientVersion));
                UserAgent.UserAgentString.Should().MatchRegex(expectedUserAgentRegexString);
            }
        }

        private class When_gets_package_dependencies : TinySpec
        {
            private Func<Task> _because;
            private readonly Mock<SourceCacheContext> _sourceCacheContext = new Mock<SourceCacheContext>();
            private readonly Mock<ILogger> _nugetLogger = new Mock<ILogger>();
            private readonly List<NuGetEndpointResources> _nuGetEndpointResources = new List<NuGetEndpointResources>();
            private readonly HashSet<SourcePackageDependencyInfo> _sourcePackageDependencyInfos = new HashSet<SourcePackageDependencyInfo>();
            private readonly HashSet<PackageDependency> _packageDependencies = new HashSet<PackageDependency>();
            private readonly Mock<SourceRepository> _sourceRepository = new Mock<SourceRepository>();
            private readonly Mock<DependencyInfoResource> _dependencyInfoResource = new Mock<DependencyInfoResource>();
            private PackageSource _packageSource;
            private ChocolateyConfiguration _configuration;

            public override void Context()
            {
                _configuration = new ChocolateyConfiguration();
                _sourceCacheContext.ResetCalls();
                _nugetLogger.ResetCalls();
                _sourceRepository.ResetCalls();
                _nuGetEndpointResources.Clear();
                _sourcePackageDependencyInfos.Clear();
                _packageDependencies.Clear();
                _sourceRepository.Setup(r => r.GetResource<DependencyInfoResource>(It.IsAny<SourceCacheContext>())).Returns(_dependencyInfoResource.Object);
                _packageSource = new PackageSource("C:\\packages");
                _sourceRepository.SetupGet(r => r.PackageSource).Returns(_packageSource);

                var chocolateySourceCacheContext = new ChocolateySourceCacheContext(_configuration);
                _nuGetEndpointResources.Add(NuGetEndpointResources.GetResourcesBySource(_sourceRepository.Object, chocolateySourceCacheContext));
            }

            public override void Because()
            {
                _because = () => NugetCommon.GetPackageDependencies(new PackageIdentity("a", new NuGetVersion(1, 0, 1000)), NuGetFramework.AnyFramework,
                    _sourceCacheContext.Object, _nugetLogger.Object, _nuGetEndpointResources, _sourcePackageDependencyInfos, _packageDependencies, _configuration);
            }

            [Fact]
            public async Task Should_request_dependencies_once()
            {
                Context();

                var adeps = new[] { new PackageDependency("b", new VersionRange(new NuGetVersion(1, 0, 1000), true, new NuGetVersion(2, 0, 0), false)), new PackageDependency("c", new VersionRange(new NuGetVersion(1, 0, 1), true, new NuGetVersion(2, 0, 0), false)) };
                var adepInfo = new SourcePackageDependencyInfo("a", new NuGetVersion(1, 0, 1000), adeps, true, _sourceRepository.Object);
                _dependencyInfoResource.Setup(r => r.ResolvePackage(It.Is<PackageIdentity>(pid => pid.Id == "a" && pid.Version == new NuGetVersion(1, 0, 1000)), It.IsAny<NuGetFramework>(), It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(adepInfo);
                var bdeps = new[] { new PackageDependency("d", new VersionRange(new NuGetVersion(1, 0, 1000), true, new NuGetVersion(2, 0, 0), false)) };
                var bdepInfo = new[] { new SourcePackageDependencyInfo("b", new NuGetVersion(1, 0, 1000), bdeps, true, _sourceRepository.Object) };
                _dependencyInfoResource.Setup(r => r.ResolvePackages("b", false, NuGetFramework.AnyFramework, It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(bdepInfo);
                var cdeps = new[] { new PackageDependency("d", new VersionRange(new NuGetVersion(1, 0, 1), true, new NuGetVersion(2, 0, 0), false)) };
                var cdepInfo = new[] { new SourcePackageDependencyInfo("c", new NuGetVersion(1, 0, 1), cdeps, true, _sourceRepository.Object) };
                _dependencyInfoResource.Setup(r => r.ResolvePackages("c", false, NuGetFramework.AnyFramework, It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(cdepInfo);
                var ddepInfo = GetDependencies("d", "e", "1.0.0", 1001, "2.0.0");
                _dependencyInfoResource.Setup(r => r.ResolvePackages("d", false, NuGetFramework.AnyFramework, It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(ddepInfo);
                var edepInfo = GetDependencies("e", null, "1.0.0", 1001, null);
                _dependencyInfoResource.Setup(r => r.ResolvePackages("e", false, NuGetFramework.AnyFramework, It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>())).ReturnsAsync(edepInfo);

                await _because();

                _dependencyInfoResource.Verify(r => r.ResolvePackage(It.Is<PackageIdentity>(pid => pid.Id == "a" && pid.Version == new NuGetVersion(1, 0, 1000)), It.IsAny<NuGetFramework>(), It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>()), Times.Once());
                _dependencyInfoResource.Verify(r => r.ResolvePackages("b", false, NuGetFramework.AnyFramework, It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>()), Times.Once());
                _dependencyInfoResource.Verify(r => r.ResolvePackages("c", false, NuGetFramework.AnyFramework, It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>()), Times.Once());
                _dependencyInfoResource.Verify(r => r.ResolvePackages("d", false, NuGetFramework.AnyFramework, It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>()), Times.Once());
                _dependencyInfoResource.Verify(r => r.ResolvePackages("e", false, NuGetFramework.AnyFramework, It.IsAny<SourceCacheContext>(), It.IsAny<ILogger>(), It.IsAny<CancellationToken>()), Times.Once());
            }

            private IEnumerable<SourcePackageDependencyInfo> GetDependencies(string packageId, string dependencyId, string lowerRangeStart, int count, string upperRange)
            {
                var dependencyInfo = new List<SourcePackageDependencyInfo>();
                var startVersion = NuGetVersion.Parse(lowerRangeStart);
                var upperVersion = !string.IsNullOrEmpty(upperRange) ? NuGetVersion.Parse(upperRange) : null;
                for (var i = startVersion.Patch; i < startVersion.Patch + count; i++)
                {
                    var packageDependency = !string.IsNullOrEmpty(dependencyId)
                        ? new[] { new PackageDependency(dependencyId, new VersionRange(new NuGetVersion(startVersion.Major, startVersion.Minor, i), true, upperVersion, false)) }
                        : Array.Empty<PackageDependency>();
                    dependencyInfo.Add(new SourcePackageDependencyInfo(packageId, new NuGetVersion(startVersion.Major, startVersion.Minor, i), packageDependency, true, _sourceRepository.Object));
                }

                return dependencyInfo;
            }
        }
    }
}
