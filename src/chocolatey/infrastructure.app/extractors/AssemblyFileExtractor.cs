namespace chocolatey.infrastructure.app.extractors
{
    using System.IO;
    using System.Reflection;
    using System.Text;
    using filesystem;

    public static class AssemblyFileExtractor
    {
        public static void extract_file_from_assembly(IFileSystem fileSystem, string manifestLocation, string filePath, bool overwriteExisting = false)
        {
            if (overwriteExisting || !fileSystem.file_exists(filePath))
            {
                fileSystem.create_directory_if_not_exists(fileSystem.get_directory_name(filePath));
                var assembly = Assembly.GetExecutingAssembly();
                var fileText = assembly.get_manifest_string(manifestLocation);
                if (string.IsNullOrWhiteSpace(fileText))
                {
                    string errorMessage = "Could not find a file in the manifest resource stream of '{0}' at '{1}'.".format_with(assembly.FullName,manifestLocation);
                    "chocolatey".Log().Error(() => errorMessage);
                    throw new FileNotFoundException(errorMessage);
                }

                fileSystem.write_file(filePath, fileText, Encoding.UTF8);
            }
        }
    }
}