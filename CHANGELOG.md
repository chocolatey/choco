##[0.9.8.24](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.24&page=1&state=closed) (unreleased)




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

 * Fix - Chocolatey 0.9.8.21 errors when using spaces or quotes with chocolatey or with batch redirect files [#367](https://github.com/chocolatey/chocolatey/issues/367)

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
