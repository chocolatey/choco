namespace chocolatey.infrastructure.filesystem
{
    using System;
    using System.IO;

    /// <summary>
    ///   Implementation of IFileSystem for Dot Net
    /// </summary>
    /// <remarks>Normally we avoid regions, however this has so many methods that we are making an exception.</remarks>
    public sealed class DotNetFileSystem : IFileSystem
    {
        /// <summary>
        ///     Gets the files.
        /// </summary>
        /// <param name="path">The path to a specified directory.</param>
        /// <param name="pattern">The search pattern.</param>
        /// <param name="option">The option specifies whether the search operation should include all subdirectories or only the current directory.</param>
        /// <returns>Returns the names of files (including their paths).</returns>
        public string[] GetFiles(string path, string pattern, SearchOption option)
        {
            return Directory.GetFiles(path, pattern, option);
        }

        /// <summary>
        ///     Gets the full path.
        /// </summary>
        /// <param name="filePath">The file or directory for which to obtain absolute path information.</param>
        /// <returns>The fully qualified location of path, such as "C:\MyFile.txt".</returns>
        public string GetFullPath(string filePath)
        {
            return Path.GetFullPath(filePath);
        }

        /// <summary>
        ///     Files the exists.
        /// </summary>
        /// <param name="filePath">The file to check.</param>
        /// <returns>Boolean - true if the caller has the required permissions and path contains the name of an existing file; otherwise, false.</returns>
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        ///     Gets the file name without extension.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The string returned by GetFileName, minus the last period (.) and all characters following it.</returns>
        public string GetFileNameWithoutExtension(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        /// <summary>
        ///     Gets the extension.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>he extension of the specified path (including the period "."), or Nothing, or String.Empty. If path is Nothing, GetExtension returns Nothing. If path does not have extension information, GetExtension returns String.Empty.</returns>
        public string GetExtension(string filePath)
        {
            return Path.GetExtension(filePath);
        }

        /// <summary>
        ///     Gets the name of the directory.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>Directory information for path, or Nothing if path denotes a root directory or is null. Returns String.Empty if path does not contain directory information.</returns>
        public string GetDirectoryName(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }

        /// <summary>
        ///     Combines strings into a path.
        /// </summary>
        /// <param name="leftItem">The first path to combine. </param>
        /// <param name="rightItems">string array of all other paths to combine.</param>
        /// <returns>The combined paths.</returns>
        public string PathCombine(string leftItem, params string[] rightItems)
        {
            var combinedPath = leftItem;
            foreach (var rightItem in rightItems)
            {
                combinedPath = Path.Combine(combinedPath, rightItem);
            }

            return combinedPath;
        }

        /// <summary>
        ///     Moves a specified file to a new location, providing the option to specify a new file name.
        /// </summary>
        /// <param name="filePath">The name of the file to move. </param>
        /// <param name="newFilePath">The new path for the file. </param>
        public void FileMove(string filePath, string newFilePath)
        {
            File.Move(filePath, newFilePath);
        }

        /// <summary>
        ///     Copies an existing file to a new file. Overwriting a file of the same name is allowed.
        /// </summary>
        /// <param name="sourceFilePath">The source file path. Teh File to copy.</param>
        /// <param name="destFilePath">The dest file path.</param>
        /// <param name="overWriteExisting">true if the destination file can be overwritten; otherwise, false.</param>
        public void FileCopy(string sourceFilePath, string destFilePath, bool overWriteExisting)
        {
            File.Copy(sourceFilePath, destFilePath, overWriteExisting);
        }

        /// <summary>
        ///     Deletes the specified file.
        /// </summary>
        /// <param name="filePath">The name of the file to be deleted. Wildcard characters are not supported.</param>
        public void FileDelete(string filePath)
        {
            if (FileExists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        ///     Determines whether the given path refers to an existing directory on disk.
        /// </summary>
        /// <param name="path">The path to test.</param>
        /// <returns>True if path refers to an existing directory; otherwise, false.</returns>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        ///     Creates all directories and subdirectories in the specified path.
        /// </summary>
        /// <param name="path">The directory path to create. </param>
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <summary>
        ///     Creates all directories and subdirectories in the specified path if they have not already been created.
        /// </summary>
        /// <param name="path">The directory path to create.</param>
        public void CreateDirectoryIfNotExists(string path)
        {
            if (!DirectoryExists(path))
            {
                CreateDirectory(path);
            }
        }

        /// <summary>
        ///     Gets the size of the file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The size, in bytes, of the current file.</returns>
        public long GetFileSize(string filePath)
        {
            return new FileInfo(filePath).Length;
        }

        /// <summary>
        ///     Gets the names of subdirectories (including their paths) in the specified directory.
        /// </summary>
        /// <param name="directory">The path for which an array of subdirectory names is returned. </param>
        /// <returns>An array of the names of subdirectories in "directory".</returns>
        public string[] GetDirectories(string directory)
        {
            return Directory.GetDirectories(directory);
        }

        /// <summary>
        ///     Returns the file name and extension of the specified path string.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The characters after the last directory character in path. If the last character of path is a directory or volume separator character, this method returns String.Empty. If path is Nothing, this method returns Nothing.</returns>
        public string GetFileName(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        /// <summary>
        ///     Gets the file mod date.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>the modification date of the specified file.</returns>
        public DateTime GetFileModDate(string filePath)
        {
            return new FileInfo(filePath).LastWriteTime;
        }
    }
}