namespace chocolatey.infrastructure.app.nuget
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NuGet;

    // ReSharper disable InconsistentNaming

    public static class NuGetFileSystemExtensions
    {
        public static void AddFiles(this IFileSystem fileSystem, IEnumerable<IPackageFile> files, string rootDir, bool preserveFilePath)
        {
            foreach (IPackageFile file in files)
            {
                string path = Path.Combine(rootDir, preserveFilePath ? file.Path : Path.GetFileName(file.Path));
                fileSystem.AddFileWithCheck(path, file.GetStream);
            }
        }

        internal static void AddFileWithCheck(this IFileSystem fileSystem, string path, Func<Stream> streamFactory)
        {
            using (Stream stream = streamFactory())
            {
                fileSystem.AddFile(path, stream);
            }
        }

        internal static void AddFileWithCheck(this IFileSystem fileSystem, string path, Action<Stream> write)
        {
            fileSystem.AddFile(path, write);
        }
    }

    // ReSharper restore InconsistentNaming
}