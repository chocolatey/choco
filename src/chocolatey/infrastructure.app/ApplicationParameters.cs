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

namespace chocolatey.infrastructure.app
{
    using System;
    using System.Security.Principal;
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
        public static readonly string LicensedAssemblyLocation = _fileSystem.file_exists(_fileSystem.combine_paths(InstallLocation, "chocolatey.licensed.dll")) ? _fileSystem.combine_paths(InstallLocation, "chocolatey.licensed.dll") : _fileSystem.combine_paths(InstallLocation, "extensions", "chocolatey", "chocolatey.licensed.dll");

#else
        public static readonly string InstallLocation = System.Environment.GetEnvironmentVariable(ChocolateyInstallEnvironmentVariableName) ?? _fileSystem.get_directory_name(_fileSystem.get_current_assembly_path());
        public static readonly string LicensedAssemblyLocation = _fileSystem.combine_paths(InstallLocation, "extensions", "chocolatey", "chocolatey.licensed.dll");
#endif

        public static readonly string CommonAppDataChocolatey = _fileSystem.combine_paths(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), Name);
        public static readonly string LoggingLocation = _fileSystem.combine_paths(InstallLocation, "logs");
        public static readonly string LoggingFile = @"chocolatey.log";
        public static readonly string LoggingSummaryFile = @"choco.summary.log";
        public static readonly string Log4NetConfigurationAssembly = @"chocolatey";
        public static string Log4NetConfigurationResource = @"chocolatey.infrastructure.logging.log4net.config.xml";
        public static readonly string ChocolateyFileResources = "chocolatey.resources";
        public static readonly string ChocolateyConfigFileResource = @"chocolatey.infrastructure.app.configuration.chocolatey.config";
        public static readonly string GlobalConfigFileLocation = _fileSystem.combine_paths(InstallLocation, "config", "chocolatey.config");
        public static readonly string LicenseFileLocation = _fileSystem.combine_paths(InstallLocation, "license", "chocolatey.license.xml");
        public static readonly string UserProfilePath = !string.IsNullOrWhiteSpace(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile, System.Environment.SpecialFolderOption.DoNotVerify)) ?
              System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile, System.Environment.SpecialFolderOption.DoNotVerify)
            : CommonAppDataChocolatey;
        public static readonly string UserLicenseFileLocation = _fileSystem.combine_paths(UserProfilePath, "chocolatey.license.xml");
        public static readonly string LicensedChocolateyAssemblySimpleName = "chocolatey.licensed";
        public static readonly string LicensedComponentRegistry = @"chocolatey.licensed.infrastructure.app.registration.ContainerBinding";
        public static readonly string LicensedConfigurationBuilder = @"chocolatey.licensed.infrastructure.app.builders.ConfigurationBuilder";
        public static readonly string LicensedEnvironmentSettings = @"chocolatey.licensed.infrastructure.app.configuration.EnvironmentSettings";
        public static readonly string PackageNamesSeparator = ";";
        public static readonly string OfficialChocolateyPublicKey = "79d02ea9cad655eb";

        public static string PackagesLocation = _fileSystem.combine_paths(InstallLocation, "lib");
        public static readonly string PackageFailuresLocation = _fileSystem.combine_paths(InstallLocation, "lib-bad");
        public static readonly string PackageBackupLocation = _fileSystem.combine_paths(InstallLocation, "lib-bkp");
        public static readonly string ShimsLocation = _fileSystem.combine_paths(InstallLocation, "bin");
        public static readonly string ChocolateyPackageInfoStoreLocation = _fileSystem.combine_paths(InstallLocation, ".chocolatey");
        public static readonly string ExtensionsLocation = _fileSystem.combine_paths(InstallLocation, "extensions");
        public static readonly string TemplatesLocation = _fileSystem.combine_paths(InstallLocation, "templates");
        public static readonly string ChocolateyCommunityFeedPushSourceOld = "https://chocolatey.org/";
        public static readonly string ChocolateyCommunityFeedPushSource = "https://push.chocolatey.org/";
        public static readonly string ChocolateyCommunityFeedSource = "https://chocolatey.org/api/v2/";
        public static readonly string ChocolateyLicensedFeedSource = "https://licensedpackages.chocolatey.org/api/v2/";
        public static readonly string ChocolateyLicensedFeedSourceName = "chocolatey.licensed";
        public static readonly string UserAgent = "Chocolatey Command Line";
        public static readonly string RegistryValueInstallLocation = "InstallLocation";
        public static readonly string AllPackages = "all";
        public static readonly string PowerShellModulePathProcessProgramFiles = _fileSystem.combine_paths(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "WindowsPowerShell\\Modules");
        public static readonly string PowerShellModulePathProcessDocuments = _fileSystem.combine_paths(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "WindowsPowerShell\\Modules");
        public static readonly string LocalSystemSidString = "S-1-5-18";
        public static readonly SecurityIdentifier LocalSystemSid = new SecurityIdentifier(LocalSystemSidString);

        public static class Environment
        {
            public static readonly string Path = "Path";
            public static readonly string PathExtensions = "PATHEXT";
            public static readonly string PsModulePath = "PSModulePath";
            public static readonly string Temp = "TEMP";
            public static readonly string SystemUserName = "SYSTEM";
            public static readonly string Username = "USERNAME";
            public static readonly string ProcessorArchitecture = "PROCESSOR_ARCHITECTURE";
            public const string ARM64_PROCESSOR_ARCHITECTURE = "ARM64";
            public static readonly string EnvironmentSeparator = ";";

            public static readonly string ChocolateyToolsLocation = "ChocolateyToolsLocation";
            public static readonly string ChocolateyPackageInstallLocation = "ChocolateyPackageInstallLocation";
            public static readonly string ChocolateyPackageInstallerType = "ChocolateyInstallerType";
            public static readonly string ChocolateyPackageExitCode = "ChocolateyExitCode";
            public static readonly string ChocolateyIgnoreChecksums = "ChocolateyIgnoreChecksums";
            public static readonly string ChocolateyAllowEmptyChecksums = "ChocolateyAllowEmptyChecksums";
            public static readonly string ChocolateyAllowEmptyChecksumsSecure = "ChocolateyAllowEmptyChecksumsSecure";
            public static readonly string ChocolateyCheckLastExitCode = "ChocolateyCheckLastExitCode";
            public static readonly string ChocolateyPowerShellHost = "ChocolateyPowerShellHost";
            public static readonly string ChocolateyForce = "ChocolateyForce";
        }

        /// <summary>
        ///   Default is 45 minutes
        /// </summary>
        public static int DefaultWaitForExitInSeconds = 2700;
        public static int DefaultWebRequestTimeoutInSeconds = 30;

        public static readonly string[] ConfigFileExtensions = new string[] {".autoconf",".config",".conf",".cfg",".jsc",".json",".jsonp",".ini",".xml",".yaml"};
        public static readonly string ConfigFileTransformExtension = ".install.xdt";
        public static readonly string[] ShimDirectorFileExtensions = new string[] {".gui",".ignore"};

        public static readonly string HashProviderFileTooBig = "UnableToDetectChanges_FileTooBig";
        public static readonly string HashProviderFileLocked = "UnableToDetectChanges_FileLocked";

        /// <summary>
        /// This is a readonly bool set to true. It is only shifted for specs.
        /// </summary>
        public static readonly bool LockTransactionalInstallFiles = true;
        public static readonly string PackagePendingFileName = ".chocolateyPending";

        /// <summary>
        /// This is a readonly bool set to true. It is only shifted for specs.
        /// </summary>
        public static readonly bool AllowPrompts = true;

        public static class ExitCodes
        {
            public static readonly int ErrorFailNoActionReboot = 350;
            public static readonly int ErrorInstallSuspend = 1604;
        }

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
            public static readonly string ProxyBypassList = "proxyBypassList";
            public static readonly string ProxyBypassOnLocal = "proxyBypassOnLocal";
            public static readonly string WebRequestTimeoutSeconds = "webRequestTimeoutSeconds";
        }

        public static class Features
        {
            public static readonly string ChecksumFiles = "checksumFiles";
            public static readonly string AllowEmptyChecksums = "allowEmptyChecksums";
            public static readonly string AllowEmptyChecksumsSecure = "allowEmptyChecksumsSecure";
            public static readonly string AutoUninstaller = "autoUninstaller";
            public static readonly string FailOnAutoUninstaller = "failOnAutoUninstaller";
            public static readonly string AllowGlobalConfirmation = "allowGlobalConfirmation";
            public static readonly string FailOnStandardError = "failOnStandardError";
            public static readonly string UsePowerShellHost = "powershellHost";
            public static readonly string LogEnvironmentValues = "logEnvironmentValues";
            public static readonly string VirusCheck = "virusCheck";
            public static readonly string FailOnInvalidOrMissingLicense = "failOnInvalidOrMissingLicense";
            public static readonly string IgnoreInvalidOptionsSwitches = "ignoreInvalidOptionsSwitches";
            public static readonly string UsePackageExitCodes = "usePackageExitCodes";
            public static readonly string UseEnhancedExitCodes = "useEnhancedExitCodes";
            public static readonly string UseFipsCompliantChecksums = "useFipsCompliantChecksums";
            public static readonly string ScriptsCheckLastExitCode = "scriptsCheckLastExitCode";
            public static readonly string ShowNonElevatedWarnings = "showNonElevatedWarnings";
            public static readonly string ShowDownloadProgress = "showDownloadProgress";
            public static readonly string StopOnFirstPackageFailure = "stopOnFirstPackageFailure";
            public static readonly string UseRememberedArgumentsForUpgrades = "useRememberedArgumentsForUpgrades";
            public static readonly string IgnoreUnfoundPackagesOnUpgradeOutdated = "ignoreUnfoundPackagesOnUpgradeOutdated";
            public static readonly string SkipPackageUpgradesWhenNotInstalled = "skipPackageUpgradesWhenNotInstalled";
            public static readonly string RemovePackageInformationOnUninstall = "removePackageInformationOnUninstall";
            public static readonly string LogWithoutColor = "logWithoutColor";
            public static readonly string ExitOnRebootDetected = "exitOnRebootDetected";
            public static readonly string LogValidationResultsOnWarnings = "logValidationResultsOnWarnings";
            public static readonly string UsePackageRepositoryOptimizations = "usePackageRepositoryOptimizations";
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

        public static bool is_debug_mode_cli_primitive()
        {
            var args = System.Environment.GetCommandLineArgs();
            var isDebug = false;
            // no access to the good stuff here, need to go a bit primitive in parsing args
            foreach (var arg in args.or_empty_list_if_null())
            {
                if (arg.contains("-debug") || arg.is_equal_to("-d") || arg.is_equal_to("/d"))
                {
                    isDebug = true;
                    break;
                }
            }

            return isDebug;
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