// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
    using chocolatey.infrastructure.platforms;
    using chocolatey.infrastructure.information;

    /// <summary>
    ///   Application constants and settings for the application
    /// </summary>
    public static class ApplicationParameters
    {
        private static readonly IFileSystem _fileSystem = new DotNetFileSystem();
        public static readonly string ChocolateyInstallEnvironmentVariableName = "ChocolateyInstall";
        public static readonly string Name = "Chocolatey";

#if FORCE_CHOCOLATEY_OFFICIAL_KEY
        // always look at the official location of the machine installation
        public static readonly string InstallLocation = System.Environment.GetEnvironmentVariable(ChocolateyInstallEnvironmentVariableName) ?? _fileSystem.GetDirectoryName(_fileSystem.GetCurrentAssemblyPath());
        public static readonly string LicensedAssemblyLocation = _fileSystem.CombinePaths(InstallLocation, "extensions", "chocolatey", "chocolatey.licensed.dll");
#elif DEBUG
        // Install location is choco.exe or chocolatey.dll
        public static readonly string InstallLocation = _fileSystem.GetDirectoryName(_fileSystem.GetCurrentAssemblyPath());
        // when being used as a reference, start by looking next to Chocolatey, then in a subfolder.
        public static readonly string LicensedAssemblyLocation = _fileSystem.FileExists(_fileSystem.CombinePaths(InstallLocation, "chocolatey.licensed.dll")) ? _fileSystem.CombinePaths(InstallLocation, "chocolatey.licensed.dll") : _fileSystem.CombinePaths(InstallLocation, "extensions", "chocolatey", "chocolatey.licensed.dll");
#else
        // Install locations is Chocolatey.dll or choco.exe - In Release mode
        // we might be testing on a server or in the local debugger. Either way,
        // start from the assembly location and if unfound, head to the machine
        // locations instead. This is a merge of official and Debug modes.
        private static IAssembly _assemblyForLocation = Assembly.GetEntryAssembly().UnderlyingType != null ? Assembly.GetEntryAssembly() : Assembly.GetExecutingAssembly();
        public static readonly string InstallLocation = _fileSystem.FileExists(_fileSystem.CombinePaths(_fileSystem.GetDirectoryName(_assemblyForLocation.CodeBase.Replace(Platform.GetPlatform() == PlatformType.Windows ? "file:///" : "file://", string.Empty)), "chocolatey.dll")) ||
                                                        _fileSystem.FileExists(_fileSystem.CombinePaths(_fileSystem.GetDirectoryName(_assemblyForLocation.CodeBase.Replace(Platform.GetPlatform() == PlatformType.Windows ? "file:///" : "file://", string.Empty)), "choco.exe")) ?
                _fileSystem.GetDirectoryName(_assemblyForLocation.CodeBase.Replace(Platform.GetPlatform() == PlatformType.Windows ? "file:///" : "file://", string.Empty)) :
                !string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable(ChocolateyInstallEnvironmentVariableName)) ?
                    System.Environment.GetEnvironmentVariable(ChocolateyInstallEnvironmentVariableName) :
                    @"C:\ProgramData\Chocolatey"
            ;

        // when being used as a reference, start by looking next to Chocolatey, then in a subfolder.
        public static readonly string LicensedAssemblyLocation = _fileSystem.FileExists(_fileSystem.CombinePaths(InstallLocation, "chocolatey.licensed.dll")) ? _fileSystem.CombinePaths(InstallLocation, "chocolatey.licensed.dll") : _fileSystem.CombinePaths(InstallLocation, "extensions", "chocolatey", "chocolatey.licensed.dll");
#endif

        public static readonly string CommonAppDataChocolatey = _fileSystem.CombinePaths(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), Name);
        public static readonly string LoggingLocation = _fileSystem.CombinePaths(InstallLocation, "logs");
        public static readonly string LoggingFile = @"chocolatey.log";
        public static readonly string LoggingSummaryFile = @"choco.summary.log";
        public static readonly string Log4NetConfigurationAssembly = @"chocolatey";
        public static string Log4NetConfigurationResource = @"chocolatey.infrastructure.logging.log4net.config.xml";
        public static readonly string ChocolateyFileResources = "chocolatey.resources";
        public static readonly string ChocolateyConfigFileResource = @"chocolatey.infrastructure.app.configuration.chocolatey.config";
        public static readonly string GlobalConfigFileLocation = _fileSystem.CombinePaths(InstallLocation, "config", "chocolatey.config");
        public static readonly string LicenseFileLocation = _fileSystem.CombinePaths(InstallLocation, "license", "chocolatey.license.xml");
        public static readonly string UserProfilePath = !string.IsNullOrWhiteSpace(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile, System.Environment.SpecialFolderOption.DoNotVerify)) ?
              System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile, System.Environment.SpecialFolderOption.DoNotVerify)
            : CommonAppDataChocolatey;
        public static readonly string HttpCacheUserLocation = _fileSystem.CombinePaths(UserProfilePath, ".chocolatey", "http-cache");
        // CommonAppDataChocolatey is always set to ProgramData\Chocolatey.
        // So we append HttpCache to that name if it is possible.
        public static readonly string HttpCacheSystemLocation = CommonAppDataChocolatey + "HttpCache";
        public static readonly string HttpCacheLocation = GetHttpCacheLocation();

        public static readonly string UserLicenseFileLocation = _fileSystem.CombinePaths(UserProfilePath, "chocolatey.license.xml");
        public static readonly string LicensedChocolateyAssemblySimpleName = "chocolatey.licensed";
        public static readonly string LicensedComponentRegistry = @"chocolatey.licensed.infrastructure.app.registration.ContainerBinding";
        public static readonly string LicensedConfigurationBuilder = @"chocolatey.licensed.infrastructure.app.builders.ConfigurationBuilder";
        public static readonly string LicensedEnvironmentSettings = @"chocolatey.licensed.infrastructure.app.configuration.EnvironmentSettings";
        public static readonly string PackageNamesSeparator = ";";
        public static readonly string UnofficialChocolateyPublicKey = "fd112f53c3ab578c";
        public static readonly string OfficialChocolateyPublicKey = "79d02ea9cad655eb";

        public static string PackagesLocation = _fileSystem.CombinePaths(InstallLocation, "lib");
        public static readonly string PackageFailuresLocation = _fileSystem.CombinePaths(InstallLocation, "lib-bad");
        public static readonly string PackageBackupLocation = _fileSystem.CombinePaths(InstallLocation, "lib-bkp");
        public static readonly string ShimsLocation = _fileSystem.CombinePaths(InstallLocation, "bin");
        public static readonly string ChocolateyPackageInfoStoreLocation = _fileSystem.CombinePaths(InstallLocation, ".chocolatey");
        public static readonly string ExtensionsLocation = _fileSystem.CombinePaths(InstallLocation, "extensions");
        public static readonly string TemplatesLocation = _fileSystem.CombinePaths(InstallLocation, "templates");
        public static readonly string HooksLocation = _fileSystem.CombinePaths(InstallLocation, "hooks");
        public static readonly string HookPackageIdExtension = ".hook";
        public static readonly string ChocolateyCommunityFeedPushSourceOld = "https://chocolatey.org/";
        public static readonly string ChocolateyCommunityFeedPushSource = "https://push.chocolatey.org/";
        public static readonly string ChocolateyCommunityGalleryUrl = "https://community.chocolatey.org/";
        public static readonly string ChocolateyCommunityFeedSource = "https://community.chocolatey.org/api/v2/";
        public static readonly string ChocolateyLicensedFeedSource = "https://licensedpackages.chocolatey.org/api/v2/";
        public static readonly string ChocolateyLicensedFeedSourceName = "chocolatey.licensed";
        public static readonly string UserAgent = "Chocolatey Command Line";
        public static readonly string RegistryValueInstallLocation = "InstallLocation";
        public static readonly string AllPackages = "all";
        public static readonly string PowerShellModulePathProcessProgramFiles = _fileSystem.CombinePaths(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles), "WindowsPowerShell\\Modules");
        public static readonly string PowerShellModulePathProcessDocuments = _fileSystem.CombinePaths(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "WindowsPowerShell\\Modules");
        public static readonly string LocalSystemSidString = "S-1-5-18";
        public static readonly SecurityIdentifier LocalSystemSid = new SecurityIdentifier(LocalSystemSidString);

        private static string GetHttpCacheLocation()
        {
            if (ProcessInformation.IsElevated() || string.IsNullOrEmpty(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile, System.Environment.SpecialFolderOption.DoNotVerify)))
            {
                return HttpCacheSystemLocation;
            }
            else
            {
                return HttpCacheUserLocation;
            }
        }

        public static class Environment
        {
            public static readonly string Path = "Path";
            public static readonly string PathExtensions = "PATHEXT";
            public static readonly string PsModulePath = "PSModulePath";
            public static readonly string Temp = "TEMP";
            public static readonly string SystemUserName = "SYSTEM";
            public static readonly string Username = "USERNAME";
            public static readonly string ProcessorArchitecture = "PROCESSOR_ARCHITECTURE";
            public const string Arm64ProcessorArchitecture = "ARM64";
            public static readonly string EnvironmentSeparator = ";";

            public static readonly string ChocolateyToolsLocation = "ChocolateyToolsLocation";
            public static readonly string ChocolateyPackageInstallLocation = "ChocolateyPackageInstallLocation";
            public static readonly string ChocolateyPackageInstallerType = "ChocolateyInstallerType";
            public static readonly string ChocolateyPackageExitCode = "ChocolateyExitCode";
            public static readonly string ChocolateyIgnoreChecksums = "ChocolateyIgnoreChecksums";
            public static readonly string ChocolateyAllowEmptyChecksums = "ChocolateyAllowEmptyChecksums";
            public static readonly string ChocolateyAllowEmptyChecksumsSecure = "ChocolateyAllowEmptyChecksumsSecure";
            public static readonly string ChocolateyPowerShellHost = "ChocolateyPowerShellHost";
            public static readonly string ChocolateyForce = "ChocolateyForce";
            public static readonly string ChocolateyExitOnRebootDetected = "ChocolateyExitOnRebootDetected";
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
            public static readonly string ShimGenExe = _fileSystem.CombinePaths(InstallLocation, "tools", "shimgen.exe");
        }

        public static class ConfigSettings
        {
            public static readonly string CacheLocation = "cacheLocation";
            public static readonly string CommandExecutionTimeoutSeconds = "commandExecutionTimeoutSeconds";
            public static readonly string Proxy = "proxy";
            public static readonly string ProxyUser = "proxyUser";
            public static readonly string ProxyPassword = "proxyPassword";
            public static readonly string ProxyBypassList = "proxyBypassList";
            public static readonly string ProxyBypassOnLocal = "proxyBypassOnLocal";
            public static readonly string WebRequestTimeoutSeconds = "webRequestTimeoutSeconds";
            public static readonly string UpgradeAllExceptions = "upgradeAllExceptions";
            public static readonly string DefaultTemplateName = "defaultTemplateName";
            public static readonly string DefaultPushSource = "defaultPushSource";
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
            public static readonly string DisableCompatibilityChecks = "disableCompatibilityChecks";
        }

        public static class Messages
        {
            public static readonly string ContinueChocolateyAction = "Moving forward with chocolatey actions.";
            public static readonly string NugetEventActionHeader = "Nuget called an event";
        }

        public static bool IsDebugModeCliPrimitive()
        {
            var args = System.Environment.GetCommandLineArgs();
            var isDebug = false;
            // no access to the good stuff here, need to go a bit primitive in parsing args
            foreach (var arg in args.OrEmpty())
            {
                if (arg.ContainsSafe("-debug") || arg.IsEqualTo("-d") || arg.IsEqualTo("/d"))
                {
                    isDebug = true;
                    break;
                }
            }

            return isDebug;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static bool is_debug_mode_cli_primitive()
            => IsDebugModeCliPrimitive();
#pragma warning restore IDE1006
    }
}
