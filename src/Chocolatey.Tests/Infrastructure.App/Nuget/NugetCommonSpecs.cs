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

namespace Chocolatey.Tests.Infrastructure.App.Nuget
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Nuget;
    using Chocolatey.Infrastructure.Filesystem;
    using Moq;
    using global::NuGet.Common;
    using global::NuGet.Packaging;
    using global::NuGet.Protocol;
    using global::NuGet.Protocol.Core.Types;
    using Should;

    public class NugetCommonSpecs
    {
        private class when_gets_remote_repository : TinySpec
        {
            private Action because;
            private readonly Mock<ILogger> nugetLogger = new Mock<ILogger>();
            private readonly Mock<IPackageDownloader> packageDownloader = new Mock<IPackageDownloader>();
            private readonly Mock<IFileSystem> filesystem = new Mock<IFileSystem>();
            private ChocolateyConfiguration configuration;
            private IEnumerable<SourceRepository> packageRepositories;

            public override void Context()
            {
                configuration = new ChocolateyConfiguration();
                nugetLogger.ResetCalls();
                packageDownloader.ResetCalls();
                filesystem.ResetCalls();

                filesystem.Setup(f => f.GetFullPath(It.IsAny<string>())).Returns((string a) =>
                {
                    return "C:\\packages\\" + a;
                });
            }

            public override void Because()
            {
                because = () => packageRepositories = NugetCommon.GetRemoteRepositories(configuration, nugetLogger.Object, filesystem.Object);
            }

            [Fact]
            public void Should_create_repository_when_source_is_null()
            {
                Context();
                configuration.Sources = null;

                because();

                packageRepositories.Count().ShouldEqual(0);
            }

            [Fact]
            public void Should_parse_http_source()
            {
                Context();
                var source = "http://nexus.example.com:8081/repository/choco";
                configuration.Sources = source;
                configuration.CacheLocation = "C:\\temp";

                because();

                packageRepositories.First().PackageSource.TrySourceAsUri.ShouldNotBeNull();
                packageRepositories.First().PackageSource.SourceUri.ToStringChecked().ShouldEqual(source);
                packageRepositories.First().PackageSource.IsHttp.ShouldBeTrue();
            }

            [Fact]
            public void Should_parse_https_source()
            {
                Context();
                var source = "https://nexus.example.com/repository/choco";
                configuration.Sources = source;
                configuration.CacheLocation = "C:\\temp";

                because();

                packageRepositories.First().PackageSource.TrySourceAsUri.ShouldNotBeNull();
                packageRepositories.First().PackageSource.SourceUri.ToStringChecked().ShouldEqual(source);
                packageRepositories.First().PackageSource.IsHttps.ShouldBeTrue();
            }

            [Fact]
            public void Should_parse_absolute_path_source()
            {
                Context();
                var source = "C:\\packages";
                configuration.Sources = source;

                because();

                packageRepositories.First().PackageSource.TrySourceAsUri.ShouldNotBeNull();
                packageRepositories.First().PackageSource.SourceUri.ToStringChecked()
                    .ShouldEqual(("file:///" + source).Replace("\\","/"));
                packageRepositories.First().PackageSource.IsLocal.ShouldBeTrue();
            }

            [Fact]
            public void Should_parse_relative_path_source()
            {
                Context();
                var source = "choco";
                var fullsource = "C:\\packages\\choco";
                configuration.Sources = source;

                because();

                packageRepositories.First().PackageSource.TrySourceAsUri.ShouldNotBeNull();
                packageRepositories.First().PackageSource.SourceUri.ToStringChecked()
                    .ShouldEqual(("file:///" + fullsource).Replace("\\", "/"));
                packageRepositories.First().PackageSource.IsLocal.ShouldBeTrue();
            }

            [Fact]
            public void Should_parse_dot_relative_path_source()
            {
                Context();
                var source = ".";
                var fullsource = "C:\\packages";
                configuration.Sources = source;

                because();

                packageRepositories.First().PackageSource.TrySourceAsUri.ShouldNotBeNull();
                packageRepositories.First().PackageSource.SourceUri.ToStringChecked()
                    .ShouldEqual(("file:///" + fullsource + "/").Replace("\\", "/"));
                packageRepositories.First().PackageSource.IsLocal.ShouldBeTrue();
            }

            [Fact]
            public void Should_parse_unc_source()
            {
                Context();
                var source = "\\\\samba-server\\choco-share";
                configuration.Sources = source;

                because();

                packageRepositories.First().PackageSource.TrySourceAsUri.ShouldNotBeNull();
                packageRepositories.First().PackageSource.SourceUri.ToStringChecked()
                    .ShouldEqual(("file:" + source).Replace("\\", "/"));
                packageRepositories.First().PackageSource.IsLocal.ShouldBeTrue();
                packageRepositories.First().PackageSource.SourceUri.IsUnc.ShouldBeTrue();
            }
        }
    }
}
