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

namespace chocolatey.infrastructure.app
{
    using System;
    using System.Text.RegularExpressions;
    using adapters;
    using filesystem;
    using Environment = System.Environment;

    /// <summary>
    ///   Application constants and settings for the application
    /// </summary>
    public static class ApplicationParameters
    {
        private static readonly IFileSystem _fileSystem = new DotNetFileSystem();
        public static readonly string ChocolateyInstallEnvironmentVariableName = "ChocolateyInstall";
        public static readonly string Name = "Chocolatey";
#if DEBUG
        public static readonly string InstallLocation = _fileSystem.get_directory_name(_fileSystem.get_current_assembly_path());
#else
        public static readonly string InstallLocation = System.Environment.GetEnvironmentVariable(ChocolateyInstallEnvironmentVariableName) ?? _fileSystem.get_directory_name(_fileSystem.get_current_assembly_path());
#endif

        public static readonly string CommonAppDataChocolatey = _fileSystem.combine_paths(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), Name);
        public static readonly string LoggingLocation = _fileSystem.combine_paths(InstallLocation, "logs");
        public static readonly string LoggingFile = @"chocolatey.log";
        public static readonly string Log4NetConfigurationAssembly = @"chocolatey";
        public static readonly string Log4NetConfigurationResource = @"chocolatey.infrastructure.logging.log4net.config.xml";
        public static readonly string ChocolateyFileResources = "chocolatey.resources";
        public static readonly string ChocolateyConfigFileResource = @"chocolatey.infrastructure.app.configuration.chocolatey.config";
        public static readonly string GlobalConfigFileLocation = _fileSystem.combine_paths(InstallLocation, "config", "chocolatey.config");
        public static readonly string LicenseFileLocation = _fileSystem.combine_paths(InstallLocation, "license", "chocolatey.license.xml");
        public static readonly string PackageNamesSeparator = ";";
        public static readonly string OfficialChocolateyPublicKey = "79d02ea9cad655eb";

        public static string PackagesLocation = _fileSystem.combine_paths(InstallLocation, "lib");
        public static readonly string PackageFailuresLocation = _fileSystem.combine_paths(InstallLocation, "lib-bad");
        public static readonly string PackageBackupLocation = _fileSystem.combine_paths(InstallLocation, "lib-bkp");
        public static readonly string ShimsLocation = _fileSystem.combine_paths(InstallLocation, "bin");
        public static readonly string ChocolateyPackageInfoStoreLocation = _fileSystem.combine_paths(InstallLocation, ".chocolatey");
        public static readonly string ExtensionsLocation = _fileSystem.combine_paths(InstallLocation, "extensions");
        public static readonly string ChocolateyCommunityFeedPushSource = "https://chocolatey.org/";
        public static readonly string ChocolateyCommunityFeedSource = "https://chocolatey.org/api/v2/";
        public static readonly string UserAgent = "Chocolatey Command Line";
        public static readonly string RegistryValueInstallLocation = "InstallLocation";
        public static readonly string AllPackages = "all";

        public static class Environment
        {
            public static readonly string Path = "Path";
            public static readonly string PathExtensions = "PATHEXT";
            public static readonly string PathExtensionsSeparator = ";";
        }

        /// <summary>
        ///   Default is 45 minutes
        /// </summary>
        public static int DefaultWaitForExitInSeconds = 2700;

        public static readonly string[] ConfigFileExtensions = new string[] {".autoconf",".config",".conf",".cfg",".jsc",".json",".jsonp",".ini",".xml",".yaml"};
        public static readonly string ConfigFileTransformExtension = ".install.xdt";
        public static readonly string[] ShimDirectorFileExtensions = new string[] {".gui",".ignore"};
       
        public static readonly string HashProviderFileTooBig = "UnableToDetectChanges_FileTooBig";
        public static readonly string HashProviderFileLocked = "UnableToDetectChanges_FileLocked";

        public static class Tools
        {
            //public static readonly string WebPiCmdExe = _fileSystem.combine_paths(InstallLocation, "nuget.exe");
            public static readonly string ShimGenExe = _fileSystem.combine_paths(InstallLocation, "tools", "shimgen.exe");
        }

        public static class ConfigSettings
        {
            public static readonly string CacheLocation = "cacheLocation";
            public static readonly string ContainsLegacyPackageInstalls = "containsLegacyPackageInstalls";
            public static readonly string CommandExecutionTimeoutSeconds = "commandExecutionTimeoutSeconds";
            public static readonly string Proxy = "proxy";
            public static readonly string ProxyUser = "proxyUser";
            public static readonly string ProxyPassword = "proxyPassword";
        }
        
        public static class Features
        {
            public static readonly string CheckSumFiles = "checksumFiles";
            public static readonly string AutoUninstaller = "autoUninstaller";
            public static readonly string FailOnAutoUninstaller = "failOnAutoUninstaller";
            public static readonly string AllowGlobalConfirmation = "allowGlobalConfirmation";
            public static readonly string FailOnStandardError = "failOnStandardError";
        }

        public static class Messages
        {
            public static readonly string ContinueChocolateyAction = "Moving forward with chocolatey actions.";
            public static readonly string NugetEventActionHeader = "Nuget called an event";
        }

        private static T try_get_config<T>(Func<T> func, T defaultValue)
        {
            try
            {
                return func.Invoke();
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        ///// <summary>
        /////   Are we in Debug Mode?
        ///// </summary>
        //public static bool IsDebug
        //{
        //    get { return TryGetConfig(() => Config.GetConfigurationSettings().Debug, false); }
        //}
    }
}