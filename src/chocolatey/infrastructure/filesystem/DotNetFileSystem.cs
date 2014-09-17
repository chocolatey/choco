namespace chocolatey.infrastructure.filesystem
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    ///   Implementation of IFileSystem for Dot Net
    /// </summary>
    /// <remarks>Normally we avoid regions, however this has so many methods that we are making an exception.</remarks>
    public sealed class DotNetFileSystem : IFileSystem
    {
        #region Path

        public string combine_paths(string leftItem, params string[] rightItems)
        {
            var combinedPath = leftItem;
            foreach (var rightItem in rightItems)
            {
                combinedPath = Path.Combine(combinedPath, rightItem);
            }

            return combinedPath;
        }

        public string get_full_path(string path)
        {
            return Path.GetFullPath(path);
        }

        public string get_temp_path()
        {
            return Path.GetTempPath();
        }

        #endregion

        #region File

        public IList<string> get_files(string directoryPath, string pattern = "*.*", SearchOption option = SearchOption.TopDirectoryOnly)
        {
            return Directory.GetFiles(directoryPath, pattern, option);
        }

        public bool file_exists(string filePath)
        {
            return File.Exists(filePath);
        }

        public string get_file_name(string filePath)
        {
            return Path.GetFileName(filePath);
        }

        public string get_file_name_without_extension(string filePath)
        {
            return Path.GetFileNameWithoutExtension(filePath);
        }

        public string get_file_extension(string filePath)
        {
            return Path.GetExtension(filePath);
        }

        public FileInfo get_file_info_for(string filePath)
        {
            return new FileInfo(filePath);
        }

        public DateTime get_file_modified_date(string filePath)
        {
            return new FileInfo(filePath).LastWriteTime;
        }

        public long get_file_size(string filePath)
        {
            return new FileInfo(filePath).Length;
        }

        public string get_file_version_for(string filePath)
        {
            return FileVersionInfo.GetVersionInfo(get_full_path(filePath)).FileVersion;
        }

        public bool is_system_file(FileInfo file)
        {
            bool isSystemFile = ((file.Attributes & FileAttributes.System) == FileAttributes.System);
            if (!isSystemFile)
            {
                //check the directory to be sure
                DirectoryInfo directoryInfo = get_directory_info_for(file.DirectoryName);
                isSystemFile = ((directoryInfo.Attributes & FileAttributes.System) == FileAttributes.System);
                this.Log().Debug(() => "Is directory \"{0}\" a system directory? {1}".format_with(file.DirectoryName, isSystemFile.to_string()));
            }
            else
            {
                this.Log().Debug(() => "File \"{0}\" is a system file.".format_with(file.FullName));
            }

            return isSystemFile;
        }

        public bool is_encrypted_file(FileInfo file)
        {
            bool isEncrypted = ((file.Attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted);
            this.Log().Debug(() => "Is file \"{0}\" an encrypted file? {1}".format_with(file.FullName, isEncrypted.to_string()));
            return isEncrypted;
        }

        public string get_file_date(FileInfo file)
        {
            return file.CreationTime < file.LastWriteTime
                       ? file.CreationTime.Date.ToString("yyyyMMdd")
                       : file.LastWriteTime.Date.ToString("yyyyMMdd");
        }

        public void move_file(string filePath, string newFilePath)
        {
            File.Move(filePath, newFilePath);
        }

        public void copy_file(string sourceFilePath, string destFilePath, bool overWriteExisting)
        {
            this.Log().Debug(() => "Attempting to copy from \"{0}\" to \"{1}\".".format_with(sourceFilePath, destFilePath));
            File.Copy(sourceFilePath, destFilePath, overWriteExisting);
        }

        public bool copy_file_unsafe(string sourceFileName, string destinationFileName, bool overwriteTheExistingFile)
        {
            this.Log().Debug(() => "Attempting to copy from \"{0}\" to \"{1}\".".format_with(sourceFileName, destinationFileName));
            //Private Declare Function apiCopyFile Lib "kernel32" Alias "CopyFileA" _
            int success = CopyFileA(sourceFileName, destinationFileName, overwriteTheExistingFile ? 0 : 1);
            return success == 0;
        }

        // ReSharper disable InconsistentNaming

        // http://msdn.microsoft.com/en-us/library/windows/desktop/aa363851.aspx
        // http://www.pinvoke.net/default.aspx/kernel32.copyfile
        /*
            BOOL WINAPI CopyFile(
              _In_  LPCTSTR lpExistingFileName,
              _In_  LPCTSTR lpNewFileName,
              _In_  BOOL bFailIfExists
            );
         */
        [DllImport("kernel32")]
        private static extern int CopyFileA(string lpExistingFileName, string lpNewFileName, int bFailIfExists);

        // ReSharper restore InconsistentNaming

        public void delete_file(string filePath)
        {
            if (file_exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public FileStream create_file(string filePath)
        {
            return new FileStream(filePath, FileMode.OpenOrCreate);
        }

        public string read_file(string filePath)
        {
            return File.ReadAllText(filePath, get_file_encoding(filePath));
        }

        public FileStream open_file_readonly(string filePath)
        {
            return File.OpenRead(filePath);
        }

        public void write_file(string filePath, string fileText)
        {
            write_file(filePath, fileText, file_exists(filePath) ? get_file_encoding(filePath) : Encoding.UTF8);
        }

        public void write_file(string filePath, string fileText, Encoding encoding)
        {
            using (FileStream fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            using (var streamWriter = new StreamWriter(fileStream, encoding))
            {
                streamWriter.Write(fileText);
                streamWriter.Flush();
                streamWriter.Close();
                fileStream.Close();
            }
        }

        public void write_file(string filePath, Func<Stream> getStream)
        {
            using (Stream incomingStream = getStream())
            using (Stream fileStream = File.Create(filePath))
            {
                incomingStream.CopyTo(fileStream);
                fileStream.Close();
            }
        }

        #endregion

        #region Directory

        public string get_current_directory()
        {
            return Directory.GetCurrentDirectory();
        }

        public IList<string> get_directories(string directoryPath)
        {
            return Directory.GetDirectories(directoryPath);
        }

        public bool directory_exists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public string get_directory_name(string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }

        public DirectoryInfo get_directory_info_for(string directoryPath)
        {
            return new DirectoryInfo(directoryPath);
        }

        public DirectoryInfo get_directory_info_from_file_path(string filePath)
        {
            return new DirectoryInfo(filePath).Parent;
        }

        public void create_directory(string directoryPath)
        {
            this.Log().Debug(() => "Attempting to create directory \"{0}\".".format_with(get_full_path(directoryPath)));
            Directory.CreateDirectory(directoryPath);
        }

        public void create_directory_if_not_exists(string directoryPath)
        {
            if (!directory_exists(directoryPath))
            {
                try
                {
                    create_directory(directoryPath);
                }
                catch (SystemException e)
                {
                    this.Log().Error("Cannot create directory \"{0}\". Error was:{1}{2}", get_full_path(directoryPath), Environment.NewLine, e);
                    throw;
                }
            }
        }

        public void delete_directory(string directoryPath, bool recursive)
        {
            this.Log().Debug(() => "Attempting to delete directory \"{0}\".".format_with(get_full_path(directoryPath)));
            Directory.Delete(directoryPath, recursive);
        }

        public void delete_directory_if_exists(string directoryPath, bool recursive)
        {
            if (directory_exists(directoryPath))
            {
                delete_directory(directoryPath, recursive);
            }
        }

        #endregion

        /// <summary>
        ///   Takes a guess at the file encoding by looking to see if it has a BOM
        /// </summary>
        /// <param name="filePath">Path to the file name</param>
        /// <returns>A best guess at the encoding of the file</returns>
        /// <remarks>http://www.west-wind.com/WebLog/posts/197245.aspx</remarks>
        public static Encoding get_file_encoding(string filePath)
        {
            // *** Use Default of Encoding.Default (Ansi CodePage)
            Encoding enc = Encoding.Default;

            // *** Detect byte order mark if any - otherwise assume default
            var buffer = new byte[5];
            var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            file.Read(buffer, 0, 5);
            file.Close();

            if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                enc = Encoding.UTF8;
            else if (buffer[0] == 0xfe && buffer[1] == 0xff)
                enc = Encoding.Unicode;
            else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
                enc = Encoding.UTF32;
            else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
                enc = Encoding.UTF7;

            return enc;
        }
    }
}