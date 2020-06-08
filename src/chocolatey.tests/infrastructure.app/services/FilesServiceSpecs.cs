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
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.cryptography;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.results;
    using chocolatey.infrastructure.services;
    using Moq;
    using Should;

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

        public class when_FilesService_reads_from_files : FilesServiceSpecsBase
        {
            private Func<PackageFiles> because;

            public override void Because()
            {
                because = () => Service.read_from_file("fake path");
            }

            [Fact]
            public void should_deserialize_when_file_exists()
            {
                Context();
                FileSystem.Setup(x => x.file_exists(It.IsAny<string>())).Returns(true);
                XmlService.Setup(x => x.deserialize<PackageFiles>(It.IsAny<string>())).Returns(new PackageFiles());

                because();
            }

            [Fact]
            public void should_not_deserialize_if_file_does_not_exist()
            {
                Context();
                FileSystem.Setup(x => x.file_exists(It.IsAny<string>())).Returns(false);

                because();

                XmlService.Verify(x => x.deserialize<PackageFiles>(It.IsAny<string>()), Times.Never);
            }
        }

        public class when_FilesService_saves_files : FilesServiceSpecsBase
        {
            private Action because;
            private PackageFiles files;

            public override void Because()
            {
                because = () => Service.save_to_file(files, "fake path");
            }

            [Fact]
            public void should_save_if_the_snapshot_is_not_null()
            {
                Context();
                files = new PackageFiles();

                because();

                XmlService.Verify(x => x.serialize(files, It.IsAny<string>()), Times.Once());
            }

            [Fact]
            public void should_not_do_anything_if_the_snapshot_is_null()
            {
                Context();
                files = null;

                because();

                XmlService.Verify(x => x.serialize(files, It.IsAny<string>()), Times.Never);
            }
        }

        public class when_FilesService_captures_files_and_install_directory_reports_choco_install_location : FilesServiceSpecsBase
        {
            private PackageFiles result;
            private PackageResult packageResult;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                packageResult = new PackageResult("bob", "1.2.3", ApplicationParameters.InstallLocation);
            }

            public override void Because()
            {
                result = Service.capture_package_files(packageResult, config);
            }

            [Fact]
            public void should_not_call_get_files()
            {
                FileSystem.Verify(x => x.get_files(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories), Times.Never);
            }

            [Fact]
            public void should_return_a_warning_if_the_install_directory_matches_choco_install_location()
            {
                packageResult.Warning.ShouldBeTrue();
            }

            [Fact]
            public void should_return_null()
            {
                result.ShouldBeNull();
            }
        }

        public class when_FilesService_captures_files_and_install_directory_reports_packages_location : FilesServiceSpecsBase
        {
            private PackageFiles result;
            private PackageResult packageResult;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                packageResult = new PackageResult("bob", "1.2.3", ApplicationParameters.PackagesLocation);
            }

            public override void Because()
            {
                result = Service.capture_package_files(packageResult, config);
            }

            [Fact]
            public void should_not_call_get_files()
            {
                FileSystem.Verify(x => x.get_files(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories), Times.Never);
            }

            [Fact]
            public void should_return_a_warning_if_the_install_directory_matches_choco_install_location()
            {
                packageResult.Warning.ShouldBeTrue();
            }

            [Fact]
            public void should_return_null()
            {
                result.ShouldBeNull();
            }
        }

        public class when_FilesService_captures_files_and_package_result_is_null : FilesServiceSpecsBase
        {
            private PackageFiles result;
            private PackageResult packageResult;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();

            public override void Context()
            {
                base.Context();
                packageResult = null;
            }

            public override void Because()
            {
                result = Service.capture_package_files(packageResult, config);
            }

            [Fact]
            public void should_not_call_get_files()
            {
                FileSystem.Verify(x => x.get_files(It.IsAny<string>(), It.IsAny<string>(), SearchOption.AllDirectories), Times.Never);
            }

            [Fact]
            public void should_return_a_non_null_object()
            {
                result.ShouldNotBeNull();
            }

            [Fact]
            public void should_return_empty_package_files()
            {
                result.Files.ShouldBeEmpty();
            }
        }

        public class when_FilesService_captures_files_happy_path : FilesServiceSpecsBase
        {
            private PackageFiles result;
            private PackageResult packageResult;
            private readonly ChocolateyConfiguration config = new ChocolateyConfiguration();
            private readonly string installDirectory = ApplicationParameters.PackagesLocation + "\\bob";
            private readonly IList<string> files = new List<string>
            {
                "file1",
                "file2"
            };

            public override void Context()
            {
                base.Context();
                packageResult = new PackageResult("bob", "1.2.3", installDirectory);

                FileSystem.Setup(x => x.get_files(ApplicationParameters.PackagesLocation + "\\bob", It.IsAny<string>(), SearchOption.AllDirectories)).Returns(files);
                HashProvider.Setup(x => x.hash_file(It.IsAny<string>())).Returns("yes");
            }

            public override void Because()
            {
                result = Service.capture_package_files(packageResult, config);
            }

            [Fact]
            public void should_return_a_PackageFiles_object()
            {
                result.ShouldNotBeNull();
            }

            [Fact]
            public void should_contain_package_files()
            {
                result.Files.ShouldNotBeEmpty();
            }

            [Fact]
            public void should_contain_the_correct_number_of_package_files()
            {
                result.Files.Count.ShouldEqual(files.Count);
            }

            [Fact]
            public void should_call_hash_provider_for_each_file()
            {
                HashProvider.Verify(x => x.hash_file(It.IsAny<string>()), Times.Exactly(files.Count));
            }
        }
    }
}
