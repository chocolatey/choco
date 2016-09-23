# Chocolatey Licensed CHANGELOG

This covers changes for the "chocolatey.extension" package, where the licensed editions of Chocolatey get their enhanced functionality.

**NOTE**: If you have a licensed edition of Chocolatey, refer to this in tandem with [Chocolatey Open source CHANGELOG](https://github.com/chocolatey/choco/blob/master/CHANGELOG.md).

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
    * Fix - Do not add similarly named items from AppSearch table to template properties more than once


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
