# Upgrade Command (choco upgrade)

Upgrades a package or a list of packages. Some may prefer to use `cup`
 as a shortcut for [[`choco upgrade`|Commandsupgrade]]. If you do not have a package
 installed, upgrade will install it.

**NOTE:** 100% compatible with older Chocolatey client (0.9.8.x and below)
 with options and switches. Add `-y` for previous behavior with no
 prompt. In most cases you can still pass options and switches with one
 dash (`-`). For more details, see [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] (`choco -?`).

## Usage

    choco upgrade <pkg|all> [<pkg2> <pkgN>] [<options/switches>]
    cup <pkg|all> [<pkg2> <pkgN>] [<options/switches>]

**NOTE:** `all` is a special package keyword that will allow you to upgrade
 all currently installed packages.

Skip upgrading certain packages with [[`choco pin`|Commandspin]] or with the option
 `--except`.

**NOTE:** [Chocolatey Pro](https://chocolatey.org/compare) / Business automatically synchronizes with 
 Programs and Features, ensuring automatically updating apps' versions
 (like Chrome) are up to date in Chocolatey's repository. 

## Examples

    choco upgrade chocolatey
    choco upgrade notepadplusplus googlechrome atom 7zip
    choco upgrade notepadplusplus googlechrome atom 7zip -dvfy
    choco upgrade git --params="'/GitAndUnixToolsOnPath /NoAutoCrlf'" -y
    choco upgrade nodejs.install --version 0.10.35
    choco upgrade git -s "'https://somewhere/out/there'"
    choco upgrade git -s "'https://somewhere/protected'" -u user -p pass
    choco upgrade all
    choco upgrade all --except="'skype,conemu'"

## See It In Action

![choco upgrade](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_upgrade.gif)


## Options and Switches

**NOTE:** Options and switches apply to all items passed, so if you are
 running a command like install that allows installing multiple
 packages, and you use `--version=1.0.0`, it is going to look for and
 try to install version 1.0.0 of every package passed. So please split
 out multiple package calls when wanting to pass specific options.

Includes [[default options/switches|CommandsReference#default-options-and-switches]] (included below for completeness).

~~~



 -?, --help, -h
     Prints out the help menu.

 -d, --debug
     Debug - Show debug messaging.

 -v, --verbose
     Verbose - Show verbose messaging.

     --acceptlicense, --accept-license
     AcceptLicense - Accept license dialogs automatically. Reserved for 
       future use.

 -y, --yes, --confirm
     Confirm all prompts - Chooses affirmative answer instead of prompting. 
       Implies --accept-license

 -f, --force
     Force - force the behavior. Do not use force during normal operation - 
       it subverts some of the smart behavior for commands.

     --noop, --whatif, --what-if
     NoOp / WhatIf - Don't actually do anything.

 -r, --limitoutput, --limit-output
     LimitOutput - Limit the output to essential information

     --timeout, --execution-timeout=VALUE
     CommandExecutionTimeout (in seconds) - The time to allow a command to 
       finish before timing out. Overrides the default execution timeout in the 
       configuration of 2700 seconds.

 -c, --cache, --cachelocation, --cache-location=VALUE
     CacheLocation - Location for download cache, defaults to %TEMP% or value 
       in chocolatey.config file.

     --allowunofficial, --allow-unofficial, --allowunofficialbuild, --allow-unofficial-build
     AllowUnofficialBuild - When not using the official build you must set 
       this flag for choco to continue.

     --failstderr, --failonstderr, --fail-on-stderr, --fail-on-standard-error, --fail-on-error-output
     FailOnStandardError - Fail on standard error output (stderr), typically 
       received when running external commands during install providers. This 
       overrides the feature failOnStandardError.

     --use-system-powershell
     UseSystemPowerShell - Execute PowerShell using an external process 
       instead of the built-in PowerShell host. Should only be used when 
       internal host is failing. Available in 0.9.10+.

 -s, --source=VALUE
     Source - The source to find the package(s) to install. Special sources 
       include: ruby, webpi, cygwin, windowsfeatures, and python. Defaults to 
       default feeds.

     --version=VALUE
     Version - A specific version to install. Defaults to unspecified.

     --pre, --prerelease
     Prerelease - Include Prereleases? Defaults to false.

     --x86, --forcex86
     ForceX86 - Force x86 (32bit) installation on 64 bit systems. Defaults to 
       false.

     --ia, --installargs, --installarguments, --install-arguments=VALUE
     InstallArguments - Install Arguments to pass to the native installer in 
       the package. Defaults to unspecified.

 -o, --override, --overrideargs, --overridearguments, --override-arguments
     OverrideArguments - Should install arguments be used exclusively without 
       appending to current package passed arguments? Defaults to false.

     --notsilent, --not-silent
     NotSilent - Do not install this silently. Defaults to false.

     --params, --parameters, --pkgparameters, --packageparameters, --package-parameters=VALUE
     PackageParameters - Parameters to pass to the package. Defaults to 
       unspecified.

     --allowdowngrade, --allow-downgrade
     AllowDowngrade - Should an attempt at downgrading be allowed? Defaults 
       to false.

 -m, --sxs, --sidebyside, --side-by-side, --allowmultiple, --allow-multiple, --allowmultipleversions, --allow-multiple-versions
     AllowMultipleVersions - Should multiple versions of a package be 
       installed? Defaults to false.

 -i, --ignoredependencies, --ignore-dependencies
     IgnoreDependencies - Ignore dependencies when upgrading package(s). 
       Defaults to false.

 -n, --skippowershell, --skip-powershell, --skipscripts, --skip-scripts, --skip-automation-scripts
     Skip Powershell - Do not run chocolateyInstall.ps1. Defaults to false.

     --failonunfound, --fail-on-unfound
     Fail On Unfound Packages - If a package is not found in feeds specified, 
       fail instead of warn.

     --failonnotinstalled, --fail-on-not-installed
     Fail On Non-installed Packages - If a package is not already intalled, 
       fail instead of installing.

 -u, --user=VALUE
     User - used with authenticated feeds. Defaults to empty.

 -p, --password=VALUE
     Password - the user's password to the source. Defaults to empty.

     --cert=VALUE
     Client certificate - PFX pathname for an x509 authenticated feeds. 
       Defaults to empty. Available in 0.9.10+.

     --cp, --certpassword=VALUE
     Certificate Password - the client certificate's password to the source. 
       Defaults to empty. Available in 0.9.10+.

     --ignorechecksum, --ignore-checksum, --ignorechecksums, --ignore-checksums
     IgnoreChecksums - Ignore checksums provided by the package. Available in 
       0.9.9.9+.

     --ignorepackagecodes, --ignorepackageexitcodes, --ignore-package-codes, --ignore-package-exit-codes
     IgnorePackageExitCodes - Exit with a 0 for success and 1 for non-succes-
       s, no matter what package scripts provide for exit codes. Overrides the 
       default feature 'usePackageExitCodes' set to 'True'. Available in 0.-
       9.10+.

     --usepackagecodes, --usepackageexitcodes, --use-package-codes, --use-package-exit-codes
     UsePackageExitCodes - Package scripts can provide exit codes. Use those 
       for choco's exit code when non-zero (this value can come from a 
       dependency package). Chocolatey defines valid exit codes as 0, 1605, 
       1614, 1641, 3010. Overrides the default feature 'usePackageExitCodes' 
       set to 'True'. Available in 0.9.10+.

     --except=VALUE
     Except - a comma-separated list of package names that should not be 
       upgraded when upgrading 'all'. Defaults to empty. Available in 0.9.10+.

     --sdc, --skipdownloadcache, --skip-download-cache
     Skip Download Cache - Use the original download even if a private CDN 
       cache is available for a package. Overrides the default feature 
       'downloadCache' set to 'True'. Available in 0.9.10+. [Licensed versions](https://chocolatey.org/compare) 
       only.

     --dc, --downloadcache, --download-cache, --use-download-cache
     Use Download Cache - Use private CDN cache if available for a package. 
       Overrides the default feature 'downloadCache' set to 'True'. Available 
       in 0.9.10+. [Licensed versions](https://chocolatey.org/compare) only.

     --svc, --skipvirus, --skip-virus, --skipviruscheck, --skip-virus-check
     Skip Virus Check - Skip the virus check for downloaded files on this ru-
       n. Overrides the default feature 'virusCheck' set to 'True'. Available 
       in 0.9.10+. [Licensed versions](https://chocolatey.org/compare) only.

     --virus, --viruscheck, --virus-check
     Virus Check - check downloaded files for viruses. Overrides the default 
       feature 'virusCheck' set to 'True'. Available in 0.9.10+. Licensed 
       versions only.

     --viruspositivesmin, --virus-positives-minimum=VALUE
     Virus Check Minimum Scan Result Positives - the minimum number of scan 
       result positives required to flag a package. Used when virusScannerType 
       is VirusTotal. Overrides the default configuration value 
       'virusCheckMinimumPositives' set to '5'. Available in 0.9.10+. Licensed 
       versions only.

     --dir, --directory, --installdir, --installdirectory, --install-dir, --install-directory=VALUE
     Install Directory Override - Override the default installation director-
       y. Chocolatey will automatically determine the type of installer and 
       pass the appropriate arguments to override the install directory. The 
       package must use Chocolatey install helpers and be installing an 
       installer for software. Available in 0.9.10+. [Licensed versions](https://chocolatey.org/compare) only.

~~~

[[Command Reference|CommandsReference]]


***NOTE:*** This documentation has been automatically generated from `choco upgrade -h`. 

