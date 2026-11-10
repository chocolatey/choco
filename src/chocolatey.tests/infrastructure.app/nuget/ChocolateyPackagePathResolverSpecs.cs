// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System.Collections.Generic;
using System.IO;
using chocolatey.infrastructure.app.nuget;
using FluentAssertions;
using Moq;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

namespace chocolatey.tests.infrastructure.app.nuget
{
    // Behavior of ChocolateyPackagePathResolver.
    //
    // Default (non-instanced) packages are version-less / no-side-by-side (the
    // resolver is constructed with useSideBySidePaths: false), so every version
    // of a given id resolves to the same lib\<id> folder. This half is a
    // characterization of long-standing behavior that must not regress.
    //
    // A package instance (choco install <id> --instance <name>) adds a trailing
    // <instance> segment — <root>\<id>\<instance> — so the id stays its own
    // path segment (ids with dots like 7zip.install are unambiguous) and the
    // default lib\<id> enumeration is untouched.
    public class ChocolateyPackagePathResolverSpecs
    {
        public abstract class ChocolateyPackagePathResolverSpecsBase : TinySpec
        {
            protected ChocolateyPackagePathResolver Resolver;
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            // Absolute on every platform, as PackagePathResolver requires.
            protected static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "choco-test", "lib");
            protected static readonly string DependenciesRoot = Path.Combine(Path.GetTempPath(), "choco-test", "dependencies");

            public override void Context()
            {
                FileSystem.ResetCalls();

                // Mirror DotNetFileSystem.CombinePaths with a real Path.Combine so
                // the resolver produces real, assertable paths.
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string[]>()))
                    .Returns((string left, string[] right) => CombinePaths(left, right));

                Resolver = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object);
            }

            public override void Because()
            {
            }

            protected static string CombinePaths(string left, string[] right)
            {
                if (right == null || right.Length == 0)
                {
                    return left;
                }

                var parts = new List<string>(right.Length + 1) { left };
                parts.AddRange(right);
                return Path.Combine(parts.ToArray());
            }
        }

        [ConcernFor("nuget")]
        public class When_getting_the_install_path_for_a_package : ChocolateyPackagePathResolverSpecsBase
        {
            private string _result;
            private readonly PackageIdentity _package = new PackageIdentity("bob", new NuGetVersion("1.0.0"));

            public override void Because()
            {
                _result = Resolver.GetInstallPath(_package);
            }

            [Fact]
            public void Should_combine_the_root_directory_with_the_package_id()
            {
                _result.Should().Be(Path.Combine(RootDirectory, "bob"));
            }

            [Fact]
            public void Should_not_include_the_version_in_the_path()
            {
                _result.Should().NotContain("1.0.0");
            }
        }

        [ConcernFor("nuget")]
        public class When_getting_the_install_path_for_two_versions_of_the_same_package : ChocolateyPackagePathResolverSpecsBase
        {
            private string _versionOnePath;
            private string _versionTwoPath;

            public override void Because()
            {
                _versionOnePath = Resolver.GetInstallPath(new PackageIdentity("bob", new NuGetVersion("1.0.0")));
                _versionTwoPath = Resolver.GetInstallPath(new PackageIdentity("bob", new NuGetVersion("2.0.0")));
            }

            [Fact]
            public void Should_resolve_both_versions_to_the_same_install_path()
            {
                _versionOnePath.Should().Be(_versionTwoPath);
            }
        }

        [ConcernFor("nuget")]
        public class When_getting_the_package_file_name : ChocolateyPackagePathResolverSpecsBase
        {
            private string _result;

            public override void Because()
            {
                _result = Resolver.GetPackageFileName(new PackageIdentity("bob", new NuGetVersion("1.0.0")));
            }

            [Fact]
            public void Should_use_the_id_and_the_nupkg_extension_without_a_version()
            {
                _result.Should().Be("bob.nupkg");
            }
        }

        [ConcernFor("nuget")]
        public class When_using_the_obsolete_install_path_overload_that_takes_a_version : ChocolateyPackagePathResolverSpecsBase
        {
            private string _result;

            public override void Because()
            {
#pragma warning disable CS0618 // deliberately exercising the obsolete overload
                _result = Resolver.GetInstallPath("bob", new NuGetVersion("9.9.9"));
#pragma warning restore CS0618
            }

            [Fact]
            public void Should_ignore_the_version_and_resolve_to_the_id_folder()
            {
                _result.Should().Be(Path.Combine(RootDirectory, "bob"));
            }
        }

        [ConcernFor("nuget")]
        public class When_creating_the_resolver : ChocolateyPackagePathResolverSpecsBase
        {
            [Fact]
            public void Should_expose_the_root_directory_it_was_created_with()
            {
                Resolver.RootDirectory.Should().Be(RootDirectory);
            }

            [Fact]
            public void Should_have_no_instance_by_default()
            {
                Resolver.Instance.Should().BeNullOrEmpty();
            }
        }

        [ConcernFor("nuget")]
        public class When_getting_the_install_path_for_an_instanced_package : ChocolateyPackagePathResolverSpecsBase
        {
            private ChocolateyPackagePathResolver _instanceResolver;
            private string _result;

            public override void Context()
            {
                base.Context();
                _instanceResolver = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "first");
            }

            public override void Because()
            {
                _result = _instanceResolver.GetInstallPath(new PackageIdentity("bob", new NuGetVersion("1.0.0")));
            }

            [Fact]
            public void Should_place_the_instance_under_the_package_id_folder()
            {
                _result.Should().Be(Path.Combine(RootDirectory, "bob", "first"));
            }

            [Fact]
            public void Should_expose_the_instance_name()
            {
                _instanceResolver.Instance.Should().Be("first");
            }
        }

        [ConcernFor("nuget")]
        public class When_getting_install_paths_for_two_instances_of_the_same_package : ChocolateyPackagePathResolverSpecsBase
        {
            private string _firstPath;
            private string _secondPath;

            public override void Because()
            {
                _firstPath = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "first").GetInstallPath("bob");
                _secondPath = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "second").GetInstallPath("bob");
            }

            [Fact]
            public void Should_resolve_each_instance_to_a_distinct_path()
            {
                _firstPath.Should().Be(Path.Combine(RootDirectory, "bob", "first"));
                _secondPath.Should().Be(Path.Combine(RootDirectory, "bob", "second"));
                _firstPath.Should().NotBe(_secondPath);
            }
        }

        [ConcernFor("nuget")]
        public class When_getting_the_install_path_for_a_dependency_of_an_instanced_package : ChocolateyPackagePathResolverSpecsBase
        {
            private ChocolateyPackagePathResolver _instanceResolver;
            private string _parentPath;
            private string _dependencyPath;

            public override void Context()
            {
                base.Context();
                _instanceResolver = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "first")
                {
                    DependenciesRootDirectory = DependenciesRoot,
                    ParentId = "bob"
                };
            }

            public override void Because()
            {
                _parentPath = _instanceResolver.GetInstallPath("bob");
                _dependencyPath = _instanceResolver.GetInstallPath("dep");
            }

            [Fact]
            public void Should_keep_the_parent_under_the_instances_root()
            {
                _parentPath.Should().Be(Path.Combine(RootDirectory, "bob", "first"));
            }

            [Fact]
            public void Should_route_the_dependency_to_the_regular_dependencies_root()
            {
                _dependencyPath.Should().Be(Path.Combine(DependenciesRoot, "dep"));
            }

            [Fact]
            public void Should_match_the_parent_id_case_insensitively()
            {
                _instanceResolver.GetInstallPath("BOB").Should().Be(Path.Combine(RootDirectory, "BOB", "first"));
            }
        }

        [ConcernFor("nuget")]
        public class When_no_parent_id_is_set_on_an_instanced_resolver : ChocolateyPackagePathResolverSpecsBase
        {
            private ChocolateyPackagePathResolver _instanceResolver;

            public override void Context()
            {
                base.Context();
                _instanceResolver = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "first")
                {
                    DependenciesRootDirectory = DependenciesRoot
                };
            }

            [Fact]
            public void Should_resolve_every_id_as_a_parent_instance()
            {
                _instanceResolver.GetInstallPath("dep").Should().Be(Path.Combine(RootDirectory, "dep", "first"));
            }
        }

        [ConcernFor("nuget")]
        public class When_a_parent_id_is_set_but_no_dependencies_root : ChocolateyPackagePathResolverSpecsBase
        {
            private ChocolateyPackagePathResolver _instanceResolver;

            public override void Context()
            {
                base.Context();
                _instanceResolver = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "first")
                {
                    ParentId = "bob"
                };
            }

            [Fact]
            public void Should_resolve_a_dependency_as_an_instance_when_there_is_no_dependencies_root()
            {
                _instanceResolver.GetInstallPath("dep").Should().Be(Path.Combine(RootDirectory, "dep", "first"));
            }

            [Fact]
            public void Should_resolve_a_dependency_as_an_instance_when_the_dependencies_root_is_only_whitespace()
            {
                _instanceResolver.DependenciesRootDirectory = "   ";

                _instanceResolver.GetInstallPath("dep").Should().Be(Path.Combine(RootDirectory, "dep", "first"));
            }
        }

        [ConcernFor("nuget")]
        public class When_the_instance_name_is_only_whitespace : ChocolateyPackagePathResolverSpecsBase
        {
            private string _result;

            public override void Because()
            {
                _result = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "   ").GetInstallPath("bob");
            }

            [Fact]
            public void Should_resolve_as_a_default_package_without_an_instance_folder()
            {
                _result.Should().Be(Path.Combine(RootDirectory, "bob"));
            }
        }

        [ConcernFor("nuget")]
        public class When_a_parent_id_is_set_on_a_resolver_without_an_instance : ChocolateyPackagePathResolverSpecsBase
        {
            private string _parentPath;
            private string _otherPath;

            public override void Context()
            {
                base.Context();
                Resolver.ParentId = "bob";
                Resolver.DependenciesRootDirectory = DependenciesRoot;
            }

            public override void Because()
            {
                _parentPath = Resolver.GetInstallPath("bob");
                _otherPath = Resolver.GetInstallPath("dep");
            }

            [Fact]
            public void Should_ignore_the_parent_id_for_the_parent()
            {
                _parentPath.Should().Be(Path.Combine(RootDirectory, "bob"));
            }

            [Fact]
            public void Should_ignore_the_dependencies_root_for_every_other_id()
            {
                _otherPath.Should().Be(Path.Combine(RootDirectory, "dep"));
            }
        }

        [ConcernFor("nuget")]
        public class When_getting_the_package_file_name_for_an_instanced_package : ChocolateyPackagePathResolverSpecsBase
        {
            private string _result;

            public override void Because()
            {
                _result = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "first")
                    .GetPackageFileName(new PackageIdentity("bob", new NuGetVersion("1.0.0")));
            }

            [Fact]
            public void Should_not_include_the_instance_or_the_version()
            {
                _result.Should().Be("bob.nupkg");
            }
        }

        [ConcernFor("nuget")]
        public class When_getting_the_install_path_of_an_instanced_package_by_identity : ChocolateyPackagePathResolverSpecsBase
        {
            private string _byIdentity;
            private string _byId;

            public override void Because()
            {
                var instanceResolver = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "first")
                {
                    ParentId = "bob",
                    DependenciesRootDirectory = DependenciesRoot
                };

                _byIdentity = instanceResolver.GetInstallPath(new PackageIdentity("bob", new NuGetVersion("2.0.0")));
                _byId = instanceResolver.GetInstallPath("bob");
            }

            [Fact]
            public void Should_resolve_the_same_path_as_by_id()
            {
                _byIdentity.Should().Be(_byId);
            }

            [Fact]
            public void Should_not_include_the_version()
            {
                _byIdentity.Should().Be(Path.Combine(RootDirectory, "bob", "first"));
            }
        }

        // One resolver is shared by a whole install, and the parent is changed for each requested package.
        [ConcernFor("nuget")]
        public class When_the_parent_id_changes_between_packages_on_one_resolver : ChocolateyPackagePathResolverSpecsBase
        {
            private string _bobWhenBobIsTheParent;
            private string _depWhenBobIsTheParent;
            private string _bobWhenDepIsTheParent;
            private string _depWhenDepIsTheParent;

            public override void Because()
            {
                var instanceResolver = new ChocolateyPackagePathResolver(RootDirectory, FileSystem.Object, "first")
                {
                    DependenciesRootDirectory = DependenciesRoot
                };

                instanceResolver.ParentId = "bob";
                _bobWhenBobIsTheParent = instanceResolver.GetInstallPath("bob");
                _depWhenBobIsTheParent = instanceResolver.GetInstallPath("dep");

                instanceResolver.ParentId = "dep";
                _bobWhenDepIsTheParent = instanceResolver.GetInstallPath("bob");
                _depWhenDepIsTheParent = instanceResolver.GetInstallPath("dep");
            }

            [Fact]
            public void Should_resolve_the_first_parent_as_an_instance_and_the_other_as_a_dependency()
            {
                _bobWhenBobIsTheParent.Should().Be(Path.Combine(RootDirectory, "bob", "first"));
                _depWhenBobIsTheParent.Should().Be(Path.Combine(DependenciesRoot, "dep"));
            }

            [Fact]
            public void Should_resolve_the_roles_the_other_way_round_once_the_parent_changes()
            {
                _depWhenDepIsTheParent.Should().Be(Path.Combine(RootDirectory, "dep", "first"));
                _bobWhenDepIsTheParent.Should().Be(Path.Combine(DependenciesRoot, "bob"));
            }
        }
    }
}
