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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.IO;
    using System.Linq;
    using configuration;
    using cryptography;
    using domain;
    using filesystem;
    using infrastructure.services;
    using logging;
    using results;

    public sealed class FilesService : IFilesService
    {
        private readonly IXmlService _xmlService;
        private readonly IFileSystem _fileSystem;
        private readonly IHashProvider _hashProvider;

        public FilesService(IXmlService xmlService, IFileSystem fileSystem, IHashProvider hashProvider)
        {
            _xmlService = xmlService;
            _fileSystem = fileSystem;
            _hashProvider = hashProvider;
        }

        public PackageFiles ReadPackageSnapshot(string filePath)
        {
            if (!_fileSystem.FileExists(filePath)) return null;

            return _xmlService.Deserialize<PackageFiles>(filePath);
        }

        private string GetPackageInstallDirectory(PackageResult packageResult)
        {
            if (packageResult == null) return null;

            var installDirectory = packageResult.InstallLocation;
            return PackageInstallDirectoryIsCorrect(installDirectory, logMessage => packageResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage))) ? installDirectory : null;
        }

        private bool PackageInstallDirectoryIsCorrect(string directory, Action<string> errorAction = null)
        {
            if (directory.ToStringSafe().IsEqualTo(string.Empty)) return false;

            if (directory.IsEqualTo(ApplicationParameters.InstallLocation) || directory.IsEqualTo(ApplicationParameters.PackagesLocation))
            {
                var logMessage = "Install location is not specific enough:{0} Erroneous install location captured as '{1}'".FormatWith(Environment.NewLine, directory);
                if (errorAction != null) errorAction.Invoke(logMessage);
                this.Log().Error(logMessage);
                return false;
            }

            return true;
        }

        public void SavePackageSnapshot(PackageFiles snapshot, string filePath)
        {
            if (snapshot == null) return;

            _xmlService.Serialize(snapshot, filePath);
        }

        public void EnsureCompatibleFileAttributes(PackageResult packageResult, ChocolateyConfiguration config)
        {
            if (packageResult == null) return;
            var installDirectory = GetPackageInstallDirectory(packageResult);
            if (installDirectory == null) return;

            EnsureCompatibleFileAttributes(installDirectory, config);
        }

        public void EnsureCompatibleFileAttributes(string directory, ChocolateyConfiguration config)
        {
            if (!PackageInstallDirectoryIsCorrect(directory)) return;

            foreach (var file in _fileSystem.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                var filePath = _fileSystem.GetFullPath(file);
                var fileInfo = _fileSystem.GetFileInfoFor(filePath);

                if (_fileSystem.IsSystemFile(fileInfo)) _fileSystem.EnsureFileAttributeRemoved(filePath, FileAttributes.System);
                if (_fileSystem.IsReadOnlyFile(fileInfo)) _fileSystem.EnsureFileAttributeRemoved(filePath, FileAttributes.ReadOnly);
                if (_fileSystem.IsHiddenFile(fileInfo)) _fileSystem.EnsureFileAttributeRemoved(filePath, FileAttributes.Hidden);
            }
        }

        public PackageFiles CaptureSnapshot(PackageResult packageResult, ChocolateyConfiguration config)
        {
            if (packageResult == null) return new PackageFiles();
            var installDirectory = GetPackageInstallDirectory(packageResult);
            if (installDirectory == null) return null;

            return CaptureSnapshot(installDirectory, config);
        }

        public PackageFiles CaptureSnapshot(string directory, ChocolateyConfiguration config)
        {
            var packageFiles = new PackageFiles();

            if (!PackageInstallDirectoryIsCorrect(directory)) return packageFiles;

            this.Log().Debug(() => "Capturing package files in '{0}'".FormatWith(directory));
            //gather all files in the folder
            var files = _fileSystem.GetFiles(directory, pattern: "*.*", option: SearchOption.AllDirectories);
            foreach (string file in files.OrEmpty().Where(f => !f.EndsWith(ApplicationParameters.PackagePendingFileName)))
            {
                packageFiles.Files.Add(GetPackageFile(file));
            }

            return packageFiles;
        }

        public PackageFile GetPackageFile(string file)
        {
            var hash = _hashProvider.ComputeFileHash(file);
            this.Log().Debug(ChocolateyLoggers.Verbose, () => " Found '{0}'{1}  with checksum '{2}'".FormatWith(file, Environment.NewLine, hash));

            return new PackageFile { Path = file, Checksum = hash };
        }
    }
}
