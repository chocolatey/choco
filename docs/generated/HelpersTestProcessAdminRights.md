# Test-ProcessAdminRights

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Test-ProcessAdminRights.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Tests whether the current process is running with administrative rights.

## Syntax

~~~powershell
Test-ProcessAdminRights
~~~

## Description

This function checks whether the current process has administrative
rights by checking if the current user identity is a member of the
Administrators group. It returns `$true` if the current process is
running with administrative rights, `$false` otherwise.

On Windows Vista and later, with UAC enabled, the returned value
represents the actual rights available to the process, e.g. if it
returns `$true`, the process is running elevated.


## Aliases

None

## Inputs

None

## Outputs

None

## Parameters
 



[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Test-ProcessAdminRights -Full`.

View the source for [Test-ProcessAdminRights](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Test-ProcessAdminRights.ps1)
