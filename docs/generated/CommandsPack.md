# Chocolatey Pack (choco pack)
***NOTE***: 100% compatible with older chocolatey client (0.9.8.32 and below) with options and switches. In most cases you can still pass options and switches with one dash (`-`). See [[how to pass arguments|CommandsReference#how-to-pass-options--switches]] for more details.

Chocolatey will attempt to package a nuspec into a compiled nupkg. Some
 may prefer to use `cpack` as a shortcut for `choco pack`.

## Usage

    choco pack [<path to nuspec>] [<options/switches>]
    cpack [<path to nuspec>] [<options/switches>]

Examples:

    choco pack
    choco pack --version 1.2.3
    choco pack path/to/nuspec

## Options and Switches

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
--version=VALUE
  Version - The version you would like to insert into the package.
```
