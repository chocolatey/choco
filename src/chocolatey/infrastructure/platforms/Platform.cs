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

namespace chocolatey.infrastructure.platforms
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using adapters;
    using filesystem;
    using Environment = adapters.Environment;

    /// <summary>
    ///   OS Platform detection
    /// </summary>
    /// <remarks>
    ///   Based on http://stackoverflow.com/questions/10138040/how-to-detect-properly-windows-linux-mac-operating-systems
    /// </remarks>
    public static class Platform
    {
        private static Lazy<IEnvironment> environment_initializer = new Lazy<IEnvironment>(() => new Environment());
        private static Lazy<IFileSystem> file_system_initializer = new Lazy<IFileSystem>(() => new DotNetFileSystem());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IEnvironment> environment, Lazy<IFileSystem> file_system)
        {
            environment_initializer = environment;
            file_system_initializer = file_system;
        }

        private static IFileSystem file_system
        {
            get { return file_system_initializer.Value; }
        }

        private static IEnvironment Environment
        {
            get { return environment_initializer.Value; }
        }

        public static PlatformType get_platform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return PlatformType.Mac;

                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (file_system.directory_exists("/Applications")
                        & file_system.directory_exists("/System")
                        & file_system.directory_exists("/Users")
                        & file_system.directory_exists("/Volumes"))
                        return PlatformType.Mac;
                    else
                        return PlatformType.Linux;
                default:
                    return PlatformType.Windows;
            }
        }

        public static Version get_version()
        {
            return Environment.OSVersion.Version;
        }

        public static string get_name()
        {
            switch (get_platform())
            {
                case PlatformType.Linux:
                    return "Linux";
                case PlatformType.Mac:
                    return "OS X";
                case PlatformType.Windows:
                    return get_windows_name(get_version());
                default:
                    return "";
            }
        }


        /// <summary>
        ///   Gets the name of the Windows version
        /// </summary>
        /// <param name="version">The version.</param>
        /// <remarks>Looked at http://www.csharp411.com/determine-windows-version-and-edition-with-c/</remarks>
        private static string get_windows_name(Version version)
        {
            var name = "Windows";
            var isServer = false;

            var osVersionInfo = new OSVERSIONINFOEX();

            osVersionInfo.dwOSVersionInfoSize = Marshal.SizeOf(typeof (OSVERSIONINFOEX));
            var success = GetVersionEx(ref osVersionInfo);
            if (success)
            {
                isServer = osVersionInfo.wProductType == ServerNT;
            }


            //https://msdn.microsoft.com/en-us/library/windows/desktop/ms724832.aspx
            //switch doesn't like a double, but a string is fine?!
            string majorMinor = version.Major + "." + version.Minor;
            switch (majorMinor)
            {
                case "10.0":
                    name = isServer ? "Windows Server 2016" : "Windows 10";
                    break;
                case "6.4":
                    name = isServer ? "Windows Server 2016" : "Windows 10";
                    break;
                case "6.3":
                    name = isServer ? "Windows Server 2012 R2" : "Windows 8.1";
                    break;
                case "6.2":
                    name = isServer ? "Windows Server 2012" : "Windows 8";
                    break;
                case "6.1":
                    name = isServer ? "Windows Server 2008 R2" : "Windows 7";
                    break;
                case "6.0":
                    name = isServer ? "Windows Server 2008" : "Windows Vista";
                    break;
                case "5.2":
                    name = isServer ? "Windows Server 2003" : "Windows XP";
                    break;
                case "5.1":
                    name = "Windows XP";
                    break;
                case "5.0":
                    name = "Windows 2000";
                    break;
            }

            return name;
        }

        // ReSharper disable InconsistentNaming

        private const int ServerNT = 3;

        /*
         https://msdn.microsoft.com/en-us/library/windows/desktop/ms724833.aspx
         
         typedef struct _OSVERSIONINFOEX {
            DWORD dwOSVersionInfoSize;
            DWORD dwMajorVersion;
            DWORD dwMinorVersion;
            DWORD dwBuildNumber;
            DWORD dwPlatformId;
            TCHAR szCSDVersion[128];
            WORD  wServicePackMajor;
            WORD  wServicePackMinor;
            WORD  wSuiteMask;
            BYTE  wProductType;
            BYTE  wReserved;
         } OSVERSIONINFOEX, *POSVERSIONINFOEX, *LPOSVERSIONINFOEX;
         */

        // ReSharper disable MemberCanBePrivate.Local
        // ReSharper disable FieldCanBeMadeReadOnly.Local

        [StructLayout(LayoutKind.Sequential)]
        private struct OSVERSIONINFOEX
        {
            public int dwOSVersionInfoSize;
            public int dwMajorVersion;
            public int dwMinorVersion;
            public int dwBuildNumber;
            public int dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string szCSDVersion;
            public short wServicePackMajor;
            public short wServicePackMinor;
            public short wSuiteMask;
            public byte wProductType;
            public byte wReserved;
        }

        // ReSharper restore FieldCanBeMadeReadOnly.Local
        // ReSharper restore MemberCanBePrivate.Local

        /*
         https://msdn.microsoft.com/en-us/library/windows/desktop/ms724451.aspx
         BOOL WINAPI GetVersionEx(
            _Inout_  LPOSVERSIONINFO lpVersionInfo
         );
         */

        [DllImport("kernel32.dll")]
        private static extern bool GetVersionEx(ref OSVERSIONINFOEX osVersionInfo);

        // ReSharper restore InconsistentNaming
    }
}