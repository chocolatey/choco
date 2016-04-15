## Chocolatey Uninstall (choco uninstall)
***NOTE***: 100% compatible with older chocolatey client (0.9.8.32 and below) with options and switches. Add `-y` for previous behavior with no prompt. In most cases you can still pass options and switches with one dash (`-`). See [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] for more details.

Uninstalls a package or a list of packages.  Some
 may prefer to use `cuninst` as a shortcut for `choco uninstall`.

To uninstall the package from your system (equivalent to uninstalling a program in "Programs and Features") via `choco uninstall`, you will most likely have to use the "Automatic Uninstaller" feature which is turned off by default (see [Note](#note) below for more information). To turn the feature on, run the following command:

    choco feature enable -n autoUninstaller

## Usage

    choco uninstall <pkg|all> [pkg2 pkgN] [options/switches]
    cuninst <pkg|all> [pkg2 pkgN] [options/switches]

**NOTE**: `all` is a special package keyword that will allow you to
 uninstall all packages.

## Examples

    choco uninstall git
    choco uninstall notepadplusplus googlechrome atom 7zip
    choco uninstall notepadplusplus googlechrome atom 7zip -dv
    choco uninstall ruby --version 1.8.7.37402
    choco uninstall nodejs.install --all-versions

## Options and Switches

**NOTE**: Options and switches apply to all items passed, so if you are uninstalling multiple packages, and you use `--version=1.0.0`, it is going to look for and try to uninstall version 1.0.0 of every package passed. So please split out multiple package calls when wanting to pass specific options.

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
--version=VALUE
  Version - A specific version to uninstall. Defaults to unspecified.

-a, --allversions, --all-versions
  AllVersions - Uninstall all versions? Defaults to false.

--ua, --uninstallargs, --uninstallarguments, --uninstall-arguments=VALUE
  UninstallArguments - Uninstall Arguments to pass to the native
  installer in the package. Defaults to unspecified.

-o, --override, --overrideargs, --overridearguments, --override-arguments
  OverrideArguments - Should uninstall arguments be used exclusively
  without appending to current package passed arguments? Defaults to
  false.

--notsilent, --not-silent
  NotSilent - Do not uninstall this silently. Defaults to false.

--params, --parameters, --pkgparameters, --packageparameters, --package-parameters=VALUE
  PackageParameters - Parameters to pass to the package. Defaults to
  unspecified.

-x, --forcedependencies, --force-dependencies
  ForceDependencies - Force dependencies to be uninstalled when
  uninstalling package(s). Defaults to false.

-n, --skippowershell, --skip-powershell
  Skip PowerShell - Do not run chocolateyUninstall.ps1. Defaults to false.
```

## See It In Action

![choco uninstall](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_uninstall.gif)

## Known Limitations
* There are no functions defined in the Chocolatey PowerShell module that would help with uninstall - yet (this means that compared to the awesome library of helper functions to get things installed, you are left more on your own to work on uninstalling those things currently).

## Note
The default behavior with the "Automatic Uninstaller" feature turned off (on by default as of 0.9.10.0) is that `choco uninstall` removes the package from your system only if the script `chocolateyUninstall.ps1` is provided by the package maintainer. In the absence of `chocolateyUninstall.ps1`, `choco uninstall` only removes the package from Chocolatey but does not remove the package from your system.

Turning on "Automatic Uninstaller" guarantees that the package is removed from your system when you run `choco uninstall`.
