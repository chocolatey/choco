// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.filesystem
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using adapters;
    using app;
    using platforms;
    using tolerance;
    using Assembly = adapters.Assembly;
    using Environment = adapters.Environment;

    /// <summary>
    ///   Implementation of IFileSystem for Dot Net
    /// </summary>
    /// <remarks>Normally we avoid regions, however this has so many methods that we are making an exception.</remarks>
    public sealed class DotNetFileSystem : IFileSystem
    {
        private readonly int TIMES_TO_TRY_OPERATION = 3;
        private static Lazy<IEnvironment> environment_initializer = new Lazy<IEnvironment>(() => new Environment());

        private void allow_retries(Action action)
        {
            FaultTolerance.retry(
                TIMES_TO_TRY_OPERATION,
                action,
                waitDurationMilliseconds: 200,
                increaseRetryByMilliseconds: 100);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void initialize_with(Lazy<IEnvironment> environment)
        {
            environment_initializer = environment;
        }
        
        private static IEnvironment Environment
        {
            get { return environment_initializer.Value; }
        }

        #region Path

        public string combine_paths(string leftItem, params string[] rightItems)
        {
            var combinedPath = Platform.get_platform() == PlatformType.Windows ? leftItem : leftItem.Replace('\\', '/');
            foreach (var rightItem in rightItems)
            {
                if (rightItem.Contains(":")) throw new ApplicationException("Cannot combine a path with ':' attempted to combine '{0}' with '{1}'".format_with(rightItem, combinedPath));
 
                var rightSide = Platform.get_platform() == PlatformType.Windows ? rightItem : rightItem.Replace('\\', '/');
                if (rightSide.StartsWith(Path.DirectorySeparatorChar.to_string()) || rightSide.StartsWith(Path.AltDirectorySeparatorChar.to_string()))
                {
                    combinedPath = Path.Combine(combinedPath, rightSide.Substring(1));
                }
                else
                {
                    combinedPath = Path.Combine(combinedPath, rightSide);
                }
            }

            return combinedPath;
        }

        public string get_full_path(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return path;

            return Path.GetFullPath(path);
        }

        public string get_temp_path()
        {
            return Path.GetTempPath();
        }

        public char get_path_directory_separator_char()
        {
            return Path.DirectorySeparatorChar;
        }

        public char get_path_separator()
        {
            return Path.PathSeparator;
        }

        public string get_executable_path(string executableName)
        {
            if (string.IsNullOrWhiteSpace(executableName)) return string.Empty;

            var isWindows = Platform.get_platform() == PlatformType.Windows;
            IList<string> extensions = new List<string>();

            if (get_file_name_without_extension(executableName).is_equal_to(executableName) && isWindows)
            {
                var pathExtensions = Environment.GetEnvironmentVariable(ApplicationParameters.Environment.PathExtensions).to_string().Split(new[] {ApplicationParameters.Environment.PathExtensionsSeparator}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var extension in pathExtensions.or_empty_list_if_null())
                {
                    extensions.Add(extension.StartsWith(".") ? extension : ".{0}".format_with(extension));
                }
            }

            // Always add empty, for when the executable name is enough.
            extensions.Add(string.Empty);

            // Gets the path to an executable based on looking in current 
            // working directory, next to the running process, then among the
            // derivatives of Path and Pathext variables, applied in order.
            var searchPaths = new List<string>();
            searchPaths.Add(get_current_directory());
            searchPaths.Add(get_directory_name(get_current_assembly_path()));
            searchPaths.AddRange(Environment.GetEnvironmentVariable(ApplicationParameters.Environment.Path).to_string().Split(new[] { get_path_separator() }, StringSplitOptions.RemoveEmptyEntries));

            foreach (var path in searchPaths.or_empty_list_if_null())
            {
                foreach (var extension in extensions.or_empty_list_if_null())
                {
                    var possiblePath = combine_paths(path, "{0}{1}".format_with(executableName, extension.to_lower()));
                    if (file_exists(possiblePath)) return possiblePath;
                }
            }

            // If not found, return the same as passed in - it may work, 
            // but possibly not.
            return executableName;
        }

        public string get_current_assembly_path()
        {
            return Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", string.Empty);
        }
    
        #endregion

        #region File

        public IEnumerable<string> get_files(string directoryPath, string pattern = "*.*", SearchOption option = SearchOption.TopDirectoryOnly)
        {
            return Directory.EnumerateFiles(directoryPath, pattern, option);
        }

        public IEnumerable<string> get_files(string directoryPath, string[] extensions, SearchOption option = SearchOption.TopDirectoryOnly)
        {
            return Directory.EnumerateFiles(directoryPath, "*.*", option)
                            .Where(f => extensions.Any(x => f.EndsWith(x, StringComparison.OrdinalIgnoreCase)));
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
            if (Platform.get_platform() == PlatformType.Windows) return Path.GetFileNameWithoutExtension(filePath);

            return Path.GetFileNameWithoutExtension(filePath.Replace('\\', '/'));
        }

        public string get_file_extension(string filePath)
        {
            if (Platform.get_platform() == PlatformType.Windows) return Path.GetExtension(filePath);

            return Path.GetExtension(filePath.Replace('\\', '/'));
        }

        public FileInfo get_file_info_for(string filePath)
        {
            return new FileInfo(filePath);
        }

        public System.DateTime get_file_modified_date(string filePath)
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
            allow_retries(() => File.Move(filePath, newFilePath));
            //Thread.Sleep(10);
        }

        public void copy_file(string sourceFilePath, string destinationFilePath, bool overwriteExisting)
        {
            this.Log().Debug(() => "Attempting to copy \"{0}\"{1} to \"{2}\".".format_with(sourceFilePath, Environment.NewLine, destinationFilePath));
            create_directory_if_not_exists(get_directory_name(destinationFilePath), ignoreError: true);

            allow_retries(() => File.Copy(sourceFilePath, destinationFilePath, overwriteExisting));
        }

        public bool copy_file_unsafe(string sourceFilePath, string destinationFilePath, bool overwriteExisting)
        {
            if (Platform.get_platform() != PlatformType.Windows)
            {
                copy_file(sourceFilePath, destinationFilePath, overwriteExisting);
                return true;
            }

            this.Log().Debug(() => "Attempting to copy from \"{0}\" to \"{1}\".".format_with(sourceFilePath, destinationFilePath));
            create_directory_if_not_exists(get_directory_name(destinationFilePath), ignoreError: true);

            //Private Declare Function apiCopyFile Lib "kernel32" Alias "CopyFileA" _
            int success = CopyFileW(sourceFilePath, destinationFilePath, overwriteExisting ? 0 : 1);
            //if (success == 0)
            //{
            //    var error = Marshal.GetLastWin32Error();
                
            //}
            return success != 0;
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

        [DllImport("kernel32", SetLastError = true)]
        private static extern int CopyFileW(string lpExistingFileName, string lpNewFileName, int bFailIfExists);

        // ReSharper restore InconsistentNaming

        public void delete_file(string filePath)
        {
            this.Log().Debug(() => "Attempting to delete file \"{0}\".".format_with(filePath));
            if (file_exists(filePath))
            {
                allow_retries(() => File.Delete(filePath));
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

        public byte[] read_file_bytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
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
            allow_retries(() =>
                {
                    using (FileStream fileStream = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                    using (var streamWriter = new StreamWriter(fileStream, encoding))
                    {
                        streamWriter.Write(fileText);
                        streamWriter.Flush();
                        streamWriter.Close();
                        fileStream.Close();
                    }
                });
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

        public IEnumerable<string> get_directories(string directoryPath)
        {
            if (!directory_exists(directoryPath)) return new List<string>();

            return Directory.EnumerateDirectories(directoryPath);
        }

        public IEnumerable<string> get_directories(string directoryPath, string pattern, SearchOption option = SearchOption.TopDirectoryOnly)
        {
            if (!directory_exists(directoryPath)) return new List<string>();

            return Directory.EnumerateDirectories(directoryPath, pattern, option);
        }

        public bool directory_exists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public string get_directory_name(string filePath)
        {
            if (Platform.get_platform() == PlatformType.Windows) return Path.GetDirectoryName(filePath);

            return Path.GetDirectoryName(filePath.Replace('\\', '/'));
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
            allow_retries(() => Directory.CreateDirectory(directoryPath));
        }

        public void move_directory(string directoryPath, string newDirectoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || string.IsNullOrWhiteSpace(newDirectoryPath)) throw new ApplicationException("You must provide a directory to move from or to.");
            if (combine_paths(directoryPath,"").is_equal_to(combine_paths(Environment.GetEnvironmentVariable("SystemDrive"),""))) throw new ApplicationException("Cannot move or delete the root of the system drive");

            try
            {
                this.Log().Debug("Moving '{0}'{1} to '{2}'".format_with(directoryPath, Environment.NewLine, newDirectoryPath));
                allow_retries(() => Directory.Move(directoryPath, newDirectoryPath));
            }
            catch (Exception ex)
            {
                this.Log().Warn("Move failed with message:{0} {1}{0} Attempting backup move method.".format_with(Environment.NewLine, ex.Message));

                create_directory_if_not_exists(newDirectoryPath, ignoreError: true);
                foreach (var file in get_files(directoryPath, "*.*", SearchOption.AllDirectories).or_empty_list_if_null())
                {
                    var destinationFile = file.Replace(directoryPath, newDirectoryPath);
                    if (file_exists(destinationFile)) delete_file(destinationFile);

                    create_directory_if_not_exists(get_directory_name(destinationFile), ignoreError: true);
                    this.Log().Debug("Moving '{0}'{1} to '{2}'".format_with(file, Environment.NewLine, destinationFile));
                    move_file(file, destinationFile);
                }

                Thread.Sleep(1000); // let the moving files finish up
                delete_directory_if_exists(directoryPath, recursive: true);
            }

            Thread.Sleep(2000); // sleep for enough time to allow the folder to be cleared
        }

        public void copy_directory(string sourceDirectoryPath, string destinationDirectoryPath, bool overwriteExisting)
        {
            create_directory_if_not_exists(destinationDirectoryPath, ignoreError: true);

            foreach (var file in get_files(sourceDirectoryPath, "*.*", SearchOption.AllDirectories).or_empty_list_if_null())
            {
                var destinationFile = file.Replace(sourceDirectoryPath, destinationDirectoryPath);
                create_directory_if_not_exists(get_directory_name(destinationFile), ignoreError: true);
                //this.Log().Debug("Copying '{0}' {1} to '{2}'".format_with(file, Environment.NewLine, destinationFile));
                copy_file(file, destinationFile, overwriteExisting);
            }

            Thread.Sleep(1500); // sleep for enough time to allow the folder to finish copying
        }

        public void create_directory_if_not_exists(string directoryPath)
        {
            create_directory_if_not_exists(directoryPath, false);
        }

        public void create_directory_if_not_exists(string directoryPath, bool ignoreError)
        {
            if (!directory_exists(directoryPath))
            {
                try
                {
                    create_directory(directoryPath);
                }
                catch (SystemException e)
                {
                    if (!ignoreError)
                    {
                        this.Log().Error("Cannot create directory \"{0}\". Error was:{1}{2}", get_full_path(directoryPath), Environment.NewLine, e);
                        throw;
                    }
                }
            }
        }

        public void delete_directory(string directoryPath, bool recursive)
        {
            if (string.IsNullOrWhiteSpace(directoryPath)) throw new ApplicationException("You must provide a directory to delete.");
            if (combine_paths(directoryPath, "").is_equal_to(combine_paths(Environment.GetEnvironmentVariable("SystemDrive"), ""))) throw new ApplicationException("Cannot move or delete the root of the system drive");
           
            this.Log().Debug(() => "Attempting to delete directory \"{0}\".".format_with(get_full_path(directoryPath)));
            allow_retries(() => Directory.Delete(directoryPath, recursive));
        }

        public void delete_directory_if_exists(string directoryPath, bool recursive)
        {
            if (directory_exists(directoryPath))
            {
                delete_directory(directoryPath, recursive);
            }
        }

        #endregion

        public void ensure_file_attribute_set(string path, FileAttributes attributes)
        {
            if (directory_exists(path))
            {
                var directoryInfo = get_directory_info_for(path);
                if ((directoryInfo.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                {
                    directoryInfo.Attributes |= FileAttributes.Hidden;
                }
            }
            if (file_exists(path))
            {
                var fileInfo = get_file_info_for(path);
                if ((fileInfo.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                {
                    fileInfo.Attributes |= FileAttributes.Hidden;
                }
            }
        }

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