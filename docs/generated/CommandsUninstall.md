# Uninstall Command (choco uninstall)

Uninstalls a package or a list of packages. Some may prefer to use
 `cuninst` as a shortcut for [[`choco uninstall`|Commandsuninstall]].

**NOTE:** 100% compatible with older chocolatey client (0.9.8.32 and below)
 with options and switches. Add `-y` for previous behavior with no
 prompt. In most cases you can still pass options and switches with one
 dash (`-`). For more details, see [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] (`choco -?`).

Choco 0.9.9+ automatically tracks registry changes for "Programs and
 Features" of the underlying software's native installers when
 installing packages. The "Automatic Uninstaller" (auto uninstaller)
 service is a feature that can use that information to automatically
 determine how to uninstall these natively installed applications. This
 means that a package may not need an explicit chocolateyUninstall.ps1
 to reverse the installation done in the install script.

Chocolatey tracks packages, which are the files in
 `$env:ChocolateyInstall\lib\packagename`. These packages may or may not
 contain the software (applications/tools) that each package represents.
 The software may actually be installed in Program Files (most native
 installers will install the software there) or elsewhere on the
 machine.

With auto uninstaller turned off, a chocolateyUninstall.ps1 is required
 to perform uninstall from the system. In the absence of
 chocolateyUninstall.ps1, choco uninstall only removes the package from
 Chocolatey but does not remove the software from your system (unless
 in the package directory).

**NOTE:** Starting in 0.9.10+, the Automatic Uninstaller (AutoUninstaller)
 is turned on by default. To turn it off, run the following command:

    choco feature disable -n autoUninstaller

**NOTE:** [Chocolatey Pro](https://chocolatey.org/compare) / Business automatically synchronizes with 
 Programs and Features, ensuring manually removed apps are 
 automatically removed from Chocolatey's repository.

## Usage

    choco uninstall <pkg|all> [pkg2 pkgN] [options/switches]
    cuninst <pkg|all> [pkg2 pkgN] [options/switches]

**NOTE:** `all` is a special package keyword that will allow you to
 uninstall all packages.


## See It In Action

![choco uninstall](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_uninstall.gif)


## Examples

    choco uninstall git
    choco uninstall notepadplusplus googlechrome atom 7zip
    choco uninstall notepadplusplus googlechrome atom 7zip -dv
    choco uninstall ruby --version 1.8.7.37402
    choco uninstall nodejs.install --all-versions

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
     Version - A specific version to uninstall. Defaults to unspecified.

 -a, --allversions, --all-versions
     AllVersions - Uninstall all versions? Defaults to false.

     --ua, --uninstallargs, --uninstallarguments, --uninstall-arguments=VALUE
     UninstallArguments - Uninstall Arguments to pass to the native installer 
       in the package. Defaults to unspecified.

 -o, --override, --overrideargs, --overridearguments, --override-arguments
     OverrideArguments - Should uninstall arguments be used exclusively 
       without appending to current package passed arguments? Defaults to false.

     --notsilent, --not-silent
     NotSilent - Do not uninstall this silently. Defaults to false.

     --params, --parameters, --pkgparameters, --packageparameters, --package-parameters=VALUE
     PackageParameters - Parameters to pass to the package. Defaults to 
       unspecified.

 -x, --forcedependencies, --force-dependencies, --removedependencies, --remove-dependencies
     RemoveDependencies - Uninstall dependencies when uninstalling package(s-
       ). Defaults to false.

 -n, --skippowershell, --skip-powershell, --skipscripts, --skip-scripts, --skip-automation-scripts
     Skip Powershell - Do not run chocolateyUninstall.ps1. Defaults to false.

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

     --autouninstaller, --use-autouninstaller
     UseAutoUninstaller - Use auto uninstaller service when uninstalling. 
       Overrides the default feature 'autoUninstaller' set to 'True'. Available 
       in 0.9.10+.

     --skipautouninstaller, --skip-autouninstaller
     SkipAutoUninstaller - Skip auto uninstaller service when uninstalling. 
       Overrides the default feature 'autoUninstaller' set to 'True'. Available 
       in 0.9.10+.

     --failonautouninstaller, --fail-on-autouninstaller
     FailOnAutoUninstaller - Fail the package uninstall if the auto 
       uninstaller reports and error. Overrides the default feature 
       'failOnAutoUninstaller' set to 'False'. Available in 0.9.10+.

     --ignoreautouninstallerfailure, --ignore-autouninstaller-failure
     Ignore Auto Uninstaller Failure - Do not fail the package if auto 
       uninstaller reports an error. Overrides the default feature 
       'failOnAutoUninstaller' set to 'False'. Available in 0.9.10+.

~~~

[[Command Reference|CommandsReference]]


***NOTE:*** This documentation has been automatically generated from `choco uninstall -h`. 

