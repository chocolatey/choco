// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey.tests.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using Moq;
    using NuGet;
    using Should;
    using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

    public class NugetServiceSpecs
    {
        public abstract class NugetServiceSpecsBase : TinySpec
        {
            protected NugetService service;
            protected Mock<IChocolateyPackageInformationService> packageInfoService = new Mock<IChocolateyPackageInformationService>();
            protected Mock<IFileSystem> fileSystem = new Mock<IFileSystem>();
            protected Mock<ILogger> nugetLogger = new Mock<ILogger>();
            protected Mock<IFilesService> filesService = new Mock<IFilesService>();
            protected Mock<IPackage> package = new Mock<IPackage>();
            protected Mock<IPackageDownloader> packageDownloader = new Mock<IPackageDownloader>();

            public override void Context()
            {
                fileSystem.ResetCalls();
                nugetLogger.ResetCalls();
                packageInfoService.ResetCalls();
                filesService.ResetCalls();
                package.ResetCalls();

                service = new NugetService(fileSystem.Object, nugetLogger.Object, packageInfoService.Object, filesService.Object, packageDownloader.Object);
            }
        }

        public class when_NugetService_backs_up_changed_files : NugetServiceSpecsBase
        {
            private Action because;
            private ChocolateyPackageInformation packageInfo;
            private const string filePath = "c:\\tests";
            private PackageFiles packageFiles;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                package.Setup(x => x.Id).Returns("bob");
                packageInfo = new ChocolateyPackageInformation(package.Object);
                packageInfo.FilesSnapshot = new PackageFiles();
                packageFiles = new PackageFiles();
                fileSystem.Setup(x => x.directory_exists(It.IsAny<string>())).Returns(true);
            }

            public override void Because()
            {
                because = () => service.backup_changed_files(filePath, config, packageInfo);
            }

            [Fact]
            public void should_ignore_an_unchanged_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = filePath,
                    Checksum = "1234"
                };
                packageFiles.Files.Add(packageFile);
                packageInfo.FilesSnapshot = packageFiles;

                var fileSystemFiles = new List<string>()
                {
                    filePath
                };
                fileSystem.Setup(x => x.get_files(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                filesService.Setup(x => x.capture_package_files(It.IsAny<string>(), config)).Returns(packageFiles);

                because();

                fileSystem.Verify(x => x.copy_file(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            }

            [Fact]
            public void should_backup_a_changed_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = filePath,
                    Checksum = "1234"
                };
                packageFiles.Files.Add(packageFile);
                packageInfo.FilesSnapshot = packageFiles;

                var packageFileWithUpdatedChecksum = new PackageFile
                {
                    Path = filePath,
                    Checksum = "4321"
                };

                var fileSystemFiles = new List<string>()
                {
                    filePath
                };
                fileSystem.Setup(x => x.get_files(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                var updatedPackageFiles = new PackageFiles();
                updatedPackageFiles.Files = new List<PackageFile>
                {
                    packageFileWithUpdatedChecksum
                };
                filesService.Setup(x => x.capture_package_files(It.IsAny<string>(), config)).Returns(updatedPackageFiles);

                because();

                fileSystem.Verify(x => x.copy_file(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            }
        }

        public class when_NugetService_removes_installation_files_on_uninstall : NugetServiceSpecsBase
        {
            private Action because;
            private ChocolateyPackageInformation packageInfo;
            private const string filePath = "c:\\tests";
            private IList<PackageFile> packageFiles;

            public override void Context()
            {
                base.Context();
                package.Setup(x => x.Id).Returns("bob");
                packageInfo = new ChocolateyPackageInformation(package.Object);
                packageInfo.FilesSnapshot = new PackageFiles();
                packageFiles = new List<PackageFile>();
                fileSystem.Setup(x => x.directory_exists(It.IsAny<string>())).Returns(true);
            }

            public override void Because()
            {
                because = () => service.remove_installation_files(package.Object, packageInfo);
            }

            [Fact]
            public void should_do_nothing_if_the_directory_no_longer_exists()
            {
                Context();
                fileSystem.ResetCalls();
                fileSystem.Setup(x => x.directory_exists(It.IsAny<string>())).Returns(false);

                var packageFile = new PackageFile
                {
                    Path = filePath,
                    Checksum = "1234"
                };
                packageFiles.Add(packageFile);
                packageInfo.FilesSnapshot.Files = packageFiles.ToList();

                var fileSystemFiles = new List<string>()
                {
                    filePath
                };
                fileSystem.Setup(x => x.get_files(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                filesService.Setup(x => x.get_package_file(It.IsAny<string>())).Returns(packageFile);

                because();

                filesService.Verify(x => x.get_package_file(It.IsAny<string>()), Times.Never);
                fileSystem.Verify(x => x.delete_file(filePath), Times.Never);
            }

            [Fact]
            public void should_remove_an_unchanged_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = filePath,
                    Checksum = "1234"
                };
                packageFiles.Add(packageFile);
                packageInfo.FilesSnapshot.Files = packageFiles.ToList();

                var fileSystemFiles = new List<string>()
                {
                    filePath
                };
                fileSystem.Setup(x => x.get_files(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                filesService.Setup(x => x.get_package_file(It.IsAny<string>())).Returns(packageFile);
                fileSystem.Setup(x => x.file_exists(filePath)).Returns(true);

                because();

                fileSystem.Verify(x => x.delete_file(filePath));
            }

            [Fact]
            public void should_not_delete_a_changed_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = filePath,
                    Checksum = "1234"
                };
                var packageFileWithUpdatedChecksum = new PackageFile
                {
                    Path = filePath,
                    Checksum = "4321"
                };
                packageFiles.Add(packageFile);
                packageInfo.FilesSnapshot.Files = packageFiles.ToList();

                var fileSystemFiles = new List<string>()
                {
                    filePath
                };
                fileSystem.Setup(x => x.get_files(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                filesService.Setup(x => x.get_package_file(It.IsAny<string>())).Returns(packageFileWithUpdatedChecksum);
                fileSystem.Setup(x => x.file_exists(filePath)).Returns(true);

                because();

                fileSystem.Verify(x => x.delete_file(filePath), Times.Never);
            }

            [Fact]
            public void should_not_delete_an_unfound_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = filePath,
                    Checksum = "1234"
                };
                var packageFileNotInOriginal = new PackageFile
                {
                    Path = "c:\\files",
                    Checksum = "4321"
                };
                packageFiles.Add(packageFile);

                packageInfo.FilesSnapshot.Files = packageFiles.ToList();

                var fileSystemFiles = new List<string>()
                {
                    filePath
                };

                fileSystem.Setup(x => x.get_files(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                filesService.Setup(x => x.get_package_file(It.IsAny<string>())).Returns(packageFileNotInOriginal);
                fileSystem.Setup(x => x.file_exists(filePath)).Returns(false);

                because();

                fileSystem.Verify(x => x.delete_file(filePath), Times.Never);
            }
        }

        public class when_NugetService_pack_noop : NugetServiceSpecsBase
        {
            private Action because;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                fileSystem.Setup(x => x.get_current_directory()).Returns("c:\\projects\\chocolatey");
            }

            public override void Because()
            {
                because = () => service.pack_noop(config);
            }

            public override void AfterEachSpec()
            {
                MockLogger.reset();
            }

            [Fact]
            public void generated_package_should_be_in_current_directory()
            {
                Context();

                because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Count.ShouldEqual(1);
                infos[0].ShouldEqual("Chocolatey would have searched for a nuspec file in \"c:\\projects\\chocolatey\" and attempted to compile it.");
            }

            [Fact]
            public void generated_package_should_be_in_specified_directory()
            {
                Context();

                config.OutputDirectory = "c:\\packages";

                because();

                var infos = MockLogger.MessagesFor(LogLevel.Info);
                infos.Count.ShouldEqual(1);
                infos[0].ShouldEqual("Chocolatey would have searched for a nuspec file in \"c:\\packages\" and attempted to compile it.");
            }
        }
    }
}
