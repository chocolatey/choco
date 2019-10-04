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

        public PackageFiles read_from_file(string filePath)
        {
            if (!_fileSystem.file_exists(filePath)) return null;

            return _xmlService.deserialize<PackageFiles>(filePath);
        }

        private string get_package_install_directory(PackageResult packageResult)
        {
            if (packageResult == null) return null;

            var installDirectory = packageResult.InstallLocation;
            return package_install_directory_is_correct(installDirectory, logMessage => packageResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage))) ? installDirectory : null;
        }

        private bool package_install_directory_is_correct(string directory, Action<string> errorAction = null)
        {
            if (directory.to_string().is_equal_to(string.Empty)) return false;

            if (directory.is_equal_to(ApplicationParameters.InstallLocation) || directory.is_equal_to(ApplicationParameters.PackagesLocation))
            {
                var logMessage = "Install location is not specific enough:{0} Erroneous install location captured as '{1}'".format_with(Environment.NewLine, directory);
                if (errorAction != null) errorAction.Invoke(logMessage);
                this.Log().Error(logMessage);
                return false;
            }

            return true;
        }

        public void save_to_file(PackageFiles snapshot, string filePath)
        {
            if (snapshot == null) return;

            _xmlService.serialize(snapshot, filePath);
        }

        public void ensure_compatible_file_attributes(PackageResult packageResult, ChocolateyConfiguration config)
        {
            if (packageResult == null) return;
            var installDirectory = get_package_install_directory(packageResult);
            if (installDirectory == null) return;

            ensure_compatible_file_attributes(installDirectory, config);
        }

        public void ensure_compatible_file_attributes(string directory, ChocolateyConfiguration config)
        {
            if (!package_install_directory_is_correct(directory)) return;

            foreach (var file in _fileSystem.get_files(directory, "*.*", SearchOption.AllDirectories))
            {
                var filePath = _fileSystem.get_full_path(file);
                var fileInfo = _fileSystem.get_file_info_for(filePath);

                if (_fileSystem.is_system_file(fileInfo)) _fileSystem.ensure_file_attribute_removed(filePath, FileAttributes.System);
                if (_fileSystem.is_readonly_file(fileInfo)) _fileSystem.ensure_file_attribute_removed(filePath, FileAttributes.ReadOnly);
                if (_fileSystem.is_hidden_file(fileInfo)) _fileSystem.ensure_file_attribute_removed(filePath, FileAttributes.Hidden);
            }
        }

        public PackageFiles capture_package_files(PackageResult packageResult, ChocolateyConfiguration config)
        {
            if (packageResult == null) return new PackageFiles();
            var installDirectory = get_package_install_directory(packageResult);
            if (installDirectory == null) return null;

            return capture_package_files(installDirectory, config);
        }

        public PackageFiles capture_package_files(string directory, ChocolateyConfiguration config)
        {
            var packageFiles = new PackageFiles();

            if (!package_install_directory_is_correct(directory)) return packageFiles;

            this.Log().Debug(() => "Capturing package files in '{0}'".format_with(directory));
            //gather all files in the folder 
            var files = _fileSystem.get_files(directory, pattern: "*.*", option: SearchOption.AllDirectories);
            foreach (string file in files.or_empty_list_if_null().Where(f => !f.EndsWith(ApplicationParameters.PackagePendingFileName)))
            {
                packageFiles.Files.Add(get_package_file(file));
            }

            return packageFiles;
        }

        public PackageFile get_package_file(string file)
        {
            var hash = _hashProvider.hash_file(file);
            this.Log().Debug(ChocolateyLoggers.Verbose, () => " Found '{0}'{1}  with checksum '{2}'".format_with(file, Environment.NewLine, hash));
            
            return new PackageFile { Path = file, Checksum = hash };
        }
    }
}