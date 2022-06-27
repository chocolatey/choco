// Copyright © 2017 - 2022 Chocolatey Software, Inc
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
using Should;
using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

namespace chocolatey.tests.infrastructure.app.services
{
    using NuGet.Packaging;

    public class ChocolateyPackageServiceSpecs
    {
        public abstract class ChocolateyPackageServiceSpecsBase : TinySpec
        {
            protected ChocolateyPackageService Service;
            protected Mock<INugetService> NugetService = new Mock<INugetService>();
            protected Mock<IPowershellService> PowershellService = new Mock<IPowershellService>();
            protected List<ISourceRunner> SourceRunners = new List<ISourceRunner>();
            protected Mock<IShimGenerationService> ShimGenerationService = new Mock<IShimGenerationService>();
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Mock<IRegistryService> RegistryService = new Mock<IRegistryService>();
            protected Mock<IChocolateyPackageInformationService> ChocolateyPackageInformationService = new Mock<IChocolateyPackageInformationService>();
            protected Mock<IFilesService> FilesService = new Mock<IFilesService>();
            protected Mock<IAutomaticUninstallerService> AutomaticUninstallerService = new Mock<IAutomaticUninstallerService>();
            protected Mock<IXmlService> XmlService = new Mock<IXmlService>();
            protected Mock<IConfigTransformService> ConfigTransformService = new Mock<IConfigTransformService>();

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
                Service = new ChocolateyPackageService(NugetService.Object, PowershellService.Object, SourceRunners, ShimGenerationService.Object, FileSystem.Object, RegistryService.Object, ChocolateyPackageInformationService.Object, FilesService.Object, AutomaticUninstallerService.Object, XmlService.Object, ConfigTransformService.Object);
            }
        }

        public class when_ChocolateyPackageService_install_from_package_config_with_custom_sources : ChocolateyPackageServiceSpecsBase
        {
            protected Mock<ISourceRunner> FeaturesRunner = new Mock<ISourceRunner>();
            protected Mock<ISourceRunner> NormalRunner = new Mock<ISourceRunner>();
            private ConcurrentDictionary<string, PackageResult> result;

            public override void Context()
            {
                base.Context();

                Configuration.PackageNames = @"C:\test\packages.config";
                Configuration.Sources = @"C:\test";

                NormalRunner.Setup(r => r.SourceType).Returns(SourceTypes.NORMAL);
                FeaturesRunner.Setup(r => r.SourceType).Returns(SourceTypes.WINDOWS_FEATURES);

                var package = new Mock<IPackageMetadata>();
                var expectedResult = new ConcurrentDictionary<string, PackageResult>();
                expectedResult.TryAdd("test-feature", new PackageResult(package.Object, "windowsfeatures", null));

                FeaturesRunner.Setup(r => r.install_run(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()))
                    .Returns(expectedResult);
                NormalRunner.Setup(r => r.install_run(It.IsAny<ChocolateyConfiguration>(), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()))
                    .Returns(new ConcurrentDictionary<string, PackageResult>());
                SourceRunners.AddRange(new[] { NormalRunner.Object, FeaturesRunner.Object });

                FileSystem.Setup(f => f.get_full_path(Configuration.PackageNames)).Returns(Configuration.PackageNames);
                FileSystem.Setup(f => f.file_exists(Configuration.PackageNames)).Returns(true);

                XmlService.Setup(x => x.deserialize<PackagesConfigFileSettings>(Configuration.PackageNames))
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
                result = Service.install_run(Configuration);
            }

            [Test]
            public void should_return_package_that_should_have_been_installed()
            {
                result.Keys.ShouldContain("test-feature");
            }

            [Test]
            public void should_have_called_runner_for_windows_features_source()
            {
                FeaturesRunner.Verify(r => r.install_run(It.Is<ChocolateyConfiguration>(c => c.PackageNames == "test-feature"), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()), Times.Once);
            }

            [Test]
            public void should_not_have_called_runner_for_windows_features_source_with_other_package_names()
            {
                FeaturesRunner.Verify(r => r.install_run(It.Is<ChocolateyConfiguration>(c => c.PackageNames != "test-feature"), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()), Times.Never);
            }

            [Test]
            public void should_not_have_called_normal_source_runner_for_non_empty_packages()
            {
                // The normal source runners will be called with an argument
                NormalRunner.Verify(r => r.install_run(It.Is<ChocolateyConfiguration>(c => c.PackageNames != string.Empty), It.IsAny<Action<PackageResult, ChocolateyConfiguration>>()), Times.Never);
            }
        }
    }
}
