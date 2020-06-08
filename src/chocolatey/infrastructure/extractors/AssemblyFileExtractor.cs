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

namespace chocolatey.infrastructure.extractors
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using adapters;
    using filesystem;
    using tolerance;

    /// <summary>
    ///   Extracts resources from an assembly.
    /// </summary>
    public static class AssemblyFileExtractor
    {
        /// <summary>
        ///   Extract text file from assembly to location on disk.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="assembly">The assembly.</param>
        /// <param name="manifestLocation">The manifest location.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="overwriteExisting">
        ///   if set to <c>true</c> [overwrite existing].
        /// </param>
        /// <exception cref="System.IO.FileNotFoundException"></exception>
        public static void extract_text_file_from_assembly(IFileSystem fileSystem, IAssembly assembly, string manifestLocation, string filePath, bool overwriteExisting = false)
        {
            if (overwriteExisting || !fileSystem.file_exists(filePath))
            {
                fileSystem.create_directory_if_not_exists(fileSystem.get_directory_name(filePath));
                var fileText = assembly.get_manifest_string(manifestLocation);
                if (string.IsNullOrWhiteSpace(fileText))
                {
                    string errorMessage = "Could not find a file in the manifest resource stream of '{0}' at '{1}'.".format_with(assembly.FullName, manifestLocation);
                    "chocolatey".Log().Error(() => errorMessage);
                    throw new FileNotFoundException(errorMessage);
                }

                fileSystem.write_file(filePath, fileText, Encoding.UTF8);
            }
        }

        /// <summary>
        ///   Extract binary file from an assembly to a location on disk
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="assembly">The assembly.</param>
        /// <param name="manifestLocation">The manifest location.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="overwriteExisting">
        ///   if set to <c>true</c> [overwrite existing].
        /// </param>
        /// <param name="throwEror">Throw an error if there are issues</param>
        public static void extract_binary_file_from_assembly(IFileSystem fileSystem, IAssembly assembly, string manifestLocation, string filePath, bool overwriteExisting = false, bool throwEror = true)
        {
            if (overwriteExisting || !fileSystem.file_exists(filePath))
            {
                FaultTolerance.try_catch_with_logging_exception(
                    () =>
                    {
                        fileSystem.create_directory_if_not_exists(fileSystem.get_directory_name(filePath));
                        fileSystem.write_file(filePath, () => assembly.get_manifest_stream(manifestLocation));        
                    }, 
                   errorMessage:"Unable to extract binary", 
                   throwError: throwEror, 
                   logWarningInsteadOfError: false, 
                   logDebugInsteadOfError: !throwEror,
                   isSilent: !throwEror);
            }
        }

        public static void extract_all_resources_to_relative_directory(IFileSystem fileSystem, IAssembly assembly, string directoryPath, IList<string> relativeDirectories, string resourcesToInclude, bool overwriteExisting = false, bool logOutput = false, bool throwError = true)
        {
            var resourceString = new StringBuilder();
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(resourcesToInclude))
                {
                    continue;
                }
                resourceString.Clear();
                resourceString.Append(resourceName);

                //var fileExtensionLocation = resourceName.LastIndexOf('.');
                //resourceString.Remove(fileExtensionLocation, resourceString.Length - fileExtensionLocation);
                resourceString.Replace(resourcesToInclude + ".", "");
                foreach (var directory in relativeDirectories)
                {
                    resourceString.Replace("{0}".format_with(directory), "{0}{1}".format_with(directory, fileSystem.get_path_directory_separator_char()));
                }

                // replacing \. with \
                resourceString.Replace("{0}.".format_with(fileSystem.get_path_directory_separator_char()), "{0}".format_with(fileSystem.get_path_directory_separator_char()));

                var fileLocation = resourceString.ToString();
                //var fileLocation = fileSystem.combine_paths("", resourceString.ToString().Split('.')) + resourceName.Substring(fileExtensionLocation);

                var filePath = fileSystem.combine_paths(directoryPath, fileLocation);
                if (logOutput) "chocolatey".Log().Debug("Unpacking {0} to '{1}'".format_with(fileLocation, filePath));
                extract_binary_file_from_assembly(fileSystem, assembly, resourceName, filePath, overwriteExisting, throwError);
            }
        }
    }
}