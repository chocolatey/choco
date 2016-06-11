# Get-UACEnabled

Determines if UAC (User Account Control) is turned on or off.

## Syntax

~~~powershell
Get-UACEnabled
~~~

## Description

This is a low level function used by Chocolatey to decide whether
prompting for elevated privileges is necessary or not.

## Notes

This checks the `EnableLUA` registry value to be determine the state of
a system.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters
 



[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-UACEnabled -Full`.
