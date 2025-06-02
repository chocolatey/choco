// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System.ComponentModel;

namespace chocolatey
{
    public static class StringResources
    {
        /// <summary>
        /// Resources for the names of available environment variables that will be created or used
        /// as part of executing Chocolatey CLI.
        /// </summary>
        /// <remarks>
        /// <para>
        /// DEV NOTICE: Mark anything that is not meant for public consumption as internal constants
        /// and not browsable, even if used in other projects.
        /// </para>
        /// <para>
        /// IMPORTANT: Do not reference any other locations in this file.
        /// The file is shared between the chocolatey and Chocolatey.PowerShell projects.
        /// </para>
        /// </remarks>
        public static class EnvironmentVariables
        {
            /// <summary>
            /// Environment variables that will be set before running any PowerShell scripts, or by
            /// one PowerShell helpers that a user can use.
            /// </summary>
            public static class Package
            {
                /// <summary>
                /// Whether debug output should be enabled when running PowerShell scripts.
                /// </summary>
                /// <remarks>In most scenarios this environment variable will always be <c>true</c></remarks>
                public const string ChocolateyEnvironmentDebug = nameof(ChocolateyEnvironmentDebug);

                /// <summary>
                /// Whether verbose output should be enabled when running PowerShell scripts.
                /// </summary>
                /// <remarks>In most scenarios this environment variable will always be <c>true</c></remarks>
                public const string ChocolateyEnvironmentVerbose = nameof(ChocolateyEnvironmentVerbose);

                /// <summary>
                /// Whether we should detect if a reboot is required or not, and exit if a reboot is required.
                /// </summary>
                public const string ChocolateyExitOnRebootDetected = nameof(ChocolateyExitOnRebootDetected);

                /// <summary>
                /// Whether the user has specified that the package should be forced to install
                /// again, even if it is installed.
                /// </summary>
                public const string ChocolateyForce = nameof(ChocolateyForce);

                /// <summary>
                /// Whether the user has requested to install 32bit version of binaries or not.
                /// </summary>
                public const string ChocolateyForceX86 = "chocolateyForceX86";

                /// <summary>
                /// Set by Chocolatey CLI as part of an install, but not used for anything in particular in packaging.
                /// </summary>
                public const string ChocolateyLastPathUpdate = nameof(ChocolateyLastPathUpdate);

                /// <summary>
                /// The type of the license that the user currently has installed on their system
                /// (business, professional, msp, architect, etc).
                /// </summary>
                /// <remarks>Business Edition Only Variable</remarks>
                public const string ChocolateyLicenseType = nameof(ChocolateyLicenseType);

                /// <summary>
                /// The location that the package was found to be installed to. When found will be
                /// outputted by Chocolatey CLI at the end of its execution.
                /// </summary>
                public const string ChocolateyPackageInstallLocation = nameof(ChocolateyPackageInstallLocation);

                /// <summary>
                /// The name or identifier of the package that is being handled.
                /// </summary>
                /// <seealso cref="PackageName" />
                public const string ChocolateyPackageName = "chocolateyPackageName";

                /// <summary>
                /// The package parameters that was passed in by the user during install, upgrade or uninstalls.
                /// </summary>
                /// <seealso cref="PackageParameters" />
                public const string ChocolateyPackageParameters = "chocolateyPackageParameters";

                /// <summary>
                /// The title of the package that is being handled.
                /// </summary>
                /// <seealso cref="PackageTitle" />
                public const string ChocolateyPackageTitle = "chocolateyPackageTitle";

                /// <summary>
                /// The normalized version of the package that is being handled.
                /// </summary>
                /// <seealso cref="PackageVersion" />
                public const string ChocolateyPackageVersion = "chocolateyPackageVersion";

                /// <summary>
                /// Whether the user has specified that we should use the built-in PowerShell Host
                /// or the Host that is available on the system.
                /// </summary>
                public const string ChocolateyPowerShellHost = nameof(ChocolateyPowerShellHost);

                /// <summary>
                /// The full semantic version of the currently running Chocolatey CLI product.
                /// </summary>
                /// <remarks>
                /// This environment variable is experimental, and is not recommended for public consumption.
                /// </remarks>
                public const string ChocolateyProductVersion = "CHOCOLATEY_VERSION_PRODUCT";

                /// <summary>
                /// How long before a web request will timeout.
                /// </summary>
                public const string ChocolateyRequestTimeout = "chocolateyRequestTimeout";

                /// <summary>
                /// How long to wait for a download to complete.
                /// </summary>
                public const string ChocolateyResponseTimeout = "chocolateyResponseTimeout";

                /// <summary>
                /// The 4-part assembly version number of Chocolatey CLI that the user is currently using.
                /// </summary>
                public const string ChocolateyVersion = "CHOCOLATEY_VERSION";

                /// <summary>
                /// Whether the user in an administrator. Even if we are not running in an elevated state.
                /// </summary>
                /// <remarks>This environment variable is experimental.</remarks>
                public const string IsAdmin = "IS_ADMIN";

                /// <summary>
                /// Whether the current process is running in an elevated state.
                /// </summary>
                public const string IsProcessElevated = "IS_PROCESSELEVATED";

                /// <summary>
                /// Whether the running operating system is a 64bit system.
                /// </summary>
                /// <remarks>This environment variable is experimental.</remarks>
                public const string OsIs64Bit = "OS_IS64BIT";

                /// <summary>
                /// The name of the running operating system as it is being reported.
                /// </summary>
                public const string OsName = "OS_NAME";

                /// <summary>
                /// The platform that Chocolatey CLI is currently running on. This will typically
                /// be Windows, macOS or Linux.
                /// </summary>
                public const string OsPlatform = "OS_PLATFORM";

                /// <summary>
                /// The version of the running operating system.
                /// </summary>
                public const string OsVersion = "OS_VERSION";

                /// <summary>
                /// The location where temporary files should be stored during PowerShell execution.
                /// </summary>
                /// <remarks>
                /// <para>
                /// When PowerShell scripts are executed, this variable will be set to the
                /// configured <see cref="chocolatey.infrastructure.app.configuration.ChocolateyConfiguration.CacheLocation"/>.
                /// </para>
                /// </remarks>
                /// <seealso cref="System.Temp"/>
                public const string Tmp = "TMP";

                /// <summary>
                /// The checksum of the cached file that is stored on the private CDN.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string CacheChecksumFormat = "CacheChecksum_{0}";

                /// <summary>
                /// The checksum type of the cached file that is stored on the private CDN.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string CacheChecksumTypeFormat = "CacheChecksumType_{0}";

                /// <summary>
                /// The name of the cached file that is stored on the private CDN.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string CacheFileFormat = "CacheFile_{0}";

                /// <summary>
                /// Whether the user has specified that packages are allowed to not validate
                /// checksums for non-https sources.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyAllowEmptyChecksums = nameof(ChocolateyAllowEmptyChecksums);

                /// <summary>
                /// Whether the user has specified that packages are allowed to not validate
                /// checksums for https sources.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyAllowEmptyChecksumsSecure = nameof(ChocolateyAllowEmptyChecksumsSecure);

                /// <summary>
                /// Whether the argument `--download-checksum` was passed by the user.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyChecksum32 = "chocolateyChecksum32";

                /// <summary>
                /// Whether the argument `--download-checksum-x64` was passed by the user.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyChecksum64 = "chocolateyChecksum64";

                /// <summary>
                /// Whether the argument `--download-checksum-type` was passed by the user.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyChecksumType32 = "chocolateyChecksumType32";

                /// <summary>
                /// Whether the argument `--download-checksum-type-x64` was passed by the user.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyChecksumType64 = "chocolateyChecksumType64";

                /// <summary>
                /// The exit code that the installation, upgrade or uninstall ended up reporting.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyExitCode = nameof(ChocolateyExitCode);

                /// <summary>
                /// Whether the user has specified that checksums should be ignored.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyIgnoreChecksums = nameof(ChocolateyIgnoreChecksums);

                /// <summary>
                /// The arguments that the user has passed to the commandline that should be added
                /// when launching an installer.
                /// </summary>
                /// <seealso cref="InstallArguments"/>
                /// <seealso cref="InstallerArguments"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyInstallArguments = "chocolateyInstallArguments";

                /// <summary>
                /// The identified type of the installer the package uses during installation.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyInstallerType = nameof(ChocolateyInstallerType);

                /// <summary>
                /// A <c>true</c> or <c>false</c> value of whether custom installer arguments was
                /// passed in by the user or not.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyInstallOverride = "chocolateyInstallOverride";

                /// <summary>
                /// The version of the package that is being handled as it is defined in the
                /// embedded nuspec file.
                /// </summary>
                /// <seealso cref="PackageNuspecVersion"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyNuspecVersion = "chocolateyPackageNuspecVersion";

                /// <summary>
                /// The location of the package content that is being handled.
                /// </summary>
                /// <remarks>Normally set to `%ChocolateyInstall\lib\PACKAGE_NAME`.</remarks>
                /// <seealso cref="PackageFolder"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyPackageFolder = "chocolateyPackageFolder";

                /// <summary>
                /// The full pre-release label of the package that is being handled.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyPackageVersionPrerelease = "chocolateyPackageVersionPrerelease";

                /// <summary>
                /// The domains that should bypass any proxies that is configured on the system.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyProxyBypassList = "chocolateyProxyBypassList";

                /// <summary>
                /// Whether local requests should bypass any configured proxy.
                /// </summary>
                /// <seealso cref="System.NoProxy" />
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyProxyBypassOnLocal = "chocolateyProxyBypassOnLocal";

                /// <summary>
                /// The URL to the proxy that should be used when downloading binary files and other
                /// web requests.
                /// </summary>
                /// <seealso cref="System.HttpProxy" />
                /// <seealso cref="System.HttpsProxy" />
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyProxyLocation = "chocolateyProxyLocation";

                /// <summary>
                /// The password of the user that is configured to be used for the proxy.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyProxyPassword = "chocolateyProxyPassword";

                /// <summary>
                /// The username of the user that is configured to be used for the proxy.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyProxyUser = "chocolateyProxyUser";

                /// <summary>
                /// Whether the package being handled has download cache available in the private CDN.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string DownloadCacheAvailable = nameof(DownloadCacheAvailable);

                /// <summary>
                /// The arguments that the user has passed to the commandline that should be added
                /// when launching an installer.
                /// </summary>
                /// <seealso cref="ChocolateyInstallArguments"/>
                /// <seealso cref="InstallerArguments"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string InstallArguments = "installArguments";

                /// <summary>
                /// The arguments that the user has passed to the commandline that should be added
                /// when launching an installer.
                /// </summary>
                /// <seealso cref="ChocolateyInstallArguments"/>
                /// <seealso cref="InstallArguments"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string InstallerArguments = "installerArguments";

                /// <summary>
                /// Whether the current execution is running in a remote context.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string IsRemote = "IS_REMOTE";

                /// <summary>
                /// Whether the current execution is running in the context of a remote desktop.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string IsRemoteDesktop = "IS_REMOTEDESKTOP";

                /// <summary>
                /// Whether the user account we are running under is a system account or not.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string IsUserSystemAccount = "IS_SYSTEM";

                /// <summary>
                /// The location of the package content that is being handled.
                /// </summary>
                /// <remarks>Normally set to `%ChocolateyInstall\lib\PACKAGE_NAME`.</remarks>
                /// <seealso cref="ChocolateyPackageFolder"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string PackageFolder = "packageFolder";

                /// <summary>
                /// The name or identifier of the package that is being handled.
                /// </summary>
                /// <seealso cref="ChocolateyPackageName"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string PackageName = "packageName";

                /// <summary>
                /// The version of the package that is being handled as it is defined in the
                /// embedded nuspec file.
                /// </summary>
                /// <seealso cref="ChocolateyNuspecVersion"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string PackageNuspecVersion = "packageNuspecVersion";

                /// <summary>
                /// The package parameters that was passed in by the user during install, upgrade or uninstalls.
                /// </summary>
                /// <seealso cref="ChocolateyPackageParameters"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string PackageParameters = "packageParameters";

                /// <summary>
                /// The title of the package that is being handled.
                /// </summary>
                /// <seealso cref="ChocolateyPackageTitle"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string PackageTitle = nameof(PackageTitle);

                /// <summary>
                /// The normalized version of the package that is being handled.
                /// </summary>
                /// <seealso cref="ChocolateyPackageVersion"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string PackageVersion = "packageVersion";

                /// <summary>
                /// Whether we are running in a 64bit process context.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ProcessIs64Bit = "PROCESS_IS64BIT";

                /// <summary>
                /// The name of the domain the user we are running under is registered to.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string UserDomainName = "USER_DOMAIN";

                /// <summary>
                /// The username of the user that Chocolatey CLI is running under.
                /// </summary>
                /// <seealso cref="System.Username"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string Username = "USER_NAME";
            }

            /// <summary>
            /// Environment variables that are part of the Operating System itself. Either by
            /// default, or read when user has specified the environment variable manually.
            /// </summary>
            /// <remarks>
            /// These environment variables will also be available in packages. They may have a
            /// different value than the system environment variable in that case.
            /// </remarks>
            public static class System
            {
                /// <summary>
                /// The location of where Chocolatey CLI is expected to be installed to.
                /// </summary>
                public const string ChocolateyInstall = nameof(ChocolateyInstall);

                /// <summary>
                /// The name of the current computer.
                /// </summary>
                public const string ComputerName = "COMPUTERNAME";

                /// <summary>
                /// The location where temporary files should be stored.
                /// </summary>
                /// <remarks>
                /// <para>
                /// When PowerShell scripts are executed, this variable will be set to the
                /// configured <see cref="chocolatey.infrastructure.app.configuration.ChocolateyConfiguration.CacheLocation"/>.
                /// </para>
                /// </remarks>
                /// <seealso cref="Package.Tmp"/>
                public const string Temp = "TEMP";

                /// <summary>
                /// Where tools should end up being installed to.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ChocolateyToolsLocation = nameof(ChocolateyToolsLocation);

                /// <summary>
                /// The name of the environment variable that should be used when looking up
                /// binaries, or when adding binaries to PATH.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string Path = "PATH";

                /// <summary>
                /// Specifies which file extensions the shell (e.g., cmd.exe) will treat as
                /// executable when a user enters a command without specifying a file extension.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string PathExtensions = "PATHEXT";

                /// <summary>
                /// The architecture of the processor that the application is running under.
                /// </summary>
                /// <remarks>Some example values: `AMD64`, `x86`, `ARM64`</remarks>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string ProcessorArchitecture = "PROCESSOR_ARCHITECTURE";

                /// <summary>
                /// The paths where PowerShell modules are expected to be located.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string PSModulePath = nameof(PSModulePath);

                /// <summary>
                /// The default location when Windows has been installed to.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string SystemRoot = nameof(SystemRoot);

                /// <summary>
                /// The username of the user that Chocolatey CLI is running under.
                /// </summary>
                /// <seealso cref="Package.Username"/>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal const string Username = "USERNAME";

                /// <summary>
                /// The root of the chocolatey bin (legacy name for <see cref="ChocolateyToolsLocation"/>).
                /// </summary>
                /// <remarks>
                /// This environment variable is only used by the alternative sources to determine
                /// the executable location.
                /// </remarks>
                /// <seealso cref="ChocolateyToolsLocation" />
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal static string ChocolateyBinRoot = nameof(ChocolateyBinRoot);

                /// <summary>
                /// One of the possible system environment variables available to set a system proxy.
                /// </summary>
                /// <remarks>
                /// This variable will also be set as part of running PowerShell scripts when a
                /// proxy has been configured.
                /// </remarks>
                /// <seealso cref="HttpsProxy" />
                /// <seealso cref="Package.ChocolateyProxyLocation" />
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal static string HttpProxy = "http_proxy";

                /// <summary>
                /// One of the possible system environment variables available to set a system proxy.
                /// </summary>
                /// <remarks>
                /// This variable will also be set as part of running PowerShell scripts when a
                /// proxy has been configured.
                /// </remarks>
                /// <seealso cref="HttpProxy" />
                /// <seealso cref="Package.ChocolateyProxyLocation" />
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal static string HttpsProxy = "https_proxy";

                /// <summary>
                /// The system defined environment variable with the same name.
                /// </summary>
                /// <remarks>
                /// This variable will also be set as part of running PowerShell scripts by using
                /// the system no_proxy variable, or it will be set to the <see cref="Package.ChocolateyProxyBypassList"/>.
                /// </remarks>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal static string NoProxy = "no_proxy";

                /// <summary>
                /// The name of the session we are running underneath.
                /// </summary>
                /// <remarks>
                /// This will commonly be <c>console</c> for normal users through the CLI, and
                /// <c>rdp-something</c> for remote users.
                /// </remarks>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal static string SessionName = "SESSIONNAME";

                /// <summary>
                /// A common environment variable defined by Windows OS on what drive is being used
                /// for the windows installation.
                /// </summary>
                [EditorBrowsable(EditorBrowsableState.Never)]
                [Browsable(false)]
                internal static string SystemDrive = nameof(SystemDrive);
            }
        }

        public static class ErrorMessages
        {
            [EditorBrowsable(EditorBrowsableState.Never)]
            [Browsable(false)]
            internal const string UnableToDowngrade = "A newer version of {0} (v{1}) is already installed.{2} Use --allow-downgrade or --force to attempt to install older versions.";
            internal const string DependencyFailedToInstall = "Failed to install {0} because a previous dependency failed.";
        }
    }
}