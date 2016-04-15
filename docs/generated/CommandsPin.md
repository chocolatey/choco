# Chocolatey Pin (choco pin)
Pin a package to suppress upgrades.

## Usage

    choco pin [list]|add|remove [<options/switches>]

## Examples

    choco pin
    choco pin list
    choco pin add -n=git
    choco pin add -n=git --version 1.2.3
    choco pin remove --name git

## Options and Switches

Includes [[default options/switches|CommandsReference#default-options-and-switches]]

```
-n, --name=VALUE
  Name - the name of the package. Required with some actions. Defaults
  to empty.
--version=VALUE Version - Used when multiple versions of a package are
installed. Defaults to empty.
```
