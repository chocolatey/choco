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

namespace chocolatey.tests.integration.infrastructure.app.services
{
    using System;
    using System.IO;
    using System.Linq;
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
            protected IFileSystem FileSystem = new DotNetFileSystem();
            protected IHashProvider HashProvider;

            public override void Context()
            {
                HashProvider = new CryptoHashProvider(FileSystem);
                Service = new FilesService(new XmlService(FileSystem, HashProvider), FileSystem, HashProvider);
            }
        }

        public class when_FilesService_encounters_locked_files : FilesServiceSpecsBase
        {
            private PackageFiles _result;
            private readonly ChocolateyConfiguration _config = new ChocolateyConfiguration();
            private PackageResult _packageResult;
            private string _contextPath;
            private string _theLockedFile;
            private FileStream _fileStream;

            public override void Context()
            {
                base.Context();
                _contextPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "infrastructure", "filesystem");
                _theLockedFile = Path.Combine(_contextPath, "Slipsum.txt");
                _packageResult = new PackageResult("bob", "1.2.3", FileSystem.get_directory_name(_theLockedFile));

                _fileStream = new FileStream(_theLockedFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                _fileStream.Close();
            }

            public override void Because()
            {
                _result = Service.capture_package_files(_packageResult, _config);
            }

            [Fact]
            public void should_not_error()
            {
                //nothing to see here
            }

            [Fact]
            public void should_log_a_warning()
            {
                MockLogger.Verify(l => l.Warn(It.IsAny<string>()), Times.AtLeastOnce);
            }

            [Fact]
            public void should_log_a_warning_about_locked_files()
            {
                bool lockedFiles = false;
                foreach (var message in MockLogger.MessagesFor(LogLevel.Warn).or_empty_list_if_null())
                {
                    if (message.Contains("The process cannot access the file")) lockedFiles = true;
                }

                lockedFiles.ShouldBeTrue();
            }

            [Fact]
            public void should_return_a_special_code_for_locked_files()
            {
                _result.Files.FirstOrDefault(x => x.Path == _theLockedFile).Checksum.ShouldEqual(ApplicationParameters.HashProviderFileLocked);
            }
        }
    }
}
