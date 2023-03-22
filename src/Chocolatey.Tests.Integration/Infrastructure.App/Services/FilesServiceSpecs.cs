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

namespace Chocolatey.Tests.Integration.Infrastructure.App.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using Chocolatey.Infrastructure.App;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Domain;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.Cryptography;
    using Chocolatey.Infrastructure.Filesystem;
    using Chocolatey.Infrastructure.Results;
    using Chocolatey.Infrastructure.Services;
    using Moq;
    using NUnit.Framework;
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

        [SetCulture("en"), SetUICulture("en")]
        public class When_FilesService_encounters_locked_files : FilesServiceSpecsBase
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
                _packageResult = new PackageResult("bob", "1.2.3", FileSystem.GetDirectoryName(_theLockedFile));

                _fileStream = new FileStream(_theLockedFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }

            public override void AfterObservations()
            {
                base.AfterObservations();
                _fileStream.Close();
                _fileStream.Dispose();
            }

            public override void Because()
            {
                _result = Service.CaptureSnapshot(_packageResult, _config);
            }

            [Fact]
            public void Should_not_error()
            {
                //nothing to see here
            }

            [Fact]
            public void Should_log_a_warning()
            {
                MockLogger.Verify(l => l.Warn(It.IsAny<string>()), Times.AtLeastOnce);
            }

            [Fact]
            [WindowsOnly]
            [Platform(Exclude = "Mono")]
            public void Should_log_a_warning_about_locked_files()
            {
                MockLogger.Verify(l => l.Warn(It.Is<string>(s => s.Contains("The process cannot access the file"))), Times.Once);
            }

            [Fact]
            public void Should_return_a_special_code_for_locked_files()
            {
                _result.Files.FirstOrDefault(x => x.Path == _theLockedFile).Checksum.ShouldEqual(ApplicationParameters.HashProviderFileLocked);
            }
        }
    }
}
