using System.Diagnostics;
using System.Linq;
using System.Text;

namespace chocolatey.infrastructure.filesystem
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Runtime.InteropServices;
    using logging;

    /// <summary>
    /// All file system access code comes through here
    /// </summary>
    public sealed class DotNetFileSystemService
    {
        #region File

        /// <summary>
        /// Determines if a file exists
        /// </summary>
        /// <param name="file_path">Path to the file</param>
        /// <returns>True if there is a file already existing, otherwise false</returns>
        public bool file_exists(string file_path)
        {
            return File.Exists(file_path);
        }

        /// <summary>
        /// Creates a file
        /// </summary>
        /// <param name="file_path">Path to the file name</param>
        /// <returns>A file stream object for use after creating the file</returns>
        public FileStream create_file(string file_path)
        {
            return new FileStream(file_path, FileMode.OpenOrCreate);
        }

        /// <summary>
        /// Opens a file
        /// </summary>
        /// <param name="file_path">Path to the file name</param>
        /// <returns>A file stream object for use after accessing the file</returns>
        public FileStream open_file_in_read_mode_from(string file_path)
        {
            return File.OpenRead(file_path);
        }

        /// <summary>
        /// Returns the contents of a file
        /// </summary>
        /// <param name="file_path">Path to the file name</param>
        /// <returns>A string of the file contents</returns>
        public string read_file_text(string file_path)
        {
            return File.ReadAllText(file_path, get_file_encoding(file_path));
        }

        public void write_file_text(string file_path, string file_text)
        {
            var encoding = file_exists(file_path) ? get_file_encoding(file_path) : Encoding.UTF8;
            write_file_text(file_path, file_text, encoding);
        }

        public void write_file_text(string file_path, string file_text, Encoding encoding)
        {
            File.WriteAllText(file_path, file_text, encoding);
        }

        /// <summary>
        /// Takes a guess at the file encoding by looking to see if it has a BOM
        /// </summary>
        /// <param name="file_path">Path to the file name</param>
        /// <returns>A best guess at the encoding of the file</returns>
        /// <remarks>http://www.west-wind.com/WebLog/posts/197245.aspx</remarks>
        public static Encoding get_file_encoding(string file_path)
        {
            // *** Use Default of Encoding.Default (Ansi CodePage)
            Encoding enc = Encoding.Default;

            // *** Detect byte order mark if any - otherwise assume default
            byte[] buffer = new byte[5];
            FileStream file = new FileStream(file_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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

        /// <summary>
        /// Copies a file from one directory to another
        /// </summary>
        /// <param name="source_file_name">Where is the file now?</param>
        /// <param name="destination_file_name">Where would you like it to go?</param>
        /// <param name="overwrite_the_existing_file">If there is an existing file already there, would you like to delete it?</param>
        public void file_copy(string source_file_name, string destination_file_name, bool overwrite_the_existing_file)
        {
            this.Log().Debug(() => "Attempting to copy from \"{0}\" to \"{1}\".".FormatWith(source_file_name, destination_file_name));
            File.Copy(source_file_name, destination_file_name, overwrite_the_existing_file);
        }

        /// <summary>
        /// Copies a file from one directory to another using PInvoke
        /// </summary>
        /// <param name="source_file_name">Where is the file now?</param>
        /// <param name="destination_file_name">Where would you like it to go?</param>
        /// <param name="overwrite_the_existing_file">If there is an existing file already there, would you like to delete it?</param>
        public void file_copy_unsafe(string source_file_name, string destination_file_name, bool overwrite_the_existing_file)
        {
            this.Log().Debug(() => "Attempting to copy from \"{0}\" to \"{1}\".".FormatWith(source_file_name, destination_file_name));
            //Private Declare Function apiCopyFile Lib "kernel32" Alias "CopyFileA" _
            int success = CopyFileA(source_file_name, destination_file_name, overwrite_the_existing_file ? 0 : 1);

            //File.Copy(source_file_name, destination_file_name, overwrite_the_existing_file);
        }

        [DllImport("kernel32")]
        private static extern int CopyFileA(string lpExistingFileName, string lpNewFileName, int bFailIfExists);


        /// <summary>
        /// Determines the file information given a path to an existing file
        /// </summary>
        /// <param name="file_path">Path to an existing file</param>
        /// <returns>FileInfo object</returns>
        public FileInfo get_file_info_from(string file_path)
        {
            return new FileInfo(file_path);
        }

        /// <summary>
        /// Determines the FileVersion of the file passed in
        /// </summary>
        /// <param name="file_path">Relative or full path to a file</param>
        /// <returns>A string representing the FileVersion of the passed in file</returns>
        public string get_file_version_from(string file_path)
        {
            return FileVersionInfo.GetVersionInfo(get_full_path(file_path)).FileVersion;
        }

        /// <summary>
        /// Determines if a file is a system file
        /// </summary>
        /// <param name="file">File to check</param>
        /// <returns>True if the file has the System attribute marked, otherwise false</returns>
        public bool is_system_file(FileInfo file)
        {
            bool is_system_file = ((file.Attributes & FileAttributes.System) == FileAttributes.System);
            if (!is_system_file)
            {
                //check the directory to be sure
                DirectoryInfo directory_info = get_directory_info_from(file.DirectoryName);
                is_system_file = ((directory_info.Attributes & FileAttributes.System) == FileAttributes.System);
                this.Log().Debug(() => "Is directory \"{0}\" a system directory? {1}".FormatWith(file.DirectoryName,
                                    is_system_file.to_string()));
            }
            else
            {
                this.Log().Debug(() => "File \"{0}\" is a system file.".FormatWith( file.FullName));
            }
            return is_system_file;
        }

        /// <summary>
        /// Determines if a file is encrypted or not
        /// </summary>
        /// <param name="file">File to check</param>
        /// <returns>True if the file has the Encrypted attribute marked, otherwise false</returns>
        public bool is_encrypted_file(FileInfo file)
        {
            bool is_encrypted = ((file.Attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted);
            this.Log().Debug(() => "Is file \"{0}\" an encrypted file? {1}".FormatWith(file.FullName, is_encrypted.to_string()));
            return is_encrypted;
        }

        /// <summary>
        /// Determines if a file has the same extension as in the list of types
        /// </summary>
        /// <param name="file_name">File to check</param>
        /// <param name="file_types">File types to check against, listed as file extensions</param>
        /// <returns>True if the file in question has a file type in the list</returns>
        public bool file_in_file_types(string file_name, string[] file_types)
        {
            if (Array.IndexOf(file_types, ".*") > -1 || Array.IndexOf(file_types, get_file_extension_from(file_name).to_lower()) > -1)
            {
                this.Log().Debug(() => "File \"{0}\" is in the approved file types of \"{1}\".".FormatWith(file_name,
                                    string.Join(";", file_types)));
                return true;
            }

            this.Log().Info(()=>"File \"{0}\" is not in the approved file types of \"{1}\".".FormatWith(file_name,string.Join(";", file_types)));
            return false;
        }

        /// <summary>
        /// Determines the older of the file dates, Creation Date or Modified Date
        /// </summary>
        /// <param name="file_path">File to analyze</param>
        /// <returns>The oldest date on the file</returns>
        public string get_file_date(string file_path)
        {
            FileInfo file = get_file_info_from(file_path);
            return file.CreationTime < file.LastWriteTime
                                  ? file.CreationTime.Date.ToString("yyyyMMdd")
                                  : file.LastWriteTime.Date.ToString("yyyyMMdd");
        }

        /// <summary>
        /// Determines the file name from the filepath
        /// </summary>
        /// <param name="file_path">Full path to file including file name</param>
        /// <returns>Returns only the file name from the filepath</returns>
        public string get_file_name_from(string file_path)
        {
            return Path.GetFileName(file_path);
        }

        /// <summary>
        /// Determines the file name from the filepath without the extension
        /// </summary>
        /// <param name="file_path">Full path to file including file name</param>
        /// <returns>Returns only the file name minus extensions from the filepath</returns>
        public string get_file_name_without_extension_from(string file_path)
        {
            return Path.GetFileNameWithoutExtension(file_path);
        }

        /// <summary>
        /// Determines the file extension for a given path to a file
        /// </summary>
        /// <param name="file_path">The file to find the extension for</param>
        /// <returns>The extension of the file.</returns>
        public string get_file_extension_from(string file_path)
        {
            return Path.GetExtension(file_path);
        }

        #endregion

        #region Directory

        /// <summary>
        /// Verifies a directory exists, if it doesn't, it creates a new directory at that location
        /// </summary>
        /// <param name="directory">Directory to verify exists</param>
        public void verify_or_create_directory(string directory)
        {
            if (!directory_exists(directory))
            {
                try
                {
                    create_directory(directory);
                }
                catch (SystemException e)
                {
                    this.Log().Error("Cannot create directory \"{0}\". Error was:{1}{2}",get_full_path(directory),Environment.NewLine, e);
                    throw;
                }
            }
            else
            {
                this.Log().Debug(() => "Directory \"{0}\" already exists".FormatWith(get_full_path(directory)));
            }
        }

        /// <summary>
        /// Determines the directory name for a given file path. Useful when working with relative files
        /// </summary>
        /// <param name="file_path">File to get the directory name from</param>
        /// <returns>Returns only the path to the directory name</returns>
        public string get_directory_name_from(string file_path)
        {
            return Path.GetDirectoryName(get_full_path(file_path));
        }

        /// <summary>
        /// Returns a DirectoryInfo object from a string
        /// </summary>
        /// <param name="directory">Full path to the directory you want the directory information for</param>
        /// <returns>DirectoryInfo object</returns>
        public DirectoryInfo get_directory_info_from(string directory)
        {
            return new DirectoryInfo(directory);
        }

        /// <summary>
        /// Returns a DirectoryInfo object from a string to a filepath
        /// </summary>
        /// <param name="file_path">Full path to the file you want directory information for</param>
        /// <returns>DirectoryInfo object</returns>
        public DirectoryInfo get_directory_info_from_file_path(string file_path)
        {
            return new DirectoryInfo(file_path).Parent;
        }

        /// <summary>
        /// Determines if a directory exists
        /// </summary>
        /// <param name="directory">Path to the directory</param>
        /// <returns>True if there is a directory already existing, otherwise false</returns>
        public bool directory_exists(string directory)
        {
            return Directory.Exists(directory);
        }

        /// <summary>
        /// Creates a directory
        /// </summary>
        /// <param name="directory">Path to the directory</param>
        /// <returns>A directory information object for use after creating the directory</returns>
        public DirectoryInfo create_directory(string directory)
        {
            this.Log().Debug(() => "Attempting to create directory \"{0}\".".FormatWith(get_full_path(directory)));
            return Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Deletes a directory
        /// </summary>
        /// <param name="directory">Path to the directory</param>
        /// <param name="recursive">Would you like to delete the directories inside of this directory? Almost always true.</param>
        public void delete_directory(string directory, bool recursive)
        {
            this.Log().Debug(() => "Attempting to delete directory \"{0}\".".FormatWith(get_full_path(directory)));
            Directory.Delete(directory, recursive);
        }

        /// <summary>
        /// Gets a list of directories inside of an existing directory
        /// </summary>
        /// <param name="directory">Directory to look for subdirectories in</param>
        /// <returns>A list of subdirectories inside of the existing directory</returns>
        public string[] get_all_directory_name_strings_in(string directory)
        {
            return Directory.GetDirectories(directory);
        }

        /// <summary>
        /// Gets a list of files inside of an existing directory
        /// </summary>
        /// <param name="directory">Path to the directory</param>
        /// <returns>A list of files inside of an existing directory</returns>
        public string[] get_all_file_name_strings_in(string directory)
        {
            return get_all_file_name_strings_in(directory, "*.*");
        }

        /// <summary>
        /// Gets a list of files inside of an existing directory
        /// </summary>
        /// <param name="directory">Path to the directory</param>
        /// <param name="pattern">Pattern or extension</param>
        /// <returns>A list of files inside of an existing directory</returns>
        public string[] get_all_file_name_strings_in(string directory, string pattern)
        {
            string[] returnList = Directory.GetFiles(directory, pattern);
            return returnList.OrderBy(get_file_name_from).ToArray();
        }

        /// <summary>
        /// Gets a list of all files inside of an existing directory, includes files in subdirectories also
        /// </summary>
        /// <param name="directory">Path to the directory</param>
        /// <param name="pattern">Pattern or extension</param>
        /// <returns>A list of files inside of an existing directory</returns>
        public string[] get_all_file_name_strings_recursively_in(string directory, string pattern)
        {
            // ArrayList will hold all file names
            ArrayList returnList = new ArrayList();

            // Create an array of filter string
            string[] MultipleFilters = pattern.Split('|');

            // for each filter find mathing file names
            foreach (string FileFilter in MultipleFilters)
            {
                // add found file names to array list
                returnList.AddRange(Directory.GetFiles(directory, FileFilter, SearchOption.AllDirectories));
            }

            // returns string array of relevant file names
            string[] returnFiles = (string[])returnList.ToArray(typeof(string));
            return returnFiles.OrderBy(get_file_name_from).ToArray();
        }

        #endregion

        /// <summary>
        /// Determines the full path to a given directory. Useful when working with relative directories
        /// </summary>
        /// <param name="path">Where to get the full path from</param>
        /// <returns>Returns the full path to the file or directory</returns>
        public string get_full_path(string path)
        {
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Combines a set of paths into one path
        /// </summary>
        /// <param name="paths">Each item in order from left to right of the path</param>
        /// <returns></returns>
        public string combine_paths(params string[] paths)
        {
            string combined_path = String.Empty;
            foreach (string path in paths)
            {
                combined_path = Path.Combine(combined_path, path);
            }

            return combined_path;
        }

    }
}