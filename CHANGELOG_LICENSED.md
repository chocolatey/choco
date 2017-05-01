# Chocolatey Licensed CHANGELOG

This covers changes for the "chocolatey.extension" package, where the licensed editions of Chocolatey get their enhanced functionality.

**NOTE**: If you have a licensed edition of Chocolatey, refer to this in tandem with [Chocolatey Open source CHANGELOG](https://github.com/chocolatey/choco/blob/master/CHANGELOG.md).

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
 * Package Internalizer (Choco Download)
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
