# Chocolatey Licensed CHANGELOG

This covers changes for the "chocolatey.extension" package, where the licensed editions of Chocolatey get their enhanced functionality.

**NOTE**: If you have a licensed edition of Chocolatey, refer to this in tandem with [Chocolatey Open source CHANGELOG](https://github.com/chocolatey/choco/blob/master/CHANGELOG.md).

## 2.0.0 (March 14, 2019)
### FEATURES
 * Chocolatey Central Management tie in - Brings Central Management client configuration to Chocolatey. Requires Chocolatey v0.10.12 along with Chocolatey Agent v0.9.0 to allow communication with Central Maangement.
 * [Security] Self-Service - allow uninstalls of ONLY packages installed by a user. Allows restricting what a user can uninstall to only what they have installed - see [licensed #2](https://github.com/chocolatey/chocolatey-licensed-issues/issues/2)

### BUG FIXES
 * Self-Service / Background Mode:
    * Fix - Package arguments not being passed from Agent to choco.exe properly (client side fixes) - see [licensed #60](https://github.com/chocolatey/chocolatey-licensed-issues/issues/60)
    * Fix - Background Service does not pass the exit code back to the console (client side) - see [licensed #51](https://github.com/chocolatey/chocolatey-licensed-issues/issues/51)
 * Package Internalizer (Choco Download):
    * Fix - PackageInternalizer - UseOriginalLocation switch being appended inappropriately when line continuation is used with no arguments on first line - see [licensed #64](https://github.com/chocolatey/chocolatey-licensed-issues/issues/64)
 * Package Upload UI:
    * Fix - Package Uploader is unable to push to a non https source - see [licensed #49](https://github.com/chocolatey/chocolatey-licensed-issues/issues/49)
 * Package Builder (Choco New):
    * Fix - Nuspec files section should not be commented out as it doesn't allow subdirectories
    * Fix - Ensure Architect and MSP are able to use Package Builder
    * Fix - Escape XML characters in generated nuspec file
 * Windows Service Management Functions:
    * Fix - When shutting down a Windows service it may hold a lock on a file for a second after it shuts down
 * Fix - Type detection holds file locks if it times out - see [licensed #59](https://github.com/chocolatey/chocolatey-licensed-issues/issues/59)
 * Fix - Ensure Architect edition has all features set appropriately

### IMPROVEMENTS
 * Package Builder - Remove "v" as part of "Name v2.2.23" In product names
 * Package Upload UI - Ability to auto populate Source dropdown list - see [licensed #50](https://github.com/chocolatey/chocolatey-licensed-issues/issues/50)
 * Package Internalizer - Allow downloading all installed packages with `--installed-packages`
 * PowerShell - Cmdlets for Getting/Setting encrypted values in JSON files


## 1.12.12 (June 12, 2018)
### FEATURES
 * Package Upload UI - Ability to right click on a nupkg, and access UI for selecting where package is pushed to

### BUG FIXES
 * Package Synchronizer (Choco Sync):
    * Fix - Do not sync out packages that were Chocolatey-managed packages
 * Windows Service Management Functions:
    * Fix - Adding domain user to Administrators fails with "A member could not be added to or removed from the local group because the member does not exist"
 * Fix - Expired Chocolatey License causes error on package installations
 * Fix - Incorrectly setting UserAgent property
 * Package Internalizer (Choco Download):
    * Fix - Append Original Location not capturing params when on last line
    * Fix - Output directory option does not remove surrounding quotes
    * Fix - Incorrect version number is used to generate dependent packages when `--version` is passed to internalizer
 * Package Builder UI:
    * Fix - Generate button is not enabled for MSP License

### IMPROVEMENTS
 * Countdown - Allow turning off warnings with a feature
 * Self-Service / Background Mode:
    * Do not show ChocolateyLocalAdmin user on logon screen
    * Ensure captured password at runtime makes it to the background service
    * Add Package Internalizer (download) to available commands


## 1.12.11 (February 1, 2018)
**NOTE**: We've identified an issue with Self-Service interactive and UAC - we are working on a fix. Please see [#36](https://github.com/chocolatey/chocolatey-licensed-issues/issues/36) and subscribe for details.

A few nice things going into this release with Package Reducer/Optimize coming out of preview, Package Internalizer allowing you to specify variable overrides

### BREAKING CHANGES
 * Package Reducer is now on by default

When we first released package reducer, it was in a preview mode to allow for folks to opt in to test it and find issues. Now we are turning it on by default. If you should experience issues with it, please contact support to file an issue (provide a zipped log as well). To turn it off should you not want to use it, you can just run `choco feature disable --name=reduceInstalledPackageSpaceUsage`.

### BUG FIXES
 * Self-Service / Background Mode:
    * Fix - Work with empty sessions. Self-service users were incorrectly bypassing background service when their active session doesn't have a name, which has become common with Windows 10.
    * Fix - Allow self-service to work when the current working directory is different from system drive or a mapped drive. Also allow it to be set to locations where any user would not have a permissions error.
 * Package Internalizer (Choco Download):
    * Fix - When a variable is set to the contents of a file, work with that appropriately. A good example of this is `$data` variable in [dotnetcore-runtime](https://github.com/dotnetcore-chocolatey/dotnetcore-chocolateypackages/blob/d5960c11dda388cc7f7ab6438eee8984774f7662/dotnetcore-runtime.install/tools/ChocolateyInstall.ps1#L4) package.
    * Fix - Allow setting variable when there is a comment on the same line following the set. An example of this is in the [Firefox](https://github.com/chocolatey/chocolatey-coreteampackages/blob/91f4e51f5c8503ca962c22a2f087b96d22ae0e9f/automatic/firefox/tools/chocolateyInstall.ps1#L26) package.
 * Package Builder (Choco New):
    * Fix - Package Builder UI should recognize Architect and MSP editions and correctly set the UI for use with them.
    * Fix - Extract icon from the proper location for the file.

### IMPROVEMENTS
 * Package Internalizer (Choco Download):
    * Override variables in the installation script. When you see something like `$locale` being set and you want to override it to your own value for internalization, you can pass that variable name and value as a key value pair. As an example, try `choco download thunderbird --internalize locale=en-US` versus `choco download thunderbird --internalize locale=es-ES`.
 * Package Reducer - `choco optimize` command out of preview.
 * Package Builder (Choco New):
    * Extract icons from MSIs as well.
    * Package Builder UI logo animzation should match chocolatey website.


## 1.12.10 (December 18, 2017)
### FEATURES
 * Run background service interactively - sometimes you install applications that are unattended but cannot be silent.

If you want self-service to interactively manage installations, run `choco feature enable --name=useBackgroundServiceInteractively`. This requires that you use the `ChocolateyLocalAdmin` account with the Chocolatey-managed password as passwords are not stored and the service would need to produce that at runtime. There are some security considerations and why this is not turned on by default. Please see [interactive self-service consideration](https://chocolatey.org/docs/features-agent-service#interactive-self-service-consideration).

### BUG FIXES
 * Windows Service Management Functions:
    * Fix - Adding user to Administrators no longer fails with "Network path was not found" when there are domain users and local users in the administrators group.
 * Package Builder (Choco New):
    * Fix - Create packages from non-admin (instead of access denied error).
    * Fix - Don't attempt to generate an icon unless a file for that icon exists.

### IMPROVEMENTS
 * Package Builder (Choco New):
    * Right click create package w/out GUI should pause on error. This allows a user to see any errors instead of closing quickly.
    * Do not generate template files if not using the default template.


## 1.12.9 (November 10, 2017)
### BUG FIXES
 * Fix: Changes in 1.12.7 restrict some install/upgrade licensed functionality from loading.
 * Windows Service Management Functions:
    * Fix - Domain Users should be found by domain context.
    * Fix - Throw Error if unable to ensure user or administrators membership.
    * Fix - Domain Users should not attempt to generate password.
 * Package Synchronizer (Choco Sync):
    * Fix - syncing new items use "sync" as user name when capturing information for Package Audit.
 * Package Builder (Choco New):
    * Fix - additional directories from programs and features not cleared each time.

### IMPROVEMENTS
 * Package Builder (Choco New):
    * Show Install Arguments in Nuspec with MSI Properties.
    * Extract icon from Programs and Features.
    * Extract icon from exe files.


## 1.12.8 (October 25, 2017)
### BUG FIXES
 * Fix - "The registered delegate for type IEnumerable threw an exception. Unable to find suitable location for change.exe."


## 1.12.7 (October 25, 2017)
### BUG FIXES
 * Fix - "ERROR: The term 'Invoke-ChocolateyProcessCmdlet' is not recognized as the name of a cmdlet, function, script file, or operable program" on upgrade all.


## 1.12.6 (October 24, 2017)
### BUG FIXES
 * Fix - `change.exe` could not be found when running Chocolatey GUI over RDP (Terminal Services).
 * Self-Service / Background Mode:
    * Fix - Set USER_CONTEXT environment variable to contain the user context information.

### IMPROVEMENTS
 * `Install-ChocolateyInstallPackage` - pass in answer file (`-AnswerFile`) to provide to stdin.


## 1.12.5 (September 28, 2017)
### BUG FIXES
 * Windows Service Management Functions:
    * Fix - Do not save created user prior to setting complex password.


## 1.12.4 (September 27, 2017)
### BUG FIXES
 * Self-Service / Background Mode:
    * Fix - Ensure that it configures and works appropriately with Chocolatey API (and the Chocolatey GUI).

### IMPROVEMENTS
 * Self-Service / Background Mode:
    * Allow commands to be configurable. Covered in more detail at https://chocolatey.org/docs/features-agent-service#setup.


## 1.12.3 (September 8, 2017)
### BUG FIXES
 * [Security] Fix - Hide admin only repositories from non-administrators with all Chocolatey commands.
 * Windows Service Management Functions:
    * Fix - Empty username causes errors. Will now set to what the configuration of Chocolatey is determining.
 * Package Internalizer (Choco Download):
    * Fix - Pass multiple package names to download/internalize (`choco download pkg1 pkg2 pkgN`).
 * Self-Service / Background Mode:
    * Fix - Don't display background services messages when background mode is not configured.
 * Package Reducer:
    * Fix - Package reducer fails the install when there is no package information.

### IMPROVEMENTS
 * choco sync is now out of preview.


## 1.12.2 (August 31, 2017)
### FEATURES
 * [Security] choco source/list - Administrator only visible repositories. Hide repositories from non-administrators who are using Chocolatey (C4B).
 * choco pin - Provide a reason when pinning to be seen in outdated by others (C4B).

### BUG FIXES
 * Package Synchronizer (Automatic Sync):
    * Fix - Ignore HKCU pkg on sync when different user instead of removing the package.
 * Package Builder (Choco New):
    * Fix - remove "rel[ease]" next to version when building package id.
    * Fix - remove 'self-installing' when building package id.
 * Fix - Logging - log sync messages directly to log file when using limit output (`-r`).
 * Fix - Download CDN Cache - If original url was switch to TLS/SSL by choco, the cache check doesn't override for the file on the CDN.

### IMPROVEMENTS
 * [Security] Runtime - Allow locking down all Chocolatey use to Administrators only (don't allow non-admins to run choco at all) - added as feature flip (see `choco feature list`).
 * Package Internalizer (Choco Download):
    * Pass multiple package names to download/internalize (`choco download pkg1 pkg2 pkgN`).
    * Downloaded resources will use download CDN cache if available.
 * Windows Service Management Functions:
    * Allow upgrading services without a restart - pass `-DoNotReinstallService` to `Install-ChocolateyWindowsService`.
 * Package Synchronizer (Choco Sync):
    * Specify package name when syncing by id (`--id="'Display Name*'" --package-id=package-id`).
 * Installation - Disable install of context menus with `--params "'/NoContextMenu'"`. Currently only new installs (removal on upgrade will come in a future release).
 * Package Builder (Choco New):
    * When an MSI has external files, ensure to copy those into the packaging as well. Requires same folder structure that would be used at install time successfully.
 * Package Reducer:
    * Treat MST files as removable binaries.


## 1.12.1 (July 13, 2017)
Among the bug fixes, we've brought Package Internalizer to the MSP edition of Chocolatey.

### BUG FIXES
 * Fix - Configuration item is not serialized, causing issues with installs with packages.config and ChocolateyGUI.
 * Package Synchronizer (Choco Sync):
    * Fix - syncing new items should capture audit information for Package Audit.
 * Self-Service / Background Mode
    * Fix - timeout should be near infinite when execution timeout is 0.
    * Fix - loading commands over PowerShell remoting can fail.
    * Fix - do not configure background service unless it applies.

### IMPROVEMENTS
 * Package Internalizer is now available for MSP edition as well.


## 1.12.0 (July 10, 2017)
### FEATURES
 * Package Reducer / Optimize (All licensed editions) - Reduce Space Usage for Chocolatey Installations

    If you have a significant number of Chocolatey packages you manage, you may notice that you also may have a pretty significant space usage under the Chocolatey lib directory. Package reducer automatically decreases the size of nupkg files to around 5KB and removes installers and zips automatically from your package install directories. This may allow you to save GBs of usage for a large amount of packages being managed.

    To learn more about Package Reducer and `choco optimize`, please see https://chocolatey.org/docs/features-package-reducer.

 * Package Audit (C4B) - Learn Who and When

    Reporting is very important, and auditing not only when your installations occurred but who installed them can be critical. There is nothing that presents this kind of information as easily as you will be able to gather it with Chocolatey for Business (C4B) and Package Audit.

    To learn more about Package Audit, please see https://chocolatey.org/docs/features-package-audit.

### IMPROVEMENTS
 * Package Internalizer (Choco Download):
    * Option (`--internalize-all-urls`) to internalize any url in a package (not just if the package scripts contain known-compatible known functions).
 * Self-Service / Background Mode:
    * Added `useBackgroundServiceWithNonAdministratorsOnly` feature switch to skip background mode for administrators.
 * Windows Service Management Functions:
    * Added `useLocalSystemForServiceInstalls` feature switch to disable using LocalSystem as the user for services by default.
    * Added config settings to provide local user / password.
    * If the password is empty or not provided, Chocolatey will manage the password securely.
    * If the user doesn't exist, the account will be created
    * Accounts are ensured to be administrators
    * Accounts are ensured to have Log on as service and batch rights.


## 1.11.0 (June 27, 2017)
### BUG FIXES
 * Self-Service / Background Mode
    * Fix - Use a URI with WCF named pipes that doesn't exclusively hold a lock on the root (blocking other services). Fallback to old URI for versions of chocolatey-agent less than 0.7.0+. - see [#12](https://github.com/chocolatey/chocolatey-licensed-issues/issues/12).
    * Fix - Ensure chocolatey.lib can send arguments through and not just choco.exe.

### IMPROVEMENTS
 * Package Synchronizer - update registry links when software updates


## 1.10.0 (June 9, 2017)
This release brings Package Throttle, Package Synchronizer's "Show All Packages in Programs and Features", and Direct Installer (aka Package-less Install) to Chocolatey. There are also numerous improvements to Package Builder, fixes and improvements to Package Internalizer and Self-Service. Package Throttle is in all licensed editions, allowing Pro, MSP, and C4B customers the ability to slow down Chocolatey as it downloads packages and any resources the packages are downloading. See more details each section below.

### BREAKING CHANGES
 * [Security] Self-Service / Background Mode - support background mode with self-service sources only by default

    If you are using self-service functionality, when you upgrade to v1.10, you will need to go to those sources that you allow to be self-service and ensure they are selected to allow self-service (`choco source list`). Otherwise by default users are going to be locked out of using those sources. The move to opt-in is best from a security standpoint and we want to give you the ability to have folks opt-in to the better behavior.

    To change this behavior back to the way it was previously, simply run `choco disable -n useBackgroundServiceWithSelfServiceSourcesOnly`. For more details see `choco feature list` or https://chocolatey.org/docs/chocolatey-configuration#self-service-background-mode

### FEATURES
 * Package Throttle - Throttle Bandwidth / Rate Limit Download Speeds

    By default, Chocolatey downloads packages and resources as fast as it can. Package Throttle gives you a way to slow down Chocolatey so it doesn't overwhelm any bandwidth restrictions you may have. This is done as both a setting and per package install/upgrade. This is in bits per second (not bytes, bits is what most network traffic is measured in). When adding at runtime, simply add `--bps=VALUE`. To set the value in the configuration, call `choco config set maximumDownloadRateBitsPerSecond VALUE`. For more details see https://chocolatey.org/docs/features-package-reducer.

 * Package Synchronizer's Show All Packages In Programs and Features

    This means packages that do not have an underlying installation can still show up in Programs and Features and be managed there as well, which allows for legacy inventory reporting systems to see all the software that is installed in the same way that Chocolatey is able to. A picture can probably best explain this feature, check out [All Package In Programs and Features](https://raw.githubusercontent.com/chocolatey/choco-wiki/e619d5d25018f9362d50749ee86554ebc4f4f04d/images/features/features_packages_in_programs_and_features.png)

    To turn this feature on, simply run the following command `choco feature enable -n showAllPackagesInProgramsAndFeatures`. For more details on the feature, see https://chocolatey.org/docs/chocolatey-configuration#package-synchronizer and https://chocolatey.org/docs/features-synchronize. This does require one additional run of `choco` to take affect (same when disabling the feature), hopefully we can remove that in the future.

 * Package-less Install / Direct Installer - Install and upgrade directly from installers (MSIs, EXEs, etc)!

    If Package Builder can create a fully unattended package for an installer, you don't even need a package first. You can simply call `choco install nameoffile.msi` or `choco upgrade nameoffile.exe` and Chocolatey will generate packaging on the fly and install/upgrade the package!

### BUG FIXES
 * Fix - choco new generates uninstall template with wrong use of registry key variable - see [#1304](https://github.com/chocolatey/choco/issues/1304)
 * Package Internalizer (Choco Download):
    * Fix - Append UseOriginalLocation for multiline so that it adds a continuation and not a hanging parameter on a new line
    * Fix - Variable replacement wasn't working for `${value}` and `$($value)`
    * Fix - Find and replace the original url in the script

### IMPROVEMENTS
 * [Security] Allow locking down new/download commands to admin only - added as feature flip switches (see `choco feature list`)
 * Self-Service / Background Mode:
    * Provide the user context to Chocolatey on install / upgrade (user context is original user requesting action)
 * Package Builder (Choco New):
    * Find MSI Properties in CustomActions and in Properties table listed in Properties ending in "Properties"
    * Show possible silent options when installer type is not determinable or is found to be custom
    * General template improvements
 * Package Internalizer (Choco Download):
    * Rewrite any scripts that `chocolateyInstall.ps1` uses into the `chocolateyInstall.ps1` file


## 1.9.8 (March 25, 2017)
### BUG FIXES
 * Fix - Ensure Chocolatey Licensed is compatible with Chocolatey v0.10.4.
 * Fix - AutoUninstaller - ensure uninstallExe is split by quotes when necessary - see [#1208](https://github.com/chocolatey/choco/issues/1208)
 * Package Synchronizer (Choco Sync):
    * Fix - Ensure template properties are cleared, even when skipping due to errors
 * Package Builder (Choco New):
    * Fix - Ensure authors is never empty
    * Fix - Before setting properties from registry, ensure they have a value.
    * Fix - Replace invalid ".-" in package id


## 1.9.7 (March 20, 2017)
### BUG FIXES
 * Fix - Support automatic decompression on downloads - see [#1056](https://github.com/chocolatey/choco/issues/1056)
 * Fix - Package Builder - Restrict Get-UninstallRegistryKey params in chocolateyUninstall.ps1
 * Fix - Package Internalizer - exit non-zero when variable replacement fails

### IMPROVEMENTS
 * Ensure PowerShell scripts use CRLF so authenticode verification doesn't fail
 * Install Directory should see `INSTALLFOLDER` MSI Property
 * Virus Scanner - provide context for other answers (No/Skip)
 * Allow both 32bit and 64bit file parameters with Install-ChocolateyInstallPackage - see [#1187](https://github.com/chocolatey/choco/issues/1187)


## 1.9.6 (March 3, 2017)
### BUG FIXES
 * Fix - Ensure silent args in logs are escaped
 * Fix - Package Internalizer - use console adapter for downloading

### IMPROVEMENTS
 * Ensure proxy values are used with Chocolatey v0.10.4+ - see [#1141](https://github.com/chocolatey/choco/issues/1141) and [#1165](https://github.com/chocolatey/choco/issues/1165)
 * Install - do not create a `.ignore` file outside Chocolatey directories - same as [#1180](https://github.com/chocolatey/choco/issues/1180)
 * Package Synchronizer (Choco Sync):
    * Use local directory for outputting created packages by default.
    * Specify output directory for created packages.


## 1.9.5 (January 31, 2017)
### BUG FIXES
 * Fix - Error when running Install-ChocolateyInstallPackage without specifying silent arguments
 * Remove *.istext file before the content-type check that creates file - see [#1012](https://github.com/chocolatey/choco/issues/1012)

### IMPROVEMENTS
 * Package Builder (Choco New):
    * Right click - create a package without bringing up the GUI.
 * Package Internalizer (Choco Download):
    * Don't delete the download directory unless `--force`
 * Set user modes for terminal services (`change user /install` | `change user /execute`)


## 1.9.4 (January 19, 2017)
### BUG FIXES
 * Fix - enabling preview features has no effect.


## 1.9.3 (January 17, 2017)
### BUG FIXES
 * Fix - trial users unable to use Business version of Package Builder UI.


## 1.9.2 (January 17, 2017)
### BUG FIXES
 * Fix - virus scanner not working properly


## 1.9.1 (January 16, 2017)
### BUG FIXES
 * Fix - Ensure Pro users can call Package Builder from the command line ("packagebuilder") after install
 * Fix - Correct title of package to "Chocolatey Licensed Edition"


## 1.9.0 (January 15, 2017)
This release brings the coveted PackageBuilder UI to the Pro+ license (minus auto detection) and adds a tabbed interface that allows you to fill out the entire nuspec. Package Synchronizer now has `choco sync` to compliment the automatic synchronization. The sync command brings all unmanaged software in Programs and Features under Chocolatey management.

### FEATURES
 * Package Synchronizer v2 - `choco sync` to associate existing packages with unmanaged software - see https://chocolatey.org/docs/features-synchronize#sync-command
 * Package Builder UI:
    * Starts at Professional edition (minus autodetection)
    * Tabbed interface
    * Tab for entire Nuspec
    * [Business] Tab to generate from Programs and Features

### BUG FIXES
 * Self-Service / Background Mode:
    * Fix - Add a line after progress is complete
 * Fix - Automatic creation of ignore file in Install-ChocolateyInstallPackage throws errors when it fails - see [#380](https://github.com/chocolatey/chocolatey/issues/380) for original issue.
 * Package Builder (Choco New):
    * Fix - remove "version/ver" if next to version number in DisplayName
    * Fix - todo / logging need to escape curly braces to properly format
    * Fix - continue on error

### IMPROVEMENTS
 * User can turn on Preview Features - `choco feature enable -n allowPreviewFeatures`
 * InstallDirectory switch added to Install-ChocolateyInstallPackage
 * Package Internalizer (Choco Download)
    * option to ignore dependencies `--ignore-dependencies`
 * Package Builder (Choco New)
    * Generates package arguments with install directory override
    * Add optional scripts - beforeModify/uninstall
    * Add other template files
    * Remove any version number from package id
    * allow for quiet logging
    * auto detection fills out more fields for MSIs
 * Self-Service / Background Mode Enhancements
    * Do not warn if command is `choco feature`
    * Provide user context when background service not available


## 1.8.4 (January 5, 2017)
### FEATURES
 * Support Self-Service Install / Background Mode - see https://chocolatey.org/docs/features-agent-service
 * Manage Windows Services

 We've introduced some service management functions to the business edition. `Install-ChocolateyWindowsService`, `Uninstall-ChocolateyWindowsService`, `Start-ChocolateyWindowsService`, and `Stop-ChocolateyWindowsService`. Those will be documented soon enough. For now some example code should suffice.

~~~powershell
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$serviceExe = Join-Path $toolsDir 'service\chocolatey-agent.exe'

$packageArgs = @{
  Name                  = 'chocolatey-agent'
  DisplayName           = 'Chocolatey Agent'
  Description           = 'Chocolatey Agent is a backgound service for Chocolatey.'
  StartupType           = 'Automatic'
  ServiceExecutablePath = $serviceExe
}

#Username, Password, -DoNotStartService are also considered

Install-ChocolateyWindowsService @packageArgs

# The other three methods simply take the service name.
Start-ChocolateyWindowsService -Name 'chocolatey-agent'
Stop-ChocolateyWindowsService -Name 'chocolatey-agent'
Uninstall-ChocolateyWindowsService -Name 'chocolatey-agent'
~~~

### BUG FIXES
 * Package Synchronizer - Do not run automatic sync when non-elevated. It fails in weird ways
 * Package Builder (Choco New):
    * Fix - fix "(Install)" - append space in nuspec Title.

### IMPROVEMENTS
 * Downloading remote files - don't show bytes, only formatted values
 * Authenticode sign licensed binaries


## 1.8.3 (December 21, 2016)
### FEATURES
 * Package Downloader/Internalizer (Choco Download):
    * Internalize package dependencies
    * Allow download to separate location than where package will edit for internal resources (`--download-location`)

### BUG FIXES
 * Fix - Directory Override - ensure override switch is passed to .install/.portable package when passing switch to meta package.
 * Fix - Ensure web requests don't time out / are configurable - see  [#732](https://github.com/chocolatey/choco/issues/732)
 * Package Builder (Choco New):
    * Fix - Cannot bind parameter because parameter 'fileType' is specified more than once for `--keep-remote`
    * Fix - UNC paths with `--use-original-location` should not use `Install-ChocolateyPackage`
    * Fix - Escape double quotes in PowerShell strings
 * Package Downloader/Internalizer (Choco Download):
    * Fix - Ignore commented out urls
    * Fix - Do not download from a url more than once even if a package has it listed more than once
    * Fix - Do not timeout for larger files

### IMPROVEMENTS
 * Directory Override - if MSI properties include INSTALLDIR or INSTALLLOCATION, use that instead of TARGETDIR
 * Package Builder (Choco New):
    * MSI Properties generated are cleaned up, duplicates removed from chocolateyInstall.ps1 comments
    * Generate from Programs and Features is faster, does not repeat for Uninstaller keys in both 32bit/64bit registry hives
    * Show downloaded file progress
 * Package Downloader/Internalizer (Choco Download):
    * When chocolateyInstall.ps1 uses other ps1 files, ensure those are loaded for token replacement
    * Show downloaded file progress


## 1.8.2 (December 12, 2016)
### BUG FIXES
 * Package Builder (Choco New):
    * Fix - Ignore first argument for name if not a name. This will also be fixed in Chocolatey v0.10.4 with [#1085](https://github.com/chocolatey/choco/issues/1085)
    * Fix - PackageBuilder UI will request administrative permissions when run by admin.
    * Fix - Urls should be set when using original location.
 * Package Downloader/Internalizer (Choco Download):
    * Fix - Do not use unparsed options as package names. Similar to [#983](https://github.com/chocolatey/choco/issues/983).
 * Countdown days counting in incorrect direction

### IMPROVEMENTS
 * Package Builder (Choco New):
    * version - add TODO if version is 0.0.0.0
    * outputdirectory option has more aliases.


## 1.8.1 (November 27, 2016)
### BUG FIXES
 * AutoUninstaller - Fix - do not fail on auto-detection of type when uninstall executable is not in the correct format.
 * Package Builder (Choco New):
    * Fix - Programs and Features - do not fail on auto-detection of type based on uninstall string

### IMPROVEMENTS
 * Package Builder (Choco New):
    * version - always set to 3 segments (x.y.z)
    * version - remove extra version segments when more than 4 segments are returned


## 1.8.0 (November 16, 2016)
Package Builder has some major improvements in this release, including a new UI! See https://chocolatey.org/blog/package-builder-announcements for details! While building some of the features for the enhancements for this release, we've made quite a few tweaks and fixes to Package Builder and we think you are going to like the results. Being able to generate packages from the installed software on a reference system is huge (`choco new --from-programs-and-features`)!

Pro users now have the ability to download packages (minus internalization). This is fantastic if you want to pull down a lot of packages quickly from a remote source. Or pull down packages and push them up to an internal source.

### FEATURES
 * Package Downloader comes to Pro+ Licenses - minus internalizer, you can now download a package using an easy command `choco download`.
 * Package Builder (Choco New):
    * Package Builder now has a UI - see https://youtu.be/qJNKR_PEQqY for details.
    * Right click on an exe, msi, zip (or other supported types) and click "Create Package..."
    * Package Builder - generate packages from installed software (Programs and Features) - see https://youtu.be/Mw_ReipnskI for details.

### BUG FIXES
 * Package Builder (Choco New):
    * Fix - silent arguments for MSU/MSP should not include the file itself.
    * Fix - remove comma if found.
    * Fix - remove trailing period.
    * Fix - Remove the entire word surrounding a version.
 * Package Internalizer (Choco Download):
    * Fix - mixed line endings were causing "Index was outside the bounds of the array" errors.
 * Package Synchronizer:
    * Fix - sync location should not be in the extensions folder, causes extension loading issues - a similar fix will be in open source to ignore the old sync location in v0.10.4 (see [#1041](https://github.com/chocolatey/choco/issues/1041)).
    * Fix - sync over an existing synced package without error.

### IMPROVEMENTS
 * Uninstall - Uninstall software not managed with Chocolatey. Use something like `choco uninstall 7-zip* --from-programs-and-features` to ensure removal from Programs and Features directly. Also requires Chocolatey v0.10.4.
 * VirusTotal - When a user cancels a virus check, set the proper exit code so that rollback occurs automatically - see [#985](https://github.com/chocolatey/choco/issues/985).
 * Package Builder (Choco New):
    * Return a TODO list when there are more things to do to finish packaging.
    * Set values to replace if they are not set.
    * Use company name for nuspec copyright if copyright is not available.
    * Provide more verbose logging when necessary.
    * Remove more installer wording - "setup", "remove only".
    * If we exhaust all other options for determining name and version, use the file name and split version from name.
    * Add or remove architecture from the package id - `choco new --include-architecture-in-name` and `choco new --remove-architecture-from-name`, respectively.
 * Package Synchronizer:
    * Synchronize to a new location per package version.


## 1.7.0 (Sep 22, 2016)
### BUG FIXES
 * Package Builder (Choco New) - Removed `fileFullPath` from install template - add back in when local zip only. Fixes an error as `file` and `fileFullPath` are aliases starting in 0.10.1.
 * Package Internalizer (Choco Download) - ensure logging does not incur log format exceptions
 * install/upgrade - Look for downloaded file at old path if replaced 'chocolatey\chocolatey' path does not exist - see [#969](https://github.com/chocolatey/choco/issues/969)

### IMPROVEMENTS
 * Package Builder (Choco New):
    * `--build-package` to build a package after Package Builder creates the package all in one go.
    * `--pause-on-error` to pause the output when there are errors for closer inspection (useful when run from a batch file).
    * Specify checksum and type - checksum will be verified against files/downloaded files.
    * Loads of improvements in how package id and title are determined and cleaned up.
    * Product versions and portions of product versions are removed from package id/title.
 * Package Internalizer (Choco Download):
    * Add `--internalize` as alias to `--recompile`.
    * Warn of issue with `-UseOriginalLocation` when only using one file.
    * Add `--append-useoriginallocation` and feature internalizeAppendUseOriginalLocation that makes the determination to add to the end of `Install-ChocolateyPackage` when using local resources.


## 1.6.3 (Sep 20, 2016)
### BUG FIXES
 * Require Chocolatey be upgraded to at least 0.10.1 due to internal incompatibilities that affect this extension.


## 1.6.2 (Sep 19, 2016 - pulled)
### BUG FIXES
 * Recompiled to work with 0.10.1. There were some internal changes that appear to affect the virus scanner and PackageBuilder.

### IMPROVEMENTS
 * Install/upgrade - support MSP (patch files)
 * PackageBuilder - support MSU/MSP files


## 1.6.1 (Sep 8, 2016)
### BUG FIXES
 * Package Builder (Choco New):
    * Fix - Do not error on missing appsearch table in MSI.
    * Fix - Do not add similarly named items from AppSearch table to template properties more than once.


## 1.6.0 (Sep 8, 2016)
Some really big improvements are now available in v1.6.0. We are excited to share them with you!

### FEATURES
 * Licensed Enhancements:
    * install/upgrade - pass sensitive arguments that are not shown/logged in Chocolatey to an installer - useful when you want to pass passwords but don't want them logged. Need Chocolatey v0.10.1+.
    * AutoUninstaller - determine type from original executable when FOSS is not able to detect installer type.
 * Package Builder (Choco New):
    * Now supports downloading from url/url64 and determining whether to keep those files remote.
    * Switch to use original file location instead of copying into package
    * Specify both 32-bit/64-bit file
    * Work with zip files

### BUG FIXES
 * Fix - changes related to working directory fixes for [#937](https://github.com/chocolatey/choco/issues/937)
 * Fix - double chocolatey folder name is not also applied to the passed in file name - see  [#908](https://github.com/chocolatey/choco/issues/908)
 * Package Builder (Choco New):
    * Fix - remove parentheses from package names
    * Fix - keep template updated
 * Package Internalizer (Choco Download):
    * Fix - handle downloaded files with the same name Sometimes the file name is the same when the architecture is different. Handle that by using the url counter for all additional files with the same name.

### IMPROVEMENTS
 * Remind About Upcoming Expiration - when the license is expiring within a month's time, remind the user about renewal
 * Package Builder (Choco New):
    * Handle -forcex86 with package creation
    * Add shimgen ignore for exes
    * Use ProductVersion when version 0.0.0.0
    * Remove the word "installer" from package name
    * Allow specifying name of the package
    * Allow template override with warning
    * Show MSI properties in install script (commented)
    * Ensure `ALLUSERS=1` when an MSI is set to per user by default
    * Automatically checksum files
    * Allow files to stay remote - use remote helpers when files stay remote
 * Package Internalizer (Choco Download):
    * handle variables in urls set like ${word}
    * Append `-UseOriginalLocation` to the end of the arguments passed to Install-ChocolateyPackage. Work with splatting properly as well


## 1.5.1 (Aug 9, 2016)
### BUG FIXES
 * Fix - Valid Exit Codes do not support values bigger than Int32.MaxValue. - see [#900](https://github.com/chocolatey/choco/issues/900)

### IMPROVEMENTS
 * Package Internalizer (Choco Download) - specify resources location (when not embedding into package)


## 1.5.0 (July 21, 2016)
### FEATURES
 * [Business] Recompiled packages support aka Package Internalizer - Download a package and all remote resources, recompiling the package to use local resources instead.
 * Synchronize w/Programs and Features - Chocolatey synchronizes manually uninstalled software with package state.

### BUG FIXES
 * Fix - Silent Args being passed as a string array cause package failure - see [#808](https://github.com/chocolatey/choco/issues/808)

### IMPROVEMENTS
 * VirusTotal - allow skipping check entirely - [#786](https://github.com/chocolatey/choco/issues/786)
 * Trial allows more features to work, but in a way that is not automatable.


## 1.4.2 (June 20, 2016)
### BUG FIXES
 * Fix - Logging is broken in some packages due to new TEMP directory - [#813](https://github.com/chocolatey/choco/issues/813)

### IMPROVEMENTS
 * Ensure log file path exists - [#758](https://github.com/chocolatey/choco/issues/758)


## 1.4.1 (June 14, 2016)
### BUG FIXES
 * PowerShell v2 assembly was not loading. There was a dependency on an incorrect version of PowerShell assemblies, causing it to only attempt to load System.Management.Automation v3 and above - [#799](https://github.com/chocolatey/choco/issues/799)


## 1.4.0 (June 13, 2016)
### FEATURES
 * BETA Testers - Recompiled packages support - Download a package and all remote resources, recompiling the package to use local resources instead.
 * BETA Testers - Synchronize w/Programs and Features - Chocolatey synchronizes manually uninstalled software with package state.
 * [Business] Create Packages from Installers aka Package Builder! Create packages directly from software installers in seconds! **Chocolatey for Business can automatically create packages for all the software your organization uses in under 5 minutes!**
 * New Command! choco support - quickly see how you can contact support - [#745](https://github.com/chocolatey/choco/issues/745)
 * Web functions for local files support - [#781](https://github.com/chocolatey/choco/issues/781)

### IMPROVEMENTS
 * Support FIPS compliant algorithms [#446](https://github.com/chocolatey/choco/issues/446)


## 1.3.2 (May 28, 2016)
### BUG FIXES
 * Get-WebFile name changes related to [#753](https://github.com/chocolatey/choco/issues/753)

### IMPROVEMENTS
 * Clarified options with version and better messaging.


## 1.3.1 (May 9, 2016)
### BUG FIXES
 * Get-WebFile name changes related to [#727](https://github.com/chocolatey/choco/issues/727)

### IMPROVEMENTS
 * Report directory switch override.


## 1.3.0 (May 2, 2016)
### FEATURES
 * Ubiquitous Install Directory Switch! When working with properly formed packages that use Install-ChocolateyPackage (or Install-ChocolateyInstallPackage), Chocolatey is able to override the native installer's directory from one single option you provide to Chocolatey. You no longer need to know what the installer type is and provide that through install arguments. See `choco install -?` and `--install-directory` option for details.
 * Generic Virus Scanner - for organizations that don't want to run checks using VirusTotal, we've provided a way for organizations to use their own virus scanner. See `choco config list` for details.

### BUG FIXES
* Fix - Content Length check may error if original location is changed. This means the permanent download location will not error on other checks.
* Fix - Original remote file name can be affected if original url has changed or is unavailable.

### IMPROVEMENTS
 * Virus Scanner exits as soon as possible on files too big for the scanner. If the file is over 500MB, the scanner cannot upload the file, so it should not ask whether it can try to upload prior to failing on the size check (previous behavior).


## 1.2.0 (March 14, 2016)
### FEATURES
 * Virus scanning for Pro users! See the [post](https://www.kickstarter.com/projects/ferventcoder/chocolatey-the-alternative-windows-store-like-yum/posts/1518468) for details!


## 1.1.0 (February 12, 2016)
### IMPROVEMENTS
 * License can now be in user profile (like `c:\Users\yourname\chocolatey.license.xml`). This is great for roaming user profiles and in multiple machine usage scenarios.
 * Download cache can be controlled with a feature flag and/or a command option. See `choco feature` and `choco install -h` for more details.

### For BETA Testers
 * Virus Check improvements
   * Throw if virus check has not been done before.
   * Messaging is clarified
   * Skip or run virus check with command options - see `choco install -h` for details.


## 1.0.2 (February 5, 2016)
### BUG FIXES
* Fix - PowerShell 5 respects Cmdlet aliases, causing overrides on functions not ready (Install-ChocolateyPackage). See the [post](https://www.kickstarter.com/projects/ferventcoder/chocolatey-the-alternative-windows-store-like-yum/posts/1484093) for details.


## 1.0.1 (February 2, 2016)
### BUG FIXES
* Fix - License location validation is incorrect.


## 1.0.0 (February 1, 2016)
### FEATURES
* Alternate Permanent Download Location - see the [post](https://www.kickstarter.com/projects/ferventcoder/chocolatey-the-alternative-windows-store-like-yum/posts/1479944) for details.
