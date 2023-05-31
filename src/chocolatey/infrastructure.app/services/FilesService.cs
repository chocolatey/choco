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
    using System.Runtime.ConstrainedExecution;
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

        public bool MovePackageUsingBackupStrategy(string sourceFolder, string destinationFolder, bool restoreSource)
        {
            var errored = false;

            try
            {
                _fileSystem.DeleteDirectoryChecked(destinationFolder, recursive: true, overrideAttributes: true, isSilent: true);
            }
            catch (Exception ex)
            {
                // We will ignore any exceptions that occur.
                this.Log().Debug("Failed to delete directory '{0}', will retry for each file.{0} {1}", destinationFolder, Environment.NewLine, ex.Message);

                foreach (var file in _fileSystem.GetFiles(destinationFolder, pattern: "*", option: SearchOption.AllDirectories))
                {
                    try
                    {
                        _fileSystem.DeleteFile(file);
                    }
                    catch (Exception fex)
                    {
                        this.Log().Warn("Unable to delete file '{0}' This may cause further executions to fail.{1} {2}", file, Environment.NewLine, fex.Message);
                    }
                }
            }

            _fileSystem.EnsureDirectoryExists(_fileSystem.GetDirectoryName(destinationFolder));

            if (_fileSystem.DirectoryExists(sourceFolder))
            {
                this.Log().Debug("Moving {0} to {1}", sourceFolder, destinationFolder);

                try
                {
                    _fileSystem.MoveDirectory(sourceFolder, destinationFolder, useFileMoveFallback: false, isSilent: true);
                }
                catch (Exception ex)
                {
                    this.Log().Warn("Unable to move directory '{0}':{1} {2}", sourceFolder, Environment.NewLine, ex.Message);
                    this.Log().Warn("Retrying by moving individual files");

                    foreach (var file in _fileSystem.GetFiles(sourceFolder, pattern: "*", option: SearchOption.AllDirectories))
                    {
                        var newLocation = file.Replace(sourceFolder, destinationFolder);
                        if (_fileSystem.FileExists(newLocation))
                        {
                            continue;
                        }

                        try
                        {
                            _fileSystem.MoveFile(file, newLocation, isSilent: true);
                        }
                        catch (Exception e)
                        {
                            this.Log().Warn("Unable to move file '{0}':{1} {2}", file, Environment.NewLine, e.Message);

                            try
                            {
                                _fileSystem.CopyFile(file, newLocation, overwriteExisting: true);
                            }
                            catch (Exception cex)
                            {
                                errored = true;
                                this.Log().Error("Unable to copy file '{0}':{1} {2}", file, Environment.NewLine, cex.Message);
                            }
                        }
                    }
                }

                if (!restoreSource)
                {
                    return errored;
                }

                try
                {
                    _fileSystem.CopyDirectory(destinationFolder, sourceFolder, overwriteExisting: true, isSilent: true);
                }
                catch (AggregateException ex)
                {
                    errored = true;
                    this.Log().Error("Error during package reset phase:{0} {1}", Environment.NewLine, string.Join(Environment.NewLine + " ", ex.InnerExceptions));
                }
                catch (Exception ex)
                {
                    errored = true;
                    this.Log().Error("Error during package reset phase:{0} {1}", Environment.NewLine, ex.Message);
                }
            }

            return errored;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public PackageFiles read_from_file(string filePath)
            => ReadPackageSnapshot(filePath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void save_to_file(PackageFiles snapshot, string filePath)
            => SavePackageSnapshot(snapshot, filePath);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void ensure_compatible_file_attributes(PackageResult packageResult, ChocolateyConfiguration config)
            => EnsureCompatibleFileAttributes(packageResult, config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void ensure_compatible_file_attributes(string directory, ChocolateyConfiguration config)
            => EnsureCompatibleFileAttributes(directory, config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public PackageFiles capture_package_files(PackageResult packageResult, ChocolateyConfiguration config)
            => CaptureSnapshot(packageResult, config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public PackageFiles capture_package_files(string directory, ChocolateyConfiguration config)
            => CaptureSnapshot(directory, config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public PackageFile get_package_file(string file)
            => GetPackageFile(file);
#pragma warning restore IDE1006
    }
}
