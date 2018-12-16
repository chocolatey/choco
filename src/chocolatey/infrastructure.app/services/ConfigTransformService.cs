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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Web.XmlTransform;
    using configuration;
    using filesystem;
    using results;
    using synchronization;
    using tolerance;

    public class ConfigTransformService : IConfigTransformService
    {
        private readonly IFileSystem _fileSystem;
        private const int MUTEX_TIMEOUT = 2000;

        public ConfigTransformService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public void run(PackageResult packageResult, ChocolateyConfiguration config)
        {
            var installDirectory = packageResult != null ? packageResult.InstallLocation : string.Empty;
            if (string.IsNullOrWhiteSpace(installDirectory) || installDirectory.is_equal_to(ApplicationParameters.InstallLocation) || installDirectory.is_equal_to(ApplicationParameters.PackagesLocation))
            {
                var logMessage = "Install location is not specific enough, cannot capture files:{0} Erroneous install location captured as '{1}'".format_with(Environment.NewLine, installDirectory);
                if (packageResult != null) packageResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                this.Log().Error(logMessage);
                return;
            }

            var transformFiles = _fileSystem.get_files(installDirectory, "*" + ApplicationParameters.ConfigFileTransformExtension, SearchOption.AllDirectories);
            foreach (var transformFile in transformFiles.or_empty_list_if_null())
            {
                this.Log().Debug(() => "Preparing transform for '{0}'".format_with(transformFile));
                var targetFileName = _fileSystem.get_file_name(transformFile.Replace(ApplicationParameters.ConfigFileTransformExtension, string.Empty));
                // target files must exist, otherwise one is added next to the transform
                var targetFiles = _fileSystem.get_files(installDirectory, targetFileName, SearchOption.AllDirectories);

                var targetFilesTest = targetFiles as IList<string> ?? targetFiles.ToList();
                if (!targetFilesTest.Any())
                {
                    targetFiles = new[] {transformFile.Replace(ApplicationParameters.ConfigFileTransformExtension, string.Empty)};
                    this.Log().Debug(() => "No matching files found for transform {0}.{1} Creating '{2}'".format_with(_fileSystem.get_file_name(transformFile), Environment.NewLine, targetFiles.FirstOrDefault()));
                }

                foreach (var targetFile in targetFilesTest.or_empty_list_if_null())
                {
                    GlobalMutex.enter(
                        () =>
                        {
                            var backupTargetFile = targetFile.Replace(ApplicationParameters.PackagesLocation, ApplicationParameters.PackageBackupLocation);

                            FaultTolerance.try_catch_with_logging_exception(
                                () =>
                                {
                                    // if there is a backup, we need to put it back in place
                                    // the user has indicated they are using transforms by putting
                                    // the transform file into the folder, so we will override
                                    // the replacement of the file and instead pull from the last 
                                    // backup and let the transform to its thing instead.
                                    if (_fileSystem.file_exists(backupTargetFile))
                                    {
                                        this.Log().Debug(()=> "Restoring backup configuration file for '{0}'.".format_with(targetFile));
                                        _fileSystem.copy_file(backupTargetFile, targetFile, overwriteExisting: true);
                                    }
                                },
                                "Error replacing backup config file",
                                throwError: false,
                                logWarningInsteadOfError: true);

                            try
                            {
                                this.Log().Info(() => "Transforming '{0}' with the data from '{1}'".format_with(_fileSystem.get_file_name(targetFile), _fileSystem.get_file_name(transformFile)));

                                using (var transformation = new XmlTransformation(_fileSystem.read_file(transformFile), isTransformAFile: false, logger: null))
                                {
                                    using (var document = new XmlTransformableDocument {PreserveWhitespace = true})
                                    {
                                        using (var inputStream = _fileSystem.open_file_readonly(targetFile))
                                        {
                                            document.Load(inputStream);
                                        }

                                        // before applying the XDT transformation, let's make a
                                        // backup of the file that should be transformed, in case
                                        // things don't go correctly
                                        this.Log().Debug(() => "Creating backup configuration file for '{0}'.".format_with(targetFile));
                                        _fileSystem.copy_file(targetFile, backupTargetFile, overwriteExisting: true);

                                        bool succeeded = transformation.Apply(document);
                                        if (succeeded)
                                        {
                                            this.Log().Debug(() => "Transform applied successfully for '{0}'".format_with(targetFile));
                                            using (var memoryStream = new MemoryStream())
                                            {
                                                document.Save(memoryStream);
                                                memoryStream.Seek(0, SeekOrigin.Begin);
                                                using (var fileStream = _fileSystem.create_file(targetFile))
                                                {
                                                    fileStream.SetLength(0);
                                                    memoryStream.CopyTo(fileStream);
                                                }
                                            }

                                            // need to test that the transformed configuration file is valid
                                            // XML.  We can test this by trying to load it again into an XML document
                                            try
                                            {
                                                this.Log().Debug(() => "Verifying transformed configuration file...");
                                                document.Load(targetFile);
                                                this.Log().Debug(() => "Transformed configuration file verified.");
                                            }
                                            catch (Exception)
                                            {
                                                this.Log().Warn(() => "Transformed configuration file doesn't contain valid XML.  Restoring backup file...");
                                                _fileSystem.copy_file(backupTargetFile, targetFile, overwriteExisting: true);
                                                this.Log().Debug(() => "Backup file restored.");
                                            }
                                        }
                                        else
                                        {
                                            // at this point, there is no need to restore the backup file,
                                            // as the resulting transform hasn't actually been written to disk.
                                            this.Log().Warn(() => "Transform failed for '{0}'".format_with(targetFile));
                                        }
                                    }
                                }
                            }
                            catch (Exception)
                            {
                                FaultTolerance.try_catch_with_logging_exception(
                                    () =>
                                    {
                                        // something went wrong with the transformation, so we should restore
                                        // the original configuration file from the backup
                                        this.Log().Warn(() => "There was a problem transforming configuration file, restoring backup file...");
                                        _fileSystem.copy_file(backupTargetFile, targetFile, overwriteExisting: true);
                                        this.Log().Debug(() => "Backup file restored.");
                                    },
                                    "Error restoring backup configuration file.");
                            }
                        }, MUTEX_TIMEOUT);
                }
            }
        }
    }
}
