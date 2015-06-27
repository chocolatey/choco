## [0.9.9.8](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.8+is%3Aclosed) (June 26, 2015)

### BUG FIXES

 * Fix: choco install -y C: deletes all files - see [#341](https://github.com/chocolatey/choco/issues/341)
 * Fix: Read-Host halts scripts rather than prompt for input - see [#219](https://github.com/chocolatey/choco/issues/219)

### IMPROVEMENTS

 * Download Progress Bar is Missing - see [#56](https://github.com/chocolatey/choco/issues/56)

## [0.9.9.7](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.7+is%3Aclosed) (June 20, 2015)

"Fix Everything. Fix All The Things" - There have been some things bugging us for a long time related to limitations with NuGet, so we decided to fix that. Like [nuspec enhancements](https://github.com/chocolatey/choco/issues/205), that crazy [content folder restriction](https://github.com/chocolatey/choco/issues/290) has been removed (I know, right?!), and we're working around [badly](https://github.com/chocolatey/choco/issues/316) [behaved](https://github.com/chocolatey/choco/issues/326) packages quite a bit more to bring you more feature parity.

Let's talk about a couple of big, like really big, BIG features just added with this release. No more packages rebooting Windows. We fixed ([#304](https://github.com/chocolatey/choco/issues/304) / [#323](https://github.com/chocolatey/choco/issues/323)) and [enhanced](https://github.com/chocolatey/choco/issues/305) up the Auto Uninstaller Service quite a bit to ensure things are working like you would expect (It goes on by default in 0.9.10 - we'll start documenting more about it soon). But wait, there's more! I haven't even told you about the big features yet

The first big feature is enhancing the nuspec. I mentioned this I know, but *now* you can use `packageSourceUrl` in the nuspec to tell folks where you are storing the source for the package! We also added `projectSourceUrl`, `docsUrl`, `mailingListUrl`, and `bugTrackerUrl`. What's even better is that the community feed has already been enhanced to look for these values. So have the templates from `choco new`. And it's backwards compatible, meaning you can still install packages that have these added nuspec enhancements without issue (but we will need to provide a fix for Nuget Package Explorer).

The second is Xml Document Transformations (XDT), which I think many folks are aware of but may not realize what it can provide. [NuGet has allowed transformations for quite awhile](https://docs.nuget.org/Create/Configuration-File-and-Source-Code-Transformations) to allow you to make changes to an `app.config`/`web.config` on install/uninstall. We are following in similar footsteps to allow you to do similar when installing/upgrading packages. We will look for `*.install.xdt` files in the package (doesn't matter where) and they will apply to configuration files with the same name in the package. This means that during upgrades we won't overwrite configuration files during upgrades that have opted into this feature. It allows you to give users a better experience during upgrades because they won't need to keep making the same changes to the xml config files each time they upgrade your package.

### FEATURES

 * Allow XDT Configuration Transforms - see [#331](https://github.com/chocolatey/choco/issues/331)
 * Prevent reboots - see [#316](https://github.com/chocolatey/choco/issues/316)
 * Enhance the nuspec - first wave - see [#205](https://github.com/chocolatey/choco/issues/205)
 * Uninstaller Service Enhancements - see [#305](https://github.com/chocolatey/choco/issues/305)

### BUG FIXES

 * When uninstall fails, do not continue removing files - see [#315](https://github.com/chocolatey/choco/issues/315)
 * Do not run autouninstaller if the package result is already a failure - see [#323](https://github.com/chocolatey/choco/issues/323)
 * Fix - Auto Uninstaller can fail if chocolateyUninstall.ps1 uninstalls prior to it running - see [#304](https://github.com/chocolatey/choco/issues/304)
 * Fix - Packages with content folders cannot have a dependency without also having a content folder - see [#290](https://github.com/chocolatey/choco/issues/290)
 * Remove ShimGen director files on upgrade/uninstall - see [#326](https://github.com/chocolatey/choco/issues/326)
 * If feature doesn't exist, throw an error - see [#317](https://github.com/chocolatey/choco/issues/317)
 * Fix - The operation completed successfully on stderr - see [#249](https://github.com/chocolatey/choco/issues/249)
 * Fix - When specific nuget version is needed by a package it is the chocolatey version that is used - see [#194](https://github.com/chocolatey/choco/issues/194)
 * When installing with *.nupkg, need to get package name from package, not file name - see [#90](https://github.com/chocolatey/choco/issues/90)
 * Fix - Choco pin list is not returning a list - see [#302](https://github.com/chocolatey/choco/issues/302)
 * Fix - A pin is not created for existing installations (prior to new choco) - see [#60](https://github.com/chocolatey/choco/issues/60)

### IMPROVEMENTS

 * Allow upgrade to always install missing packages - see [#300](https://github.com/chocolatey/choco/issues/300)
 * Enhance Templates - see [#296](https://github.com/chocolatey/choco/issues/296)
 * Always log debug output to the log file - see [#319](https://github.com/chocolatey/choco/issues/319)
 * Warn when unable to snapshot locked files - see [#313](https://github.com/chocolatey/choco/issues/313)
 * Use %systemroot% in place of %windir%. PATH exceed 2048 breaks choco - see [#252](https://github.com/chocolatey/choco/issues/252)
 * Add fault tolerance to registry snapshot checks - see [#337](https://github.com/chocolatey/choco/issues/337)

## [0.9.9.6](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.6+is%3Aclosed) (May 16, 2015)

Some really large fixes this release, especially removing all files that are installed to the package directory if they haven't changed, including ensuring that the nupkg file is always removed on successful uninstalls. The really big add some folks are going to like is the new outdated command. Some more variables that were misused have been brought back, which allows some packages (like Atom) to be installed again without issue. If you can believe some people never read these, we decided to add a note to the installer prompt to let people know about -y.

### FEATURES

 * Outdated Command - Use `choco outdated` to see outdated packages - see [#170](https://github.com/chocolatey/choco/issues/170)

### BUG FIXES

 * Fix - NotSilent Switch Not Working - see [#281](https://github.com/chocolatey/choco/issues/281)
 * Fix - Silent installation of choco without admin is not possible - see [#274](https://github.com/chocolatey/choco/issues/274)
 * Fix - Package resolves to latest version from any source - see [#279](https://github.com/chocolatey/choco/issues/279)
 * Fix - Install fails when shortcut creation fails - see [#264](https://github.com/chocolatey/choco/issues/264)
 * Fix - Error deserializing response of type Registry - see [#257](https://github.com/chocolatey/choco/issues/257)
 * Fix - Auto uninstaller should not depend on optional InstallLocation value - see [#255](https://github.com/chocolatey/choco/issues/255)
 * Fix - Nupkg is left but reported as successfully uninstalled by NuGet - see [#254](https://github.com/chocolatey/choco/issues/254)
 * Fix - SHA1 checksum compared as MD5 for Install-ChocolateyZipPackage - see [#253](https://github.com/chocolatey/choco/issues/253)
 * Fix - Auto uninstaller strips off "/" and "-" in arguments - see [#212](https://github.com/chocolatey/choco/issues/212)

### IMPROVEMENTS

 * Uninstall removes all installed files if unchanged - see [#121](https://github.com/chocolatey/choco/issues/121)
 * Auto uninstaller should convert /I to /X for Msi Uninstalls - see [#271](https://github.com/chocolatey/choco/issues/271)
 * Bring back more variables for feature parity - see [#267](https://github.com/chocolatey/choco/issues/267)
 * Mention -y in the prompt - see [#265](https://github.com/chocolatey/choco/issues/265)

## [0.9.9.5](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.5+is%3Aclosed) (April 20, 2015)

### BREAKING CHANGES

 * Renamed short option `p` to `i` for list --include-programs so that `p` could be ubiquitous for password across commands that optionally can pass a password - see [#240](https://github.com/chocolatey/choco/issues/240)

### BUG FIXES

 * Fix - Secure Sources Not Working - see [#240](https://github.com/chocolatey/choco/issues/240)
 * Fix - Generate-BinFile / Remove-BinFile - see [#230](https://github.com/chocolatey/choco/issues/230)
 * Fix - cpack should only include files from nuspec - see [#232](https://github.com/chocolatey/choco/issues/232)
 * Fix - cpack should leave nupkg in current directory - see [#231](https://github.com/chocolatey/choco/issues/231)
 * Fix - Install-PowerShellCommand uses incorrect path - see [#241](https://github.com/chocolatey/choco/issues/241)
 * Fix - choco list source with redirects does not resolve - see [#171](https://github.com/chocolatey/choco/issues/171)
 * Fix - choco tried to resolve disabled repo - see [#169](https://github.com/chocolatey/choco/issues/169)
 * Fix - cpack nuspec results in "The path is not of a legal form" - see [#164](https://github.com/chocolatey/choco/issues/164)
 * Fix - cpack hangs on security related issue - see [#160](https://github.com/chocolatey/choco/issues/160)
 * Fix - spelling error in "package has been upgradeed successfully" - see [#64](https://github.com/chocolatey/choco/issues/64)

### IMPROVEMENTS

 * Api Key and Source matching could be more intuitive - see [#228](https://github.com/chocolatey/choco/issues/238)
 * Remove warning about allowGlobalConfirmation being enabled - see [#237](https://github.com/chocolatey/choco/issues/237)
 * Include log file path when saying 'See the log for details' - see [#187](https://github.com/chocolatey/choco/issues/187)
 * Uninstall prompts for version when there is only one installed - see [#186](https://github.com/chocolatey/choco/issues/186)
 * Do not offer a default option when prompting for a user choice - see [#185](https://github.com/chocolatey/choco/issues/185)
 * Remove the warning note about skipping, and instead show the warning when selecting skip - see [#183](https://github.com/chocolatey/choco/issues/183)
 * Do not print PowerShell install/update scripts by default - see [#182](https://github.com/chocolatey/choco/issues/182)

## [0.9.9.4](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.4+is%3Aclosed) (March 30, 2015)

### BUG FIXES

 * Fix - The term 'false' is not recognized as the name of a cmdlet - see [#215](https://github.com/chocolatey/choco/issues/215)

### IMPROVEMENTS

 * Some packages use non-API variables like $installArguments - see [#207](https://github.com/chocolatey/choco/issues/207)

## [0.9.9.3](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.3+is%3Aclosed) (March 29, 2015)

### BUG FIXES

 * Fix - Install .NET Framework immediately during install - see [#168](https://github.com/chocolatey/choco/issues/168)
 * Fix - Do not error on Set-Acl during install/upgrade - see [#163](https://github.com/chocolatey/choco/issues/163)
 * Fix - Do not escape curly braces in powershell script - see [#208](https://github.com/chocolatey/choco/issues/208)
 * Fix - Formatting issues on --noop command logging - see [#202](https://github.com/chocolatey/choco/issues/202)
 * Fix - Uninstaller check doesn't find 32-bit registry keys - see [#197](https://github.com/chocolatey/choco/issues/197)
 * Fix - Uninstaller errors on short path to msiexec - see [#211](https://github.com/chocolatey/choco/issues/211)

### IMPROVEMENTS

 * Some packages use non-API variables like $installArguments - see [#207](https://github.com/chocolatey/choco/issues/207)
 * Add Generate-BinFile to Helpers (widely used but never part of API) - see [#145](https://github.com/chocolatey/choco/issues/145)
 * Add Remove-BinFile to Helpers - see [#195](https://github.com/chocolatey/choco/issues/195)
 * Get-ChocolateyWebFile should create path if it doesn't exist - see [#167](https://github.com/chocolatey/choco/issues/167)

## [0.9.9.2](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.2+is%3Aclosed) (March 6, 2015)

### BUG FIXES

 * Fix - Allow passing install arguments again (regression in 0.9.9 series) - see [#150](https://github.com/chocolatey/choco/issues/150)
 * Fix - Allow apostrophes to be used as quotes - quoting style that worked with previous client - see [#141](https://github.com/chocolatey/choco/issues/141)
 * Fix - Shims write errors to stderr - see [#142](https://github.com/chocolatey/choco/issues/142) and [ShimGen #14](https://github.com/chocolatey/shimgen/issues/14)

### IMPROVEMENTS

 * Upgrade `-r` should always return a value - see [#153](https://github.com/chocolatey/choco/issues/153)

## [0.9.9.1](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.1+is%3Aclosed) (March 3, 2015)

### BUG FIXES

 * Fix - Get-BinRoot broken - see [#144](https://github.com/chocolatey/choco/issues/144)

## [0.9.9](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9+is%3Aclosed) (March 3, 2015)

This also includes issues that were being tracked in the old Chocolatey repository: [Chocolatey 0.9.9](https://github.com/chocolatey/chocolatey/issues?q=is%3Aclosed+label%3Av0.9.9).

The two links above will not capture everything that has changed, since this is a complete rewrite. We broke everything. If this were a v1+, it would be a major release. But we are less than v1, so 0.9.9 it is! ;)

Okay, so we didn't really break everything. We have maintained nearly full compatibility with how you pass options into choco, although the output may be a bit different (but better, we hope) and in at least one case, additional switches (or a feature setting) is/are required - we limited this to security related changes only.

We also fixed and improved a bunch of things, so we feel the trade off is well worth the changes.

We'll try to capture everything here that you should know about. Please call `choco -?` or `choco.exe -h` to get started.

### KNOWN ISSUES

 * [Known Issues](https://github.com/chocolatey/choco/labels/Bug)
 * TEMPORARY `install all` is missing - this is expected to be back in 0.9.10 - see [#23](https://github.com/chocolatey/choco/issues/23)
 * Alternative sources (`webpi`,`ruby`,`python`,`cygwin`, `windowsfeature`) do not work yet. This is expected to be fixed in 0.9.10 - see [#14](https://github.com/chocolatey/choco/issues/14)
 * Progress bar is missing when downloading until we are using internal posh components for Packages - see [#56](https://github.com/chocolatey/choco/issues/56)
 * See [Feature Parity](https://github.com/chocolatey/choco/labels/FeatureParity) for items not yet reimplemented from older PowerShell Chocolatey client (v0.9.8.32 and below).

### BREAKING CHANGES

 * [Security] **Prompt for confirmation**: For security reasons, we now stop for confirmation before changing the state of the system on most commands. You can pass `-y` to confirm any prompts or set a value in the config that will globally confirm - see [#52](https://github.com/chocolatey/choco/issues/52) (**NOTE**: This is one of those additional switches we were talking about)
 * [Security] If your default installation is still at `c:\Chocolatey`, this version will force a move to ProgramData and update the environment settings - see [#7](https://github.com/chocolatey/choco/issues/7)
 * **Configuration Breaking Changes:**
   1. You now have one config file to interact with in %ChocolateyInstall%\config - your user config is no longer valid and can be removed once you migrate settings to the config.
   2. The config will no longer be overwritten on upgrade.
   3. Choco no longer interacts with NuGet's config file at all. You will need to reset all of your apiKeys (see features for `apikey`). On the plus side, the keys will work for all users of the machine, unlike NuGet's apiKeys (only work for the user that sets them).
   4. This also means you can no longer use `useNugetForSources`. It has been removed as a config setting.
 * **Packaging Changes:**
   1. Choco now installs packages without version numbers on folders. This means quite a few things...
   2. Upgrading packages doesn't install a new version next to an old version, it actually upgrades.
   3. Dependencies resolve at highest available version, not the minimum version as before - see [Chocolatey #415](https://github.com/chocolatey/chocolatey/issues/415)
 * **Package Maintenance Changes**:
   1. Read the above about apikey changes
   2. Read above about dependency resolution changes.
 * **Deprecated/Removed Commands:**
   1. `installmissing` has been removed. It was deprecated awhile ago, so this should not be a surprise.
   2. `choco version` has been deprecated and will be removed in v1. Use `choco upgrade pkgName --noop` or `choco upgrade pkgName -whatif` instead.
   3. `Write-ChocolateySuccess`, `Write-ChocolateyFailure` have been deprecated.
   4. `update` is now `upgrade`. `update` has been deprecated and will be removed/replaced in v1. Update will be reincarnated later for a different purpose. **Hint**: It rhymes with smackage pindexes.

### FEATURES

 * In app documentation! Use `choco -?`, `choco -h` or `choco commandName -?` to learn about each command, complete with examples!
 * WhatIf/Noop mode for all commands (`--noop` can also be specified as `-whatif`) - see [Chocolatey #263](https://github.com/chocolatey/chocolatey/issues/263) and [Default Options and Switches](https://github.com/chocolatey/choco/wiki/CommandsReference#default-options-and-switches)
 * Performs like a package manager, expect to see queries failing because of unmet dependency issues.
 * **New Commands:**
   1. `pin` - Suppress upgrades. This allows you to 'pin' an install to a particular version - see [#1](https://github.com/chocolatey/choco/issues/1), [Chocolatey #5](https://github.com/chocolatey/chocolatey/issues/5) and [Pin Command](https://github.com/chocolatey/choco/wiki/CommandsPin)
   2. `apikey` - see [ApiKey Command](https://github.com/chocolatey/choco/wiki/CommandsApiKey)
   3. `new` - see [New Command](https://github.com/chocolatey/choco/wiki/CommandsNew) and [Chocolatey #157](https://github.com/chocolatey/chocolatey/issues/157)
 * New ways to pass arguments! See [How to Pass Options/Switches](https://github.com/chocolatey/choco/wiki/CommandsReference#how-to-pass-options--switches)
 * Did we mention there is a help menu that is actually helpful now? Shiny!
 * AutoUninstaller!!!! But it is not enabled by default this version. See [#15](https://github.com/chocolatey/choco/issues/15), [#9](https://github.com/chocolatey/choco/issues/9) and [Chocolatey #6](https://github.com/chocolatey/chocolatey/issues/6)
 * **New Helpers:**
   1. `Install-ChocolateyShortcut` - see [Chocolatey #238](https://github.com/chocolatey/chocolatey/pull/238), [Chocolatey #235](https://github.com/chocolatey/chocolatey/issues/235) and [Chocolatey #218](https://github.com/chocolatey/chocolatey/issues/218)

### BUG FIXES

Probably a lot of bug fixes that may not make it here, but here are the ones we know about.

 * Fix - Cannot upgrade from prerelease to same version released - see [Chocolatey #122](https://github.com/chocolatey/chocolatey/issues/122)
 * Fix - install `--force` should not use cache - see [Chocolatey #199](https://github.com/chocolatey/chocolatey/issues/199)
 * Fix - force dependencies as well - see [--force-dependencies](https://github.com/chocolatey/choco/wiki/CommandsInstall) and [Chocolatey #199](https://github.com/chocolatey/chocolatey/issues/199)
 * Fix - Chocolatey should not stop on error - see [Chocolatey #192](https://github.com/chocolatey/chocolatey/issues/192)
 * Fix - Upgrading does not remove previous version - see [Chocolatey #259](https://github.com/chocolatey/chocolatey/issues/259)
 * Fix - Non-elevated shell message spills errors - see [Chocolatey #540](https://github.com/chocolatey/chocolatey/issues/540)
 * Fix - Package names are case sensitive for some sources - see [Chocolatey #589](https://github.com/chocolatey/chocolatey/issues/589)
 * Fix - Install-ChocolateyVsixPackage doesn't check for correct VS 2012 path - see [Chocolatey #601](https://github.com/chocolatey/chocolatey/issues/601)
 * Fix - Chocolatey behaves strangely after ctrl+c - see [Chocolatey #608](https://github.com/chocolatey/chocolatey/issues/608)
 * Fix - Uninstall doesn't respect version setting - see [Chocolatey #612](https://github.com/chocolatey/chocolatey/issues/612)
 * Fix - No update after download error - see [Chocolatey #637](https://github.com/chocolatey/chocolatey/issues/637)
 * Fix - cup ends silently on error - see [Chocolatey #312](https://github.com/chocolatey/chocolatey/issues/312)
 * Fix - cpack silently fails when dependency .NET 4.0+ is not met - see [Chocolatey #270](https://github.com/chocolatey/chocolatey/issues/270)
 * Fix - Regression in cver all in 0.9.8.27 - see [Chocolatey #530](https://github.com/chocolatey/chocolatey/issues/530)
 * Fix - Certain installs and updates fail with a "process with an Id of xxxx is not running" error - see [Chocolatey #603](https://github.com/chocolatey/chocolatey/issues/603)

### IMPROVEMENTS

 * [Security] Allow keeping `c:\chocolatey` install directory with environment variable - see [#17](https://github.com/chocolatey/choco/issues/17)
 * [Security] Require switch on unofficial build - see [#36](https://github.com/chocolatey/choco/issues/36)
 * Install script updates  - see [#7](https://github.com/chocolatey/choco/issues/7)
 * Ensure Chocolatey pkg is installed properly in lib folder - This means you can take a dependency on a minimum version of Chocolatey (we didn't like that before) - see [#19](https://github.com/chocolatey/choco/issues/19)
 * Uninstall - allow abort - see [#43](https://github.com/chocolatey/choco/issues/43)
 * Support for HTTPS basic authorization - see [Chocolatey #128](https://github.com/chocolatey/chocolatey/issues/128)
 * Smooth out success/failure logging - see [Chocolatey #154](https://github.com/chocolatey/chocolatey/issues/154)
 * Add $env:CHOCOLATEY_VERSION - see [Chocolatey #251](https://github.com/chocolatey/chocolatey/issues/251)
 * Replace ascii cue with visual cues - see [Chocolatey #376](https://github.com/chocolatey/chocolatey/pull/376)
 * Uninstall all versions of an app - see [Chocolatey #389](https://github.com/chocolatey/chocolatey/issues/389)
 * Add parameters in packages.config files - see [Packages.config](https://github.com/chocolatey/choco/wiki/CommandsInstall#packagesconfig), [Chocolatey #472](https://github.com/chocolatey/chocolatey/issues/472), and [#10](https://github.com/chocolatey/choco/issues/10)
 * Choco pack should support `-version` - see [Chocolatey #526](https://github.com/chocolatey/chocolatey/issues/526)
 * Enhancements to Start-ChocolateyProcessAsAdmin - see [Chocolatey #564](https://github.com/chocolatey/chocolatey/pull/564)
 * Install-ChocolateyFileAssociation - add label to new file types - see [Chocolatey #564](https://github.com/chocolatey/chocolatey/pull/564)
 * Clean up the verobsity of Chocolatey - see [Chocolatey #374](https://github.com/chocolatey/chocolatey/issues/374)
 * Compact choco upgrade --noop option - see [Chocolatey #414](https://github.com/chocolatey/chocolatey/issues/414)
 * Remove references to the Chocolatey gods - see [Chocolatey #669](https://github.com/chocolatey/chocolatey/issues/669)
 * Shims now have noop (`--shimgen-noop`) and help (`--shimgen-help`) switches - see [ShimGen #8](https://github.com/chocolatey/shimgen/issues/8) and [ShimGen #10](https://github.com/chocolatey/shimgen/issues/10)
 * Shims will terminate underlying process on termination signal - see [ShimGen #11](https://github.com/chocolatey/shimgen/issues/11)
 * Shims now have gui (`--shimgen-gui`) and exit (`--shimgen-exit`) switches - see [ShimGen #13](https://github.com/chocolatey/shimgen/issues/13) and [ShimGen #12](https://github.com/chocolatey/shimgen/issues/12)
 * Dat help menu tho. I mean srsly guise - see [Chocolatey #641](https://github.com/chocolatey/chocolatey/issues/641)

##[0.9.8.33](https://github.com/chocolatey/chocolatey/issues?q=label%3Av0.9.8.33+is%3Aclosed) (Feb 11, 2015)

FEATURES:

 * Dynamically export helpers (this fixes helpers that were not visible before) - [#628](https://github.com/chocolatey/chocolatey/pull/628)

IMPROVEMENTS:

 * Accept `-y` as a parameter, Add warning about -y for 0.9.9.
 * Company name misspelled in shims - [#673](https://github.com/chocolatey/chocolatey/issues/673) and [shimgen #9](https://github.com/chocolatey/shimgen/issues/9)

##[0.9.8.32](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.32&page=1&state=closed) (January 22, 2015)

BUG FIXES:

 * Fix - Chocolatey-Install should return non-zero exit code if chocolateyInstall.ps1 fails - [#568](https://github.com/chocolatey/chocolatey/issues/568) & [#658](https://github.com/chocolatey/chocolatey/pull/658)

##[0.9.8.31](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.31&page=1&state=closed) (January 7, 2015)

BUG FIXES:

 * Fix - Shim doesn't always shift off the first argument - [#655](https://github.com/chocolatey/chocolatey/issues/655) & [ShimGen #7](https://github.com/chocolatey/shimgen/issues/7)
 * Fix - If executable isn't available, fallback to default icon - [#579](https://github.com/chocolatey/chocolatey/issues/579)

##[0.9.8.30](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.30&page=1&state=closed) (January 6, 2015)

FEATURES:

 * Use icon of the executable with generated shim - [#579](https://github.com/chocolatey/chocolatey/issues/579) & [ShimGen #2](https://github.com/chocolatey/shimgen/issues/2)

BUG FIXES:

 * Fix - Shims don't correctly handle spaces in path to shim - [#654](https://github.com/chocolatey/chocolatey/issues/654) & [ShimGen #5](https://github.com/chocolatey/shimgen/issues/5)

##[0.9.8.29](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.29&page=1&state=closed) (January 2, 2015)

FEATURES:

 * Use icon of the executable with generated shim - [#579](https://github.com/chocolatey/chocolatey/issues/579) & [ShimGen #2](https://github.com/chocolatey/shimgen/issues/2)
 * Allow setting custom temp download location - [#307](https://github.com/chocolatey/chocolatey/issues/307)

IMPROVEMENTS:

 * Don't assume $env:TEMP or $env:UserProfile are set - [#647](https://github.com/chocolatey/chocolatey/issues/647)
 * Remove Kickstarter message.

##[0.9.8.28](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.28&page=1&state=closed) (November 4, 2014)

BREAKING CHANGES:

 * You may need to update your saved API key for chocolatey, due to [#599](https://github.com/chocolatey/chocolatey/issues/599) we have switched push to ensure https.

BUG FIXES:

 * Fix - Shim argument parsing needs fixed for quoting - [ShimGen #1](https://github.com/chocolatey/shimgen/issues/1)
 * Fix - Forcing x86 does not use 32bit checksum - [#535](https://github.com/chocolatey/chocolatey/issues/535)
 * Fix - Powershell v2 fails to download SSLv3 files - [#531](https://github.com/chocolatey/chocolatey/issues/531)
 * Fix - Get-ChocolateyUnzip fails due to Wait-Process exception - [#571](https://github.com/chocolatey/chocolatey/issues/571)

IMPROVEMENTS:

 * Use default credentials for internet if available - [#577](https://github.com/chocolatey/chocolatey/issues/577)
 * Add moderation message on push - [#600](https://github.com/chocolatey/chocolatey/issues/600)
 * Restrict all calls to chocolatey.org to HTTPS - [#599](https://github.com/chocolatey/chocolatey/issues/599)
 * Batch fallback should quote path for spaces - [#558](https://github.com/chocolatey/chocolatey/issues/558)

##[0.9.8.27](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.27&page=1&state=closed) (July 13, 2014)

BUG FIXES:

 * Fix - Posh v3+ Ignores -Wait when run from cmd.exe - [#516](https://github.com/chocolatey/chocolatey/pull/516)

##[0.9.8.26](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.26&page=1&state=closed) (July 12, 2014)

BUG FIXES:

 * Fix - Allow spaces in arguments to chocolatey again - Regenerate chocolatey included shims to take advantage of shimgen fixes - [#507](https://github.com/chocolatey/chocolatey/issues/507)
 * Fix - Default path has changed, causing running without closing shell to have issues again - [#510](https://github.com/chocolatey/chocolatey/issues/510)
 * Fix - Working directory of shimgen generated files points to path target executable is in (GUI apps only) - [#508](https://github.com/chocolatey/chocolatey/issues/508)
 * Fix - cpack/cpush returns zero exit code even when error occurs - [#256](https://github.com/chocolatey/chocolatey/issues/256) & [#384](https://github.com/chocolatey/chocolatey/issues/384)
 * Fix - Install error throws another error due to true instead of $true - [#514](https://github.com/chocolatey/chocolatey/pull/514)
 * Fix - Posh v3+ Ignores -Wait when run from cmd.exe - [#516](https://github.com/chocolatey/chocolatey/pull/516)

IMPROVEMENTS:

 * Allow to pass shimgen specific parameters - [#509](https://github.com/chocolatey/chocolatey/issues/509)
 * Issue warning if user is not running an elevated shell - [#519](https://github.com/chocolatey/chocolatey/issues/519)

##[0.9.8.25](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.25&page=1&state=closed) (July 7, 2014)

BUG FIXES:

 * Fix - Shims that require admin may fail on UAC enforced machines (System.ComponentModel.Win32Exception: The requested operation requires elevation) - [#505](https://github.com/chocolatey/chocolatey/issues/505)
 * Fix - Do not check content-length if there isn't a content-length returned from Get-WebHeaders - [#504](https://github.com/chocolatey/chocolatey/issues/504)

##[0.9.8.24](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.24&page=1&state=closed) (July 3, 2014)

BREAKING CHANGES:

 * Enhancement - Default install to C:\ProgramData\chocolatey - [#452](https://github.com/chocolatey/chocolatey/issues/452) & [#494](https://github.com/chocolatey/chocolatey/issues/494)
 * Don't allow $binroot to be set to c:\ - [#434](https://github.com/chocolatey/chocolatey/issues/434) - this is meant to be temporary while other pieces are fixed.

FEATURES:

 * Checksum downloaded files - [#427](https://github.com/chocolatey/chocolatey/issues/427)
 * Replace Batch Redirector with Shims - [#372](https://github.com/chocolatey/chocolatey/issues/372)
 * New Helper - Get-UACEnabled - [#451](https://github.com/chocolatey/chocolatey/issues/451)
 * Enhancement - Install to Machine environment variable - [#453](https://github.com/chocolatey/chocolatey/issues/453)
 * Enhancement - Install the .NET framework 4.0 requirement - [#255](https://github.com/chocolatey/chocolatey/issues/255)
 * Update environment using command (RefreshEnv) - [#134](https://github.com/chocolatey/chocolatey/issues/134)
 * `-quiet` parameter that silences almost all output / allow shutting off real write-host - [#416](https://github.com/chocolatey/chocolatey/pull/416) & [#411](https://github.com/chocolatey/chocolatey/issues/411)
 * New Helpers - Test-ProcessAdminRights, Get-EnvironmentVariableNames, Get-EnvironmentVariable, Set-EnvironmentVariable - [#486](https://github.com/chocolatey/chocolatey/pull/486)

BUG FIXES:

 * Fix - Cannot bind argument to parameter 'Path' because it is an empty string - [#371](https://github.com/chocolatey/chocolatey/issues/371)
 * Fix - clist -source webpi doesn't prompt for admin access - [#293](https://github.com/chocolatey/chocolatey/issues/293)
 * Fix - Get-ChocolateyUnzip silently fails due to incorrect usage of System32 (File System Redirector Issues) - [#476](https://github.com/chocolatey/chocolatey/pull/476) & [#455](https://github.com/chocolatey/chocolatey/issues/455)
 * Fix - 7za.exe is subject to UAC file virtualization - [#454](https://github.com/chocolatey/chocolatey/issues/454)
 * Fix - "You cannot call a method on a null-valued expression" introduced somewhere. - [#430](https://github.com/chocolatey/chocolatey/issues/430)
 * Fix - Get-BinRoot defaulted to "C:\" instead of "C:\tools" - [#421](https://github.com/chocolatey/chocolatey/pull/421)
 * Fix - Get-ProcessorBits doesn't return the bitness of the OperatingSystem - [#396](https://github.com/chocolatey/chocolatey/pull/396)
 * Fix - Fix Invoke for Install All from a Feed (DEPRECATED by #446 - in improvements below) - [#381](https://github.com/chocolatey/chocolatey/issues/381)
 * Fix - Upgrade to 0.9.8.24 produces cannot find Update-SessionEnvironment when using cmd.exe - [#459](https://github.com/chocolatey/chocolatey/issues/459)
 * Fix - Package depending on newer chocolatey version is installed using existing version of chocolatey - [#460](https://github.com/chocolatey/chocolatey/issues/460)
 * Fix - Bash improvements - [#383](https://github.com/chocolatey/chocolatey/pull/383)
 * Fix - Resolve issue with DISM "missing" or with the 32-bit DISM being called on a 64-bit system - [#393](https://github.com/chocolatey/chocolatey/pull/393)
 * Fix - Do NOT throw if missing a chocolateyuninstall.ps1 - [#499](https://github.com/chocolatey/chocolatey/issues/499)

IMPROVEMENTS:

 * Do not download if file already cached - [#428](https://github.com/chocolatey/chocolatey/issues/428) & [#109](https://github.com/chocolatey/chocolatey/pull/109)
 * If *.ignore file failes to create, do not fail the process - [#380](https://github.com/chocolatey/chocolatey/issues/380)
 * Validate downloaded file is the right size - [#429](https://github.com/chocolatey/chocolatey/issues/429)
 * Add perf to Chocolatey-List & allow to return as object - [#426](https://github.com/chocolatey/chocolatey/issues/426)
 * Chocolatey-List LocalOnly performance improvements - [#425](https://github.com/chocolatey/chocolatey/pull/425)
 * Chocolatey-Version Improvements - [#445](https://github.com/chocolatey/chocolatey/issues/445)
 * Remove Invoke-Chocolatey Function to improve handling - [#446](https://github.com/chocolatey/chocolatey/issues/446)
 * Don't create a window during Run-Nuget.ps1 - [#450](https://github.com/chocolatey/chocolatey/pull/450)
 * Generate _env.cmd file instead of bat file for consistency - [#469](https://github.com/chocolatey/chocolatey/pull/469)
 * Remove-BinFile removes shim.exes when installing a package - [#449](https://github.com/chocolatey/chocolatey/pull/449)
 * Remove annoying "Reading environment variables from registry. Please wait..." - [#440](https://github.com/chocolatey/chocolatey/pull/440)
 * Replace ascii cue to visual cue for "installing package" - [#376](https://github.com/chocolatey/chocolatey/pull/376)
 * Clean up the verbosity of chocolatey - [#374](https://github.com/chocolatey/chocolatey/issues/374)
 * Improve chocolatey setup as administrator - [#486](https://github.com/chocolatey/chocolatey/pull/486)
 * Simplify Chocolatey-Update - [#493](https://github.com/chocolatey/chocolatey/issues/493)
 * Update to Nuget.exe 2.8.2 - [#379](https://github.com/chocolatey/chocolatey/issues/379)

##[0.9.8.23](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.23&page=1&state=closed) (November 11, 2013)

BUG FIXES:

 * Fix - Chocolatey 0.9.8.22 incorrectly reports version as alpha1 [#368](https://github.com/chocolatey/chocolatey/issues/368)
 * Fix - Some chocolatey commands with no arguments error [#369](https://github.com/chocolatey/chocolatey/issues/369)


##[0.9.8.22](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.22&page=1&state=closed) (November 10, 2013)

BREAKING CHANGES:

 * To use spaces and quotes, one should now use single quotation marks. It works best in both powershell and cmd.

FEATURES:

 * Enhancement - Add switch to force x86 when packages have both versions - [#365](https://github.com/chocolatey/chocolatey/issues/365)
 * Enhancement - Allow passing parameters to packages - [#159](https://github.com/chocolatey/chocolatey/issues/159)

BUG FIXES:

 * Fix - Chocolatey 0.9.8.21 errors when using spaces or quotes with chocolatey or with batch redirect files. - [#367](https://github.com/chocolatey/chocolatey/issues/367)


##[0.9.8.21](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.21&page=1&state=closed) (November 7, 2013)

BREAKING CHANGES:

 * Enhancement - For local package searching, use choco list -lo or choco search -lo. The execution speed is greatly increased. cver for local has been deprecated. - [#276](https://github.com/chocolatey/chocolatey/issues/276)
 * Breaking - Chocolatey default source no longer includes Nuget official feed. This will help improve response time and greatly increase relevant results. - [#349](https://github.com/chocolatey/chocolatey/issues/349)

FEATURES:

 * Enhancement - Support for Server Core - [#59](https://github.com/chocolatey/chocolatey/issues/59)
 * Enhancement - Add a switch for ignoring dependencies on install `-ignoredependencies` - [#131](https://github.com/chocolatey/chocolatey/issues/131)
 * Command - `choco` is now a default term
 * Command - search is now a command (aliases list) - `choco search something [-localonly]`
 * Function - `Get-ProcessorBits` - tells you whether a processor is x86 or x64. This functionality was in chocolatey already but has been globalized for easy access. - [#231](https://github.com/chocolatey/chocolatey/issues/231) & [#229](https://github.com/chocolatey/chocolatey/issues/229)
 * Function - `Get-BinRoot` - Gives package maintainers the ability to call one command that gets them the tools/bin root. This gives you the location where folks want certain packages installed. - [#359](https://github.com/chocolatey/chocolatey/pull/359)

IMPROVEMENTS:

 * Enhancement - Install multiple packages by specifying them all on the same line - [#191](https://github.com/chocolatey/chocolatey/issues/191)
 * Enhancement - Install .NET Framework 4.0 requirement if not already installed - [#255](https://github.com/chocolatey/chocolatey/issues/255)
 * Enhancement - Refresh command line PATH after installs - partial to [#134](https://github.com/chocolatey/chocolatey/issues/134) - Previously we were just doing it in chocolatey with [#158](https://github.com/chocolatey/chocolatey/issues/158)
 * Enhancement - Allow chocolatey to install when zip shell extensions are disabled - [#297](https://github.com/chocolatey/chocolatey/issues/297)
 * Enhancement - Support for bash and similar shells - [#347](https://github.com/chocolatey/chocolatey/issues/347) & [#258](https://github.com/chocolatey/chocolatey/issues/258)
 * Enhancement - Allow file uri to be used when downloading files - [#322](https://github.com/chocolatey/chocolatey/issues/322)
 * Enhancement - Chocolatey version all versions returned for specific local package. - [#260](https://github.com/chocolatey/chocolatey/issues/260)
 * Enhancement - Exit codes return appropriately - [#210](https://github.com/chocolatey/chocolatey/issues/210)
 * Enhancement - Better logging support - [#208](https://github.com/chocolatey/chocolatey/issues/208)
 * Enhancement - Pass through exit codes from binned batch files - https://github.com/chocolatey/chocolatey/issues/360
 * Enhancement - Support MSU file type - https://github.com/chocolatey/chocolatey/pull/348

BUG FIXES:

 * Fix - Treat installation failures appropriately - [#10](https://github.com/chocolatey/chocolatey/issues/10)
 * Fix - Using newer versions of nuget breaks chocolatey - [#303](https://github.com/chocolatey/chocolatey/issues/303)
 * Fix - Chocolatey incorrectly reports 64 bit urls when downloading anything - [#331](https://github.com/chocolatey/chocolatey/issues/331)
 * Fix - Executing `cuninst` without parameters shouldn't do anything - [#267](https://github.com/chocolatey/chocolatey/issues/267) & [#265](https://github.com/chocolatey/chocolatey/issues/265)
 * Fix - VSIX installer helper is finding the wrong Visual Studio version - [#262](https://github.com/chocolatey/chocolatey/issues/262)
 * Fix - Renaming logs appending `.old` results in error - [#225](https://github.com/chocolatey/chocolatey/issues/225)
 * Fix - Minor typo in uninstall script "uninINstalling" - [#247](https://github.com/chocolatey/chocolatey/issues/247)
 * Fix - Bug in Get-ChocolateyUnzip throws issues sometimes [#244](https://github.com/chocolatey/chocolatey/issues/244) & [#242](https://github.com/chocolatey/chocolatey/issues/242)
 * Fix - Minor typo "succesfully" - [#241](https://github.com/chocolatey/chocolatey/issues/241)


##[0.9.8.20](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.20&page=1&state=closed) (December 11, 2012)

FEATURES:

 * Command - Windows Feature feed - [#150](https://github.com/chocolatey/chocolatey/pull/150)
 * Function - Add function to install environment variables - [#149](https://github.com/chocolatey/chocolatey/pull/149)
 * Function - Function to associate file extensions with installed executables - [#146](https://github.com/chocolatey/chocolatey/pull/146)
 * Function - Helper function to create explorer context menu items - [#144](https://github.com/chocolatey/chocolatey/pull/144)
 * Function - Helper function for pinning items to task bar - [#143](https://github.com/chocolatey/chocolatey/pull/143) & [#141](https://github.com/chocolatey/chocolatey/pull/141)
 * Command - Sources command - [#138](https://github.com/chocolatey/chocolatey/pull/138)
 * Command - Provide a way to list all the installed packages - [#125](https://github.com/chocolatey/chocolatey/issues/125)

IMPROVEMENTS:

 * Enhancement - Added FTP support for the chocolatey file downloader. - [#137](https://github.com/chocolatey/chocolatey/pull/137)
 * Enhancement - Block installer exe from being "bin"-ed - [#174](https://github.com/chocolatey/chocolatey/issues/174)
 * Enhancement - Making the unzip process silent - [#180](https://github.com/chocolatey/chocolatey/pull/180)
 * Enhancement - Makes install args more explicit - [#179](https://github.com/chocolatey/chocolatey/pull/179)
 * Enhancement - Update Write-Progress every 5000 iterations instead of every iteration - [#177](https://github.com/chocolatey/chocolatey/pull/177)
 * Enhancement - Codeplex Support - [#176](https://github.com/chocolatey/chocolatey/issues/176)
 * Enhancement - Fix downloads greater than 2GB - [#173](https://github.com/chocolatey/chocolatey/pull/173)
 * Enhancement - Add -verbose switch for clist support to see package description - [#166](https://github.com/chocolatey/chocolatey/pull/166)
 * Enhancement - Refresh env vars after Install - [#158](https://github.com/chocolatey/chocolatey/pull/158) & [#153](https://github.com/chocolatey/chocolatey/issues/153)
 * Enhancement - Add EditorConfig file denoting coding style. - [#123](https://github.com/chocolatey/chocolatey/pull/123)
 * Enhancement - Chocolatey-Version Remote Check - [#119](https://github.com/chocolatey/chocolatey/pull/119)
 * Enhancement - Write every unzip path/file to a text file - [#114](https://github.com/chocolatey/chocolatey/pull/114)

BUG FIXES:

 * Fix - "Execution of NuGet not detected" error. - [#151](https://github.com/chocolatey/chocolatey/pull/151)
 * Fix - chocolatey.bat can't find chocolatey.cmd - [#152](https://github.com/chocolatey/chocolatey/issues/152)
 * Fix - `chocolatey version all` prints only the last package's information - [#183](https://github.com/chocolatey/chocolatey/pull/183)
 * Fix - Issue with $processor.addresswidth var - [#121](https://github.com/chocolatey/chocolatey/pull/121)

##[0.9.8.19](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.19&page=1&state=closed) (July 2, 2012)

FEATURES:

 * Enhancement - Allow community extensions - [#115](https://github.com/chocolatey/chocolatey/issues/115)

BUG FIXES:

 * Fix - PowerShell v3 doesn't like foreach loop (prefers ForEach-Object) - [#116](https://github.com/chocolatey/chocolatey/pull/116)
 * Fix - Cannot install Python packages on Windows 8 - [#117](https://github.com/chocolatey/chocolatey/issues/117)

##[0.9.8.18](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.18&sort=created&direction=desc&state=closed&page=1) (June 16, 2012)

BUG FIXES:

 * Fix - 0.9.8.17 installer doesn't create chocolatey folder if it doesn't exist - [#112](https://github.com/chocolatey/chocolatey/issues/112)

##[0.9.8.17](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.17&sort=created&direction=desc&state=closed&page=1) (June 15, 2012)

FEATURES:

 * Enhancement - Support for naive uninstall - [#96](https://github.com/chocolatey/chocolatey/issues/96)

IMPROVEMENTS:

 * Enhancement - Sources specified through config (or nuget.config) - [#101](https://github.com/chocolatey/chocolatey/pull/101)
 * Enhancement - Chocolatey should support multiple sources - [#82](https://github.com/chocolatey/chocolatey/issues/82)
 * Enhancement - Use Cygwin as a package source - [#93](https://github.com/chocolatey/chocolatey/pull/93)
 * Enhancement - Use Python as a package source (uses easy_install) - [#100](https://github.com/chocolatey/chocolatey/issues/100)
 * Enhancement - Use Default Credentials before Get-Credentials when using proxy on web call - [#83](https://github.com/chocolatey/chocolatey/pull/83)
 * Enhancement - Reduce the verbosity of running chocolatey - [#84](https://github.com/chocolatey/chocolatey/issues/84)
 * Enhancement - Support opening links to "GUI" type applications in a different way than the console apps -  [#76](https://github.com/chocolatey/chocolatey/issues/76)
 * Enhancement - Do not create batch redirects for certain executables in package folder - [#106](https://github.com/chocolatey/chocolatey/issues/106)
 * Enhancement - Add a -debug switch - [#85](https://github.com/chocolatey/chocolatey/issues/85)
 * Enhancement - Improve pipelining of cver by returning an object - [#94](https://github.com/chocolatey/chocolatey/pull/94)

BUG FIXES:

 * Fix - Packages.config source now uses chocolatey/nuget sources by default instead of empty - [#79](https://github.com/chocolatey/chocolatey/issues/79)
 * Fix - Executable batch links not created for "prerelease" versions - [#88](https://github.com/chocolatey/chocolatey/issues/88)
 * Fix - Issue where latest version is not returned - [#92](https://github.com/chocolatey/chocolatey/pull/92)
 * Fix - Prerelease versions now broken out as separate versions - [#90](https://github.com/chocolatey/chocolatey/issues/90)
 * Fix - During install PowerShell session gets bad $env:ChocolateyInstall variable - [#80](https://github.com/chocolatey/chocolatey/issues/80)
 * Fix - Build path with spaces now works - [#102](https://github.com/chocolatey/chocolatey/pull/102)

##0.9.8.16 (February 27, 2012)

BUG FIXES:

 * Small fix to installer for upgrade issues from 0.9.8.15

##[0.9.8.15](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.15&sort=created&direction=desc&state=closed&page=1) (February 27, 2012)

**BREAKING CHANGES:**

 * Enhancement - Chocolatey's default folder is now C:\Chocolatey (and no longer C:\NuGet) - [#58](https://github.com/chocolatey/chocolatey/issues/58)
 * Enhancement - Use -force to reinstall existing packages - [#45](https://github.com/chocolatey/chocolatey/issues/45)

FEATURES:

 * Enhancement - Install now supports **all** with a custom package source to install every package from a source! - [#46](https://github.com/chocolatey/chocolatey/issues/46)

IMPROVEMENTS:

 * Enhancement - Support Prerelease flag for Install - [#71](https://github.com/chocolatey/chocolatey/issues/71)
 * Enhancement - Support Prerelease flag for Update/Version - [#72](https://github.com/chocolatey/chocolatey/issues/72)
 * Enhancement - Support Prerelease flag in List - [#74](https://github.com/chocolatey/chocolatey/issues/74)

BUG FIXES:

 * Fix - Parsing the wrong version when trying to update - [#73](https://github.com/chocolatey/chocolatey/issues/73)

##[0.9.8.14](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.14&sort=created&direction=desc&state=closed&page=1) (February 6, 2012)

IMPROVEMENTS:

 * Enhancement - Pass ValidExitCodes to Install Helpers - [#54](https://github.com/chocolatey/chocolatey/issues/54)
 * Enhancement - Add 64-bit url to Install-ChocolateyZipPackage - [#48](https://github.com/chocolatey/chocolatey/issues/48)
 * Enhancement - Add 64-bit url to Install-ChocolateyPowershellCommand - [#57](https://github.com/chocolatey/chocolatey/issues/57)
 * Enhancement - Make the main helpers work with files not coming over HTTP - [#51](https://github.com/chocolatey/chocolatey/issues/51)
 * Enhancement - Upgrade NuGet.exe to 1.6.0 to take advantage of prerelease packaging - [#64](https://github.com/chocolatey/chocolatey/issues/64)

BUG FIXES:

 * Fix - The packages.config feature has broken naming packages with '.config' - [#56](https://github.com/chocolatey/chocolatey/issues/56)
 * Fix - CList includes all versions without adding the switch - [#60](https://github.com/chocolatey/chocolatey/issues/60)
 * Fix - When NuGet.exe failes to run due to .NET Framework 4.0 not installed, chocolatey should report that. - [#65](https://github.com/chocolatey/chocolatey/issues/65)

##[0.9.8.13](https://github.com/chocolatey/chocolatey/issues?labels=0.9.8.13&sort=created&direction=desc&state=closed&page=1) (January 8, 2012)

FEATURES:

 * New Command! Enhancement - Integration with Ruby Gems (`cgem packageName` or `cinst packageName -source ruby`) - [#29](https://github.com/chocolatey/chocolatey/issues/29)
 * New Command! Enhancement - Integration with Web PI (`cwebpi packageName` or `cinst packageName -source webpi`) - [#28](https://github.com/chocolatey/chocolatey/issues/28)
 * Enhancement - Call chocolatey install with packages.config file (thanks AnthonyMastrean!) - [#31](https://github.com/chocolatey/chocolatey/issues/31) and [#43](https://github.com/chocolatey/chocolatey/pull/43) and [#50](https://github.com/chocolatey/chocolatey/issues/50)
 * New Command! Enhancement - Chocolatey Push (`chocolatey push packageName.nupkg` or `cpush packageName.nupkg`) - [#36](https://github.com/chocolatey/chocolatey/issues/36)
 * New Command! Enhancement - Chocolatey Pack (`chocolatey pack [packageName.nuspec]` or `cpack [packageName.nuspec]`) - [#35](https://github.com/chocolatey/chocolatey/issues/35)

IMPROVEMENTS:

 * Enhancement - @datachomp feature - Override Installer Arguments `chocolatey install packageName -installArgs "args to override" -override` or `cinst packageName -ia "args to override" -o`) - [#40](https://github.com/chocolatey/chocolatey/issues/40)
 * Enhancement - @datachomp feature - Append Installer Arguments (`chocolatey install packageName -installArgs "args to append"` or `cinst packageName -ia "args to append"`) - [#39](https://github.com/chocolatey/chocolatey/issues/39)
 * Enhancement - Run installer in not silent mode (`chocolatey install packageName -notSilent` or `cinst packageName -notSilent`) - [#42](https://github.com/chocolatey/chocolatey/issues/42)
 * Enhancement - List available Web PI packages (`clist -source webpi`) - [#37](https://github.com/chocolatey/chocolatey/issues/37)
 * Enhancement - List command should allow the All or AllVersions switch - [#38](https://github.com/chocolatey/chocolatey/issues/38)
 * Enhancement - Any install will create the ChocolateyInstall environment variable so that installers can take advantage of it - [#30](https://github.com/chocolatey/chocolatey/issues/30)

BUG FIXES:

 * Fixing an issue on proxy display message (Thanks jasonmueller!) - [#44](https://github.com/chocolatey/chocolatey/pull/44)
 * Fixing the source path to allow for spaces (where chocolatey is installed) - [#33](https://github.com/chocolatey/chocolatey/issues/33)
 * Fixing the culture to InvariantCulture to eliminate the turkish "I" issue - [#22](https://github.com/chocolatey/chocolatey/issues/22)

##0.9.8.12 (November 20, 2011)

IMPROVEMENTS:

 * Enhancement - Reducing the number of window pop ups - [#25](https://github.com/chocolatey/chocolatey/issues/25)

BUG FIXES:

 * Fixed an issue with write-host and write-error overrides that happens in the next version of powershell - [#24](https://github.com/chocolatey/chocolatey/pull/24)
 * Fixing an issue that happens when powershell is not on the path - [#23](https://github.com/chocolatey/chocolatey/issues/23)
 * Fixing the replacement of capital ".EXE" in addition to lowercase ".exe" when creating batch redirects - [#26](https://github.com/chocolatey/chocolatey/issues/26)

##0.9.8.11 (October 4, 2011)

BUG FIXES:

 * Fixing an update issue if the package only exists on chocolatey.org - [#16](https://github.com/chocolatey/chocolatey/issues/16)
 * Fixing an issue with install missing if the package never existed - [#13](https://github.com/chocolatey/chocolatey/issues/13)

##0.9.8.10 (September 17, 2011)

FEATURES:

 * New Helper! Install-ChocolateyPowershellCommand - install a powershell script as a command - [#11](https://github.com/chocolatey/chocolatey/issues/11)

##0.9.8.9 (September 10, 2011)

BUG FIXES:

 * Reinstalls an existing package if -version is passed (first surfaced in 0.9.8.7 w/NuGet 1.5) - [#9](https://github.com/chocolatey/chocolatey/issues/9)

##0.9.8.8 (September 10, 2011)

BUG FIXES:

 * Fixing version comparison - [#4](https://github.com/chocolatey/chocolatey/issues/4)
 * Fixed package selector to not select like named packages (i.e. ruby.devkit when getting information about ruby) - [#3](https://github.com/chocolatey/chocolatey/issues/3)

##0.9.8.7 (September 2, 2011)

IMPROVEMENTS:

 * Added proxy support based on [#1](https://github.com/chocolatey/chocolatey/issues/1)
 * Updated to work with NuGet 1.5 - [#2](https://github.com/chocolatey/chocolatey/issues/2)

##0.9.8.6 (July 27, 2011)

BUG FIXES:

 * Fixed a bug introduced in 0.9.8.5 - Start-ChocolateyProcessAsAdmin erroring out when setting machine path as a result of trying to log the message.

##0.9.8.5 (July 27, 2011)

IMPROVEMENTS:

 * Improving Run-ChocolateyProcessAsAdmin to allow for running entire functions as administrator by importing helpers to that command if using PowerShell.
 * Updating some of the notes.

BUG FIXES:

 * Fixed bug in installer when User Environment Path is null.

##0.9.8.4 (July 27, 2011)

BUG FIXES:

 * Fixed a small issue with the Install-ChocolateyDesktopLink

##0.9.8.3 (July 7, 2011)

**BREAKING CHANGES:**

 * Chocolatey no longer runs the entire powershell script as an administrator. With the addition of the Start-ChocolateyProcessAsAdmin, this is how you will get to administrative tasks outside of the helpers.

FEATURES:

 * New chocolatey command! InstallMissing allows you to install a package only if it is not already installed. Shortcut is 'cinstm'.
 * New Helper! Install-ChocolateyPath - give it a path for out of band items that are not imported to path with chocolatey
 * New Helper! Start-ChocolateyProcessAsAdmin - this allows you to run processes as administrator
 * New Helper! Install-ChocolateyDesktopLink - put shortcuts on the desktop

IMPROVEMENTS:

 * NuGet updated to v1.4
 * Much of the error handling is improved. There are two new Helpers to call (ChocolateySuccess and Write-ChocolateyFailure).
 * Chocolatey no longer needs administrative rights to install itself.

##0.9.8.2 (May 21, 2011)

FEATURES:

 * You now have the option of a custom installation folder. Thanks Jason Jarrett!

##0.9.8.1 (May 18, 2011)

BUG FIXES:

 * General fix to bad character in file. Fixed selection for update as well.

##0.9.8 (May 4, 2011)

**BREAKING CHANGES:**

 * A dependency will not reinstall once it has been installed. To have it reinstall, you can install it directly (or delete it from the repository and run the core package).

IMPROVEMENTS:

 * Shortcuts have been added: 'cup' for 'chocolatey update', 'cver' for 'chocolatey version', and 'clist' for 'chocolatey list'.
 * Update only runs if newer version detected.
 * Calling update with no arguments will update chocolatey.
 * Calling update with all will update your entire chocolatey repository.

##0.9.7.3 (April 30, 2011)

BUG FIXES:

 * Fixing Install-ChocolateyZipPackage so that it works again.

##0.9.7.2 (April 29, 2011)

BUG FIXES:

 * Fixing an underlying issue with not having silent arguments for exe files.

##0.9.7.1 (April 29, 2011)

BUG FIXES:

 * Fixing an introduced bug where the downloader didn't get the file name passed to it.

##0.9.7 (April 29, 2011)

FEATURES:

 * New helper added Install-ChocolateyInstallPackage - this was previously part of the download & install and has been broken out.
 * New chocolatey command! Version allows you to see if a package you have installed is the most up to date. Leave out package and it will check for chocolatey itself.

IMPROVEMENTS:

 * The powershell module is automatically loaded, so packages no longer need to import the module. This means one line chocolateyInstall.ps1 files!
 * Error handling is improved.
 * Silent installer override for msi has been removed to allow for additional arguments that need to be passed.

##0.9.6.4 (April 26, 2011)

IMPROVEMENTS:

 * Remove powershell execution timeout.

##0.9.6.3 (April 25, 2011)

FEATURES:

 * New Helper added Install-ChocolateyZipPackage - this wraps the two upper commands into one smaller command and addresses the file name bug.

##0.9.6.2 (April 25, 2011)

BUG FIXES:

 * Addressed a small bug in getting back the file name from the helper.

##0.9.6.1 (April 23, 2011)

IMPROVEMENTS:

 * Adding in ability to find a dependency when the version doesn't exist.

##0.9.6 (April 23, 2011)

IMPROVEMENTS:

 * Can execute powershell and chocolatey without having to change execution rights to powershell system wide.

FEATURES:

 * New Helper added - Get-ChocolateyWebFile - downloads a file from a url and gives you back the location of the file once complete.
 * New Helper added - Get-ChocolateyZipContents - unzips a file to a directory of your choosing.

##0.9.5 (April 21, 2011)

FEATURES:

 * Helper for native installer added (Install-ChocolateyPackage). Reduces the amount of powershell necessary to download and install a native package to two lines from over 25.

IMPROVEMENTS:

 * Helper outputs progress during download.
 * Dependency runner is complete.

##0.9.4 (April 10, 2011)

IMPROVEMENTS:

 * List command has a filter.
 * Package license acceptance terms notated.

##0.9.3 (April 4, 2011)

IMPROVEMENTS:

 * You can now pass -source and -version to install command.

##0.9.2 (April 4, 2011)

FEATURES:

 * List command added.

##0.9.1 (March 30, 2011)

IMPROVEMENTS:

 * Shortcut for 'chocolatey install' - 'cinst' now available.
