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

namespace chocolatey.tests.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.services;
    using Moq;
    using NuGet.Common;
    using NuGet.Packaging;
    using FluentAssertions;
    using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

    public class NugetServiceSpecs
    {
        public abstract class NugetServiceSpecsBase : TinySpec
        {
            protected NugetService Service;
            protected Mock<IChocolateyPackageInformationService> PackageInfoService = new Mock<IChocolateyPackageInformationService>();
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Mock<ILogger> NugetLogger = new Mock<ILogger>();
            protected Mock<IFilesService> FilesService = new Mock<IFilesService>();
            protected Mock<IPackageMetadata> Package = new Mock<IPackageMetadata>();
            protected Mock<IPackageDownloader> PackageDownloader = new Mock<IPackageDownloader>();
            protected Mock<IRuleService> RuleService = new Mock<IRuleService>();

            public override void Context()
            {
                FileSystem.ResetCalls();
                NugetLogger.ResetCalls();
                PackageInfoService.ResetCalls();
                FilesService.ResetCalls();
                Package.ResetCalls();

                Service = new NugetService(FileSystem.Object, NugetLogger.Object, PackageInfoService.Object, FilesService.Object, RuleService.Object);
            }
        }

        public class When_NugetService_backs_up_changed_files : NugetServiceSpecsBase
        {
            private Action _because;
            private ChocolateyPackageInformation _packageInfo;
            private const string FilePath = "c:\\tests";
            private PackageFiles _packageFiles;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                Package.Setup(x => x.Id).Returns("bob");
                _packageInfo = new ChocolateyPackageInformation(Package.Object);
                _packageInfo.FilesSnapshot = new PackageFiles();
                _packageFiles = new PackageFiles();
                FileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            }

            public override void Because()
            {
                _because = () => Service.BackupChangedFiles(FilePath, _config, _packageInfo);
            }

            [Fact]
            public void Should_ignore_an_unchanged_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = FilePath,
                    Checksum = "1234"
                };
                _packageFiles.Files.Add(packageFile);
                _packageInfo.FilesSnapshot = _packageFiles;

                var fileSystemFiles = new List<string>()
                {
                    FilePath
                };
                FileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                FilesService.Setup(x => x.CaptureSnapshot(It.IsAny<string>(), _config)).Returns(_packageFiles);

                _because();

                FileSystem.Verify(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
            }

            [Fact]
            public void Should_backup_a_changed_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = FilePath,
                    Checksum = "1234"
                };
                _packageFiles.Files.Add(packageFile);
                _packageInfo.FilesSnapshot = _packageFiles;

                var packageFileWithUpdatedChecksum = new PackageFile
                {
                    Path = FilePath,
                    Checksum = "4321"
                };

                var fileSystemFiles = new List<string>()
                {
                    FilePath
                };
                FileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                var updatedPackageFiles = new PackageFiles();
                updatedPackageFiles.Files = new List<PackageFile>
                {
                    packageFileWithUpdatedChecksum
                };
                FilesService.Setup(x => x.CaptureSnapshot(It.IsAny<string>(), _config)).Returns(updatedPackageFiles);

                _because();

                FileSystem.Verify(x => x.CopyFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
            }
        }

        public class When_NugetService_removes_installation_files_on_uninstall : NugetServiceSpecsBase
        {
            private Action _because;
            private ChocolateyPackageInformation _packageInfo;
            private const string FilePath = "c:\\tests";
            private IList<PackageFile> _packageFiles;

            public override void Context()
            {
                base.Context();
                Package.Setup(x => x.Id).Returns("bob");
                _packageInfo = new ChocolateyPackageInformation(Package.Object);
                _packageInfo.FilesSnapshot = new PackageFiles();
                _packageFiles = new List<PackageFile>();
                FileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            }

            public override void Because()
            {
                _because = () => Service.RemoveInstallationFiles(Package.Object, _packageInfo);
            }

            [Fact]
            public void Should_do_nothing_if_the_directory_no_longer_exists()
            {
                Context();
                FileSystem.ResetCalls();
                FileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);

                var packageFile = new PackageFile
                {
                    Path = FilePath,
                    Checksum = "1234"
                };
                _packageFiles.Add(packageFile);
                _packageInfo.FilesSnapshot.Files = _packageFiles.ToList();

                var fileSystemFiles = new List<string>()
                {
                    FilePath
                };
                FileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                FilesService.Setup(x => x.GetPackageFile(It.IsAny<string>())).Returns(packageFile);

                _because();

                FilesService.Verify(x => x.GetPackageFile(It.IsAny<string>()), Times.Never);
                FileSystem.Verify(x => x.DeleteFile(FilePath), Times.Never);
            }

            [Fact]
            public void Should_remove_an_unchanged_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = FilePath,
                    Checksum = "1234"
                };
                _packageFiles.Add(packageFile);
                _packageInfo.FilesSnapshot.Files = _packageFiles.ToList();

                var fileSystemFiles = new List<string>()
                {
                    FilePath
                };
                FileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                FilesService.Setup(x => x.GetPackageFile(It.IsAny<string>())).Returns(packageFile);
                FileSystem.Setup(x => x.FileExists(FilePath)).Returns(true);

                _because();

                FileSystem.Verify(x => x.DeleteFile(FilePath));
            }

            [Fact]
            public void Should_not_delete_a_changed_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = FilePath,
                    Checksum = "1234"
                };
                var packageFileWithUpdatedChecksum = new PackageFile
                {
                    Path = FilePath,
                    Checksum = "4321"
                };
                _packageFiles.Add(packageFile);
                _packageInfo.FilesSnapshot.Files = _packageFiles.ToList();

                var fileSystemFiles = new List<string>()
                {
                    FilePath
                };
                FileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                FilesService.Setup(x => x.GetPackageFile(It.IsAny<string>())).Returns(packageFileWithUpdatedChecksum);
                FileSystem.Setup(x => x.FileExists(FilePath)).Returns(true);

                _because();

                FileSystem.Verify(x => x.DeleteFile(FilePath), Times.Never);
            }

            [Fact]
            public void Should_not_delete_an_unfound_file()
            {
                Context();

                var packageFile = new PackageFile
                {
                    Path = FilePath,
                    Checksum = "1234"
                };
                var packageFileNotInOriginal = new PackageFile
                {
                    Path = "c:\\files",
                    Checksum = "4321"
                };
                _packageFiles.Add(packageFile);

                _packageInfo.FilesSnapshot.Files = _packageFiles.ToList();

                var fileSystemFiles = new List<string>()
                {
                    FilePath
                };

                FileSystem.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories)).Returns(fileSystemFiles);
                FilesService.Setup(x => x.GetPackageFile(It.IsAny<string>())).Returns(packageFileNotInOriginal);
                FileSystem.Setup(x => x.FileExists(FilePath)).Returns(false);

                _because();

                FileSystem.Verify(x => x.DeleteFile(FilePath), Times.Never);
            }
        }

        public class When_NugetService_pack_noop : NugetServiceSpecsBase
        {
            private Action _because;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                FileSystem.Setup(x => x.GetCurrentDirectory()).Returns("c:\\projects\\chocolatey");
            }

            public override void Because()
            {
                _because = () => Service.PackDryRun(_config);
            }

            public override void AfterEachSpec()
            {
                MockLogger.Reset();
            }

            [Fact]
            public void Generated_package_should_be_in_current_directory()
            {
                Context();

                _because();

                var infos = MockLogger.MessagesFor(tests.LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,"Chocolatey would have searched for a nuspec file in \"c:\\projects\\chocolatey\" and attempted to compile it.");
            }

            [Fact]
            public void Generated_package_should_be_in_specified_directory()
            {
                Context();

                _config.OutputDirectory = "c:\\packages";

                _because();

                var infos = MockLogger.MessagesFor(tests.LogLevel.Info);
                infos.Should().ContainSingle();
                infos.Should().HaveElementAt(0,"Chocolatey would have searched for a nuspec file in \"c:\\packages\" and attempted to compile it.");
            }
        }
    }
}
