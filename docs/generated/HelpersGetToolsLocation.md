# Get-ToolsLocation

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-ToolsLocation.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Gets the top level location for tools/software installed outside of
package folders.

## Syntax

~~~powershell
Get-ToolsLocation
~~~

## Description

Creates or uses an environment variable that a user can control to
communicate with packages about where they would like software that is
not installed through native installers, but doesn't make much sense
to be kept in package folders. Most software coming in packages stays
with the package itself, but there are some things that seem to fall
out of this category, like things that have plugins that are installed
into the same directory as the tool. Having that all combined in the
same package directory could get tricky.

## Notes

This is the successor to the poorly named `Get-BinRoot`. Available as
`Get-ToolsLocation` in 0.9.10+. If you need compatibility with pre
0.9.10, please use `Get-BinRoot`.

Sets an environment variable called `ChocolateyToolsLocation`. If the
older `ChocolateyBinRoot` is set, it uses the value from that and
removes the older variable.

## Aliases

`Get-BinRoot`


## Inputs

None

## Outputs

None

## Parameters
 



[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-ToolsLocation -Full`.

View the source for [Get-ToolsLocation](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-ToolsLocation.ps1)
