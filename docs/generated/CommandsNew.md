# Chocolatey New (choco new)
Chocolatey will generate package specification files for a new package.

## Usage

     choco new <name> [<options/switches>] [<property=value> <propertyN=valueN>]

Possible properties to pass:
```
    packageversion
    maintainername
    maintainerrepo
    installertype
    url
    url64
    silentargs
```

## Examples

    choco new bob
    choco new bob -a --version 1.2.0 maintainername="This guy"
    choco new bob silentargs="/S" url="https://somewhere/out/there.msi"

## Options and Switches

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
-a, --auto, --automaticpackage
  AutomaticPackage - Generate automatic package instead of normal.
  Defaults to false

--name=VALUE
  Name [Required]- the name of the package. Can be passed as first
  parameter without "--name=".

--version=VALUE
  Version - the version of the package. Can also be passed as the
  property `packageversion=somevalue`.

--maintainer=VALUE
    Maintainer - the name of the maintainer. Can also be passed as the
    property `maintainername=somevalue`.
```
