# Chocolatey Open Source CHANGELOG
This covers changes for the "chocolatey" and "chocolatey.lib" packages, which are available as FOSS.

**NOTE**: If you have a licensed edition of Chocolatey ("chocolatey.extension"), refer to this in tandem with [Chocolatey Licensed CHANGELOG](https://github.com/chocolatey/choco/blob/master/CHANGELOG_LICENSED.md).

## [0.10.13](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.13+is%3Aclosed) - (March 15, 2019)
### BUG FIXES
 * Fix - Licensed - Licensed code failing when using licensed PowerShell functions - see [#1767](https://github.com/chocolatey/choco/issues/1767)


## [0.10.12](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.12+is%3Aclosed) - (March 14, 2019)
We are pretty excited to finally share a new Chocolatey release! And this release won't disappoint. Loads of bug fixes, enhanced exit codes for search, list, info and outdated when results are returned versus nothing being returned, and some really nice improvements.

Perhaps the biggest addition this release is the ability to halt installation if a reboot is detected ([#1038](https://github.com/chocolatey/choco/issues/1038)). Once you turn this feature on, if you are installing some packages and somewhere in the middle of that there is a need for a reboot, Chocolatey will stop and exit with either exit code 350 (pending reboot prior to anything) or 1604 (install incomplete), indicating a reboot is needed to continue. It won't reboot for you, as it is just a package manager - but it will stop execution so nothing that may error on install is attemtped. You'll need to opt into this feature, so see [#1038](https://github.com/chocolatey/choco/issues/1038) for details.

If you've long hated the default console colors, we've spent quite a bit of time detecting the background console color and adjusting the colorization output of Chocolatey for this release ([#1131](https://github.com/chocolatey/choco/issues/1131)). You might give that a whirl and see if you can turn back on console colors for good.

We've added the ability to validate the configuration and system state at a global level ([#1746](https://github.com/chocolatey/choco/issues/1746)). It's early, but expect that we'll do a lot more to really provide good experiences in this area.

A bug that is worth noting as fixed is having choco exit when a source fails instead of ignoring it ([#612](https://github.com/chocolatey/choco/issues/612)). This is now fixed!

The last thing worth noting in the summary is Enhanced Exit Codes, or providing more intentional exit codes that mean something instead of just 0 or 1 ([#1758](https://github.com/chocolatey/choco/issues/1758)). In this release, outdated and search commands will have additional exit codes that mean something. This is noted in the next section, so please read over and see how to shut off this behavior if you see it breaking any integration you might be using (including your own scripts).

### BREAKING CHANGES
 * outdated - Exit 2 when there are packages out of date - see [#1602](https://github.com/chocolatey/choco/issues/1602)
 * search/list/info - Exit 2 when no results are returned - see [#1724](https://github.com/chocolatey/choco/issues/1724)

We've listed these as breaking changes as it may affect tools that are integrating with Chocolatey and interpreting the output of the exit code. In these cases, it would likely temporarily break those tools until they've had a chance to release new versions of their tools. If you run into this, you simply need to turn off the feature "useEnhancedExitCodes". That is as simple as `choco feature disable --name="'useEnhancedExitCodes'"` ([#1758](https://github.com/chocolatey/choco/issues/1758)).

### FEATURES
 * Exit when reboot is detected - w/350 for pending & w/1604 on dependency package requiring reboot - see [#1038](https://github.com/chocolatey/choco/issues/1038)

### BUG FIXES
 * [Security] Fix - upgrade - remove automation scripts prior to upgrade even if changed - see [#1689](https://github.com/chocolatey/choco/issues/1689)
 * [Security] Fix - scripts - Digitally sign the init.ps1 PowerShell file as well  - see [#1665](https://github.com/chocolatey/choco/issues/1665)
 * Fix - When a source fails, choco exits instead of moving to next source - see [#612](https://github.com/chocolatey/choco/issues/612)
 * Fix - Upgrade all reuses overridden package parameters when useRememberedArgumentsForUpgrades feature is turned on - see [#1443](https://github.com/chocolatey/choco/issues/1443)
 * Fix - Passing `--execution-timeout=0` doesn't override the default execution timeout in the configuration - see [#1747](https://github.com/chocolatey/choco/issues/1747)
 * Fix - ChocolateyLastPathUpdate environment variable stores date as locale-specific - see [#1604](https://github.com/chocolatey/choco/issues/1604)
 * [POSH Host] Fix - install/upgrade/uninstall - PowerShell host should exit with 1 instead of -1 if there is a package error - see [#1734](https://github.com/chocolatey/choco/issues/1734)
 * Fix - Logging - warnings for ".registry.bad." files are emitted with "-r" switch - see [#1580](https://github.com/chocolatey/choco/issues/1580)
 * Fix - Logging - ".registry.bad" files are created for actually valid registry snapshots - see [#1581](https://github.com/chocolatey/choco/issues/1746)
 * Fix - list/search - Listing local packages fails if no sources are enabled - see [#661](https://github.com/chocolatey/choco/issues/661)
 * Fix - uninstall - Object reference exception when there are no sources - see [#1584](https://github.com/chocolatey/choco/issues/1584)
 * Fix - Logging - self-service errors attempting to write to the config when using Chocolatey GUI - see [#1649](https://github.com/chocolatey/choco/issues/1649)
 * Fix - source list - running with -r fails to escape pipe (|) char - see [#1614](https://github.com/chocolatey/choco/issues/1614)
 * Fix - source add - Adding a source allows an empty url - see [#1582](https://github.com/chocolatey/choco/issues/1582)
 * Fix - Get-ChocolateyWebFile - Ensure PSVersionTable is used for PowerShell Version - see [#1623](https://github.com/chocolatey/choco/issues/1623)
 * Fix - Install-ChocolateyShortcut - Don't create a folder if environment variable is used - see [#1687](https://github.com/chocolatey/choco/issues/1687)
 * Fix - `choco --version` includes warnings, breaks version parsing - see [#1562](https://github.com/chocolatey/choco/issues/1562)
 * Fix - Uninstall-ChocolateyZipPackage failing with Path error - see [#1550](https://github.com/chocolatey/choco/issues/1550)
 * Fix - Uninstall-ChocolateyZipPackage fails from null passed to Test-Path - see [#1546](https://github.com/chocolatey/choco/issues/1546)
 * Fix - Get-ChocolateyUnzip - Ensure 7z cmd window is hidden - see [#1642](https://github.com/chocolatey/choco/issues/1642)
 * [API] Fix - Resolve assemblies globally without locking - see [#1735](https://github.com/chocolatey/choco/issues/1735)

### IMPROVEMENTS
 * [Security] tools - Update 7z to 18.06 - see [#1704](https://github.com/chocolatey/choco/issues/1704)
 * [Security] Refreshenv script leaves temporary file behind - see [#1549](https://github.com/chocolatey/choco/issues/1549)
 * Control enhanced exit codes with a feature switch - see [#1758](https://github.com/chocolatey/choco/issues/1758)
 * Logging - better default colors - see [#1131](https://github.com/chocolatey/choco/issues/1131)
 * Validate config / system state across everything - see [#1746](https://github.com/chocolatey/choco/issues/1746)
 * upgrade - switch for not installing if not installed - see [#1646](https://github.com/chocolatey/choco/issues/1646)
 * outdated - improve performance of `choco outdated` - see [#1397](https://github.com/chocolatey/choco/issues/1397)
 * search/list - Add alias "find" for search - see [#1744](https://github.com/chocolatey/choco/issues/1744)
 * apikey - Enable removal of API key via CLI - see [#1301](https://github.com/chocolatey/choco/issues/1301)
 * Logging - Choco --log-file option should create log files relative to current directory - see [#1603](https://github.com/chocolatey/choco/issues/1603)
 * Logging - Don't suggest installing separate "checksum" tool - see [#981](https://github.com/chocolatey/choco/issues/981)
 * template - Add notes to uninstaller file string on how to correctly parse the value from the registry - see [#1644](https://github.com/chocolatey/choco/issues/1644)
 * Pro/Business - license - If license is found in top-level folder or named wrong, choco should warn - see [#1503](https://github.com/chocolatey/choco/issues/1503)
 * [API] Allow verifying DI Container in release build - see [#1738](https://github.com/chocolatey/choco/issues/1738)
 * [API] ability to get consistent hash of ConfigFileSettings class - see [#1612](https://github.com/chocolatey/choco/issues/1612)


## [0.10.11](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.11+is%3Aclosed) (May 4, 2018)
### BUG FIXES
 * Fix - AutoUninstaller - Captures registry snapshot escaping quotes - unable to find path for uninstall - see [#1540](https://github.com/chocolatey/choco/issues/1540)
 * Fix - Installation/Setup - Use of Write-Host in Install-ChocolateyPath.ps1 prevents non-interactive installation of Chocolatey itself - see [#1560](https://github.com/chocolatey/choco/issues/1560)
 * Fix - Logging - GUID in software name: "Chocolatey had an error formatting string" - see [#1543](https://github.com/chocolatey/choco/issues/1543)

### IMPROVEMENTS
 * [Security] RAR Extraction with older 7zip uses uninitialized memory (CVE-2018-10115) - see [#1557](https://github.com/chocolatey/choco/issues/1557)
 * Tab Completion - Modify profile if file exists but is empty - see [#991](https://github.com/chocolatey/choco/issues/991)


## [0.10.10](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.10+is%3Aclosed) (April 12, 2018)
### BUG FIXES
 * Fix - Installing Chocolatey 0.10.9 results in an exit code of 1 - see [#1529](https://github.com/chocolatey/choco/issues/1529)
 * Fix - Proxy bypass list with "*" will return regex quantifier parsing errors - see [#1532](https://github.com/chocolatey/choco/issues/1532)
 * Fix - NuGet cache folders - choco should always attempt to remove and should find in the cacheLocation when set - see [#1527](https://github.com/chocolatey/choco/issues/1527)

### IMPROVEMENTS
 * Logging - Exclusive File Lock on Non-Essential Logs - see [#1531](https://github.com/chocolatey/choco/issues/1531)


## [0.10.9](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.9+is%3Aclosed) (March 25, 2018)
The vendored 7Zip had a couple of security findings that necessitated a release. There is also a lot of goodness going into this release as well. We've fixed XDT transforms not to keep extra data around (requiring manual fixes). We've resolved some issues surrounding compatibility with Get-PackageParameters in the chocolatey-core.extension package and what's now built into Chocolatey. That should now work appropriately, and the built-in method should be preferred, so if you are using `--package-parameters-sensitive`, those will be added as well when you do have the chocolatey-core.extension package also installed.

We've also brought in the long desired logging with no colorization. You can set that as a switch or globally with a feature flipper. With outdated/upgrade, you can now ignore unfound packages along with already skipping pinned packages. That will help you reduce your output to only the things it finds upgrades for that can be upgraded.

### BUG FIXES
 * [Security] Fix - Pro/Business - Logging - Get-PackageParameters should not log sensitive params - see [#1460](https://github.com/chocolatey/choco/issues/1460)
 * Fix - XDT transform causes xml file to have extra data in it (unusable until manually fixed) - see [#1482](https://github.com/chocolatey/choco/issues/1482)
 * Fix - Escape package registry information to reduce unreadable files - see [#1505](https://github.com/chocolatey/choco/issues/1505)
 * Fix - Uninstall-ChocolateyZipPackage is unable to find zip contents file - see [#1415](https://github.com/chocolatey/choco/issues/1415)
 * Fix - Get-PackageParameters - Resolve differences between chocolatey.core-extension and built-in method - see [#1490](https://github.com/chocolatey/choco/issues/1490)
 * Fix - Get-PackageParameters - force built-in method to be preferred over chocolatey-core.extension method - see [#1476](https://github.com/chocolatey/choco/issues/1476)
 * Fix - Get-PackageParameters should handle urls - see [#1459](https://github.com/chocolatey/choco/issues/1459)
 * Fix - Setting output directory with proper quoting can result in "The given path's format is not supported." - see [#1517](https://github.com/chocolatey/choco/issues/1517)
 * Fix - Logging - PowerShell script contents logging should not error if they have contents mistaken for log formatting - see [#1489](https://github.com/chocolatey/choco/issues/1489)
 * Fix - Incorrect documentation for Install-ChocolateyInstallPackage - see [#1416](https://github.com/chocolatey/choco/issues/1416)
 * [API] Fix - Logging - Loggers should always be checked for initialization - see [#1447](https://github.com/chocolatey/choco/issues/1447)
 * Fix - Pro/Business - Expired licenses should not cause Chocolatey errors - see [#1500](https://github.com/chocolatey/choco/issues/1500)

### IMPROVEMENTS
 * [Security] RAR extraction with older 7zip can cause memory corruption (CVE-2018-5996) / ZIP Shrink vulnerability (CVE-2017-17969) - see [#1478](https://github.com/chocolatey/choco/issues/1478)
 * Provide friendly error messages on well-known exit codes - see [#1526](https://github.com/chocolatey/choco/issues/1526)
 * Capture password securely during validation when only the user name has been provided - see [#1524](https://github.com/chocolatey/choco/issues/1524)
 * Outdated/Upgrade - Option/feature to ignore unfound packages - see [#1398](https://github.com/chocolatey/choco/issues/1398)
 * Installation/Setup: run choco once to initialize the config file - see [#1401](https://github.com/chocolatey/choco/issues/1401)
 * Logging - Log access denied for config file to log file only - see [#1445](https://github.com/chocolatey/choco/issues/1445)
 * Ability to pick x64/x86 runtime binaries for shimming by architecture without needing PowerShell scripts - see [#1365](https://github.com/chocolatey/choco/issues/1365)
 * Logging - Add '--no-color' as a global option and 'logWithoutColor' feature - see [#100](https://github.com/chocolatey/choco/issues/100)
 * Reset colors after abnormal exit - see [#474](https://github.com/chocolatey/choco/issues/474)
 * [API] Logging - Set logging levels debug/verbose/trace - see [#1448](https://github.com/chocolatey/choco/issues/1448)
 * [API] Logging - Sync already logged items when setting custom logging - see [#1446](https://github.com/chocolatey/choco/issues/1446)
 * [API] Fix - Ensure one instantiation of GetChocolatey at a time - see [#1400](https://github.com/chocolatey/choco/issues/1400)
 * Pro/Business - Uninstall - Keep stored package information by default - see [#1399](https://github.com/chocolatey/choco/issues/1399)
 * Pro/Business - Logging - See licensing logging output - see [#1488](https://github.com/chocolatey/choco/issues/1488)


## [0.10.8](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.8+is%3Aclosed) (August 30, 2017)
With this release, Package Parameters are fully supported from both the user side and the packaging side. Check out [the documentation](https://chocolatey.org/docs/helpers-get-packageparameters) and check out the [walkthrough](https://chocolatey.org/docs/how-to-parse-package-parameters-argument) on how to use package parameters in your packaging. Note if you are pushing packages to the community repository, you must continue to take a dependency on the `chocolatey-core.extension` as a polyfill for older versions until at least six months after a new feature is released.

What you can do with logging has greatly increased your ability to have more power over how it works and deeper output to determine errors (we've expanded `--trace` [#1379](https://github.com/chocolatey/choco/issues/1379)). This release also gives packagers and users more power when working with the AutoUninstaller - opt-out ([#1257](https://github.com/chocolatey/choco/issues/1257)) and passing arguments to the uninstaller ([#1133](https://github.com/chocolatey/choco/issues/1133)).

Another noteworthy addition is the ability to pass custom properties to Choco like you would do with `nuget pack`, allowing better integration with packages you manage in Visual Studio ([#1313](https://github.com/chocolatey/choco/issues/1313)).

### FEATURES
 * Function - Get-PackageParameters - see [#1393](https://github.com/chocolatey/choco/issues/1393)

### BUG FIXES
 * Fix - "Value cannot be null" running choco outdated - see [#1383](https://github.com/chocolatey/choco/issues/1383)
 * Fix - Package parameters are ignored on when install directly points to nupkg/nuspec - see [#1155](https://github.com/chocolatey/choco/issues/1155)
 * Fix - Logging - log4net Logger location (the code location logging) is incorrect - see [#1377](https://github.com/chocolatey/choco/issues/1377)
 * [API] Fix - GetConfiguration() sets the configuration for other calls - see [#1347](https://github.com/chocolatey/choco/issues/1347)
 * [API] Fix - Pro/Business - Self-Service information not returned for sources - see [#1394](https://github.com/chocolatey/choco/issues/1394)
 * [API] Fix - Config output is being set to Regular Output - see [#1396](https://github.com/chocolatey/choco/issues/1396)

### IMPROVEMENTS
 * [Security][POSH Host] Implement Read-Host -AsSecureString- see [#1335](https://github.com/chocolatey/choco/issues/1335)
 * AutoUninstaller - Opt-out from packages - see [#1257](https://github.com/chocolatey/choco/issues/1257)
 * AutoUninstaller - Use Uninstall Arguments and Override Arguments if provided - see [#1133](https://github.com/chocolatey/choco/issues/1133)
 * pack - Pass arbitrary properties like nuget pack - see [#1313](https://github.com/chocolatey/choco/issues/1313)
 * list - Sub-command should not require admin access (and not prompt) - see [#1353](https://github.com/chocolatey/choco/issues/1353)
 * new - Package Templates - Do not treat binaries in template as text files - see [#1385](https://github.com/chocolatey/choco/issues/1385)
 * new - Package Templates - Add more helpful items, like a todo list to the default template - see [#1386](https://github.com/chocolatey/choco/issues/1386)
 * Document how to pass multiple sources on CLI - see [#1331](https://github.com/chocolatey/choco/issues/1331)
 * Logging - Trace output should provide deep logging information - see [#1379](https://github.com/chocolatey/choco/issues/1379)
 * Logging - Allow external log4net config file - see [#1378](https://github.com/chocolatey/choco/issues/1378)
 * Logging - Allow capturing output to an additional log file - see [#1376](https://github.com/chocolatey/choco/issues/1376)
 * [API] Make log4net dependency less restrictive - see [#1395](https://github.com/chocolatey/choco/issues/1395)
 * Pro/Business - source/list - Support for Admin Only Sources - Limit non-admin list to self service only - see [#1265](https://github.com/chocolatey/choco/issues/1265)


## [0.10.7](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.7+is%3Aclosed) (June 8, 2017)
### BREAKING CHANGES
 * Set requested execution level back to asInvoker while determining more advanced elevated scenarios - see [#1324](https://github.com/chocolatey/choco/issues/1324)

   After much deliberation with the community, we're moving execution policy back to the default of `asInvoker` to make it work like it did prior to 0.10.4. However we are leaving it open for you to change it to whatever execution level you want by keeping the manifest external from choco.exe. We will be looking more at advanced scenarios - the discussion is at [#1307](https://github.com/chocolatey/choco/issues/1307). If you don't have a GitHub account, feel free to start a thread on the mailing list (and if you are a customer, you have private channels to voice your opinions on this change).

   Moving to "asInvoker" means that Chocolatey will not ask for elevated privileges prior to execution, so you will need to remember to do that yourself. If you go to `$env:ChocolateyInstall`, you will find `choco.exe.manifest`, and you have freedom to adjust the execution level as you see fit. There is one catch, you will need to do it on every install/upgrade of Chocolatey until [#1206](https://github.com/chocolatey/choco/issues/1206) is implemented.

### BUG FIXES
 * Fix - Add file/file64 not as aliases, but use them to set url/url64 if empty - see [#1323](https://github.com/chocolatey/choco/issues/1323)
 * Fix - Automatic Uninstaller doesn't split multiple paths - see [#1327](https://github.com/chocolatey/choco/issues/1327)
 * Fix - choco list / search / info - fails with local directory source - see [#1325](https://github.com/chocolatey/choco/issues/1325)
 * Fix - When version is four digits, Chocolatey version heading is not shown - see [#1326](https://github.com/chocolatey/choco/issues/1326)
 * Fix - search / list - page/page-size not honored for exact search in 0.10.6 - see [#1322](https://github.com/chocolatey/choco/issues/1322)
 * Fix - Deserializing failures on package info files should not fail the choco run - see [#1328](https://github.com/chocolatey/choco/issues/1328)

### IMPROVEMENTS
 * Use `$packageArgs` in default template for uninstall script - see [#1330](https://github.com/chocolatey/choco/issues/1330)


## [0.10.6.1](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.6.1+is%3Aclosed) (June 3, 2017)
### BUG FIXES
 * Fix - shimgen fails with unrecognized option: '/errorendlocation' in .NET 4.0 only environments - see [#1321](https://github.com/chocolatey/choco/issues/1321)
 * Fix - Do not fail extracting resources at runtime - see [#1318](https://github.com/chocolatey/choco/issues/1318)
 * Fix - Silently fail when deleting choco.exe.old - see [#1319](https://github.com/chocolatey/choco/issues/1319)


## [0.10.6](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.6+is%3Aclosed) (June 1, 2017)

This release includes fixes and adjustments to the API to make it more usable. Search / List has also been improved with the data that it returns when verbose/detailed, along with info always returning a package with information instead of erroring sometimes. The search results from the community package repository now match what you see on the website.

### BUG FIXES
 * Fix - choco.exe.manifest is ignored because it is extracted AFTER first choco.exe access - see [#1292](https://github.com/chocolatey/choco/issues/1292)
 * Fix - Chocolatey config changes in 0.10.4+ - The process cannot access the file because it is being used by another process - see [#1241](https://github.com/chocolatey/choco/issues/1241)
 * Fix - PowerShell sees authenticode hash as changed in scripts that are UTF8 (w/out BOM) that contain unicode characters - see [#1225](https://github.com/chocolatey/choco/issues/1225)
 * Fix - Chocolatey timed out immediately when execution timeout was infinite (0) - see [#1224](https://github.com/chocolatey/choco/issues/1224)
 * Fix - Multiple authenticated sources with same base url fail when authentication is different - see [#1248](https://github.com/chocolatey/choco/issues/1248)
 * Fix - choco list / search / info - Some packages can't be found - see [#1004](https://github.com/chocolatey/choco/issues/1004)
 * Fix - chocolatey.config gets corrupted when multiple processes access simultaneously - see [#1258](https://github.com/chocolatey/choco/issues/1258)
 * Fix - Update ShimGen to 0.8.x to address some issues - see [#1243](https://github.com/chocolatey/choco/issues/1243)
 * Fix - AutoUninstaller should skip uninstall keys if they are empty - see [#1315](https://github.com/chocolatey/choco/issues/1315)
 * Fix - Trace logging should only occur on when trace is enabled - see [#1309](https://github.com/chocolatey/choco/issues/1309)
 * Fix - RefreshEnv.cmd doesn't set the correct PATH - see [#1227](https://github.com/chocolatey/choco/issues/1227)
 * Fix - choco new generates uninstall template with wrong use of registry key variable - see [#1304](https://github.com/chocolatey/choco/issues/1304)
 * [API] Fix- chocolatey.lib nuget package has incorrect documentation xml - see [#1247](https://github.com/chocolatey/choco/issues/1247)
 * [API] Fix - Chocolatey file cache still adds a 'chocolatey' directory on each install - see [#1231](https://github.com/chocolatey/choco/issues/1231)
 * [API] Fix - List and Count should implement similar functionality as run - see [#1298](https://github.com/chocolatey/choco/issues/1298)
 * Pro/Business - [API] Fix - Ensure DLL can work with licensed code - see [#1287](https://github.com/chocolatey/choco/issues/1287)

### IMPROVEMENTS
 * Default package push url now uses push subdomain - see [#1285](https://github.com/chocolatey/choco/issues/1285)
 * Report process id in the log files - see [#1239](https://github.com/chocolatey/choco/issues/1239)
 * choco info / list / search - Include summary on detailed view - see [#1253](https://github.com/chocolatey/choco/issues/1253)
 * choco info / list /search - Include release notes on detailed view - see [#1263](https://github.com/chocolatey/choco/issues/1263)
 * choco list / search - Option to list packages only by name - see [#1237](https://github.com/chocolatey/choco/issues/1237)
 * choco list / search - Allow sorting package results by relevance - see [#1101](https://github.com/chocolatey/choco/issues/1101)
 * choco list / search - Search by tags only - see [#1033](https://github.com/chocolatey/choco/issues/1033)
 * choco outdated - Option to leave out pinned packages - see [#994](https://github.com/chocolatey/choco/issues/994)
 * Install-ChocolateyPackage and other functions should alias File/File64 - see [#1284](https://github.com/chocolatey/choco/issues/1284)
 * Install-ChocolateyPowerShellCommand should alias File/FileFullPath for PsFileFullPath - see [#1311](https://github.com/chocolatey/choco/issues/1311)
 * Logging - capture more information about a user (user name, domain, remote?, system?) - see [#615](https://github.com/chocolatey/choco/issues/615)
 * Stop saying "0 packages failed." - see [#1259](https://github.com/chocolatey/choco/issues/1259)
 * [API] provide a way to see ChocolateyConfiguration - see [#1267](https://github.com/chocolatey/choco/issues/1267)
 * [API] Attempt to get ChocolateyInstall environment variable prior to extraction - see [#1297](https://github.com/chocolatey/choco/issues/1297)
 * [API] Expose Container directly - see [#1294](https://github.com/chocolatey/choco/issues/1294)
 * Pro/Business - Support for Package Audit (who installed packages) - see [#1238](https://github.com/chocolatey/choco/issues/1238)
 * Pro/Business - [API] Ensure configuration retains base info between uses - see [#1296](https://github.com/chocolatey/choco/issues/1296)


## [0.10.5](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.5+is%3Aclosed) (March 30, 2017)
### BUG FIXES
 * Fix - Start-ChocolateyProcessAsAdmin errors when running PowerShell scripts - see [#1220](https://github.com/chocolatey/choco/issues/1220)

### IMPROVEMENTS
 * Show machine readable output with `choco outdated -r` - see [#1222](https://github.com/chocolatey/choco/issues/1222)


## [0.10.4](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.4+is%3Aclosed) (March 30, 2017)

We're dubbing this the "10-4 good buddy" release. We've added some major functionality and fixes we think you are going to find top notch - dare we say as smooth as really expensive chocolate? A lot of work for this release has been provided by the community. Remember that Chocolatey is only as good as the support that comes from the community! Be sure to thank other community members for the awesome that is Chocolatey and Chocolatey 10-4. We've closed over 30 bugs and added over 40 enhancements (75 tickets in total)!

Proxy support just got some major enhancements with the ability to not only [specify proxy information at runtime](https://github.com/chocolatey/choco/issues/1173), but also to [set bypass lists and bypassing on local connections](https://github.com/chocolatey/choco/issues/1165) and [configure source repositories to bypass proxies](https://github.com/chocolatey/choco/issues/262). A major issue with [changing command execution timeout](https://github.com/chocolatey/choco/issues/1059) was just fixed. And there used to be a tiny chance you might [corrupt the choco config when running multiple choco processes](https://github.com/chocolatey/choco/issues/1047) -but now that is much better handled.

We've also made [package itself display download progress](https://github.com/chocolatey/choco/issues/1134), which is great when software binaries are embedded in packages. For you folks looking to remove any progress (like when using Vagrant), now you can use [`--no-progress`](https://github.com/chocolatey/choco/issues/917). When NuGet.Core has issues, those issues will have more visibility into why things are failing without needing a debugging log. Speaking of some extreme visibility, see network traffic with [`--trace`](https://github.com/chocolatey/choco/issues/1182).

We've got a few possible breaking changes that could affect you, see what we've written about them below.

This also marks the first release that uses the [Chocolatey Software digital certificate for signing](https://github.com/chocolatey/choco/issues/1214) instead of the RealDimensions Software, LLC certificate.

Another major feature released in preview is [using remembered arguments on upgrade](https://github.com/chocolatey/choco/issues/797). This is in preview in 0.10.4 and will be turned to 'on' automatically in a future release. We are going to be continually making it better and won't turn it on by default until it is ready. If you want to turn it on and start using it, once you have 0.10.4 installed, run `choco feature enable -n useRememberedArgumentsForUpgrades`. You can also do this per command with `--use-remembered-arguments`. You can also turn it off per command with `--ignore-remembered-arguments`. We've also really described a lot of important considerations and thoughts related to using this so there are no surprises. Please do read the issue notes at length if you plan to use this feature to reduce confusion.

### BREAKING CHANGES
 * Run with highestAvailable Execution Level by default - see [#1054](https://github.com/chocolatey/choco/issues/1054)

   One longstanding request with Chocolatey was to have it always request admin privileges before running. This has been a hope that it would cut down on the accidental runs of Chocolatey in a command shell that is not elevated and needing to open one that is elevated. This UAC (User account control) setting is handled by something called an application manifest (app.manifest). We had it set to "asInvoker", or run with the context of the user that ran the command. We've moved this to "highestAvailable", which means if you are a non-admin, it will just run under your context, but if you are an admin in a non-elevated shell, it will ask for elevated permissions to run. There is also "requireAdministrator", which locks execution down to administrators only.

   Moving to "highestAvailable" allows for that asking of privileges that you are used to, up front before it runs. However one additional thing we did here was give you more control over this setting now. We used to embed the app manifest into choco.exe. We now set it next to choco.exe (base install under `$env:ChocolateyInstall`, you will find `choco.exe.manifest`) so you have more freedom to adjust those execution levels as you see fit. There is one catch, you will need to do it on every install/upgrade of Chocolatey until [#1206](https://github.com/chocolatey/choco/issues/1206) is implemented.

 * When a prerelease is installed, it should upgrade to latest prerelease unless excluded - typically seen in choco upgrade all - see [#686](https://github.com/chocolatey/choco/issues/686)

   When you run `choco upgrade all`, it never catches the prereleases. However if you run `choco upgrade all --pre`, it may upgrade some of your stable installs to prereleases. Neither of these situations are desirable. So by default, we've made it so that `choco upgrade all` just does the right thing, which is to upgrade your stable releases to the latest stable release and your prerelease packages will upgrade to the absolute latest available, whether that be stable or prerelease. If you need to change the behavior back to the old way for upgrade all, simply add the `--exclude-prerelease` option.

 * Fix - Passing Allow Downgrade To upgrade against a prerelease may downgrade it to last stable version - see [#1212](https://github.com/chocolatey/choco/issues/1212)

   This is a bug fix that was allowing a prerelease to be downgraded accidentally to the last stable version if you ran `choco upgrade somepackage --allow-downgrade` without a particular version and without `--pre`. Now while this would be less affected with #686 above, it could still happen. It's a bug. The only reason this was marked as breaking change is that someone could be depending on the buggy behavior. So heads up, this bug is now fixed. If you are attempting to downgrade, make sure you specify the version you want it to go down to.

### FEATURES
 * [Security][Preview] Use Remembered Arguments for a Package During Upgrades - You must opt in for this to work - see [#797](https://github.com/chocolatey/choco/issues/797)
 * Show download progress for the packages themselves - see [#1134](https://github.com/chocolatey/choco/issues/1134)
 * Set Explicit Proxy Bypass List / Bypass On Local - see [#1165](https://github.com/chocolatey/choco/issues/1165)
 * Option/feature to stop installation when a package fails - see [#1151](https://github.com/chocolatey/choco/issues/1151)
 * Add File64 to Install-ChocolateyInstallPackage and Get-ChocolateyUnzip - see [#1187](https://github.com/chocolatey/choco/issues/1187)

### BUG FIXES
 * [Security] Fix - PowerShell sees authenticode hash as changed in scripts that were signed with Unix Line Endings (`LF`) - unable to use `AllSigned` - see [#1203](https://github.com/chocolatey/choco/issues/1203)
 * [Security] Fix - chocolatey setup - Use https for downloading .NET Framework 4x if not installed - see [#1112](https://github.com/chocolatey/choco/issues/1112)
 * Fix - chocolatey.config gets corrupted when multiple processes access simultaneously - see [#1047](https://github.com/chocolatey/choco/issues/1047)
 * Fix - "commandExecutionTimeoutSeconds" always reverts to 2700 when deprecated config setting is 0 - see [#1059](https://github.com/chocolatey/choco/issues/1059)
 * Fix - Allow Chocolatey version check with FIPS - see [#1193](https://github.com/chocolatey/choco/issues/1193)
 * Fix - Chocolatey doesn't always decompress downloads appropriately (support automatic decompression) - see [#1056](https://github.com/chocolatey/choco/issues/1056)
 * Fix - Load built-in Chocolatey functions, then load extensions - see [#1200](https://github.com/chocolatey/choco/issues/1200)
 * Fix - Use provided checksum type - see [#1018](https://github.com/chocolatey/choco/issues/1018)
 * Fix - MSU fails to install with space in path - see [#1177](https://github.com/chocolatey/choco/issues/1177)
 * Fix - Unable to disable failOnInvalidOrMissingLicense feature - see [#1069](https://github.com/chocolatey/choco/issues/1069)
 * Fix - PowerShell (Start-ChocolateyProcessAsAdmin) should only import the installerModule and not the profile - see [#1013](https://github.com/chocolatey/choco/issues/1013)
 * Fix - Automatic Uninstaller should skip when uninstaller executable does not exist - see [#1035](https://github.com/chocolatey/choco/issues/1035)
 * Fix - Package installation often fails with ERROR: You cannot call a method on a null-valued expression - see [#1141](https://github.com/chocolatey/choco/issues/1141)
 * Fix - Text file determination fails to throw an error because it catches it - see [#1010](https://github.com/chocolatey/choco/issues/1010)
 * Fix - Delete the .istext file before the content-type check - see [#1012](https://github.com/chocolatey/choco/issues/1012)
 * Fix - new command - don't add unparsed options as the name - see [#1085](https://github.com/chocolatey/choco/issues/1085)
 * Fix - Proxy settings ignored for local connections - see [#497](https://github.com/chocolatey/choco/issues/497)
 * Fix - RefreshEnv / Update-SessionEnvironment changes current user to SYSTEM - see [#902](https://github.com/chocolatey/choco/issues/902)
 * Fix - Set-EnvironmentVariable writes an error when installing Chocolatey as SYSTEM - see [#1043](https://github.com/chocolatey/choco/issues/1043)
 * Fix - Get-FtpFile fails with integer overflow when downloading file more than 2gb in size - see [#1098](https://github.com/chocolatey/choco/issues/1098)
 * Fix - Uninstall-ChocolateyPackage prints out warning if the passed file path starts and ends with quotes - see [#1039](https://github.com/chocolatey/choco/issues/1039)
 * Fix - Get-UninstallRegistryKey fixes/improvements - see [#815](https://github.com/chocolatey/choco/issues/815)
 * Fix - Unzip specific folder feature is broken after introducing 7zip - see [#676](https://github.com/chocolatey/choco/issues/676)
 * Fix - Join-Path error when installing Chocolatey as SYSTEM - see [#1042](https://github.com/chocolatey/choco/issues/1042)
 * Fix - `$env:OS_NAME` is 'Windows' for Windows 10 - see [#1178](https://github.com/chocolatey/choco/issues/1178)
 * Fix - choco install relativepath/to/some.nuspec fails - see [#906](https://github.com/chocolatey/choco/issues/906)
 * Fix - When pointing to a nupkg, choco should use only that nupkg to install and not a newer version in the same directory - see [#523](https://github.com/chocolatey/choco/issues/523)
 * Fix - Automatic uninstaller should split by quotes when necessary - see [#1208](https://github.com/chocolatey/choco/issues/1208)
 * [API] Fix - lib should merge the AlphaFS dependency - see [#1148](https://github.com/chocolatey/choco/issues/1148)
 * [API] Fix - don't reset loggers on setting custom automatically - see [#1121](https://github.com/chocolatey/choco/issues/1121)
 * [API] Fix - Chocolatey file cache adds a 'chocolatey' directory on each install - see [#1210](https://github.com/chocolatey/choco/issues/1210)
 * [API] Fix - Getting Local List of Package may leave config in undesirable state - see [#1213](https://github.com/chocolatey/choco/issues/1213)
 * Fix - Pro/Business - Chocolatey Licensed Feed May Show Up More Than Once - see [#1166](https://github.com/chocolatey/choco/issues/1166)
 * Fix - Pro/Business - Synchronized packages with DLLs are attempted to be imported by Chocolatey's PowerShell Extensions Loader - see [#1041](https://github.com/chocolatey/choco/issues/1041)

### IMPROVEMENTS
 * [Security] Username and password for `choco apikey` not encrypted in output - see [#1106](https://github.com/chocolatey/choco/issues/1106)
 * [Security] Sign Binaries / Authenticode Signatures with Chocolatey Software digital certificate - see [#1214](https://github.com/chocolatey/choco/issues/1214)
 * Setting commandExecutionTimeout to 0 means never time out - see [#1202](https://github.com/chocolatey/choco/issues/1202)
 * Configure sources to skip proxy - see [#262](https://github.com/chocolatey/choco/issues/262)
 * Set proxy information at runtime - see [#1173](https://github.com/chocolatey/choco/issues/1173)
 * Start-ChocolateyProcessAsAdmin should not elevate when already elevated - see [#1126](https://github.com/chocolatey/choco/issues/1126)
 * Add `--no-progress` cli switch for hidding progress bars - see [#917](https://github.com/chocolatey/choco/issues/917)
 * Note web status errors on package install failures - see [#1172](https://github.com/chocolatey/choco/issues/1172)
 * Always let Nuget.Core log - see [#1095](https://github.com/chocolatey/choco/issues/1095)
 * Make choco get its proxy settings also from environment variables - see [#605](https://github.com/chocolatey/choco/issues/605)
 * Remove quotes in process passed to Start-ChocolateyProcessAsAdmin / CommandExecutor - see [#1167](https://github.com/chocolatey/choco/issues/1167)
 * Increase download buffer size in Get-FtpFile to speed up downloads - see [#1099](https://github.com/chocolatey/choco/issues/1099)
 * Trace network traffic - see [#1182](https://github.com/chocolatey/choco/issues/1182)
 * Upgrade 7Zip to 16.04 - see [#1184](https://github.com/chocolatey/choco/issues/1184)
 * Do not create .ignore file if outside of Chocolatey location - see [#1180](https://github.com/chocolatey/choco/issues/1180)
 * Help should exit zero if called with the help switch, otherwise non-zero on bad commands - see [#473](https://github.com/chocolatey/choco/issues/473)
 * "Licensed messages" may address users in a somewhat unprofessional manner - see [#1111](https://github.com/chocolatey/choco/issues/1111)
 * Show the entire text to turn on the allowGlobalConfirmation flag - see [#1053](https://github.com/chocolatey/choco/issues/1053)
 * Running `choco` should produce name/version and further instructions - see [#1083](https://github.com/chocolatey/choco/issues/1083)
 * Typo in Install-ChocolateyPowershellCommand - see [#1088](https://github.com/chocolatey/choco/issues/1088)
 * Update `choco new pkg` template to give example of handling nested quoted paths - see [#1067](https://github.com/chocolatey/choco/issues/1067)
 * Add Aliases for Install-ChocolateyVsixPackage - see [#1146](https://github.com/chocolatey/choco/issues/1146)
 * Add Chocolatey Software to copyright - see [#1209](https://github.com/chocolatey/choco/issues/1209)
 * Pro/Business - Feature to Disable Non-Elevated Warnings - see [#1118](https://github.com/chocolatey/choco/issues/1118)
 * Pro/Business - Package Throttle - Bitrate limit packages and downloads (support) - see [#454](https://github.com/chocolatey/choco/issues/454)
 * Pro/Business - Allow version overrides for local packages - see [#942](https://github.com/chocolatey/choco/issues/942)
 * Pro/Business - List include programs should not show items from Package Synchronizer's Programs and Features Package Sync - see [#1205](https://github.com/chocolatey/choco/issues/1205)
 * Pro/Business - Show better messaging when unable to load licensed assembly - see [#1145](https://github.com/chocolatey/choco/issues/1145)
 * Pro/Business - PowerShell Functions should allow overriding urls - see [#1117](https://github.com/chocolatey/choco/issues/1117)
 * Pro/Business - Automatic Uninstaller - allow remove directly - see [#1119](https://github.com/chocolatey/choco/issues/1119)
 * Pro/Business - Add Chocolatey Architect edition license SKU - see [#1075](https://github.com/chocolatey/choco/issues/1075)
 * Pro/Business - Ensure sync command can be machine parseable - quiet logging - see [#1147](https://github.com/chocolatey/choco/issues/1147)
 * Pro/Business - Configure a source to be allowed for self-service - see [#1181](https://github.com/chocolatey/choco/issues/1181)


## [0.10.3](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.3+is%3Aclosed) (October 7, 2016)
### BREAKING CHANGES
 * Fix - Do Not Check `$LastExitCode` - Only error a package install if script errors or set a different exit code when it is specifically set - see [#1000](https://github.com/chocolatey/choco/issues/1000)

   Starting in v0.9.10, Chocolatey started checking `$LASTEXITCODE` in addition to the script command success as a way to be more helpful in determining package failures. This meant it offered the ability to capture when a script exited with `Exit 1` and handle that accordingly. However that really has never been a recommended scenario for returning errors from scripts and is not seen in the wild anywhere so it is believed that those that may be affected are very few.

   Checking `$LastExitCode` checks the last executable's exit code when the script specifically does not call `Exit` (which is . This can lead to very perplexing failures, such as running a successful xcopy that exits with `2` and seeing package failures without understanding why. Since it is not typically recommended to call `Exit` to return a value from PowerShell because of issues with different hosts, it's less of a concern to only look at explicit failures. For folks that may need it, allow failing a package again by the last external command exit code or `exit` from a PowerShell script. Note that it is not recommended to use exit with a number to return from PowerShell scripts. Instead you should use `$env:ChocolateyExitCode` or `Set-PowerShellExitCode` (first available in v0.9.10) to ensure proper setting of the exit code.

   If you need the prior behavior, please turn on the feature `scriptsCheckLastExitCode`.

### BUG FIXES
 * Fix - chocolateybeforemodify runs after modifying (moving) chocolatey lib package files - see [#995](https://github.com/chocolatey/choco/issues/995)
 * Fix - The refreshenv command throws an error about Write-FunctionCallLogMessage when ran in PowerShell on 0.10.2 - see [#996](https://github.com/chocolatey/choco/issues/996)


## [0.10.2](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.2+is%3Aclosed) (September 30, 2016)
We're dubbing this the "Every Joe" release in honor of a friend that just lost his fight with brain cancer. If you want to help further research, please make a donation to a cancer research association of your choosing (e.g. the [American Brain Tumor Assocation](http://www.abta.org/thank-you.html)).

A couple of important fixes/enhancements in this release. Most of the improvements are about providing better feedback to you and fixing minor issues. The big one surrounds when packages set a download path for a file using `$env:TEMP`, choco will ensure that the file can still be found for later use.

### BUG FIXES
 * Fix - Downloaded file is at old `$env:TEMP\chocolatey\chocolatey` location, but install calls with just `$env:TEMP\chocolatey\` location - see [#969](https://github.com/chocolatey/choco/issues/969)
 * Fix - [Pro/Business] UseOriginalLocation fails when there is no 64bit file - see [#972](https://github.com/chocolatey/choco/issues/972)
 * Fix - Do not use unparsed options as package names - see [#983](https://github.com/chocolatey/choco/issues/983)

### IMPROVEMENTS
 * Start-ChocolateyProcessAsAdmin enhancements - see [#977](https://github.com/chocolatey/choco/issues/977)
 * Log PowerShell function calls better - see [#976](https://github.com/chocolatey/choco/issues/976)
 * Allow environment variables in some config settings - see [#971](https://github.com/chocolatey/choco/issues/971)
 * [Pro/Business] Provide license type to environment variables - see [#968](https://github.com/chocolatey/choco/issues/968)
 * Note that chocolateyUninstall.ps1 may no longer required in template - see [#982](https://github.com/chocolatey/choco/issues/982)
 * Provide guidance when licensed only options are passed to FOSS - see [#984](https://github.com/chocolatey/choco/issues/984)
 * Rollback automatically when a user cancels an operation - see [#985](https://github.com/chocolatey/choco/issues/985)
 * Explain how to workaround a failing uninstall - see [#573](https://github.com/chocolatey/choco/issues/573)
 * Remove extra forward slashes in url - see [#986](https://github.com/chocolatey/choco/issues/986)
 * Side by side uninstall enhancements - see [#992](https://github.com/chocolatey/choco/issues/992)


## [0.10.1](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.1+is%3Aclosed) (September 19, 2016)
We're dubbing this the "Shhh! Keep that secret please" release. We've found that when passing in passwords and other sensitive arguments, those items can end up in the logs in clear text. We've addressed this in [#948](https://github.com/chocolatey/choco/issues/948) and [#953](https://github.com/chocolatey/choco/issues/953). When it comes to passing sensitive arguments through to native installers, you can set up environment variables with those sensitive args and pass those arguments directly through to `Start-ChocolateyProcessAsAdmin`. If you prefer a better experience, the licensed version allows passing sensitive options directly through choco.exe as `--install-arguments-sensitive` and `--package-parameters-sensitive`. Read more in the [Licensed CHANGELOG](https://github.com/chocolatey/choco/blob/master/CHANGELOG_LICENSED.md).

Perhaps the biggest improvement in this release is that Chocolatey will automatically look to see if it can download binaries over HTTPS when provided an HTTP url. If so, Chocolatey will switch to downloading the binaries over SSL. This provides better security in downloading and knowing you are getting the binary from the source location instead of a possible man in the middle location, especially when the package does not provide checksums for verification.

Another improvement you may not even notice, but we think you will love is that Chocolatey now supports TLS v1.2 transport which presents a nice transparent increase in security. You will need to have at least .NET Framework 4.5 installed to take advantage of this feature.

### FEATURES
 * [Security] Support TLS v1.2 - see [#458](https://github.com/chocolatey/choco/issues/458)
 * [Security] Attempt to download packages via HTTPS connection - see [#746](https://github.com/chocolatey/choco/issues/746)
 * [Security] Pro/Business - Pass sensitive arguments to installers - see [#948](https://github.com/chocolatey/choco/issues/948)
 * Search (and info) by version - see [#935](https://github.com/chocolatey/choco/issues/935)

### BUG FIXES
 * [Security] Fix - Passwords in command line options are logged in clear text - see [#953](https://github.com/chocolatey/choco/issues/953)
 * [Security] Fix - For PowerShell v2 - if switch down to SSLv3 protocol fails, go back to original protocol - see [#958](https://github.com/chocolatey/choco/issues/958)
 * Fix - Unzipping to ProgramFiles/System32 is Subject to File System Redirection - see [#960](https://github.com/chocolatey/choco/issues/960)
 * Fix - Run without login - see [#945](https://github.com/chocolatey/choco/issues/945)
 * Fix - Support Long Paths - see [#934](https://github.com/chocolatey/choco/issues/934)
 * Fix - help should not issue warning about elevated command shell - see [#893](https://github.com/chocolatey/choco/issues/893)
 * Fix - Licensed Feed cannot be disabled - see [#959](https://github.com/chocolatey/choco/issues/959)
 * Fix - Choco with unknown command should show help menu - see [#938](https://github.com/chocolatey/choco/issues/938)
 * Fix - Get-FtpFile error when file is missing (called through Get-ChocolateyWebFile) - see [#920](https://github.com/chocolatey/choco/issues/920)
 * Fix - Skip Get-WebFileName for FTP - see [#957](https://github.com/chocolatey/choco/issues/957)
 * Fix - Chocolatey-InstallChocolateyPackage fix for double chocolatey folder name is not also applied to the passed in file name - see [#908](https://github.com/chocolatey/choco/issues/908)
 * Fix - Start-ProcessAsAdmin - working directory should be from the location of the executable - see [#937](https://github.com/chocolatey/choco/issues/937)
 * [POSH Host] Fix - PowerShell Host - Package scripts setting values can affect packages that depend on them - see [#719](https://github.com/chocolatey/choco/issues/719)
 * Fix - Transactional install - pending check may fail if the lib folder doesn't exist - see [#954](https://github.com/chocolatey/choco/issues/954)
 * Fix - Start-ChocolateyProcessAsAdmin Module Import for PowerShell causes errors - see [#901](https://github.com/chocolatey/choco/issues/901)

### IMPROVEMENTS
 * Transactional Install - Improve concurrent operations (pending) - see [#943](https://github.com/chocolatey/choco/issues/943)
 * Uninstall-ChocolateyPackage should set unrecognized fileType to exe - see [#964](https://github.com/chocolatey/choco/issues/964)
 * Powershell functions - Allow access to package title, not only ID - see [#925](https://github.com/chocolatey/choco/issues/925)
 * Option to apply package parameters / install arguments to dependent packages - see [#839](https://github.com/chocolatey/choco/issues/839)
 * Get-ChocolateyWebFile download check enhancements - see [#952](https://github.com/chocolatey/choco/issues/952)
 * Do not treat unknown checksum types as MD5 - see [#932](https://github.com/chocolatey/choco/issues/932)
 * Pro/Business - Install-ChocolateyPackage - UseOriginalLocation - see [#950](https://github.com/chocolatey/choco/issues/950)
 * Auto determine checksum type - see [#922](https://github.com/chocolatey/choco/issues/922)
 * Ensure PowerShell functions have parameter name parity - see [#941](https://github.com/chocolatey/choco/issues/941)
 * Output from installer should go to verbose log - see [#940](https://github.com/chocolatey/choco/issues/940)


## [0.10.0](https://github.com/chocolatey/choco/issues?q=milestone%3A0.10.0+is%3Aclosed) (August 11, 2016)
What was planned for 0.9.10.4 is now 0.10.0. This is due partly to a breaking change we are making for security purposes and a move to provide better a better versioning scheme for the remainder of the sub-v1 versions of Chocolatey. Instead of 0.y.z.0 being considered where major verions occur in the sub 1 series, 0.y.0 will now be considered where those major versions occur. We also are moving right along towards v1 (and hope to be there in 2017).

0.10.0 carries the fixes for 0.9.10.4 and includes a major security enhancement (checksum requirement).

### BREAKING CHANGES
 * [Security] Checksum requirement and enhancements - see [#112](https://github.com/chocolatey/choco/issues/112)

Checksums in package scripts are meant as a measure to validate the originally intended downloaded resources used in the creation of a package are the same files that are received at a future date. This also ensures that the same files that are checked by all parts of moderation (if applicable) are the same files that are received by users for a package. This is seen mostly on the community repository because it is public and packages are subject to copyright laws (distribution rights), which typically requires the package scripts to download software from the official distribution locations. The Chocolatey framework has had the ability to use checksums in package scripts since [July 2014](https://chocolatey.org/packages/chocolatey/0.9.8.24#releasenotes).

**What is the requirement?** choco will now fail if a package download resources from HTTP/FTP and does not use checksums to verify those downloaded resources. The requirement for HTTP/FTP is [#112](https://github.com/chocolatey/choco/issues/112). We are considering also requiring it for [HTTPS (#895)](https://github.com/chocolatey/choco/issues/895) as well. You can optionally set a feature (`allowEmptyChecksumsSecure`) to ensure packages using HTTPS also use checksums.

**How does this protect the community anymore than before?** During moderation review, there is a check of these downloaded binaries against VirusTotal (which verifies these binaries against 50-60+ different virus scanners). The binaries are also verified for installation purposes against a test computer. With an independent 3rd party checksum in the package itself, it guarantees that the files received by a user from those remote sources are the exact same files that were used in the verification process.

**Why the requirement, and why now?** This is a measure of protection for the Chocolatey community. HTTP is easy to hack with both DNS poisoning and MITM (man in the middle) attacks. Without independent verification of the integrity of the downloaded resources, users can be left susceptible to these issues. We've been planning a move to require checksums for awhile now, with a planned longer and smoother transition for package maintainers to get packages updated to reduce breakages. Unfortunately there was a recent event with [FOSSHub getting hacked](http://www.audacityteam.org/compromised-download-partner/) (the [community repository had 8 possibly affected packages](http://us8.campaign-archive1.com/?u=86a6d80146a0da7f2223712e4&id=f2fe8dbe6b) and [we quickly took action](http://us8.campaign-archive1.com/?u=86a6d80146a0da7f2223712e4&id=2cbe87d486)), which necessitated a need for us to move in a much swifter fashion to ensure the protection of the community sooner, rather than later. The changes in Chocolatey represented by the checksum changes are a major step in the process to ensure protection. Requiring for HTTPS as well will mitigate any future compromises of software distribution sites that are used with Chocolatey packages.

**Can I shut this behavior off or opt out per package?**
You can shut off the checksum requirement by enabling the feature `allowEmptyChecksums`. This will return Chocolatey to previous behavior. We strongly recommend against it.

You can shut it off or turn it per package install/upgrade with `--allow-empty-checksums` and `--require-checksums`, respectively. See https://chocolatey.org/docs/commands-install / https://chocolatey.org/docs/commands-upgrade.

You can also disable the feature `allowEmptyChecksumsSecure` to enforce checksums for packages that download from secure locations (HTTPS).

**Other things I should know?** Users also now have the ability to pass their own checksums and checksumtypes into the install. See https://chocolatey.org/docs/commands-install / https://chocolatey.org/docs/commands-upgrade.

### KNOWN ISSUES
 * [Known Issues](https://github.com/chocolatey/choco/labels/Bug)

### FEATURES
 * Pro/Business - Download a package without installing it - see [#108](https://github.com/chocolatey/choco/issues/108)

### BUG FIXES
 * Fix - Installing choco on Windows 10 Vagrant box stops Vagrant from being able to manage the box - see [#834](https://github.com/chocolatey/choco/issues/834)
 * Fix - 64bit 7z.exe on 32bit system in chocolatey\tools - see [#836](https://github.com/chocolatey/choco/issues/836)
 * Fix - [POSH Host] PowerShell exit code does not reset between packages in a single run - see [#854](https://github.com/chocolatey/choco/issues/854)
 * Fix - Uninstall-ChocolateyZipPackage is failing - see [#871](https://github.com/chocolatey/choco/issues/871)
 * Fix - "C:\Program Files\WindowsPowerShell\Modules" is missing in PSModulePath for cmd.exe [#830](https://github.com/chocolatey/choco/issues/830)
 * Fix - Environment variables update fixes [#840](https://github.com/chocolatey/choco/issues/840)
 * Fix - Handle null items better - see [#853](https://github.com/chocolatey/choco/issues/853)
 * Fix - HKCU may not have Environment (Install of Chocolatey) - see [#375](https://github.com/chocolatey/choco/issues/375)
 * Fix - Progress of download does not clear the whole output line - see [#875](https://github.com/chocolatey/choco/issues/875)
 * Fix - Wrong download progress reported during package upgrade - see [#872](https://github.com/chocolatey/choco/issues/872)
 * Fix - Uninstall not supporting side-by-side => ChocolateyUninstall.ps1 not run - see [#862](https://github.com/chocolatey/choco/issues/862)
 * Fix - Uninstall ignores the version parameter - see [#861](https://github.com/chocolatey/choco/issues/861)
 * Fix - Search by exact or by id only is case sensitive for remote sources - see [#889](https://github.com/chocolatey/choco/issues/889)
 * Fix - Deprecated links inserted in .nuspec files created by `choco new ...` - see [#870](https://github.com/chocolatey/choco/issues/870)
 * Fix - Get-OSArchitectureWidth doesn't do what it says it does - see [#828](https://github.com/chocolatey/choco/issues/828)
 * Fix - When Choco fails to get a package from NuGet Core, fail the package with exit code 1 - see [#867](https://github.com/chocolatey/choco/issues/867)
 * Fix - Illegal characters in path - see [#857](https://github.com/chocolatey/choco/issues/857)
 * Fix - Get-OSArchitectureWidth doesn't do what it says it does - see [#828](https://github.com/chocolatey/choco/issues/828)
 * Fix - Pro/Business - Choco install config file fails on licensed assembly - see [#866](https://github.com/chocolatey/choco/issues/866)
 * Fix - DISM /all doesn't run anywhere but Windows 6.2 -- no dependencies get installed - see [#897](https://github.com/chocolatey/choco/issues/897)

### IMPROVEMENTS
 * Do not install tab completion (edit of profile) under certain conditions - see [#833](https://github.com/chocolatey/choco/issues/833)
 * Choco install with packages.config should print out the packages to install - see [#878](https://github.com/chocolatey/choco/issues/878)
 * Larger default log file size and retention - see [#852](https://github.com/chocolatey/choco/issues/852)
 * Allow getting installer type to be overridden - see [#885](https://github.com/chocolatey/choco/issues/885)
 * Pack - Add optional output folder option - see [#598](https://github.com/chocolatey/choco/issues/598)
 * Little command name correction on init.ps1 - see [#595](https://github.com/chocolatey/choco/issues/595)
 * Tab completion - don't query if there is a file in the folder that meets completion - see [#847](https://github.com/chocolatey/choco/issues/847)


## [0.9.10.3](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.10.3+is%3Aclosed) (June 23, 2016)
### BUG FIXES
 * Fix - Ignore ValidPackage Exit Codes for Real - see [#827](https://github.com/chocolatey/choco/issues/827)
 * Fix - Cache folder running under SYSTEM account should be C:\Windows\TEMP - see [#826](https://github.com/chocolatey/choco/issues/826)
 * Fix - Built-in 7zip doesn't behave properly - see [#775](https://github.com/chocolatey/choco/issues/775)
 * Fix - Successful installer exit codes not recognized by choco should return 0 - see [#821](https://github.com/chocolatey/choco/issues/821)
 * Fix - NotSilent fails with "Cannot bind argument to parameter statements because it is an empty string" - see [#819](https://github.com/chocolatey/choco/issues/819)
 * Fix - Silent Args being passed as a string array cause package failure - see [#808](https://github.com/chocolatey/choco/issues/808)

### IMPROVEMENTS
 * Hold pending check for 10 seconds / provide means of explicitly overriding the transactional install cleanup - see [#822](https://github.com/chocolatey/choco/issues/822)
 * Pro/Business - Add runtime skip option to allow skipping the virus scanner - see [#786](https://github.com/chocolatey/choco/issues/786)


## [0.9.10.2](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.10.2+is%3Aclosed) (June 19, 2016)
### BUG FIXES
 * Fix - Chocolatey Licensed is unable to find 0.9.10.x (only 0.9.10.0) - see [#814](https://github.com/chocolatey/choco/issues/814)
 * Fix - Logging is broken in some packages due to new TEMP directory - see [#813](https://github.com/chocolatey/choco/issues/813)
 * [API] Fix - When performing an Install/Uninstall/Upgrade operation through the API, an error is throw for "chocolatey.resources" - see [#811](https://github.com/chocolatey/choco/issues/811)

### IMPROVEMENTS
 * Ensure log file path exists - and fix the log file arguments if necessary - see [#758](https://github.com/chocolatey/choco/issues/758)


## [0.9.10.1](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.10.1+is%3Aclosed) (June 18, 2016)
### BUG FIXES
 * Fix - Cannot bind argument to parameter 'exitCode' because it is null - see [#810](https://github.com/chocolatey/choco/issues/810)

### IMPROVEMENTS
 * [Security] Upgrade to 7zip 16.02 to overcome CVE-2016-2334/CVE-2016-2335 - see [#812](https://github.com/chocolatey/choco/issues/812)


## [0.9.10](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.10+is%3Aclosed) (June 17, 2016)

![Chocolatey Logo](https://cdn.rawgit.com/chocolatey/choco/14a627932c78c8baaba6bef5f749ebfa1957d28d/docs/logo/chocolateyicon.gif "Chocolatey")

The "I got 99 problems, but a package manager ain't one" release. With the release of 0.9.10 (or if you prefer 0.9.10.0), we're about to make everything 100% better in your Windows package management world. We've addressed over 100 features and bugs in this release. We looked at how we could improve PowerShell and we've come out with a [competely internal host](https://github.com/chocolatey/choco/issues/8) that can Prompt and Read-Host in a way that times out and selects default values after a period of time. Speaking of PowerShell, how about some tab completion `choco &lt;tab&gt;` to `choco install node&lt;tab&gt;`? How about never having to [close and reopen your shell again](https://github.com/chocolatey/choco/issues/664)?

Alternative sources (`-source webpi`, `-s windowsfeature`, etc) are back! I mean, am I right?! Have you heard of auto uninstaller? If Chocolatey has installed something that works with Programs and Features, Chocolatey knows how to uninstall it without an uninstall script about 90+% of the time. This feature was in beta for the 0.9.9 series, it is on by default in 0.9.10 (unless you disabled it after trying it, you will need to reenable it, see `choco feature` for more details).

Here's one you probably never knew existed - extensions. Chocolatey has had the ability to extend itself by adding PowerShell modules for years, and most folks either didn't know it existed or have never used them. We've enhanced them a bit in preparation for the licensed version of Chocolatey.

We redesigned our `choco new` default packaging template and we've made managing templates as easy as managing packages.

`choco search`/`choco list` has so many enhancements, you may not need to visit dot org again. [See it in action](https://chocolatey.org/docs/commands-list#see-it-in-action).
* [search -v provides moderation related information and a world of nuspec information](https://github.com/chocolatey/choco/issues/493)
* [search by id only](https://github.com/chocolatey/choco/issues/663)
* [search by id exact](https://github.com/chocolatey/choco/issues/453)
* [search by approved only, not broken, and/or by download cache](https://github.com/chocolatey/choco/issues/670)
* [sort by version](https://github.com/chocolatey/choco/issues/668)
* [search with paging](https://github.com/chocolatey/choco/issues/427)

There are 150 tickets closed for this release! We've included remediation steps for when a breaking change affects you. Mostly if you have been using Chocolatey in a recommended way, you won't even notice any adverse changes. There are a number of things we thought to highlight, and quite a few security enhancements coming in this release (look for the [Security] tag on the ticket summary).

### BREAKING CHANGES
 * Only fail automation scripts (chocolateyInstall.ps1) if the script returns non-zero exit code - see [#445](https://github.com/chocolatey/choco/issues/445)

The 0.9.8 series would only fail a package with terminating errors. The 0.9.9 series took that a bit further and started failing packages if anything wrote to stderr. It turns out that is a bad idea. Only when PowerShell exits with non-zero (which comes with terminating errors) should the package fail due to this. If you need the old behavior of the 0.9.9 series, you can get it back with a switch (`--fail-on-standard-error` and its aliases) and/or a feature flip (`failOnStandardError`).

 * Fix - Force reinstall, force upgrade, and uninstall should delete the download cache - see [#590](https://github.com/chocolatey/choco/issues/590)

If you set a custom cache directory for downloads, it will no longer use a "chocolatey" subdirectory under that. You may need to make any adjustments if this is going to affect you.

 * Exit with the same exit code as the software being installed - see [#512](https://github.com/chocolatey/choco/issues/512)

There are more exit codes from Chocolatey now that indicate success -`0`, `1605`, `1614`, `1641`, and `3010`. You may need to adjust anything you were using that would only check for 0 and nonzero.
If you need the previous behavior, be sure to disable the feature `usePackageExitCodes` or use the `--ignore-package-exit-codes` switch in your choco commands.

 * PowerShell module functions adjusted for automatic documentation - see [#697](https://github.com/chocolatey/choco/issues/697)

If you were using any of the functions in a non-recommended way or not compliant with the examples, you are going to find breakages in the functions as some of the things that were called out as non-optional are now enforced. This shouldn't affect most folks.

 * [Security] Explicit permissions - remove inheritance/lock down to admins - see [#398](https://github.com/chocolatey/choco/issues/398)

This further restricts the default installation location by removing all permissions and inheritance of permissions, explicitly giving Administrator/LocalSystem to Full access, and Users are granted Read and Execute. In prior installations, we ensured Modify access to the installing user, but that has been removed for security reasons. Should you need the previous behavior, set `$env:ChocolateyInstallAllowCurrentUser="true"`.

### KNOWN ISSUES
 * [Known Issues](https://github.com/chocolatey/choco/labels/Bug)

### FEATURES
 * Alternative Sources - see [#14](https://github.com/chocolatey/choco/issues/14)
 * [POSH Host] Use Internal PowerShell Host - see [#8](https://github.com/chocolatey/choco/issues/8)
 * Run a script before uninstall/upgrade (chocolateyBeforeModify.ps1) to allow for things like services to shutdown - see [#268](https://github.com/chocolatey/choco/issues/268)
 * Manage package templates with a specially named package and special package folder - see [#542](https://github.com/chocolatey/choco/issues/542)
 * Support for custom headers - see [#332](https://github.com/chocolatey/choco/issues/332)
 * [Security] Show moderation-related information in search results - see [#493](https://github.com/chocolatey/choco/issues/493)
 * New Helper - Get-ToolsLocation helper (replacement for Get-BinRoot) - see [#631](https://github.com/chocolatey/choco/issues/631)
 * Choco list/search should have exact filter search - see [#453](https://github.com/chocolatey/choco/issues/453)
 * RefreshEnv (Refresh Environment Variables) Should also work in PowerShell - see [#664](https://github.com/chocolatey/choco/issues/664)
 * Provide PowerShell tab completion for Chocolatey - see [#412](https://github.com/chocolatey/choco/issues/412)
 * [Security] Sign the powershell scripts and assemblies - see [#501](https://github.com/chocolatey/choco/issues/501)
 * Add a `choco info` command to show info for one package - see [#644](https://github.com/chocolatey/choco/issues/644)
 * Mark packages pending until install completes successfully - see [#198](https://github.com/chocolatey/choco/issues/198)
 * Resolve sources by name - see [#356](https://github.com/chocolatey/choco/issues/356)
 * Uninstall-ChocolateyEnvironmentVariable function - see [#772](https://github.com/chocolatey/choco/issues/772)
 * Get-UninstallRegistryKey function - see [#739](https://github.com/chocolatey/choco/issues/739)
 * Pro/Business - Ubiquitous Install Directory Switch - see [#258](https://github.com/chocolatey/choco/issues/258)
 * Pro/Business - Runtime Virus Scanning - see [virus scanning](https://chocolatey.org/docs/features-virus-check)
 * Pro/Business - Private CDN cache for downloads - see [private CDN cache](https://chocolatey.org/docs/features-private-cdn)
 * Pro/Business - Sync "choco installed status" with "Windows installed status" - see [#567](https://github.com/chocolatey/choco/issues/567)

### BUG FIXES
 * [Security] Fix - Only load the Chocolatey PowerShell module from a known location - see [#560](https://github.com/chocolatey/choco/issues/560)
 * [Security] Fix - Package source authentication at http://location/path doesn't also use http://location/ (base url) - see [#466](https://github.com/chocolatey/choco/issues/466)
 * [Security] Fix - When defining a proxy without credentials - proxy password is shown in plain text - see [#503](https://github.com/chocolatey/choco/issues/503)
 * [Security] Fix - Fully qualify shutdown command - see [#702](https://github.com/chocolatey/choco/issues/702)
 * [Security] Fix - MSI packages fail install with `Could not find 'msiexec'` - see [#723](https://github.com/chocolatey/choco/issues/723)
 * Fix - Force should set allow-downgrade to true - see [#585](https://github.com/chocolatey/choco/issues/585)
 * Fix - Do not use NuGet package cache - see [#479](https://github.com/chocolatey/choco/issues/479)
 * Fix - Pack doesn't include chocolatey-specific metadata - see [#607](https://github.com/chocolatey/choco/issues/607)
 * Fix - TEMP environment variable is 8.3 Path on some systems - see [#532](https://github.com/chocolatey/choco/issues/532)
 * Fix - `$packageName` should be present for zip uninstalls in uninstall script template - see [#534](https://github.com/chocolatey/choco/issues/534)
 * Fix - Debug/Verbose messages not logged in automation scripts (chocolateyInstall.ps1) - see [#520](https://github.com/chocolatey/choco/issues/520)
 * Fix - Escape log output for variables that have data from external sources - see [#565](https://github.com/chocolatey/choco/issues/565)
 * Fix - Choco new silentargs can't pass in args in the param=value format - see [#510](https://github.com/chocolatey/choco/issues/510)
 * Fix - Exception if no source is enabled - see [#490](https://github.com/chocolatey/choco/issues/490)
 * Fix - Chocolatey command help output written to standard error instead of standard out - see [#468](https://github.com/chocolatey/choco/issues/468)
 * Fix - Logger doesn't clear cached NullLoggers - see [#516](https://github.com/chocolatey/choco/issues/516)
 * Fix - DISM "/All" argument in the wrong position - see [#480](https://github.com/chocolatey/choco/issues/480)
 * Fix - Pro/Business - Installing/uninstalling extensions should rename files in use - see [#594](https://github.com/chocolatey/choco/issues/594)
 * Fix - Running Get-WebFileName in PowerShell 5 fails and sometimes causes package errors - see [#603](https://github.com/chocolatey/choco/issues/603)
 * Fix - Merging assemblies on a machine running .Net 4.5 or higher produces binaries incompatible with .Net 4 - see [#392](https://github.com/chocolatey/choco/issues/392)
 * Fix - [API] - Incorrect log4net version in chocolatey.lib dependencies - see [#390](https://github.com/chocolatey/choco/issues/390)
 * [POSH Host] Fix - Message after Download progress is on the same line sometimes - see [#525](https://github.com/chocolatey/choco/issues/525)
 * [POSH Host] Fix - PowerShell internal process - "The handle is invalid." - see [#526](https://github.com/chocolatey/choco/issues/526)
 * [POSH Host] Fix - The handle is invalid - when output is being redirected and a package attempts to write to a filestream - see [#572](https://github.com/chocolatey/choco/issues/572)
 * [POSH Host] Fix - Write-Host adding multiple line breaks - see [#672](https://github.com/chocolatey/choco/issues/672)
 * [POSH Host] Fix - PowerShell Host doesn't show colorization overrides - see [#674](https://github.com/chocolatey/choco/issues/674)
 * [POSH Host] Fix - $profile is empty string when installing packages - does not automatically install the ChocolateyProfile - see [#667](https://github.com/chocolatey/choco/issues/667)
 * [POSH Host] Fix - Getting LCID doesn't work properly with the built-in PowerShell - see [#741](https://github.com/chocolatey/choco/issues/741)
 * [POSH Host] Fix - Host.Version should return actual PowerShell version - see [#708](https://github.com/chocolatey/choco/issues/708)
 * Fix - Verbose shows in output on debug switch - see [#611](https://github.com/chocolatey/choco/issues/611)
 * Fix - Get-ChocolateyUnzip captures files that don't belong to the package / Unzip should not do a full disk scan - see [#616](https://github.com/chocolatey/choco/issues/616) and [#155](https://github.com/chocolatey/choco/issues/155)
 * Fix - Package succeeds but software install silently fails when Install-ChocolateyInstallPackage has the wrong arguments - see [#629](https://github.com/chocolatey/choco/issues/629)
 * Fix - ShimGen handling of spaces and arguments that have shimgen in them - see [#647](https://github.com/chocolatey/choco/issues/647)
 * Fix - PowerShell v2 - Choco installer messages can't actually be warnings (causes FileStream errors) - see [#666](https://github.com/chocolatey/choco/issues/666)
 * Fix - Installing chocolatey removes $env:PSModulePath changes for current PowerShell session - see [#295](https://github.com/chocolatey/choco/issues/295)
 * Fix - Notice for Get-BinRoot deprecation won't be displayed - see [#673](https://github.com/chocolatey/choco/issues/673)
 * Fix - choco new creates a bad ChocolateyUninstall.ps1 script which does not work.  - see [#460](https://github.com/chocolatey/choco/issues/460)
 * Fix - ShimGen fails when file metadata has strings that need literals - see [#677](https://github.com/chocolatey/choco/issues/677)
 * Fix - Install-ChocolateyPath Expands Variables in PATH, Overwriting Preexisting Variables - see [#303](https://github.com/chocolatey/choco/issues/303)
 * Fix - Install-ChocolateyShortcut gives invalid warning when target is a web url - see [#592](https://github.com/chocolatey/choco/issues/592)
 * Fix - Argument Parsing failures should be reported as warnings and not debug messages - see [#571](https://github.com/chocolatey/choco/issues/571)
 * Fix - choco pack returns zero exit code when Nuget.Core validation errors - see [#469](https://github.com/chocolatey/choco/issues/469)
 * Fix - `Install-ChocolateyPath` updates `PATH` to `REG_SZ`, which may break using Windows dir and system32 tools - see [#699](https://github.com/chocolatey/choco/issues/699)
 * Fix - Removing environment variables sets empty environment variables - see [#724](https://github.com/chocolatey/choco/issues/724)
 * Fix - Environment Variable Changes Require Reboot - see [#728](https://github.com/chocolatey/choco/issues/728)
 * Fix - Get-WebFileName determines strange file name - see [#727](https://github.com/chocolatey/choco/issues/727)
 * Fix - Package params are also applied to dependent package - see [#733](https://github.com/chocolatey/choco/issues/733)
 * Fix - Use package name/version from environment, not parameters - see [#751](https://github.com/chocolatey/choco/issues/751)
 * Fix - Get-WebFileName Does Not Match on Invalid Characters - see [#753](https://github.com/chocolatey/choco/issues/753)
 * Fix - `choco new` cannot introduce multistage folder hierarchy template - see [#706](https://github.com/chocolatey/choco/issues/706)
 * Fix - Empty $env:ChocolateyToolsLocation combine error - see [#756](https://github.com/chocolatey/choco/issues/756)
 * Fix - Installing chocolatey removes $env:PSModulePath changes for current powershell session - see [#295](https://github.com/chocolatey/choco/issues/295)
 * Fix - Some environment variables are set too early for options/switches to have an effect - see [#620](https://github.com/chocolatey/choco/issues/620)
 * [API] Fix - Issue when attempting to execute run command through API - see [#769](https://github.com/chocolatey/choco/issues/769)
 * Fix - Logging of upgrade messages - placement of some messages is incorrect - see [#557](https://github.com/chocolatey/choco/issues/557)
 * Fix - Get-WebFile fails with - The term '//continue' is not recognized as the name of a cmdlet - see [#789](https://github.com/chocolatey/choco/issues/789)
 * Fix - Unable to read registry snapshot file - see [#487](https://github.com/chocolatey/choco/issues/487)
 * Fix - Pro/Business - Licensed version has an incorrect dependency on PowerShell assemblies and will only load v3 and above - see [#799](https://github.com/chocolatey/choco/issues/799)
 * Fix - Exit codes in package scripts should work - see [#802](https://github.com/chocolatey/choco/issues/802)
 * Fix - Running choco new creates a bad nuspec - see [#801](https://github.com/chocolatey/choco/issues/801)

### IMPROVEMENTS
 * AutoUninstaller is on by default - see [#308](https://github.com/chocolatey/choco/issues/308)
 * Use the actual download file name instead of providing one - see [#435](https://github.com/chocolatey/choco/issues/435)
 * Unset Configuration Values - see [#551](https://github.com/chocolatey/choco/issues/551)
 * Ability to run "choco upgrade all" ignoring specific packages - see [#293](https://github.com/chocolatey/choco/issues/293)
 * Extensions enhancements - see [#588](https://github.com/chocolatey/choco/issues/588)
 * Show human-readable file sizes when downloading - see [#363](https://github.com/chocolatey/choco/issues/363)
 * [Security] Warn about environment changes - see [#563](https://github.com/chocolatey/choco/issues/563)
 * Warn when execution timeout has elapsed - see [#561](https://github.com/chocolatey/choco/issues/561)
 * Update nuspec to make it easier to get started - see [#535](https://github.com/chocolatey/choco/issues/535)
 * Suppress verbose output to verbose - like with 7-zip - see [#476](https://github.com/chocolatey/choco/issues/476)
 * Choco push moderation message only on push to dot org - see [#601](https://github.com/chocolatey/choco/issues/601)
 * Allow tools/bin root to be root of the drive again - see [#628](https://github.com/chocolatey/choco/issues/628)
 * File description of ShimGen shims should match original as closely as possible - see [#374](https://github.com/chocolatey/choco/issues/374)
 * Shim Generation should automatically detect GUI - see [#634](https://github.com/chocolatey/choco/issues/634)
 * Don't show 32 bit wording unless there is explicitly both versions available - see [#642](https://github.com/chocolatey/choco/issues/642)
 * Allow passing arbitrary key/value arguments to new command when generating packages from templates - see [#658](https://github.com/chocolatey/choco/issues/658)
 * Choco search/list should be able to search just by Id - see [#663](https://github.com/chocolatey/choco/issues/663)
 * Search by approved, by not broken, by download cache - see [#670](https://github.com/chocolatey/choco/issues/670)
 * Save nuspec files with package installs - see [#623](https://github.com/chocolatey/choco/issues/623)
 * Show a prompt character when asking a multiple choice question - see [#184](https://github.com/chocolatey/choco/issues/184)
 * When prompting for a user yes/no answer, use a short [y/n] representation - see [#181](https://github.com/chocolatey/choco/issues/181)
 * Default package template should include LICENSE.txt and VERIFICATION.txt for packages with binaries - see [#675](https://github.com/chocolatey/choco/issues/675)
 * choco list/search aliases for -v - '-detail' and '-detailed' - see [#646](https://github.com/chocolatey/choco/issues/646)
 * Log normal output to a secondary log - see [#682](https://github.com/chocolatey/choco/issues/682)
 * Display Package test status information on install/upgrade - see [#696](https://github.com/chocolatey/choco/issues/696)
 * Report when reboots are necessary from package installs - see [#712](https://github.com/chocolatey/choco/issues/712)
 * Report loaded extensions - see [#715](https://github.com/chocolatey/choco/issues/715)
 * Exit with specific codes on certain actions - see [#707](https://github.com/chocolatey/choco/issues/707)
 * Determine if Downloaded File is HTML or Plain Text - see [#649](https://github.com/chocolatey/choco/issues/649)
 * Interactively prompt with timeout on some questions - see [#710](https://github.com/chocolatey/choco/issues/710)
 * [POSH Host] Exit code from PowerShell Host should be useful - see [#709](https://github.com/chocolatey/choco/issues/709)
 * Update environment for scripts after setting environment variables - see [#729](https://github.com/chocolatey/choco/issues/729)
 * Clean up any temp nuget folder actions after NuGet operations - see [#622](https://github.com/chocolatey/choco/issues/622)
 * Ensure Web Requests and Responses Do Not Timeout - make configurable - see [#732](https://github.com/chocolatey/choco/issues/732)
 * Combine timeout from push and execution timeout as one parameter - see [#752](https://github.com/chocolatey/choco/issues/752)
 * Override autouninstaller / failonautouninstaller fail with switches for uninstall  - see [#515](https://github.com/chocolatey/choco/issues/515)
 * Offer to remove actual package (*.install/*.portable) when removing meta/virtual package - see [#735](https://github.com/chocolatey/choco/issues/735)
 * Provide more info in package summary - see [#455](https://github.com/chocolatey/choco/issues/455)
 * Report install location - see [#689](https://github.com/chocolatey/choco/issues/689)
 * Track MSI Information Better - see [#755](https://github.com/chocolatey/choco/issues/755)
 * Support for client certificates - see [#399](https://github.com/chocolatey/choco/issues/399)
 * choco feature list formatting enhancements - see [#742](https://github.com/chocolatey/choco/issues/742)
 * choco new --original-template - see [#737](https://github.com/chocolatey/choco/issues/737)
 * Update Get-FtpFile with fixes for Get-WebFile - see [#765](https://github.com/chocolatey/choco/issues/765)
 * Rename Get-ProcessorBits as a more appropriately named Get-OSArchitectureWidth - see [#713](https://github.com/chocolatey/choco/issues/713)
 * Allow passing no 32-bit url and fail the package on 32-bit systems - see [#527](https://github.com/chocolatey/choco/issues/527)
 * Enhance Install-ChocolateyShortcut to support WindowStyle, Pin to Taskbar and Run As Administrator checkbox - see [#519](https://github.com/chocolatey/choco/issues/519)
 * [Security] Allow hashing files for checksums with FIPS compliant algorithms - see [#446](https://github.com/chocolatey/choco/issues/446)
 * After upgrading provide summary of upgraded packages - see [#759](https://github.com/chocolatey/choco/issues/759)
 * Web functions - Check for local file and return early - see [#781](https://github.com/chocolatey/choco/issues/781)
 * Refresh environment variables after each install - see [#439](https://github.com/chocolatey/choco/issues/439)
 * Capture Arguments for a Package during Install/Upgrade - see [#358](https://github.com/chocolatey/choco/issues/358)
 * If config update fails, log to debug instead of warn - see [#793](https://github.com/chocolatey/choco/issues/793)
 * Remove extra empty lines when doing choco upgrade all - see [#796](https://github.com/chocolatey/choco/issues/796)
 * Mention required permissions if user has no access - see [#794](https://github.com/chocolatey/choco/issues/794)
 * Pro/Business - Also check for license in User Profile location - see [#606](https://github.com/chocolatey/choco/issues/606)
 * Pro/Business - Set download cache information if available - see [#562](https://github.com/chocolatey/choco/issues/562)
 * Pro/Business - Allow commands to be added - see [#583](https://github.com/chocolatey/choco/issues/583)
 * Pro/Business - Load/Provide hooks for licensed version - see [#584](https://github.com/chocolatey/choco/issues/584)
 * Pro/Business - On valid license, add pro/business source automatically - see [#604](https://github.com/chocolatey/choco/issues/604)
 * Pro/Business - Add switch to fail on invalid or missing license - see [#596](https://github.com/chocolatey/choco/issues/596)
 * Pro/Business - add ignore invalid switches/parameters - see [#586](https://github.com/chocolatey/choco/issues/586)
 * Pro/Business - Don't prompt to upload file for virus scanning if it is too large - see [#695](https://github.com/chocolatey/choco/issues/695)
 * Pro/Business - add 'support' command - see [#745](https://github.com/chocolatey/choco/issues/745)
 * Pro/Business - Adjust environment settings warning to suggest upgrade - see [#795](https://github.com/chocolatey/choco/issues/795)
 * API - Add the ability to retrieve package count for a Source - see [#431](https://github.com/chocolatey/choco/issues/431)
 * API - Chocolatey Lib still marks vital package information as internal - see [#433](https://github.com/chocolatey/choco/issues/433)
 * API - Add paging to list command - see [#427](https://github.com/chocolatey/choco/issues/427)
 * API - Choco search should sort by version - see [#668](https://github.com/chocolatey/choco/issues/668)
 * API - Switch dll to .NET Client Profile - see [#680](https://github.com/chocolatey/choco/issues/680)


## [0.9.9.12](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.12+is%3Aclosed) (March 18, 2016)
### BUG FIXES
 * Fix - PowerShell "Collection is read-only" - see [#659](https://github.com/chocolatey/choco/issues/659)


## [0.9.9.11](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.11+is%3Aclosed) (October 6, 2015)
### BUG FIXES
 * Fix - Pin list is broken - see [#452](https://github.com/chocolatey/choco/issues/452)


## [0.9.9.10](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.10+is%3Aclosed) (October 3, 2015)

Not to be confused with 0.9.10 (this is not that version). This fixes a small but extremely significant issue with relation to configuration managers and other tools that use choco.

### BUG FIXES
 * Fix - List output for other tools messed up in 0.9.9.9 (pipe separator missing) - see [#450](https://github.com/chocolatey/choco/issues/450)
 * Fix - accidentally escaped characters in "new" -help - see [#447](https://github.com/chocolatey/choco/issues/447)


## [0.9.9.9](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.9+is%3Aclosed) (October 2, 2015)

With this release you can completely configure choco from the command line (including the priority of sources). Choco now allows you to create [custom package templates](https://github.com/chocolatey/choco/issues/76). Choco has [proper proxy support](https://github.com/chocolatey/choco/issues/243) now. We also squashed up some bugs, like the infinite download loop that happens if the connection is lost. We've also improved the installation experience of Chocolatey itself, [unpacking all of the required setup files in the chocolatey package](https://github.com/chocolatey/choco/issues/347) and improving the messaging output during the bootstrapping process. Chocolatey also [doesn't try to write config updates every command](https://github.com/chocolatey/choco/issues/364), unless something actually changes in the config file. And last but not least for mentions, the issue of [choco not recognizing itself as needing upgraded after being installed by the bootstrapper](https://github.com/chocolatey/choco/issues/414) is now fixed.

### FEATURES
 * Config Command - see [#417](https://github.com/chocolatey/choco/issues/417)
 * Create Custom Package Templates - see [#76](https://github.com/chocolatey/choco/issues/76)
 * Proxy Support - see [#243](https://github.com/chocolatey/choco/issues/243)

### BUG FIXES
 * Fix - [Security] Remove rollback should validate it exists in choco install backup directory - see [#387](https://github.com/chocolatey/choco/issues/387)
 * Fix - Ensure chocolatey is installed into the lib folder during initial install - see [#414](https://github.com/chocolatey/choco/issues/414)
 * Fix - Infinite loop downloading files if connection is lost - see [#285](https://github.com/chocolatey/choco/issues/285)
 * Fix - list / search results blocking until completion instead of streaming output - see [#143](https://github.com/chocolatey/choco/issues/143)
 * Fix - default template install script for MSI silentArgs are bad - see [#354](https://github.com/chocolatey/choco/issues/354)
 * Fix - Deleting read-only files fails - see [#338](https://github.com/chocolatey/choco/issues/338) and [#263](https://github.com/chocolatey/choco/issues/263)
 * Fix - If the package uses $packageParameters instead of $env:PackageParameters, quotes are removed - see [#406](https://github.com/chocolatey/choco/issues/406)
 * Fix - Choco upgrade not downloading new installer if current installer is the same size - see [#405](https://github.com/chocolatey/choco/issues/405)
 * Fix - Exit with non-zero code if install/upgrade version and a newer version is installed - see [#365](https://github.com/chocolatey/choco/issues/365)
 * Fix - Chocolately can permanently corrupt the config file if an operation is interrupted - see [#355](https://github.com/chocolatey/choco/issues/355)
 * Fix - Handle PowerShell's `InitializeDefaultDrives` Error (that should just be a warning) - see [#349](https://github.com/chocolatey/choco/issues/349)
 * Fix - Checksumming can not be turned off by the feature flag - see [#33](https://github.com/chocolatey/choco/issues/33)
 * Fix - Process with an id of is not running errors on 0.9.9.8 - see [#346](https://github.com/chocolatey/choco/issues/346)
 * Fix - Export cmdlets for automation scripts - see [#422](https://github.com/chocolatey/choco/issues/422)

### IMPROVEMENTS
 * [Security] Add SHA-2 (sha256 / sha512) to checksum - see [#113](https://github.com/chocolatey/choco/issues/113)
 * Sources should have explicit priority order- see [#71](https://github.com/chocolatey/choco/issues/71)
 * Unpack the powershell files just before packaging up the nupkg (Installing chocolatey meta) - see [#347](https://github.com/chocolatey/choco/issues/347)
 * API - List --localonly not working by default - see [#223](https://github.com/chocolatey/choco/issues/223)
 * API - Expose package results - see [#132](https://github.com/chocolatey/choco/issues/132)
 * API - Externalize IPackage and its interfaces - see [#353](https://github.com/chocolatey/choco/issues/353)
 * Enhance "Access to path is denied" message on no admin rights - see [#177](https://github.com/chocolatey/choco/issues/177)
 * Only update chocolatey.config if there are changes - see [#364](https://github.com/chocolatey/choco/issues/364)
 * Modify source when attempting to add a source with same name but different URL - see [#88](https://github.com/chocolatey/choco/issues/88)
 * Features should contain description - see [#416](https://github.com/chocolatey/choco/issues/416)
 * Chocolatey Installer - removing modules not loaded - see [#442](https://github.com/chocolatey/choco/issues/442)
 * Chocolatey Installer - Don't use Write-Host - see [#444](https://github.com/chocolatey/choco/issues/444)
 * Set environment variables once configuration is complete - see [#420](https://github.com/chocolatey/choco/issues/420)
 * Enhance Package Template for 0.9.9.9 - see [#366](https://github.com/chocolatey/choco/issues/366)


## [0.9.9.8](https://github.com/chocolatey/choco/issues?q=milestone%3A0.9.9.8+is%3Aclosed) (June 26, 2015)
### BUG FIXES
 * Fix - [Security] choco install -y C: deletes all files - see [#341](https://github.com/chocolatey/choco/issues/341)
 * Fix - Read-Host halts scripts rather than prompt for input - see [#219](https://github.com/chocolatey/choco/issues/219)

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
   1. `installmissing` and 'cinstm' have been removed. They were deprecated awhile ago, so this should not be a surprise. For equivalent functionality see the plain install/cinst commands.
   2. `choco version` has been deprecated and will be removed in v1. Use `choco upgrade pkgName --noop` or `choco upgrade pkgName -whatif` instead.
   3. `Write-ChocolateySuccess`, `Write-ChocolateyFailure` have been deprecated.
   4. `update` is now `upgrade`. `update` has been deprecated and will be removed/replaced in v1. Update will be reincarnated later for a different purpose. **Hint**: It rhymes with smackage pindexes.

### FEATURES
 * In app documentation! Use `choco -?`, `choco -h` or `choco commandName -?` to learn about each command, complete with examples!
 * WhatIf/Noop mode for all commands (`--noop` can also be specified as `-whatif`) - see [Chocolatey #263](https://github.com/chocolatey/chocolatey/issues/263) and [Default Options and Switches](https://chocolatey.org/docs/commands-reference#how-to-pass-options-switches)
 * Performs like a package manager, expect to see queries failing because of unmet dependency issues.
 * **New Commands:**
   1. `pin` - Suppress upgrades. This allows you to 'pin' an install to a particular version - see [#1](https://github.com/chocolatey/choco/issues/1), [Chocolatey #5](https://github.com/chocolatey/chocolatey/issues/5) and [Pin Command](https://chocolatey.org/docs/commands-pin)
   2. `apikey` - see [ApiKey Command](https://chocolatey.org/docs/commands-apikey)
   3. `new` - see [New Command](https://chocolatey.org/docs/commands-new) and [Chocolatey #157](https://github.com/chocolatey/chocolatey/issues/157)
 * New ways to pass arguments! See [How to Pass Options/Switches](https://chocolatey.org/docs/commands-reference#how-to-pass-options-switches)
 * Did we mention there is a help menu that is actually helpful now? Shiny!
 * AutoUninstaller!!!! But it is not enabled by default this version. See [#15](https://github.com/chocolatey/choco/issues/15), [#9](https://github.com/chocolatey/choco/issues/9) and [Chocolatey #6](https://github.com/chocolatey/chocolatey/issues/6)
 * **New Helpers:**
   1. `Install-ChocolateyShortcut` - see [Chocolatey #238](https://github.com/chocolatey/chocolatey/pull/238), [Chocolatey #235](https://github.com/chocolatey/chocolatey/issues/235) and [Chocolatey #218](https://github.com/chocolatey/chocolatey/issues/218)

### BUG FIXES
Probably a lot of bug fixes that may not make it here, but here are the ones we know about.

 * Fix - Cannot upgrade from prerelease to same version released - see [Chocolatey #122](https://github.com/chocolatey/chocolatey/issues/122)
 * Fix - install `--force` should not use cache - see [Chocolatey #199](https://github.com/chocolatey/chocolatey/issues/199)
 * Fix - force dependencies as well - see [--force-dependencies](https://chocolatey.org/docs/commands-install) and [Chocolatey #199](https://github.com/chocolatey/chocolatey/issues/199)
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
 * Add parameters in packages.config files - see [Packages.config](https://chocolatey.org/docs/commands-install#packages.config), [Chocolatey #472](https://github.com/chocolatey/chocolatey/issues/472), and [#10](https://github.com/chocolatey/choco/issues/10)
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


## [0.9.8.33](https://github.com/chocolatey/chocolatey/issues?q=label%3Av0.9.8.33+is%3Aclosed) (Feb 11, 2015)
### FEATURES
 * Dynamically export helpers (this fixes helpers that were not visible before) - [#628](https://github.com/chocolatey/chocolatey/pull/628)

### IMPROVEMENTS
 * Accept `-y` as a parameter, Add warning about -y for 0.9.9.
 * Company name misspelled in shims - [#673](https://github.com/chocolatey/chocolatey/issues/673) and [shimgen #9](https://github.com/chocolatey/shimgen/issues/9)


## [0.9.8.32](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.32&page=1&state=closed) (January 22, 2015)
### BUG FIXES
 * Fix - Chocolatey-Install should return non-zero exit code if chocolateyInstall.ps1 fails - [#568](https://github.com/chocolatey/chocolatey/issues/568) & [#658](https://github.com/chocolatey/chocolatey/pull/658)


## [0.9.8.31](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.31&page=1&state=closed) (January 7, 2015)
### BUG FIXES
 * Fix - Shim doesn't always shift off the first argument - [#655](https://github.com/chocolatey/chocolatey/issues/655) & [ShimGen #7](https://github.com/chocolatey/shimgen/issues/7)
 * Fix - If executable isn't available, fallback to default icon - [#579](https://github.com/chocolatey/chocolatey/issues/579)


## [0.9.8.30](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.30&page=1&state=closed) (January 6, 2015)
### FEATURES
 * Use icon of the executable with generated shim - [#579](https://github.com/chocolatey/chocolatey/issues/579) & [ShimGen #2](https://github.com/chocolatey/shimgen/issues/2)

### BUG FIXES
 * Fix - Shims don't correctly handle spaces in path to shim - [#654](https://github.com/chocolatey/chocolatey/issues/654) & [ShimGen #5](https://github.com/chocolatey/shimgen/issues/5)


## [0.9.8.29](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.29&page=1&state=closed) (January 2, 2015)
### FEATURES
 * Use icon of the executable with generated shim - [#579](https://github.com/chocolatey/chocolatey/issues/579) & [ShimGen #2](https://github.com/chocolatey/shimgen/issues/2)
 * Allow setting custom temp download location - [#307](https://github.com/chocolatey/chocolatey/issues/307)

### IMPROVEMENTS
 * Don't assume $env:TEMP or $env:UserProfile are set - [#647](https://github.com/chocolatey/chocolatey/issues/647)
 * Remove Kickstarter message.


## [0.9.8.28](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.28&page=1&state=closed) (November 4, 2014)
### BREAKING CHANGES
 * You may need to update your saved API key for chocolatey, due to [#599](https://github.com/chocolatey/chocolatey/issues/599) we have switched push to ensure https.

### BUG FIXES
 * Fix - Shim argument parsing needs fixed for quoting - [ShimGen #1](https://github.com/chocolatey/shimgen/issues/1)
 * Fix - Forcing x86 does not use 32bit checksum - [#535](https://github.com/chocolatey/chocolatey/issues/535)
 * Fix - Powershell v2 fails to download SSLv3 files - [#531](https://github.com/chocolatey/chocolatey/issues/531)
 * Fix - Get-ChocolateyUnzip fails due to Wait-Process exception - [#571](https://github.com/chocolatey/chocolatey/issues/571)

### IMPROVEMENTS
 * Use default credentials for internet if available - [#577](https://github.com/chocolatey/chocolatey/issues/577)
 * Add moderation message on push - [#600](https://github.com/chocolatey/chocolatey/issues/600)
 * Restrict all calls to chocolatey.org to HTTPS - [#599](https://github.com/chocolatey/chocolatey/issues/599)
 * Batch fallback should quote path for spaces - [#558](https://github.com/chocolatey/chocolatey/issues/558)

## [0.9.8.27](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.27&page=1&state=closed) (July 13, 2014)
### BUG FIXES
 * Fix - Posh v3+ Ignores -Wait when run from cmd.exe - [#516](https://github.com/chocolatey/chocolatey/pull/516)


## [0.9.8.26](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.26&page=1&state=closed) (July 12, 2014)
### BUG FIXES
 * Fix - Allow spaces in arguments to chocolatey again - Regenerate chocolatey included shims to take advantage of shimgen fixes - [#507](https://github.com/chocolatey/chocolatey/issues/507)
 * Fix - Default path has changed, causing running without closing shell to have issues again - [#510](https://github.com/chocolatey/chocolatey/issues/510)
 * Fix - Working directory of shimgen generated files points to path target executable is in (GUI apps only) - [#508](https://github.com/chocolatey/chocolatey/issues/508)
 * Fix - cpack/cpush returns zero exit code even when error occurs - [#256](https://github.com/chocolatey/chocolatey/issues/256) & [#384](https://github.com/chocolatey/chocolatey/issues/384)
 * Fix - Install error throws another error due to true instead of $true - [#514](https://github.com/chocolatey/chocolatey/pull/514)
 * Fix - Posh v3+ Ignores -Wait when run from cmd.exe - [#516](https://github.com/chocolatey/chocolatey/pull/516)

### IMPROVEMENTS
 * Allow to pass shimgen specific parameters - [#509](https://github.com/chocolatey/chocolatey/issues/509)
 * Issue warning if user is not running an elevated shell - [#519](https://github.com/chocolatey/chocolatey/issues/519)


## [0.9.8.25](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.25&page=1&state=closed) (July 7, 2014)
### BUG FIXES
 * Fix - Shims that require admin may fail on UAC enforced machines (System.ComponentModel.Win32Exception: The requested operation requires elevation) - [#505](https://github.com/chocolatey/chocolatey/issues/505)
 * Fix - Do not check content-length if there isn't a content-length returned from Get-WebHeaders - [#504](https://github.com/chocolatey/chocolatey/issues/504)


## [0.9.8.24](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.24&page=1&state=closed) (July 3, 2014)
### BREAKING CHANGES
 * Enhancement - Default install to C:\ProgramData\chocolatey - [#452](https://github.com/chocolatey/chocolatey/issues/452) & [#494](https://github.com/chocolatey/chocolatey/issues/494)
 * Don't allow $binroot to be set to c:\ - [#434](https://github.com/chocolatey/chocolatey/issues/434) - this is meant to be temporary while other pieces are fixed.

### FEATURES
 * Checksum downloaded files - [#427](https://github.com/chocolatey/chocolatey/issues/427)
 * Replace Batch Redirector with Shims - [#372](https://github.com/chocolatey/chocolatey/issues/372)
 * New Helper - Get-UACEnabled - [#451](https://github.com/chocolatey/chocolatey/issues/451)
 * Enhancement - Install to Machine environment variable - [#453](https://github.com/chocolatey/chocolatey/issues/453)
 * Enhancement - Install the .NET framework 4.0 requirement - [#255](https://github.com/chocolatey/chocolatey/issues/255)
 * Update environment using command (RefreshEnv) - [#134](https://github.com/chocolatey/chocolatey/issues/134)
 * `-quiet` parameter that silences almost all output / allow shutting off real write-host - [#416](https://github.com/chocolatey/chocolatey/pull/416) & [#411](https://github.com/chocolatey/chocolatey/issues/411)
 * New Helpers - Test-ProcessAdminRights, Get-EnvironmentVariableNames, Get-EnvironmentVariable, Set-EnvironmentVariable - [#486](https://github.com/chocolatey/chocolatey/pull/486)

### BUG FIXES
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

### IMPROVEMENTS
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


## [0.9.8.23](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.23&page=1&state=closed) (November 11, 2013)
### BUG FIXES
 * Fix - Chocolatey 0.9.8.22 incorrectly reports version as alpha1 [#368](https://github.com/chocolatey/chocolatey/issues/368)
 * Fix - Some chocolatey commands with no arguments error [#369](https://github.com/chocolatey/chocolatey/issues/369)


## [0.9.8.22](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.22&page=1&state=closed) (November 10, 2013)
### BREAKING CHANGES
 * To use spaces and quotes, one should now use single quotation marks. It works best in both powershell and cmd.

### FEATURES
 * Enhancement - Add switch to force x86 when packages have both versions - [#365](https://github.com/chocolatey/chocolatey/issues/365)
 * Enhancement - Allow passing parameters to packages - [#159](https://github.com/chocolatey/chocolatey/issues/159)

### BUG FIXES
 * Fix - Chocolatey 0.9.8.21 errors when using spaces or quotes with chocolatey or with batch redirect files. - [#367](https://github.com/chocolatey/chocolatey/issues/367)


## [0.9.8.21](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.21&page=1&state=closed) (November 7, 2013)
### BREAKING CHANGES
 * Enhancement - For local package searching, use choco list -lo or choco search -lo. The execution speed is greatly increased. cver for local has been deprecated. - [#276](https://github.com/chocolatey/chocolatey/issues/276)
 * Breaking - Chocolatey default source no longer includes Nuget official feed. This will help improve response time and greatly increase relevant results. - [#349](https://github.com/chocolatey/chocolatey/issues/349)

### FEATURES
 * Enhancement - Support for Server Core - [#59](https://github.com/chocolatey/chocolatey/issues/59)
 * Enhancement - Add a switch for ignoring dependencies on install `-ignoredependencies` - [#131](https://github.com/chocolatey/chocolatey/issues/131)
 * Command - `choco` is now a default term
 * Command - search is now a command (aliases list) - `choco search something [-localonly]`
 * Function - `Get-ProcessorBits` - tells you whether a processor is x86 or x64. This functionality was in chocolatey already but has been globalized for easy access. - [#231](https://github.com/chocolatey/chocolatey/issues/231) & [#229](https://github.com/chocolatey/chocolatey/issues/229)
 * Function - `Get-BinRoot` - Gives package maintainers the ability to call one command that gets them the tools/bin root. This gives you the location where folks want certain packages installed. - [#359](https://github.com/chocolatey/chocolatey/pull/359)

### IMPROVEMENTS
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

### BUG FIXES
 * Fix - Treat installation failures appropriately - [#10](https://github.com/chocolatey/chocolatey/issues/10)
 * Fix - Using newer versions of nuget breaks chocolatey - [#303](https://github.com/chocolatey/chocolatey/issues/303)
 * Fix - Chocolatey incorrectly reports 64 bit urls when downloading anything - [#331](https://github.com/chocolatey/chocolatey/issues/331)
 * Fix - Executing `cuninst` without parameters shouldn't do anything - [#267](https://github.com/chocolatey/chocolatey/issues/267) & [#265](https://github.com/chocolatey/chocolatey/issues/265)
 * Fix - VSIX installer helper is finding the wrong Visual Studio version - [#262](https://github.com/chocolatey/chocolatey/issues/262)
 * Fix - Renaming logs appending `.old` results in error - [#225](https://github.com/chocolatey/chocolatey/issues/225)
 * Fix - Minor typo in uninstall script "uninINstalling" - [#247](https://github.com/chocolatey/chocolatey/issues/247)
 * Fix - Bug in Get-ChocolateyUnzip throws issues sometimes [#244](https://github.com/chocolatey/chocolatey/issues/244) & [#242](https://github.com/chocolatey/chocolatey/issues/242)
 * Fix - Minor typo "succesfully" - [#241](https://github.com/chocolatey/chocolatey/issues/241)


## [0.9.8.20](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.20&page=1&state=closed) (December 11, 2012)
### FEATURES
 * Command - Windows Feature feed - [#150](https://github.com/chocolatey/chocolatey/pull/150)
 * Function - Add function to install environment variables - [#149](https://github.com/chocolatey/chocolatey/pull/149)
 * Function - Function to associate file extensions with installed executables - [#146](https://github.com/chocolatey/chocolatey/pull/146)
 * Function - Helper function to create explorer context menu items - [#144](https://github.com/chocolatey/chocolatey/pull/144)
 * Function - Helper function for pinning items to task bar - [#143](https://github.com/chocolatey/chocolatey/pull/143) & [#141](https://github.com/chocolatey/chocolatey/pull/141)
 * Command - Sources command - [#138](https://github.com/chocolatey/chocolatey/pull/138)
 * Command - Provide a way to list all the installed packages - [#125](https://github.com/chocolatey/chocolatey/issues/125)

### IMPROVEMENTS
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

### BUG FIXES
 * Fix - "Execution of NuGet not detected" error. - [#151](https://github.com/chocolatey/chocolatey/pull/151)
 * Fix - chocolatey.bat can't find chocolatey.cmd - [#152](https://github.com/chocolatey/chocolatey/issues/152)
 * Fix - `chocolatey version all` prints only the last package's information - [#183](https://github.com/chocolatey/chocolatey/pull/183)
 * Fix - Issue with $processor.addresswidth var - [#121](https://github.com/chocolatey/chocolatey/pull/121)


## [0.9.8.19](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.19&page=1&state=closed) (July 2, 2012)
### FEATURES
 * Enhancement - Allow community extensions - [#115](https://github.com/chocolatey/chocolatey/issues/115)

### BUG FIXES
 * Fix - PowerShell v3 doesn't like foreach loop (prefers ForEach-Object) - [#116](https://github.com/chocolatey/chocolatey/pull/116)
 * Fix - Cannot install Python packages on Windows 8 - [#117](https://github.com/chocolatey/chocolatey/issues/117)


## [0.9.8.18](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.18&sort=created&direction=desc&state=closed&page=1) (June 16, 2012)
### BUG FIXES
 * Fix - 0.9.8.17 installer doesn't create chocolatey folder if it doesn't exist - [#112](https://github.com/chocolatey/chocolatey/issues/112)


## [0.9.8.17](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.17&sort=created&direction=desc&state=closed&page=1) (June 15, 2012)
### FEATURES
 * Enhancement - Support for naive uninstall - [#96](https://github.com/chocolatey/chocolatey/issues/96)

### IMPROVEMENTS
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

### BUG FIXES
 * Fix - Packages.config source now uses chocolatey/nuget sources by default instead of empty - [#79](https://github.com/chocolatey/chocolatey/issues/79)
 * Fix - Executable batch links not created for "prerelease" versions - [#88](https://github.com/chocolatey/chocolatey/issues/88)
 * Fix - Issue where latest version is not returned - [#92](https://github.com/chocolatey/chocolatey/pull/92)
 * Fix - Prerelease versions now broken out as separate versions - [#90](https://github.com/chocolatey/chocolatey/issues/90)
 * Fix - During install PowerShell session gets bad $env:ChocolateyInstall variable - [#80](https://github.com/chocolatey/chocolatey/issues/80)
 * Fix - Build path with spaces now works - [#102](https://github.com/chocolatey/chocolatey/pull/102)


## [0.9.8.16](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.16&sort=created&direction=desc&state=closed&page=1) (February 27, 2012)
### BUG FIXES
 * Small fix to installer for upgrade issues from 0.9.8.15


## [0.9.8.15](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.15&sort=created&direction=desc&state=closed&page=1) (February 27, 2012)
### BREAKING CHANGES
 * Enhancement - Chocolatey's default folder is now C:\Chocolatey (and no longer C:\NuGet) - [#58](https://github.com/chocolatey/chocolatey/issues/58)
 * Enhancement - Use -force to reinstall existing packages - [#45](https://github.com/chocolatey/chocolatey/issues/45)

### FEATURES
 * Enhancement - Install now supports **all** with a custom package source to install every package from a source! - [#46](https://github.com/chocolatey/chocolatey/issues/46)

### IMPROVEMENTS
 * Enhancement - Support Prerelease flag for Install - [#71](https://github.com/chocolatey/chocolatey/issues/71)
 * Enhancement - Support Prerelease flag for Update/Version - [#72](https://github.com/chocolatey/chocolatey/issues/72)
 * Enhancement - Support Prerelease flag in List - [#74](https://github.com/chocolatey/chocolatey/issues/74)

### BUG FIXES
 * Fix - Parsing the wrong version when trying to update - [#73](https://github.com/chocolatey/chocolatey/issues/73)


## [0.9.8.14](https://github.com/chocolatey/chocolatey/issues?labels=v0.9.8.14&sort=created&direction=desc&state=closed&page=1) (February 6, 2012)
### IMPROVEMENTS
 * Enhancement - Pass ValidExitCodes to Install Helpers - [#54](https://github.com/chocolatey/chocolatey/issues/54)
 * Enhancement - Add 64-bit url to Install-ChocolateyZipPackage - [#48](https://github.com/chocolatey/chocolatey/issues/48)
 * Enhancement - Add 64-bit url to Install-ChocolateyPowershellCommand - [#57](https://github.com/chocolatey/chocolatey/issues/57)
 * Enhancement - Make the main helpers work with files not coming over HTTP - [#51](https://github.com/chocolatey/chocolatey/issues/51)
 * Enhancement - Upgrade NuGet.exe to 1.6.0 to take advantage of prerelease packaging - [#64](https://github.com/chocolatey/chocolatey/issues/64)

### BUG FIXES
 * Fix - The packages.config feature has broken naming packages with '.config' - [#56](https://github.com/chocolatey/chocolatey/issues/56)
 * Fix - CList includes all versions without adding the switch - [#60](https://github.com/chocolatey/chocolatey/issues/60)
 * Fix - When NuGet.exe failes to run due to .NET Framework 4.0 not installed, chocolatey should report that. - [#65](https://github.com/chocolatey/chocolatey/issues/65)


## [0.9.8.13](https://github.com/chocolatey/chocolatey/issues?labels=0.9.8.13&sort=created&direction=desc&state=closed&page=1) (January 8, 2012)
### FEATURES
 * New Command! Enhancement - Integration with Ruby Gems (`cgem packageName` or `cinst packageName -source ruby`) - [#29](https://github.com/chocolatey/chocolatey/issues/29)
 * New Command! Enhancement - Integration with Web PI (`cwebpi packageName` or `cinst packageName -source webpi`) - [#28](https://github.com/chocolatey/chocolatey/issues/28)
 * Enhancement - Call chocolatey install with packages.config file (thanks AnthonyMastrean!) - [#31](https://github.com/chocolatey/chocolatey/issues/31) and [#43](https://github.com/chocolatey/chocolatey/pull/43) and [#50](https://github.com/chocolatey/chocolatey/issues/50)
 * New Command! Enhancement - Chocolatey Push (`chocolatey push packageName.nupkg` or `cpush packageName.nupkg`) - [#36](https://github.com/chocolatey/chocolatey/issues/36)
 * New Command! Enhancement - Chocolatey Pack (`chocolatey pack [packageName.nuspec]` or `cpack [packageName.nuspec]`) - [#35](https://github.com/chocolatey/chocolatey/issues/35)

### IMPROVEMENTS
 * Enhancement - @datachomp feature - Override Installer Arguments `chocolatey install packageName -installArgs "args to override" -override` or `cinst packageName -ia "args to override" -o`) - [#40](https://github.com/chocolatey/chocolatey/issues/40)
 * Enhancement - @datachomp feature - Append Installer Arguments (`chocolatey install packageName -installArgs "args to append"` or `cinst packageName -ia "args to append"`) - [#39](https://github.com/chocolatey/chocolatey/issues/39)
 * Enhancement - Run installer in not silent mode (`chocolatey install packageName -notSilent` or `cinst packageName -notSilent`) - [#42](https://github.com/chocolatey/chocolatey/issues/42)
 * Enhancement - List available Web PI packages (`clist -source webpi`) - [#37](https://github.com/chocolatey/chocolatey/issues/37)
 * Enhancement - List command should allow the All or AllVersions switch - [#38](https://github.com/chocolatey/chocolatey/issues/38)
 * Enhancement - Any install will create the ChocolateyInstall environment variable so that installers can take advantage of it - [#30](https://github.com/chocolatey/chocolatey/issues/30)

### BUG FIXES
 * Fixing an issue on proxy display message (Thanks jasonmueller!) - [#44](https://github.com/chocolatey/chocolatey/pull/44)
 * Fixing the source path to allow for spaces (where chocolatey is installed) - [#33](https://github.com/chocolatey/chocolatey/issues/33)
 * Fixing the culture to InvariantCulture to eliminate the turkish "I" issue - [#22](https://github.com/chocolatey/chocolatey/issues/22)


## 0.9.8.12 (November 20, 2011)
### IMPROVEMENTS
 * Enhancement - Reducing the number of window pop ups - [#25](https://github.com/chocolatey/chocolatey/issues/25)

### BUG FIXES
 * Fixed an issue with write-host and write-error overrides that happens in the next version of powershell - [#24](https://github.com/chocolatey/chocolatey/pull/24)
 * Fixing an issue that happens when powershell is not on the path - [#23](https://github.com/chocolatey/chocolatey/issues/23)
 * Fixing the replacement of capital ".EXE" in addition to lowercase ".exe" when creating batch redirects - [#26](https://github.com/chocolatey/chocolatey/issues/26)


## 0.9.8.11 (October 4, 2011)
### BUG FIXES
 * Fixing an update issue if the package only exists on chocolatey.org - [#16](https://github.com/chocolatey/chocolatey/issues/16)
 * Fixing an issue with install missing if the package never existed - [#13](https://github.com/chocolatey/chocolatey/issues/13)


## 0.9.8.10 (September 17, 2011)
### FEATURES
 * New Helper! Install-ChocolateyPowershellCommand - install a powershell script as a command - [#11](https://github.com/chocolatey/chocolatey/issues/11)


## 0.9.8.9 (September 10, 2011)
### BUG FIXES
 * Reinstalls an existing package if -version is passed (first surfaced in 0.9.8.7 w/NuGet 1.5) - [#9](https://github.com/chocolatey/chocolatey/issues/9)


## 0.9.8.8 (September 10, 2011)
### BUG FIXES
 * Fixing version comparison - [#4](https://github.com/chocolatey/chocolatey/issues/4)
 * Fixed package selector to not select like named packages (i.e. ruby.devkit when getting information about ruby) - [#3](https://github.com/chocolatey/chocolatey/issues/3)


## 0.9.8.7 (September 2, 2011)
### IMPROVEMENTS
 * Added proxy support based on [#1](https://github.com/chocolatey/chocolatey/issues/1)
 * Updated to work with NuGet 1.5 - [#2](https://github.com/chocolatey/chocolatey/issues/2)


## 0.9.8.6 (July 27, 2011)
### BUG FIXES
 * Fixed a bug introduced in 0.9.8.5 - Start-ChocolateyProcessAsAdmin erroring out when setting machine path as a result of trying to log the message.


## 0.9.8.5 (July 27, 2011)
### IMPROVEMENTS
 * Improving Run-ChocolateyProcessAsAdmin to allow for running entire functions as administrator by importing helpers to that command if using PowerShell.
 * Updating some of the notes.

### BUG FIXES
 * Fixed bug in installer when User Environment Path is null.


## 0.9.8.4 (July 27, 2011)
### BUG FIXES
 * Fixed a small issue with the Install-ChocolateyDesktopLink


## 0.9.8.3 (July 7, 2011)
### BREAKING CHANGES
 * Chocolatey no longer runs the entire powershell script as an administrator. With the addition of the Start-ChocolateyProcessAsAdmin, this is how you will get to administrative tasks outside of the helpers.

### FEATURES
 * New chocolatey command! InstallMissing allows you to install a package only if it is not already installed. Shortcut is 'cinstm'.
 * New Helper! Install-ChocolateyPath - give it a path for out of band items that are not imported to path with chocolatey
 * New Helper! Start-ChocolateyProcessAsAdmin - this allows you to run processes as administrator
 * New Helper! Install-ChocolateyDesktopLink - put shortcuts on the desktop

### IMPROVEMENTS
 * NuGet updated to v1.4
 * Much of the error handling is improved. There are two new Helpers to call (ChocolateySuccess and Write-ChocolateyFailure).
 * Chocolatey no longer needs administrative rights to install itself.


## 0.9.8.2 (May 21, 2011)
### FEATURES
 * You now have the option of a custom installation folder. Thanks Jason Jarrett!


## 0.9.8.1 (May 18, 2011)
### BUG FIXES
 * General fix to bad character in file. Fixed selection for update as well.


## 0.9.8 (May 4, 2011)
### BREAKING CHANGES
 * A dependency will not reinstall once it has been installed. To have it reinstall, you can install it directly (or delete it from the repository and run the core package).

### IMPROVEMENTS
 * Shortcuts have been added: 'cup' for 'chocolatey update', 'cver' for 'chocolatey version', and 'clist' for 'chocolatey list'.
 * Update only runs if newer version detected.
 * Calling update with no arguments will update chocolatey.
 * Calling update with all will update your entire chocolatey repository.


## 0.9.7.3 (April 30, 2011)
### BUG FIXES
 * Fixing Install-ChocolateyZipPackage so that it works again.


## 0.9.7.2 (April 29, 2011)
### BUG FIXES
 * Fixing an underlying issue with not having silent arguments for exe files.


## 0.9.7.1 (April 29, 2011)
### BUG FIXES
 * Fixing an introduced bug where the downloader didn't get the file name passed to it.


## 0.9.7 (April 29, 2011)
### FEATURES
 * New helper added Install-ChocolateyInstallPackage - this was previously part of the download & install and has been broken out.
 * New chocolatey command! Version allows you to see if a package you have installed is the most up to date. Leave out package and it will check for chocolatey itself.

### IMPROVEMENTS
 * The powershell module is automatically loaded, so packages no longer need to import the module. This means one line chocolateyInstall.ps1 files!
 * Error handling is improved.
 * Silent installer override for msi has been removed to allow for additional arguments that need to be passed.


## 0.9.6.4 (April 26, 2011)
### IMPROVEMENTS
 * Remove powershell execution timeout.


## 0.9.6.3 (April 25, 2011)
### FEATURES
 * New Helper added Install-ChocolateyZipPackage - this wraps the two upper commands into one smaller command and addresses the file name bug.


## 0.9.6.2 (April 25, 2011)
### BUG FIXES
 * Addressed a small bug in getting back the file name from the helper.


## 0.9.6.1 (April 23, 2011)
### IMPROVEMENTS
 * Adding in ability to find a dependency when the version doesn't exist.


## 0.9.6 (April 23, 2011)
### IMPROVEMENTS
 * Can execute powershell and chocolatey without having to change execution rights to powershell system wide.

### FEATURES
 * New Helper added - Get-ChocolateyWebFile - downloads a file from a url and gives you back the location of the file once complete.
 * New Helper added - Get-ChocolateyZipContents - unzips a file to a directory of your choosing.


## 0.9.5 (April 21, 2011)
### FEATURES
 * Helper for native installer added (Install-ChocolateyPackage). Reduces the amount of powershell necessary to download and install a native package to two lines from over 25.

### IMPROVEMENTS
 * Helper outputs progress during download.
 * Dependency runner is complete.


## 0.9.4 (April 10, 2011)
### IMPROVEMENTS
 * List command has a filter.
 * Package license acceptance terms notated.


## 0.9.3 (April 4, 2011)
### IMPROVEMENTS
 * You can now pass -source and -version to install command.


## 0.9.2 (April 4, 2011)
### FEATURES
 * List command added.


## 0.9.1 (March 30, 2011)
### IMPROVEMENTS
 * Shortcut for 'chocolatey install' - 'cinst' now available.
