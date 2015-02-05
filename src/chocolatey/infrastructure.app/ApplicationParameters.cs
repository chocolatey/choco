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
        public static readonly string Name = "Chocolatey";
#if DEBUG
        public static readonly string InstallLocation = _fileSystem.get_directory_name(Assembly.GetExecutingAssembly().Location);
#else
        public static readonly string InstallLocation = Environment.GetEnvironmentVariable("ChocolateyInstall") ??  _fileSystem.get_directory_name(Assembly.GetExecutingAssembly().Location);
#endif

        public static readonly string CommonAppDataChocolatey = _fileSystem.combine_paths(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Name);
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
        public static string PackageFailuresLocation = _fileSystem.combine_paths(InstallLocation, "lib-bad");
        public static string ShimsLocation = _fileSystem.combine_paths(InstallLocation, "bin");
        public static string ChocolateyPackageInfoStoreLocation = _fileSystem.combine_paths(InstallLocation, ".chocolatey");
        public static readonly string ChocolateyCommunityFeedPushSource = "https://chocolatey.org/";
        public static readonly string UserAgent = "Chocolatey Command Line";
        public static readonly string RegistryValueInstallLocation = "InstallLocation";
        public static readonly string RollbackPackageSuffix = "._.previous";

        /// <summary>
        ///   Default is 45 minutes
        /// </summary>
        public static int DefaultWaitForExitInSeconds = 2700;

        public static class Tools
        {
            //public static readonly string WebPiCmdExe = _fileSystem.combine_paths(InstallLocation, "nuget.exe");
            public static readonly string ShimGenExe = _fileSystem.combine_paths(InstallLocation, "tools", "shimgen.exe");
        }

        public static class Features
        {
            public static readonly string CheckSumFiles = "checksumFiles";
            public static readonly string AutoUninstaller = "autoUninstaller";
            public static readonly string AllowInsecureConfirmation = "allowInsecureConfirmation";
        }

        public static class Messages
        {
            public static readonly string ContinueChocolateyAction = "Moving forward with chocolatey actions.";
            public static readonly string NugetEventActionHeader = "Nuget called an event";
        }

        public static class OutputParser
        {
            //todo: This becomes the WebPI parsing stuff instead
            public static class Nuget
            {
                public const string PACKAGE_NAME_GROUP = "PkgName";
                public const string PACKAGE_VERSION_GROUP = "PkgVersion";
                public static readonly Regex AlreadyInstalled = new Regex(@"already installed", RegexOptions.Compiled);
                public static readonly Regex NotInstalled = new Regex(@"not installed", RegexOptions.Compiled);
                public static readonly Regex Installing = new Regex(@"Installing", RegexOptions.Compiled);
                public static readonly Regex ResolvingDependency = new Regex(@"Attempting to resolve dependency", RegexOptions.Compiled);
                public static readonly Regex PackageName = new Regex(@"'(?<{0}>[.\S]+)\s?".format_with(PACKAGE_NAME_GROUP), RegexOptions.Compiled);
                public static readonly Regex PackageVersion = new Regex(@"(?<{0}>[\d\.]+[\-\w]*)[[)]?'".format_with(PACKAGE_VERSION_GROUP), RegexOptions.Compiled);
            }
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