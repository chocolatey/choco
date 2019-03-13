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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.IO;
    using System.Text;
    using configuration;
    using NuGet;
    using domain;
    using infrastructure.configuration;
    using tolerance;
    using IFileSystem = filesystem.IFileSystem;

    public class ChocolateyPackageInformationService : IChocolateyPackageInformationService
    {
        private readonly IFileSystem _fileSystem;
        private readonly IRegistryService _registryService;
        private readonly IFilesService _filesService;
        private readonly ChocolateyConfiguration _config;
        private const string REGISTRY_SNAPSHOT_FILE = ".registry";
        private const string REGISTRY_SNAPSHOT_BAD_FILE = ".registry.bad";
        private const string FILES_SNAPSHOT_FILE = ".files";
        private const string SILENT_UNINSTALLER_FILE = ".silentUninstaller";
        private const string SIDE_BY_SIDE_FILE = ".sxs";
        private const string PIN_FILE = ".pin";
        private const string ARGS_FILE = ".arguments";
        private const string EXTRA_FILE = ".extra";
        private const string VERSION_OVERRIDE_FILE = ".version";

        public ChocolateyPackageInformationService(IFileSystem fileSystem, IRegistryService registryService, IFilesService filesService)
        {
            _fileSystem = fileSystem;
            _registryService = registryService;
            _filesService = filesService;
            _config = Config.get_configuration_settings();
        }

        public ChocolateyPackageInformationService(IFileSystem fileSystem, IRegistryService registryService, IFilesService filesService, ChocolateyConfiguration config)
        {
            _fileSystem = fileSystem;
            _registryService = registryService;
            _filesService = filesService;
            _config = config;
        }

        public ChocolateyPackageInformation get_package_information(IPackage package)
        {
            var packageInformation = new ChocolateyPackageInformation(package);
            if (package == null)
            {
                if (_config.RegularOutput) { this.Log().Debug("No package information as package is null."); }
                return packageInformation;
            }

            var pkgStorePath = _fileSystem.combine_paths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".format_with(package.Id, package.Version.to_string()));
            if (!_fileSystem.directory_exists(pkgStorePath))
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
".format_with(_fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_BAD_FILE));

            try
            {
                if (_fileSystem.file_exists(_fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_BAD_FILE)))
                {
                    if (_config.RegularOutput) this.Log().Warn(deserializationErrorMessage);
                }
                else
                {
                    packageInformation.RegistrySnapshot = _registryService.read_from_file(_fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_FILE));
                }
            }
            catch (Exception e)
            {
                if (_config.RegularOutput) this.Log().Warn(@"A .registry file at '{0}'
 has errored attempting to read it. This file will be renamed to 
 '{1}' The error:
 {2} 
 ".format_with(_fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_FILE), _fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_BAD_FILE), e.ToString()));

                FaultTolerance.try_catch_with_logging_exception(
                    () =>
                    {
                        if (_config.RegularOutput) this.Log().Warn(deserializationErrorMessage);

                        // rename the bad registry file so that it isn't processed again
                        _fileSystem.move_file(_fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_FILE), _fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_BAD_FILE));
                    },
                    "Unable to read registry snapshot file for {0} (located at {1})".format_with(package.Id, _fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_FILE)),
                    throwError: false,
                    logWarningInsteadOfError: true,
                    isSilent: true
                );
            }

            FaultTolerance.try_catch_with_logging_exception(
                () =>
                    {
                        packageInformation.FilesSnapshot = _filesService.read_from_file(_fileSystem.combine_paths(pkgStorePath, FILES_SNAPSHOT_FILE));
                    },
                    "Unable to read files snapshot file",
                    throwError: false,
                    logWarningInsteadOfError: true,
                    isSilent:true
                 );

            packageInformation.HasSilentUninstall = _fileSystem.file_exists(_fileSystem.combine_paths(pkgStorePath, SILENT_UNINSTALLER_FILE));
            packageInformation.IsSideBySide = _fileSystem.file_exists(_fileSystem.combine_paths(pkgStorePath, SIDE_BY_SIDE_FILE));
            packageInformation.IsPinned = _fileSystem.file_exists(_fileSystem.combine_paths(pkgStorePath, PIN_FILE));
            var argsFile = _fileSystem.combine_paths(pkgStorePath, ARGS_FILE);
            if (_fileSystem.file_exists(argsFile)) packageInformation.Arguments = _fileSystem.read_file(argsFile);
            var extraInfoFile = _fileSystem.combine_paths(pkgStorePath, EXTRA_FILE);
            if (_fileSystem.file_exists(extraInfoFile)) packageInformation.ExtraInformation = _fileSystem.read_file(extraInfoFile);

            var versionOverrideFile = _fileSystem.combine_paths(pkgStorePath, VERSION_OVERRIDE_FILE);
            if (_fileSystem.file_exists(versionOverrideFile))
            {

                FaultTolerance.try_catch_with_logging_exception(
                () =>
                    {
                        packageInformation.VersionOverride = new SemanticVersion(_fileSystem.read_file(versionOverrideFile).trim_safe());
                    },
                    "Unable to read version override file",
                    throwError: false,
                    logWarningInsteadOfError: true
                 );
            }

            return packageInformation;
        }

        public void save_package_information(ChocolateyPackageInformation packageInformation)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.ChocolateyPackageInfoStoreLocation);
            _fileSystem.ensure_file_attribute_set(ApplicationParameters.ChocolateyPackageInfoStoreLocation, FileAttributes.Hidden);

            if (packageInformation.Package == null)
            {
                if (_config.RegularOutput) this.Log().Debug("No package information to save as package is null.");
                return;
            }

            var pkgStorePath = _fileSystem.combine_paths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".format_with(packageInformation.Package.Id, packageInformation.Package.Version.to_string()));
            _fileSystem.create_directory_if_not_exists(pkgStorePath);

            if (packageInformation.RegistrySnapshot != null)
            {
                _registryService.save_to_file(packageInformation.RegistrySnapshot, _fileSystem.combine_paths(pkgStorePath, REGISTRY_SNAPSHOT_FILE));
            }

            if (packageInformation.FilesSnapshot != null)
            {
                FaultTolerance.try_catch_with_logging_exception(
              () =>
              {
                  _filesService.save_to_file(packageInformation.FilesSnapshot, _fileSystem.combine_paths(pkgStorePath, FILES_SNAPSHOT_FILE));
              },
                  "Unable to save files snapshot",
                  throwError: false,
                  logWarningInsteadOfError: true
               );
            }

            if (!string.IsNullOrWhiteSpace(packageInformation.Arguments))
            {
                var argsFile = _fileSystem.combine_paths(pkgStorePath, ARGS_FILE);
                if (_fileSystem.file_exists(argsFile)) _fileSystem.delete_file(argsFile);
                _fileSystem.write_file(argsFile, packageInformation.Arguments);
            }
            else
            {
                _fileSystem.delete_file(_fileSystem.combine_paths(pkgStorePath, ARGS_FILE));
            }

            if (!string.IsNullOrWhiteSpace(packageInformation.ExtraInformation))
            {
                var extraFile = _fileSystem.combine_paths(pkgStorePath, EXTRA_FILE);
                if (_fileSystem.file_exists(extraFile)) _fileSystem.delete_file(extraFile);
                _fileSystem.write_file(extraFile, packageInformation.ExtraInformation);
            }
            else
            {
                _fileSystem.delete_file(_fileSystem.combine_paths(pkgStorePath, EXTRA_FILE));
            }

            if (packageInformation.VersionOverride != null)
            {
                var versionOverrideFile = _fileSystem.combine_paths(pkgStorePath, VERSION_OVERRIDE_FILE);
                if (_fileSystem.file_exists(versionOverrideFile)) _fileSystem.delete_file(versionOverrideFile);
                _fileSystem.write_file(versionOverrideFile, packageInformation.VersionOverride.to_string());
            }
            else
            {
                _fileSystem.delete_file(_fileSystem.combine_paths(pkgStorePath, VERSION_OVERRIDE_FILE));
            }

            if (packageInformation.HasSilentUninstall)
            {
                _fileSystem.write_file(_fileSystem.combine_paths(pkgStorePath, SILENT_UNINSTALLER_FILE), string.Empty, Encoding.ASCII);
            }
            if (packageInformation.IsSideBySide)
            {
                _fileSystem.write_file(_fileSystem.combine_paths(pkgStorePath, SIDE_BY_SIDE_FILE), string.Empty, Encoding.ASCII);
            }
            else
            {
                _fileSystem.delete_file(_fileSystem.combine_paths(pkgStorePath, SIDE_BY_SIDE_FILE));
            }

            if (packageInformation.IsPinned)
            {
                _fileSystem.write_file(_fileSystem.combine_paths(pkgStorePath, PIN_FILE), string.Empty, Encoding.ASCII);
            }
            else
            {
                _fileSystem.delete_file(_fileSystem.combine_paths(pkgStorePath, PIN_FILE));
            }
        }

        public void remove_package_information(IPackage package)
        {
            var pkgStorePath = _fileSystem.combine_paths(ApplicationParameters.ChocolateyPackageInfoStoreLocation, "{0}.{1}".format_with(package.Id, package.Version.to_string()));
            if (_config.RegularOutput) this.Log().Info("Removing Package Information for {0}".format_with(pkgStorePath));
            _fileSystem.delete_directory_if_exists(pkgStorePath, recursive: true);
        }
    }
}