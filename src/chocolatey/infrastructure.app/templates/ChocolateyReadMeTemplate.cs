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

using static chocolatey.StringResources;

namespace chocolatey.infrastructure.app.templates
{
    public class ChocolateyReadMeTemplate
    {
        public static string Template =
            $@"## Summary
How do I create packages? See https://docs.chocolatey.org/en-us/create/create-packages

If you are submitting packages to the community feed (https://community.chocolatey.org)
always try to ensure you have read, understood and adhere to the create
packages wiki link above.

## Automatic Packaging Updates?
Consider making this package an automatic package, for the best
maintainability over time. Read up at https://docs.chocolatey.org/en-us/create/automatic-packages

## Shim Generation
Any executables you include in the package or download (but don't call
install against using the built-in functions) will be automatically shimmed.

This means those executables will automatically be included on the path.
Shim generation runs whether the package is self-contained or uses automation
scripts.

By default, these are considered console applications.

If the application is a GUI, you should create an empty file next to the exe
named 'name.exe.gui' e.g. 'bob.exe' would need a file named 'bob.exe.gui'.
See https://docs.chocolatey.org/en-us/create/create-packages#how-do-i-set-up-shims-for-applications-that-have-a-gui

If you want to ignore the executable, create an empty file next to the exe
named 'name.exe.ignore' e.g. 'bob.exe' would need a file named
'bob.exe.ignore'.
See https://docs.chocolatey.org/en-us/create/create-packages#how-do-i-exclude-executables-from-getting-shims

## Self-Contained?
If you have a self-contained package, you can remove the automation scripts
entirely and just include the executables, they will automatically get shimmed,
which puts them on the path. Ensure you have the legal right to distribute
the application though. See https://docs.chocolatey.org/en-us/information/legal.

You should read up on the Shim Generation section to familiarize yourself
on what to do with GUI applications and/or ignoring shims.

## Automation Scripts
You have a powerful use of Chocolatey, as you are using PowerShell. So you
can do just about anything you need. Choco has some very handy built-in
functions that you can use, these are sometimes called the helpers.

### Built-In Functions
https://docs.chocolatey.org/en-us/create/functions

A note about a couple:
* Get-ToolsLocation - used to get you the 'tools' root, which by default is set to 'c:\tools', not the chocolateyInstall bin folder - see https://docs.chocolatey.org/en-us/create/functions/get-toolslocation
* Install-BinFile - used for non-exe files - executables are automatically shimmed... - see https://docs.chocolatey.org/en-us/create/functions/install-binfile
* Uninstall-BinFile - used for non-exe files - executables are automatically shimmed - see https://docs.chocolatey.org/en-us/create/functions/uninstall-binfile

### Getting package specific information
Use the package parameters pattern - see https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument

### Need to mount an ISO?
https://docs.chocolatey.org/en-us/guides/create/mount-an-iso-in-chocolatey-package

### Environment Variables
Chocolatey makes a number of environment variables available (You can access any of these with $env:TheVariableNameBelow):

 * {EnvironmentVariables.System.Temp}/{EnvironmentVariables.Package.Tmp} - Overridden to the CacheLocation, but may be the same as the original {EnvironmentVariables.System.Temp} folder
 * {EnvironmentVariables.System.ChocolateyInstall} - Top level folder where Chocolatey is installed
 * {EnvironmentVariables.Package.ChocolateyPackageName} - The name of the package, equivalent to the `<id />` field in the nuspec
 * {EnvironmentVariables.Package.ChocolateyPackageTitle} - The title of the package, equivalent to the `<title />` field in the nuspec
 * {EnvironmentVariables.Package.ChocolateyPackageVersion} - The normalized version of the package, equivalent to a normalized edition of the `<version />` field in the nuspec
 * {EnvironmentVariables.Package.ChocolateyPackageFolder} - The top level location of the package folder  - the folder where Chocolatey has downloaded and extracted the NuGet package, typically `C:\ProgramData\chocolatey\lib\packageName`.

#### Advanced Environment Variables
The following are more advanced settings:

 * {EnvironmentVariables.Package.ChocolateyPackageParameters} - Parameters to use with packaging, not the same as install arguments (which are passed directly to the native installer). Based on `--package-parameters`.
 * {EnvironmentVariables.Package.ChocolateyVersion} - The version of Choco you normally see. Use if you are 'lighting' things up based on choco version. Otherwise take a dependency on the specific version you need.
 * {EnvironmentVariables.Package.ChocolateyForceX86} = If available and set to 'true', then user has requested 32bit version. Automatically handled in built in Choco functions.
 * {EnvironmentVariables.Package.OsPlatform} - Like Windows, macOS, Linux.
 * {EnvironmentVariables.Package.OsVersion} - The version of OS, like 10.0 something something for Windows.
 * {EnvironmentVariables.Package.OsName} - The reported name of the OS.
 * {EnvironmentVariables.Package.Username} = The user name
 * {EnvironmentVariables.Package.UserDomainName} = The user domain name (could also be local computer name)
 * {EnvironmentVariables.Package.IsProcessElevated} = Is the process elevated?
 * {EnvironmentVariables.Package.IsUserSystemAccount} = Is the user the system account?
 * {EnvironmentVariables.Package.IsRemoteDesktop} = Is the user in a terminal services session?
 * {EnvironmentVariables.System.ChocolateyToolsLocation} - formerly '{EnvironmentVariables.System.ChocolateyBinRoot}' ('{EnvironmentVariables.System.ChocolateyBinRoot}' will be removed with Chocolatey v2.0.0), this is where tools being installed outside of Chocolatey packaging will go.

#### Set By Options and Configuration
Some environment variables are set based on options that are passed, configuration and/or features that are turned on:

 * {EnvironmentVariables.Package.ChocolateyEnvironmentDebug} - Was `--debug` passed? If using the built-in PowerShell host, this is always true (but only logs debug messages to console if `--debug` was passed)
 * {EnvironmentVariables.Package.ChocolateyEnvironmentVerbose} - Was `--verbose` passed? If using the built-in PowerShell host, this is always true (but only logs verbose messages to console if `--verbose` was passed).
 * {EnvironmentVariables.Package.ChocolateyExitOnRebootDetected} - Are we exiting on a detected reboot? Set by ` --exit-when-reboot-detected`  or the feature `exitOnRebootDetected`
 * {EnvironmentVariables.Package.ChocolateyForce} - Was `--force` passed?
 * {EnvironmentVariables.Package.ChocolateyForceX86} - Was `-x86` passed?
 * {EnvironmentVariables.Package.ChocolateyRequestTimeout} - How long before a web request will time out. Set by config `webRequestTimeoutSeconds`
 * {EnvironmentVariables.Package.ChocolateyResponseTimeout} - How long to wait for a download to complete? Set by config `commandExecutionTimeoutSeconds`
 * {EnvironmentVariables.Package.ChocolateyPowerShellHost} - Are we using the built-in PowerShell host? Set by `--use-system-powershell` or the feature `powershellHost`

#### Business Edition Variables

 * ChocolateyInstallArgumentsSensitive - Encrypted arguments passed from command line `--install-arguments-sensitive` that are not logged anywhere.
 * ChocolateyPackageParametersSensitive - Package parameters passed from command line `--package-parameters-sensitive` that are not logged anywhere.
 * ChocolateyLicensedVersion - What version is the licensed edition on?
 * {EnvironmentVariables.Package.ChocolateyLicenseType} - What edition / type of the licensed edition is installed?
 * USER_CONTEXT - The original user context - different when self-service is used (Licensed)

#### Experimental Environment Variables
The following are experimental or use not recommended:

 * {EnvironmentVariables.Package.OsIs64Bit} = This may not return correctly - it may depend on the process the app is running under
 * {EnvironmentVariables.Package.ChocolateyProductVersion} = the version of Choco that may match {EnvironmentVariables.Package.ChocolateyVersion} but may be different - based on git describe
 * {EnvironmentVariables.Package.IsAdmin} = Is the user an administrator? But doesn't tell you if the process is elevated.
 * {EnvironmentVariables.Package.IsRemote} = Is the user in a remote session?

#### Not Useful Or Anti-Pattern If Used

 * {EnvironmentVariables.Package.ChocolateyInstallOverride} = Not for use in package automation scripts. Based on `--override-arguments` being passed.
 * {EnvironmentVariables.Package.ChocolateyInstallArguments} = The installer arguments meant for the native installer. You should use {EnvironmentVariables.Package.ChocolateyPackageParameters} instead. Based on `--install-arguments` being passed.
 * {EnvironmentVariables.Package.ChocolateyIgnoreChecksums} - Was `--ignore-checksums` passed or the feature `checksumFiles` turned off?
 * {EnvironmentVariables.Package.ChocolateyAllowEmptyChecksums} - Was `--allow-empty-checksums` passed or the feature `allowEmptyChecksums` turned on?
 * {EnvironmentVariables.Package.ChocolateyAllowEmptyChecksumsSecure} - Was `--allow-empty-checksums-secure` passed or the feature `allowEmptyChecksumsSecure` turned on?
 * {EnvironmentVariables.Package.ChocolateyChecksum32} - Was `--download-checksum` passed?
 * {EnvironmentVariables.Package.ChocolateyChecksumType32} - Was `--download-checksum-type` passed?
 * {EnvironmentVariables.Package.ChocolateyChecksum64} - Was `--download-checksum-x64` passed?
 * {EnvironmentVariables.Package.ChocolateyChecksumType64} - Was `--download-checksum-type-x64` passed?
 * ChocolateyPackageExitCode - The exit code of the script that just ran - usually set by `Set-PowerShellExitCode`
 * {EnvironmentVariables.Package.ChocolateyLastPathUpdate} - Set by Chocolatey as part of install, but not used for anything in particular in packaging.
 * {EnvironmentVariables.Package.ChocolateyProxyLocation} - The explicit proxy location as set in the configuration `proxy`
 * ChocolateyDownloadCache - Use available download cache? Set by `--skip-download-cache`, `--use-download-cache`, or feature `downloadCache`
 * {EnvironmentVariables.Package.ChocolateyProxyBypassList} - Explicitly set locations to ignore in configuration `proxyBypassList`
 * {EnvironmentVariables.Package.ChocolateyProxyBypassOnLocal} - Should the proxy bypass on local connections? Set based on configuration `proxyBypassOnLocal`
 * {EnvironmentVariables.System.HttpProxy} - Set by original `http_proxy` passthrough, or same as `{EnvironmentVariables.Package.ChocolateyProxyLocation}` if explicitly set.
 * {EnvironmentVariables.System.HttpsProxy} - Set by original `https_proxy` passthrough, or same as `{EnvironmentVariables.Package.ChocolateyProxyLocation}` if explicitly set.
 * {EnvironmentVariables.System.NoProxy} - Set by original `no_proxy` passthrough, or same as `{EnvironmentVariables.Package.ChocolateyProxyBypassList}` if explicitly set.

";
    }
}
