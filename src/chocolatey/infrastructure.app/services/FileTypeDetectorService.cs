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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using configuration;
    using filesystem;
    using infrastructure.commands;
    using platforms;

    public class FileTypeDetectorService : IFileTypeDetectorService
    {
        private readonly IFileSystem _fileSystem;

        private const int DIE_SHOWERRORS = 0x00000001;
        private const int DIE_SHOWOPTIONS = 0x00000002;
        private const int DIE_SHOWVERSION = 0x00000004;
        private const int DIE_SHOWENTROPY = 0x00000008;
        private const int DIE_SINGLELINEOUTPUT = 0x00000010;
        private const int DIE_SHOWFILEFORMATONCE = 0x00000020;

        public FileTypeDetectorService(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        //// http://stackoverflow.com/a/8861895/18475
        //// http://stackoverflow.com/a/2864714/18475
        //[DllImport("kernel32.dll", SetLastError = true)]
        //private static extern bool SetDllDirectory(string lpPathName);

        //public struct DieInternal
        //{
        //    [SuppressUnmanagedCodeSecurity]
        //    [DllImport("diedll", CallingConvention = CallingConvention.StdCall, EntryPoint = "_DIE_scanExW@20", SetLastError = true)]
        //    internal static extern int scanExW_0([MarshalAs(UnmanagedType.LPWStr)] string pwszFileName, sbyte[] pszOutBuffer, int nOutBufferSize, uint nFlags, [MarshalAs(UnmanagedType.LPWStr)] string pwszDataBase);
        //}


        //public string scan_file(string filePath)
        //{
        //    if (Platform.get_platform() != PlatformType.Windows)
        //    {
        //        this.Log().Debug("Unable to detect file types when not on Windows");
        //        return string.Empty;
        //    }
        //    try
        //    {
        //        var successPath = SetDllDirectory(dieDllLocation);
        //        if (!successPath)
        //        {
        //            "chocolatey".Log().Warn("Error during native SetDllDirectory call - {0}".format_with(Marshal.GetLastWin32Error()));
        //        }
        //        var outputBuffer = new sbyte[1024];
        //        int outputBufferSize = outputBuffer.Length;
        //        const uint flags = DIE_SINGLELINEOUTPUT;

        //        var success = DieInternal.scanExW_0(filePath, outputBuffer, outputBufferSize, flags, databaseLocation);
        //        if (success != 0)
        //        {
        //            "chocolatey".Log().Warn("Error during native _DIE_scanExW call - {0}".format_with(Marshal.GetLastWin32Error()));
        //        }

        //        byte[] outputBytes = Array.ConvertAll(outputBuffer, (a) => (byte)a);

        //        var output = Encoding.UTF8.GetString(outputBytes).to_string().Trim('\0');

        //        return output;
        //    }
        //    catch (Exception ex)
        //    {
        //        this.Log().Warn("Unable to detect type for '{0}':{1} {2}".format_with(_fileSystem.get_file_name(filePath), Environment.NewLine, ex.Message));
        //        return string.Empty;
        //    }
        //}


        public string scan_file(string filePath)
        {
            if (Platform.get_platform() != PlatformType.Windows)
            {
                this.Log().Debug("Unable to detect file types when not on Windows");
                return string.Empty;
            }

            // http://stackoverflow.com/q/5503951/18475
            // http://stackoverflow.com/a/10852827/18475
            // http://scottbilas.com/blog/automatically-choose-32-or-64-bit-mixed-mode-dlls/
            //todo: convert this to call the exe assembly instead. The dll is 32bit and won't work with AnyCPU

            filePath = _fileSystem.get_full_path(filePath);
            var dieDllLocation = _fileSystem.combine_paths(ApplicationParameters.InstallLocation, "tools", "detector");
            var diecPath = _fileSystem.combine_paths(ApplicationParameters.InstallLocation, "tools", "detector","diec.exe");
            var databaseLocation = _fileSystem.get_full_path(_fileSystem.combine_paths(ApplicationParameters.InstallLocation, "tools", "detector", "db"));
            
            this.Log().Debug("Attempting to detect type for '{0}' with db '{1}'".format_with(_fileSystem.get_file_name(filePath), databaseLocation));

            if (!_fileSystem.directory_exists(diecPath) || !_fileSystem.directory_exists(databaseLocation))
            {
                var dieZipLocation = _fileSystem.combine_paths(ApplicationParameters.InstallLocation, "tools", "detector.zip");
                unzip_die_files(dieZipLocation, dieDllLocation);
            }
           
            if (!_fileSystem.file_exists(filePath))
            {
                this.Log().Warn("File not found at '{0}'. Unable to detect type for inexistent file.".format_with(filePath));
                return string.Empty;
            }

            try
            {
                //todo: detector databaselocation fails now - it's based on the current directory relative path
                //C:\\ProgramData\\chocolatey\\tools\\detector\\db
                var output = string.Empty;
               // var success = CommandExecutor.execute_static(diecPath, "\"{0}\" -singlelineoutput:yes -showoptions:no -showversion:no -database:\"{1}\"".format_with(filePath, databaseLocation), 
                var success = CommandExecutor.execute_static(diecPath, "\"{0}\"".format_with(filePath), 
                    60, 
                    _fileSystem.get_current_directory(), 
                    (s, e) =>
                    {
                        if (!string.IsNullOrWhiteSpace(e.Data)) output = e.Data;
                    },
                    (s, e) =>
                    {
                        this.Log().Warn("{0}".format_with(e.Data));
                    }, 
                    false, 
                    false);
               
                return output;
            }
            catch (Exception ex)
            {
                this.Log().Warn("Unable to detect type for '{0}':{1} {2}".format_with(_fileSystem.get_file_name(filePath), Environment.NewLine, ex.Message));
                return string.Empty;
            }
        }

        private void unzip_die_files(string zipPath, string extractDirectory)
        {
            //todo: replace with https://github.com/adamhathcock/sharpcompress
            var sevenZip = _fileSystem.combine_paths(ApplicationParameters.InstallLocation, "tools", "7za.exe");
            CommandExecutor.execute_static(sevenZip, "x -aoa -o\"{0}\" -y \"{1}\"".format_with(extractDirectory, zipPath), 30, _fileSystem.get_current_directory(), (s, e) => { }, (s, e) => { }, false, false);
        }
    }
}
