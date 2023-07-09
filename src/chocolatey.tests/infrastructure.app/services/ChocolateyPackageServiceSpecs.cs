// Copyright © 2017 - 2023 Chocolatey Software, Inc
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.filesystem;
using chocolatey.infrastructure.results;
using chocolatey.infrastructure.services;
using Moq;
using NUnit.Framework;
using FluentAssertions;
using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

namespace chocolatey.tests.infrastructure.app.services
{
    using System.IO;
    using chocolatey.infrastructure.app.registration;
    using NuGet.Packaging;

    public class ChocolateyPackageServiceSpecs
    {
        public abstract class ChocolateyPackageServiceSpecsBase : TinySpec
        {
            protected ChocolateyPackageService Service;
            protected Mock<INugetService> NugetService = new Mock<INugetService>();
            protected Mock<IPowershellService> PowershellService = new Mock<IPowershellService>();
            protected Mock<IShimGenerationService> ShimGenerationService = new Mock<IShimGenerationService>();
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Mock<IRegistryService> RegistryService = new Mock<IRegistryService>();
            protected Mock<IChocolateyPackageInformationService> ChocolateyPackageInformationService = new Mock<IChocolateyPackageInformationService>();
            protected Mock<IFilesService> FilesService = new Mock<IFilesService>();
            protected Mock<IAutomaticUninstallerService> AutomaticUninstallerService = new Mock<IAutomaticUninstallerService>();
            protected Mock<IXmlService> XmlService = new Mock<IXmlService>();
            protected Mock<IConfigTransformService> ConfigTransformService = new Mock<IConfigTransformService>();
            protected Mock<IContainerResolver> ContainerResolver = new Mock<IContainerResolver>();

            protected ChocolateyConfiguration Configuration = new ChocolateyConfiguration();

            public override void Context()
            {
                NugetService.ResetCalls();
                PowershellService.ResetCalls();
                ShimGenerationService.ResetCalls();
                FileSystem.ResetCalls();
                RegistryService.ResetCalls();
                ChocolateyPackageInformationService.ResetCalls();
                FilesService.ResetCalls();
                AutomaticUninstallerService.ResetCalls();
                XmlService.ResetCalls();
                ConfigTransformService.ResetCalls();
                ContainerResolver.ResetCalls();
                Service = new ChocolateyPackageService(
                    NugetService.Object,
                    PowershellService.Object,
                    ShimGenerationService.Object,
                    FileSystem.Object,
                    RegistryService.Object,
                    ChocolateyPackageInformationService.Object,
                    FilesService.Object,
                    AutomaticUninstallerService.Object,
                    XmlService.Object,
                    ConfigTransformService.Object,
                    ContainerResolver.Object);
            }
        }

        public class When_ChocolateyPackageService_install_from_package_config_with_custom_sources : ChocolateyPackageServiceSpecsBase
        {
            protected Mock<IInstallSourceRunner> WindowsFeaturesRunner = new Mock<IInstallSourceRunner>();

            private ConcurrentDictionary<string, PackageResult> _result;

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = @"C:\test\packages.config";
                Configuration.Sources = @"C:\test";

                WindowsFeaturesRunner.Setup(r => r.SourceType).Returns(SourceTypes.WindowsFeatures);

                var package = new Mock<IPackageMetadata>();
                var expectedResult = new ConcurrentDictionary<string, PackageResult>();
                expectedResult.TryAdd("test-feature", new PackageResult(package.Object, "windowsfeatures", null));

                WindowsFeaturesRunner.Setup(r => r.Install(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()))
                    .Returns(expectedResult);
                NugetService.Setup(n => n.Install(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()))
                    .Returns(expectedResult);

                ContainerResolver.Setup(c => c.ResolveAll<IInstallSourceRunner>()).Returns(new[] { WindowsFeaturesRunner.Object });

                FileSystem.Setup(f => f.GetFullPath(Configuration.PackageNames)).Returns(Configuration.PackageNames);
                FileSystem.Setup(f => f.FileExists(Configuration.PackageNames)).Returns(true);

                XmlService.Setup(x => x.Deserialize<PackagesConfigFileSettings>(Configuration.PackageNames))
                    .Returns(new PackagesConfigFileSettings
                    {
                        Packages = new HashSet<PackagesConfigFilePackageSetting>
                        {
                            new PackagesConfigFilePackageSetting
                            {
                                Id = "test-feature",
                                Source = "windowsfeatures"
                            }
                        }
                    });
            }

            public override void Because()
            {
                _result = Service.Install(Configuration);
            }

            [Test]
            public void Should_return_package_that_should_have_been_installed()
            {
                _result.Keys.Should().Contain("test-feature");
            }

            [Test]
            public void Should_have_called_runner_for_windows_features_source()
            {
                WindowsFeaturesRunner
                    .Verify(r => r.Install(It.Is<ChocolateyConfiguration>(c => c.PackageNames == "test-feature"), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()), Times.Once);
            }

            [Test]
            public void Should_not_have_called_runner_for_windows_features_source_with_other_package_names()
            {
                WindowsFeaturesRunner.Verify(r => r.Install(It.Is<ChocolateyConfiguration>(c => c.PackageNames != "test-feature"), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()), Times.Never);
                WindowsFeaturesRunner.Verify(r => r.Install(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()), Times.Never);
            }

            [Test]
            public void Should_not_have_called_normal_source_runner_for_non_empty_packages()
            {
                // The normal source runners will be called with an argument
                NugetService.Verify(r => r.Install(It.Is<ChocolateyConfiguration>(c => c.PackageNames != string.Empty), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()), Times.Never);
            }
        }

        public class When_ChocolateyPackageService_tries_to_install_nupkg_file : ChocolateyPackageServiceSpecsBase
        {
            protected Action Action;

            public override void Context()
            {
                base.Context();
                Action = () => Service.InstallDryRun(Configuration);
                Configuration.CommandName = "install";
            }

            public override void Because()
            {
            }

            [Fact]
            public void Should_throw_exception_when_full_path_is_passed_to_install_run()
            {
                var directory = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "testing");
                Configuration.PackageNames = Path.Combine(
                    directory,
                    "my-package.nupkg");
                FileSystem.Setup(f => f.GetFilenameWithoutExtension(Configuration.PackageNames))
                    .Returns("my-package");
                FileSystem.Setup(f => f.GetFileName(Configuration.PackageNames))
                    .Returns("my-package.nupkg");
                FileSystem.Setup(f => f.GetDirectoryName(Configuration.PackageNames))
                    .Returns(directory);

                var ex = TryRun(Action);
                var message = GetExpectedLocalValue(directory, "my-package");

                ex.Message.Should().Be(message);
            }

            [Fact]
            public void Should_throw_exception_when_full_file_prefixed_path_is_passed_to_install_run()
            {
                var directory = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "testing");
                var filePath = Path.Combine(directory, "my-package.nupkg");
                Configuration.PackageNames = new Uri(filePath).AbsoluteUri;
                FileSystem.Setup(f => f.GetFilenameWithoutExtension(filePath))
                    .Returns("my-package");
                FileSystem.Setup(f => f.GetFileName(filePath))
                    .Returns("my-package.nupkg");
                FileSystem.Setup(f => f.GetDirectoryName(filePath))
                    .Returns(directory);

                var ex = TryRun(Action);
                var message = GetExpectedLocalValue(directory, "my-package");
                ex.Message.Should().Be(message);
            }

            [Fact, Categories.Unc]
            public void Should_throw_exception_when_UNC_path_is_passed_to_install_run()
            {
                var directory = UNCHelper.ConvertLocalFolderPathToIpBasedUncPath(Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "testing"));
                var filePath = Path.Combine(directory, "my-package.nupkg");
                Configuration.PackageNames = new Uri(filePath).AbsoluteUri;
                FileSystem.Setup(f => f.GetFilenameWithoutExtension(filePath))
                    .Returns("my-package");
                FileSystem.Setup(f => f.GetFileName(filePath))
                    .Returns("my-package.nupkg");
                FileSystem.Setup(f => f.GetDirectoryName(filePath))
                    .Returns(directory);

                var ex = TryRun(Action);
                var message = GetExpectedUncValue(directory, "my-package");
                ex.Message.Should().Be(message);
            }

            [Fact]
            public void Should_throw_exception_when_remote_path_is_passed_to_install_run()
            {
                Configuration.PackageNames = "https://test.com/repository/awesome-package.nupkg";

                var ex = TryRun(Action);
                ex.Message.Should().Be("Package name cannot point directly to a local, or remote file. Please use the --source argument and point it to a local file directory, UNC directory path or a NuGet feed instead.");
            }

            [Fact]
            public void Should_throw_exception_when_passed_in_path_to_nupkg_is_relative_and_it_exists()
            {
                Configuration.PackageNames = "test.1.5.0.nupkg";
                var directory = Environment.CurrentDirectory;
                var fullPath = Path.Combine(directory, Configuration.PackageNames);
                FileSystem.Setup(f => f.FileExists(Configuration.PackageNames)).Returns(true);
                FileSystem.Setup(f => f.GetFullPath(Configuration.PackageNames)).Returns(fullPath);
                FileSystem.Setup(f => f.GetDirectoryName(fullPath)).Returns(directory);
                FileSystem.Setup(f => f.GetFileName(fullPath)).Returns("test.1.5.0.nupkg");
                FileSystem.Setup(f => f.GetFilenameWithoutExtension(fullPath)).Returns("test.1.5.0");

                var ex = TryRun(Action);
                var expectedMessage = GetExpectedLocalValue(Environment.CurrentDirectory, "test", "1.5.0");
                ex.Message.Should().Be(expectedMessage);
            }

            [Fact]
            public void Should_throw_exception_with_expected_message_when_installing_pre_release_nupkg()
            {
                Configuration.PackageNames = "test.2.0-alpha.nupkg";
                var directory = Environment.CurrentDirectory;
                var fullPath = Path.Combine(directory, Configuration.PackageNames);
                FileSystem.Setup(f => f.FileExists(Configuration.PackageNames)).Returns(true);
                FileSystem.Setup(f => f.GetFullPath(Configuration.PackageNames)).Returns(fullPath);
                FileSystem.Setup(f => f.GetDirectoryName(fullPath)).Returns(directory);
                FileSystem.Setup(f => f.GetFileName(fullPath)).Returns(Configuration.PackageNames);
                FileSystem.Setup(f => f.GetFilenameWithoutExtension(fullPath)).Returns("test.2.0-alpha");

                var ex = TryRun(Action);
                var expectedMessage = GetExpectedLocalValue(Environment.CurrentDirectory, "test", "2.0.0-alpha", prerelease: true);
                ex.Message.Should().Be(expectedMessage);
            }

            [Fact]
            public void Should_throw_exception_with_expected_message_when_installing_nupkg_and_directory_path_is_null()
            {
                Configuration.PackageNames = "test.2.0.nupkg";
                FileSystem.Setup(f => f.FileExists(Configuration.PackageNames)).Returns(true);
                FileSystem.Setup(f => f.GetFullPath(Configuration.PackageNames)).Returns(Configuration.PackageNames);
                FileSystem.Setup(f => f.GetFileName(Configuration.PackageNames)).Returns(Configuration.PackageNames);
                FileSystem.Setup(f => f.GetFilenameWithoutExtension(Configuration.PackageNames)).Returns("test.2.0");

                var ex = TryRun(Action);
                var expectedMessage = GetExpectedLocalValue(string.Empty, "test", "2.0.0", prerelease: false);
                ex.Message.Should().Be(expectedMessage);
            }

            [Fact]
            public void Should_throw_exception_when_passed_in_path_to_nupkg_is_relative_and_it_does_not_exist()
            {
                Configuration.PackageNames = "package.nupkg";

                var ex = TryRun(Action);

                ex.Message.Should().Be("Package name cannot point directly to a local, or remote file. Please use the --source argument and point it to a local file directory, UNC directory path or a NuGet feed instead.");
            }

            [Fact]
            public void Should_throw_exception_when_nuspec_file_is_passed_as_package_name()
            {
                Configuration.PackageNames = "test-package.nuspec";

                var ex = TryRun(Action);
                ex.Message.Should().Be("Package name cannot point directly to a package manifest file. Please create a package by running 'choco pack' on the .nuspec file first.");
            }

            private string GetExpectedUncValue(string path, string name, string version = null, bool prerelease = false)
            {
                var sb = new StringBuilder("Package name cannot be a path to a file on a UNC location.")
                    .AppendLine()
                    .AppendLine()
                    .Append("To ")
                    .Append(Configuration.CommandName)
                    .AppendLine(" a file in a UNC location, you may use:")
                    .Append("  choco ").Append(Configuration.CommandName).Append(" ")
                    .Append(name);

                if (!string.IsNullOrEmpty(version))
                {
                    sb.Append(" --version=\"").Append(version).Append("\"");
                }

                if (prerelease)
                {
                    sb.Append(" --prerelease");
                }

                if (!string.IsNullOrEmpty(path))
                {
                    sb.Append(" --source=\"").Append(path).Append("\"");
                }

                return sb.AppendLine().ToString();
            }

            private string GetExpectedLocalValue(string path, string name, string version = null, bool prerelease = false)
            {
                var sb = new StringBuilder("Package name cannot be a path to a file on a remote, or local file system.")
                    .AppendLine()
                    .AppendLine()
                    .Append("To ")
                    .Append(Configuration.CommandName)
                    .AppendLine(" a local, or remote file, you may use:")
                    .Append("  choco ").Append(Configuration.CommandName).Append(" ")
                    .Append(name);

                if (!string.IsNullOrEmpty(version))
                {
                    sb.Append(" --version=\"").Append(version).Append("\"");
                }

                if (prerelease)
                {
                    sb.Append(" --prerelease");
                }

                if (!string.IsNullOrEmpty(path))
                {
                    sb.Append(" --source=\"").Append(path).Append("\"");
                }

                return sb.AppendLine().ToString();
            }

            private static Exception TryRun(Action action)
            {
                try
                {
                    action();
                    return null;
                }
                catch (Exception ex)
                {
                    ex.Should().BeOfType<ApplicationException>();
                    return ex;
                }
            }
        }

        public class When_ChocolateyPackageService_tries_to_install_noop_nupkg_file : When_ChocolateyPackageService_tries_to_install_nupkg_file
        {
            public override void Context()
            {
                base.Context();
                Action = () => Service.InstallDryRun(Configuration);
            }
        }

        public class When_ChocolateyPackageService_tries_to_upgrade_nupkg_file : When_ChocolateyPackageService_tries_to_install_nupkg_file
        {
            public override void Context()
            {
                base.Context();
                Action = () => Service.Upgrade(Configuration);
                Configuration.CommandName = "upgrade";
            }
        }

        public class When_ChocolateyPackageService_tries_to_upgrade_noop_nupkg_file : When_ChocolateyPackageService_tries_to_install_nupkg_file
        {
            public override void Context()
            {
                base.Context();
                Action = () => Service.UpgradeDryRun(Configuration);
                Configuration.CommandName = "upgrade";
            }
        }
    }
}
