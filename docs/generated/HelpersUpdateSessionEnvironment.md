# Update-SessionEnvironment

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Update-SessionEnvironment.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Updates the environment variables of the current powershell session with
any environment variable changes that may have occured during a
Chocolatey package install.

## Syntax

~~~powershell
Update-SessionEnvironment
~~~

## Description

When Chocolatey installs a package, the package author may add or change
certain environment variables that will affect how the application runs
or how it is accessed. Often, these changes are not visible to the
current PowerShell session. This means the user needs to open a new
PowerShell session before these settings take effect which can render
the installed application nonfunctional until that time.

Use the Update-SessionEnvironment command to refresh the current
PowerShell session with all environment settings possibly performed by
Chocolatey package installs.

## Notes

This method is also added to the user's PowerShell profile as
`refreshenv`. When called as `refreshenv`, the method will provide
additional output.

Preserves `PSModulePath` as set by the process starting in 0.9.10.

## Aliases

`refreshenv`


## Inputs

None

## Outputs

None

## Parameters
 



[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Update-SessionEnvironment -Full`.

View the source for [Update-SessionEnvironment](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Update-SessionEnvironment.ps1)
