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
using System;
using System.IO;
using System.Linq;
using System.Text;
using chocolatey.infrastructure.app;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.app.domain;
using chocolatey.infrastructure.app.services;
using chocolatey.infrastructure.services;
using Moq;
using NuGet.Packaging;
using NuGet.Versioning;
using FluentAssertions;
using IFileSystem = chocolatey.infrastructure.filesystem.IFileSystem;

namespace chocolatey.tests.infrastructure.app.services
{
    public class ChocolateyPackageInformationServiceSpecs
    {
        // A service that can be told where it keeps the information of a package, as an extension that keeps some of it
        // elsewhere does, or left to use the default location.
        public class TestablePackageInformationService : ChocolateyPackageInformationService
        {
            private readonly string _storePath;

            public TestablePackageInformationService(IFileSystem fileSystem, IRegistryService registryService, IFilesService filesService, ChocolateyConfiguration config, string storePath)
                : base(fileSystem, registryService, filesService, config)
            {
                _storePath = storePath;
            }

            protected override string GetStorePath(ChocolateyPackageInformation packageInformation)
            {
                return _storePath ?? base.GetStorePath(packageInformation);
            }

            public string DefaultStorePath(ChocolateyPackageInformation packageInformation)
            {
                return base.GetStorePath(packageInformation);
            }
        }

        public abstract class ChocolateyPackageInformationServiceSpecsBase : TinySpec
        {
            protected const string CustomStore = @"c:\custom\store";
            protected Mock<IFileSystem> FileSystem;
            protected Mock<IRegistryService> RegistryService;
            protected Mock<IFilesService> FilesService;
            protected Mock<IPackageMetadata> Package;
            protected ChocolateyConfiguration Config;

            protected static string DefaultRoot
            {
                get { return ApplicationParameters.ChocolateyPackageInfoStoreLocation; }
            }

            protected static string DefaultStore(string folder)
            {
                return Path.Combine(DefaultRoot, folder);
            }

            protected virtual string Version
            {
                get { return "1.2.3"; }
            }

            public override void Context()
            {
                FileSystem = new Mock<IFileSystem>();
                FileSystem.Setup(x => x.CombinePaths(It.IsAny<string>(), It.IsAny<string[]>()))
                    .Returns<string, string[]>((left, right) => Path.Combine(new[] { left }.Concat(right).ToArray()));
                RegistryService = new Mock<IRegistryService>();
                FilesService = new Mock<IFilesService>();
                Config = new ChocolateyConfiguration { RegularOutput = false };

                Package = new Mock<IPackageMetadata>();
                Package.Setup(x => x.Id).Returns("instancepackage");
                Package.Setup(x => x.Version).Returns(NuGetVersion.Parse(Version));
            }

            protected TestablePackageInformationService Service(string storePath = null)
            {
                return new TestablePackageInformationService(FileSystem.Object, RegistryService.Object, FilesService.Object, Config, storePath);
            }

            protected void DirectoryExists(string path)
            {
                FileSystem.Setup(x => x.DirectoryExists(path)).Returns(true);
            }

            protected void FileExists(string path)
            {
                FileSystem.Setup(x => x.FileExists(path)).Returns(true);
            }

            protected ChocolateyPackageInformation Information()
            {
                return new ChocolateyPackageInformation(Package.Object);
            }

            protected bool IsUnderDefaultRoot(string path)
            {
                return path != null && path.StartsWith(DefaultRoot, StringComparison.OrdinalIgnoreCase);
            }
        }

        public class When_locating_the_store_of_a_package_that_has_no_store_yet : ChocolateyPackageInformationServiceSpecsBase
        {
            private string _path;

            public override void Because()
            {
                _path = Service().DefaultStorePath(Information());
            }

            [Fact]
            public void Should_use_the_id_and_normalized_version_under_the_chocolatey_store()
            {
                _path.Should().Be(DefaultStore("instancepackage.1.2.3"));
            }
        }

        public class When_locating_the_store_of_a_package_that_has_both_the_current_and_a_legacy_store : ChocolateyPackageInformationServiceSpecsBase
        {
            private string _path;

            protected override string Version
            {
                get { return "4.2.0.0"; }
            }

            public override void Context()
            {
                base.Context();

                DirectoryExists(DefaultStore("instancepackage.4.2.0"));
                DirectoryExists(DefaultStore("instancepackage.4.2"));
            }

            public override void Because()
            {
                _path = Service().DefaultStorePath(Information());
            }

            [Fact]
            public void Should_prefer_the_current_store()
            {
                _path.Should().Be(DefaultStore("instancepackage.4.2.0"));
            }
        }

        public class When_locating_the_store_of_a_package_installed_before_2_0_with_four_version_numbers : ChocolateyPackageInformationServiceSpecsBase
        {
            private string _path;

            protected override string Version
            {
                get { return "4.0.0.0"; }
            }

            public override void Context()
            {
                base.Context();

                DirectoryExists(DefaultStore("instancepackage.4.0.0.0"));
            }

            public override void Because()
            {
                _path = Service().DefaultStorePath(Information());
            }

            [Fact]
            public void Should_use_the_legacy_store_named_with_all_four_numbers()
            {
                _path.Should().Be(DefaultStore("instancepackage.4.0.0.0"));
            }
        }

        public class When_locating_the_store_of_a_package_installed_with_two_version_numbers : ChocolateyPackageInformationServiceSpecsBase
        {
            private string _path;

            protected override string Version
            {
                get { return "4.2.0.0"; }
            }

            public override void Context()
            {
                base.Context();

                DirectoryExists(DefaultStore("instancepackage.4.2"));
            }

            public override void Because()
            {
                _path = Service().DefaultStorePath(Information());
            }

            [Fact]
            public void Should_use_the_legacy_store_named_with_two_numbers()
            {
                _path.Should().Be(DefaultStore("instancepackage.4.2"));
            }
        }

        // Reading, saving and removing all go through the location the service reports, so a service that keeps the
        // information somewhere else has every one of them honour that.
        public class When_getting_the_information_of_a_package_from_a_store_the_service_relocates : ChocolateyPackageInformationServiceSpecsBase
        {
            private ChocolateyPackageInformation _information;

            public override void Context()
            {
                base.Context();

                DirectoryExists(CustomStore);
                FileExists(Path.Combine(CustomStore, ".pin"));

                // The default store exists as well, so reading from it by mistake would look at its files.
                DirectoryExists(DefaultStore("instancepackage.1.2.3"));
            }

            public override void Because()
            {
                _information = Service(CustomStore).Get(Package.Object);
            }

            [Fact]
            public void Should_read_it_from_the_relocated_store()
            {
                _information.IsPinned.Should().BeTrue();
            }

            [Fact]
            public void Should_not_look_in_the_default_store()
            {
                FileSystem.Verify(x => x.FileExists(It.Is<string>(p => IsUnderDefaultRoot(p))), Times.Never);
            }
        }

        public class When_getting_the_information_of_a_package_whose_relocated_store_does_not_exist : ChocolateyPackageInformationServiceSpecsBase
        {
            private ChocolateyPackageInformation _information;

            public override void Context()
            {
                base.Context();

                // Only the default store has one.
                DirectoryExists(DefaultStore("instancepackage.1.2.3"));
                FileExists(Path.Combine(DefaultStore("instancepackage.1.2.3"), ".pin"));
            }

            public override void Because()
            {
                _information = Service(CustomStore).Get(Package.Object);
            }

            [Fact]
            public void Should_return_the_information_without_reading_anything()
            {
                _information.IsPinned.Should().BeFalse();
            }
        }

        public class When_getting_the_information_of_a_package_from_the_default_store : ChocolateyPackageInformationServiceSpecsBase
        {
            private ChocolateyPackageInformation _information;

            public override void Context()
            {
                base.Context();

                DirectoryExists(DefaultStore("instancepackage.1.2.3"));
                FileExists(Path.Combine(DefaultStore("instancepackage.1.2.3"), ".pin"));
            }

            public override void Because()
            {
                _information = Service().Get(Package.Object);
            }

            [Fact]
            public void Should_read_it_from_the_default_store()
            {
                _information.IsPinned.Should().BeTrue();
            }
        }

        public class When_saving_the_information_of_a_package_to_a_store_the_service_relocates : ChocolateyPackageInformationServiceSpecsBase
        {
            public override void Because()
            {
                var information = Information();
                information.Arguments = "--params";
                information.IsPinned = true;

                Service(CustomStore).Save(information);
            }

            [Fact]
            public void Should_create_the_relocated_store()
            {
                FileSystem.Verify(x => x.EnsureDirectoryExists(CustomStore), Times.Once);
            }

            [Fact]
            public void Should_write_the_arguments_to_the_relocated_store()
            {
                FileSystem.Verify(x => x.WriteFile(Path.Combine(CustomStore, ".arguments"), "--params"), Times.Once);
            }

            [Fact]
            public void Should_write_the_pin_to_the_relocated_store()
            {
                FileSystem.Verify(x => x.WriteFile(Path.Combine(CustomStore, ".pin"), string.Empty, Encoding.ASCII), Times.Once);
            }

            [Fact]
            public void Should_not_write_to_the_default_store()
            {
                FileSystem.Verify(x => x.WriteFile(It.Is<string>(p => IsUnderDefaultRoot(p)), It.IsAny<string>()), Times.Never);
                FileSystem.Verify(x => x.WriteFile(It.Is<string>(p => IsUnderDefaultRoot(p)), It.IsAny<string>(), It.IsAny<Encoding>()), Times.Never);
            }
        }

        public class When_saving_the_information_of_a_package_to_the_default_store : ChocolateyPackageInformationServiceSpecsBase
        {
            public override void Because()
            {
                var information = Information();
                information.Arguments = "--params";
                information.IsPinned = true;

                Service().Save(information);
            }

            [Fact]
            public void Should_write_the_arguments_to_the_default_store()
            {
                FileSystem.Verify(x => x.WriteFile(Path.Combine(DefaultStore("instancepackage.1.2.3"), ".arguments"), "--params"), Times.Once);
            }

            [Fact]
            public void Should_write_the_pin_to_the_default_store()
            {
                FileSystem.Verify(x => x.WriteFile(Path.Combine(DefaultStore("instancepackage.1.2.3"), ".pin"), string.Empty, Encoding.ASCII), Times.Once);
            }
        }

        public class When_removing_the_information_of_a_package_from_a_store_the_service_relocates : ChocolateyPackageInformationServiceSpecsBase
        {
            public override void Because()
            {
                Service(CustomStore).Remove(Package.Object);
            }

            [Fact]
            public void Should_remove_the_relocated_store()
            {
                FileSystem.Verify(x => x.DeleteDirectoryChecked(CustomStore, true), Times.Once);
            }

            [Fact]
            public void Should_not_remove_anything_from_the_default_store()
            {
                FileSystem.Verify(x => x.DeleteDirectoryChecked(It.Is<string>(p => IsUnderDefaultRoot(p)), It.IsAny<bool>()), Times.Never);
            }
        }

        public class When_removing_the_information_of_a_package_from_the_default_store : ChocolateyPackageInformationServiceSpecsBase
        {
            public override void Because()
            {
                Service().Remove(Package.Object);
            }

            [Fact]
            public void Should_remove_the_default_store()
            {
                FileSystem.Verify(x => x.DeleteDirectoryChecked(DefaultStore("instancepackage.1.2.3"), true), Times.Once);
            }
        }
    }
}
