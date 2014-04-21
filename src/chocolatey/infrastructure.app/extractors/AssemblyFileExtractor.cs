namespace chocolatey.infrastructure.app.extractors
{
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using filesystem;

    /// <summary>
    /// Extracts resources from an assembly.
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
        public static void extract_text_file_from_assembly(IFileSystem fileSystem, Assembly assembly, string manifestLocation, string filePath, bool overwriteExisting = false)
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
        public static void extract_binary_file_from_assembly(IFileSystem fileSystem, Assembly assembly, string manifestLocation, string filePath, bool overwriteExisting = false)
        {
            if (overwriteExisting || !fileSystem.file_exists(filePath))
            {
                fileSystem.create_directory_if_not_exists(fileSystem.get_directory_name(filePath));
                fileSystem.write_file(filePath, () => assembly.get_manifest_stream(manifestLocation));
            }
        }

        public static void extract_all_chocolatey_resources_to_relative_directory(IFileSystem fileSystem, Assembly assembly, string directoryPath, IList<string> relativeDirectories, bool overwriteExisting = false)
        {
            const string resourcesToInclude = "chocolatey.resources";
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
                    resourceString.Replace("{0}".format_with(directory), "{0}{1}".format_with(directory, Path.DirectorySeparatorChar));
                }

                resourceString.Replace("{0}.".format_with(Path.DirectorySeparatorChar), "{0}".format_with(Path.DirectorySeparatorChar));

                var fileLocation = resourceString.ToString();
                //var fileLocation = fileSystem.combine_paths("", resourceString.ToString().Split('.')) + resourceName.Substring(fileExtensionLocation);

                var filePath = fileSystem.combine_paths(directoryPath, fileLocation);
                extract_binary_file_from_assembly(fileSystem, assembly, resourceName, filePath);
            }
        }
    }
}