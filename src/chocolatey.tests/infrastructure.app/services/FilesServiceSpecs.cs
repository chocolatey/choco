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
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.cryptography;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.results;
    using chocolatey.infrastructure.services;
    using Moq;
    using FluentAssertions;

    public class FilesServiceSpecs
    {
        public abstract class FilesServiceSpecsBase : TinySpec
        {
            protected FilesService Service;
            protected Mock<IXmlService> XmlService = new Mock<IXmlService>();
            protected Mock<IFileSystem> FileSystem = new Mock<IFileSystem>();
            protected Mock<IHashProvider> HashProvider = new Mock<IHashProvider>();

            public override void Context()
            {
                XmlService.ResetCalls();
                FileSystem.ResetCalls();
                HashProvider.ResetCalls();
                Service = new FilesService(XmlService.Object, FileSystem.Object, HashProvider.Object);
            }
        }

        public class When_FilesService_reads_from_files : FilesServiceSpecsBase
        {
            private Func<PackageFiles> _because;

            public override void Because()
            {
                _because = () => Service.ReadPackageSnapshot("fake path");
            }

            [Fact]
            public void Should_deserialize_when_file_exists()
            {
                Context();
                FileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
                XmlService.Setup(x => x.Deserialize<PackageFiles>(It.IsAny<string>())).Returns(new PackageFiles());

                _because();
            }

            [Fact]
            public void Should_not_deserialize_if_file_does_not_exist()
            {
                Context();
                FileSystem.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

                _because();

                XmlService.Verify(x => x.Deserialize<PackageFiles>(It.IsAny<string>()), Times.Never);
            }
        }

        public class When_FilesService_saves_files : FilesServiceSpecsBase
        {
            private Action _because;
            private PackageFiles _files;

            public override void Because()
            {
                _because = () => Service.SavePackageSnapshot(_files, "fake path");
            }

            [Fact]
            public void Should_save_if_the_snapshot_is_not_null()
            {
                Context();
                _files = new PackageFiles();

                _because();

                XmlService.Verify(x => x.Serialize(_files, It.IsAny<string>()), Times.Once());
            }

            [Fact]
            public void Should_not_do_anything_if_the_snapshot_is_null()
            {
                Context();
                _files = null;

                _because();

                XmlService.Verify(x => x.Serialize(_files, It.IsAny<string>()), Times.Never);
            }
        }

        public class When_FilesService_captures_files_and_install_directory_reports_choco_install_location : FilesServiceSpecsBase
        {
            private PackageFiles _result;
            private PackageResult _packageResult;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                _packageResult = new PackageResult("bob", "1.2.3", ApplicationParameters.InstallLocation);
            }

            public override void Because()
            {
                _result = Service.CaptureSnapshot(_packageResult, _config);
            }

            [Fact]
            public void Should_not_call_get_files()
            {
                FileSystem.Verify(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories), Times.Never);
            }

            [Fact]
            public void Should_return_a_warning_if_the_install_directory_matches_choco_install_location()
            {
                _packageResult.Warning.Should().BeTrue();
            }

            [Fact]
            public void Should_return_null()
            {
                _result.Should().BeNull();
            }
        }

        public class When_FilesService_captures_files_and_install_directory_reports_packages_location : FilesServiceSpecsBase
        {
            private PackageFiles _result;
            private PackageResult _packageResult;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                _packageResult = new PackageResult("bob", "1.2.3", ApplicationParameters.PackagesLocation);
            }

            public override void Because()
            {
                _result = Service.CaptureSnapshot(_packageResult, _config);
            }

            [Fact]
            public void Should_not_call_get_files()
            {
                FileSystem.Verify(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories), Times.Never);
            }

            [Fact]
            public void Should_return_a_warning_if_the_install_directory_matches_choco_install_location()
            {
                _packageResult.Warning.Should().BeTrue();
            }

            [Fact]
            public void Should_return_null()
            {
                _result.Should().BeNull();
            }
        }

        public class When_FilesService_captures_files_and_package_result_is_null : FilesServiceSpecsBase
        {
            private PackageFiles _result;
            private PackageResult _packageResult;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                _packageResult = null;
            }

            public override void Because()
            {
                _result = Service.CaptureSnapshot(_packageResult, _config);
            }

            [Fact]
            public void Should_not_call_get_files()
            {
                FileSystem.Verify(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories), Times.Never);
            }

            [Fact]
            public void Should_return_a_non_null_object()
            {
                _result.Should().NotBeNull();
            }

            [Fact]
            public void Should_return_empty_package_files()
            {
                _result.Files.Should().BeEmpty();
            }
        }

        public class When_FilesService_captures_files_happy_path : FilesServiceSpecsBase
        {
            private PackageFiles _result;
            private PackageResult _packageResult;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private readonly string _installDirectory = ApplicationParameters.PackagesLocation + "\\bob";
            private readonly IList<string> _files = new List<string>
            {
                "file1",
                "file2"
            };

            public override void Context()
            {
                base.Context();
                _packageResult = new PackageResult("bob", "1.2.3", _installDirectory);

                FileSystem.Setup(x => x.GetFiles(ApplicationParameters.PackagesLocation + "\\bob", It.IsAny<string>(), SearchOption.AllDirectories)).Returns(_files);
                HashProvider.Setup(x => x.ComputeFileHash(It.IsAny<string>())).Returns("yes");
            }

            public override void Because()
            {
                _result = Service.CaptureSnapshot(_packageResult, _config);
            }

            [Fact]
            public void Should_return_a_PackageFiles_object()
            {
                _result.Should().NotBeNull();
            }

            [Fact]
            public void Should_contain_package_files()
            {
                _result.Files.Should().NotBeEmpty();
            }

            [Fact]
            public void Should_contain_the_correct_number_of_package_files()
            {
                _result.Files.Should().HaveCount(_files.Count);
            }

            [Fact]
            public void Should_call_hash_provider_for_each_file()
            {
                HashProvider.Verify(x => x.ComputeFileHash(It.IsAny<string>()), Times.Exactly(_files.Count));
            }
        }
    }
}
