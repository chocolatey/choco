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
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using configuration;
    using domain;
    using infrastructure.configuration;
    using NuGet.Packaging;
    using NuGet.Versioning;
    using results;
    using tolerance;
    using IFileSystem = filesystem.IFileSystem;

    public class ChocolateyPackageInformationService : IChocolateyPackageInformationService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private readonly IFilesService _filesService;
        private readonly ChocolateyConfiguration _config;
        private const string RegistrySnapshotFile = ".registry";
        private const string RegistrySnapshotBadFile = ".registry.bad";
        private const string FilesSnapshotFile = ".files";
        private const string SilentUninstallerFile = ".silentUninstaller";
        private const string SideBySideFile = ".sxs";
        private const string PinFile = ".pin";
        private const string ArgsFile = ".arguments";
        private const string ExtraFile = ".extra";
        private const string VersionOverrideFile = ".version";

        // We need to store the package identifiers we have warned about
        // to prevent duplicated outputs.
        private HashSet<string> _deprecationWarning = new HashSet<string>();

        public ChocolateyPackageInformationService(IFileSystem fileSystem, IRegistryService registryService, IFilesService filesService)
        {
            _fileSystem = fileSystem;
            _registryService = registryService;
            _filesService = filesService;
            _config = Config.GetConfigurationSettings();
        }

        public ChocolateyPackageInformationService(IFileSystem fileSystem, IRegistryService registryService, IFilesService filesService, ChocolateyConfiguration config)
        {
            _fileSystem = fileSystem;
            _registryService = registryService;
            _filesService = filesService;
            _config = config;
        }

        public ChocolateyPackageInformation Get(IPackageMetadata package)
        {
            var packageInformation = new ChocolateyPackageInformation(package);
            if (package == null)
            {
                if (_config.RegularOutput) { this.Log().Debug("No package information as package is null."); }
                return packageInformation;
            }

            var pkgStorePath = GetStorePath(_fileSystem, package.Id, package.Version);

            if (!_fileSystem.DirectoryExists(pkgStorePath))
            {
                return packageInformation;
            }

            var deserializationErrorMessage = @"
A corrupt .registry file exists at {0}.
 Open this file in a text editor, and remove/escape any characters that
 are regarded as illegal within XML strings not surrounded by CData.
 These are typically the characters &, `<`, and `>`. Again, this
 is an XML document, so you will see many < and > characters, so just
 focus exclusively in the string values not surrounded by CData. Once
 these have been corrected, rename the .registry.bad file to .registry.
 Once saved, try running the same Chocolatey command that was just
 executed, so verify problem is fixed.
 NOTE: It will not be possible to rename the file in Windows Explorer.
 Instead, you can use the following PowerShell command:
 Move-Item .\.registry.bad .\.registry
".FormatWith(_fileSystem.CombinePaths(pkgStorePath, RegistrySnapshotBadFile));

            try
            {
                if (_fileSystem.FileExists(_fileSystem.CombinePaths(pkgStorePath, RegistrySnapshotBadFile)))
                {
                    if (_config.RegularOutput) this.Log().Warn(deserializationErrorMessage);
                }
                else
                {
                    packageInformation.RegistrySnapshot = _registryService.ReadRegistrySnapshot(_fileSystem.CombinePaths(pkgStorePath, RegistrySnapshotFile));
                }
            }
            catch (Exception e)
            {
                if (_config.RegularOutput) this.Log().Warn(@"A .registry file at '{0}'
 has errored attempting to read it. This file will be renamed to
 '{1}' The error:
 {2}
 ".FormatWith(_fileSystem.CombinePaths(pkgStorePath, RegistrySnapshotFile), _fileSystem.CombinePaths(pkgStorePath, RegistrySnapshotBadFile), e.ToString()));

                FaultTolerance.TryCatchWithLoggingException(
                    () =>
                    {
                        if (_config.RegularOutput) this.Log().Warn(deserializationErrorMessage);

                        // rename the bad registry file so that it isn't processed again
                        _fileSystem.MoveFile(_fileSystem.CombinePaths(pkgStorePath, RegistrySnapshotFile), _fileSystem.CombinePaths(pkgStorePath, RegistrySnapshotBadFile));
                    },
                    "Unable to read registry snapshot file for {0} (located at {1})".FormatWith(package.Id, _fileSystem.CombinePaths(pkgStorePath, RegistrySnapshotFile)),
                    throwError: false,
                    logWarningInsteadOfError: true,
                    isSilent: true
                );
            }

            FaultTolerance.TryCatchWithLoggingException(
                () =>
                    {
                        packageInformation.FilesSnapshot = _filesService.ReadPackageSnapshot(_fileSystem.CombinePaths(pkgStorePath, FilesSnapshotFile));
                    },
                    "Unable to read files snapshot file",
                    throwError: false,
                    logWarningInsteadOfError: true,
                    isSilent:true
                 );

            packageInformation.HasSilentUninstall = _fileSystem.FileExists(_fileSystem.CombinePaths(pkgStorePath, SilentUninstallerFile));
            packageInformation.IsPinned = _fileSystem.FileExists(_fileSystem.CombinePaths(pkgStorePath, PinFile));
            var argsFile = _fileSystem.CombinePaths(pkgStorePath, ArgsFile);
            if (_fileSystem.FileExists(argsFile)) packageInformation.Arguments = _fileSystem.ReadFile(argsFile);
            var extraInfoFile = _fileSystem.CombinePaths(pkgStorePath, ExtraFile);
            if (_fileSystem.FileExists(extraInfoFile)) packageInformation.ExtraInformation = _fileSystem.ReadFile(extraInfoFile);

            var versionOverrideFile = _fileSystem.CombinePaths(pkgStorePath, VersionOverrideFile);
            if (_fileSystem.FileExists(versionOverrideFile))
            {

                FaultTolerance.TryCatchWithLoggingException(
                () =>
                    {
                        packageInformation.VersionOverride = new NuGetVersion(_fileSystem.ReadFile(versionOverrideFile).TrimSafe());
                    },
                    "Unable to read version override file",
                    throwError: false,
                    logWarningInsteadOfError: true
                 );
            }

            return packageInformation;
        }

        public void Save(ChocolateyPackageInformation packageInformation)
        {
            _fileSystem.EnsureDirectoryExists(ApplicationParameters.ChocolateyPackageInfoStoreLocation);
            _fileSystem.EnsureFileAttributeSet(ApplicationParameters.ChocolateyPackageInfoStoreLocation, FileAttributes.Hidden);

            if (packageInformation.Package == null)
            {
                if (_config.RegularOutput) this.Log().Debug("No package information to save as package is null.");
                return;
            }

            var pkgStorePath = GetStorePath(_fileSystem, packageInformation.Package.Id, packageInformation.Package.Version);

            _fileSystem.EnsureDirectoryExists(pkgStorePath);

            if (packageInformation.RegistrySnapshot != null)
            {
                _registryService.SaveRegistrySnapshot(packageInformation.RegistrySnapshot, _fileSystem.CombinePaths(pkgStorePath, RegistrySnapshotFile));
            }

            if (packageInformation.FilesSnapshot != null)
            {
                FaultTolerance.TryCatchWithLoggingException(
              () =>
              {
                  _filesService.SavePackageSnapshot(packageInformation.FilesSnapshot, _fileSystem.CombinePaths(pkgStorePath, FilesSnapshotFile));
              },
                  "Unable to save files snapshot",
                  throwError: false,
                  logWarningInsteadOfError: true
               );
            }

            if (!string.IsNullOrWhiteSpace(packageInformation.Arguments))
            {
                var argsFile = _fileSystem.CombinePaths(pkgStorePath, ArgsFile);
                if (_fileSystem.FileExists(argsFile)) _fileSystem.DeleteFile(argsFile);
                _fileSystem.WriteFile(argsFile, packageInformation.Arguments);
            }
            else
            {
                _fileSystem.DeleteFile(_fileSystem.CombinePaths(pkgStorePath, ArgsFile));
            }

            if (!string.IsNullOrWhiteSpace(packageInformation.ExtraInformation))
            {
                var extraFile = _fileSystem.CombinePaths(pkgStorePath, ExtraFile);
                if (_fileSystem.FileExists(extraFile)) _fileSystem.DeleteFile(extraFile);
                _fileSystem.WriteFile(extraFile, packageInformation.ExtraInformation);
            }
            else
            {
                _fileSystem.DeleteFile(_fileSystem.CombinePaths(pkgStorePath, ExtraFile));
            }

            if (packageInformation.VersionOverride != null)
            {
                var versionOverrideFile = _fileSystem.CombinePaths(pkgStorePath, VersionOverrideFile);
                if (_fileSystem.FileExists(versionOverrideFile)) _fileSystem.DeleteFile(versionOverrideFile);
                _fileSystem.WriteFile(versionOverrideFile, packageInformation.VersionOverride.ToNormalizedStringChecked());
            }
            else
            {
                _fileSystem.DeleteFile(_fileSystem.CombinePaths(pkgStorePath, VersionOverrideFile));
            }

            if (packageInformation.HasSilentUninstall)
            {
                _fileSystem.WriteFile(_fileSystem.CombinePaths(pkgStorePath, SilentUninstallerFile), string.Empty, Encoding.ASCII);
            }

            // Legacy side-by-side installation data cleanup
            _fileSystem.DeleteFile(_fileSystem.CombinePaths(pkgStorePath, SideBySideFile));

            if (packageInformation.IsPinned)
            {
                _fileSystem.WriteFile(_fileSystem.CombinePaths(pkgStorePath, PinFile), string.Empty, Encoding.ASCII);
            }
            else
            {
                _fileSystem.DeleteFile(_fileSystem.CombinePaths(pkgStorePath, PinFile));
            }
        }

        public void Remove(IPackageMetadata package)
        {
            var pkgStorePath = GetStorePath(_fileSystem, package.Id, package.Version);
            if (_config.RegularOutput) this.Log().Info("Removing Package Information for {0}".FormatWith(pkgStorePath));
            _fileSystem.DeleteDirectoryChecked(pkgStorePath, recursive: true);
        }

        private static string GetStorePath(IFileSystem fileSystem, string id, NuGetVersion version)
        {
            var preferredStorePath = fileSystem.CombinePaths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".FormatWith(id, version.ToNormalizedStringChecked()));

            if (fileSystem.DirectoryExists(preferredStorePath))
            {
                return preferredStorePath;
            }

            // Legacy handling for old package versions that was installed prior to v2.0.0.
            // Do not remove the call to `ToStringSafe`.

            var pkgStorePath = fileSystem.CombinePaths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".FormatWith(id, version.ToStringSafe()));

            if (fileSystem.DirectoryExists(pkgStorePath))
            {
                return pkgStorePath;
            }

            // Legacy handling for package version that was installed originally as 4.0.0.0

            var versionFull = version.IsPrerelease
                ? "{0}-{1}".FormatWith(version.Version.ToStringSafe(), version.Release)
                : version.Version.ToStringSafe();
            pkgStorePath = fileSystem.CombinePaths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".FormatWith(id, versionFull));

            if (fileSystem.DirectoryExists(pkgStorePath))
            {
                return pkgStorePath;
            }

            // Legacy handling for package versions that was installed originally as "4.2"

            if (version.Version.Revision == 0 && version.Version.Build == 0)
            {
                versionFull = version.IsPrerelease
                    ? "{0}.{1}-{2}".FormatWith(version.Major, version.Minor, version.Release)
                    : "{0}.{1}".FormatWith(version.Major, version.Minor);
                pkgStorePath = fileSystem.CombinePaths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".FormatWith(id, versionFull));

                if (fileSystem.DirectoryExists(pkgStorePath))
                {
                    return pkgStorePath;
                }
            }

            return preferredStorePath;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public ChocolateyPackageInformation get_package_information(IPackageMetadata package)
            => Get(package);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void save_package_information(ChocolateyPackageInformation packageInformation)
            => Save(packageInformation);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void remove_package_information(IPackageMetadata package)
            => Remove(package);
#pragma warning restore IDE1006
    }
}
