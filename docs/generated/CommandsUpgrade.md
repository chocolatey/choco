# Chocolatey Upgrade (choco upgrade)
***NOTE***: 100% compatible with older chocolatey client (0.9.8.32 and below) with options and switches. Add `-y` for previous behavior with no prompt. In most cases you can still pass options and switches with one dash (`-`). See [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] for more details.

Upgrades an existing package to the latest version, if there is a newer
version available. Some
 may prefer to use `cup` as a shortcut for `choco upgrade`.

Upgrades a package or a list of packages.

## Usage

    choco upgrade <pkg|all> [<pkg2> <pkgN>] [<options/switches>]
    cup <pkg|all> [<pkg2> <pkgN>] [<options/switches>]

**NOTE**: `all` is a special package keyword that will allow you to upgrade
 all currently installed packages.

**NOTE**: If you do not have a package installed, upgrade will error.

## Examples

    choco upgrade chocolatey
    choco upgrade notepadplusplus googlechrome atom 7zip
    choco upgrade notepadplusplus googlechrome atom 7zip -dvfy
    choco upgrade git --params="/GitAndUnixToolsOnPath /NoAutoCrlf" -y
    choco upgrade nodejs.install --version 0.10.35
    choco upgrade git -s "https://somewhere/out/there"
    choco upgrade git -s "https://somewhere/protected" -u user -p pass


## Options and Switches

**NOTE**: Options and switches apply to all items passed, so if you are installing multiple packages, and you use `--version=1.0.0`, it is going to look for and try to install version 1.0.0 of every package passed. So please split out multiple package calls when wanting to pass specific options.

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
-s, --source=VALUE
  Source - The source to find the package(s) to install. Special sources
  include: ruby, webpi, cygwin, windowsfeatures, and python. Defaults to
  default feeds.

--version=VALUE
  Version - A specific version to install. Defaults to unspecified.

--pre, --prerelease
  Prerelease - Include Prereleases? Defaults to false.

--x86, --forcex86
  ForceX86 - Force x86 (32bit) installation on 64 bit systems. Defaults
  to false.

--ia, --installargs, --installarguments, --install-arguments=VALUE
  InstallArguments - Install Arguments to pass to the native installer
  in the package. Defaults to unspecified.

-o, --override, --overrideargs, --overridearguments,
--override-arguments
  OverrideArguments - Should install arguments be used exclusively
  without appending to current package passed arguments? Defaults to
  false.

--notsilent, --not-silent
  NotSilent - Do not install this silently. Defaults to false.

--params, --parameters, --pkgparameters, --packageparameters,
--package-parameters=VALUE
  PackageParameters - Parameters to pass to the package. Defaults to
  unspecified.

-m, --sxs, --sidebyside, --side-by-side, --allowmultiple,
--allow-multiple, --allowmultipleversions, --allow-multiple-versions
  AllowMultipleVersions - Should multiple versions of a package be
  installed? Defaults to false.

-i, --ignoredependencies, --ignore-dependencies
  IgnoreDependencies - Ignore dependencies when upgrading package(s).
  Defaults to false.

-n, --skippowershell, --skip-powershell
  Skip Powershell - Do not run chocolateyInstall.ps1. Defaults to false.

 -u, --user=VALUE
     User - used with authenticated feeds. Defaults to empty.

 -p, --password=VALUE
     Password - the user's password to the source. Defaults to empty.
```

## See It In Action

![choco upgrade](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_upgrade.gif)
