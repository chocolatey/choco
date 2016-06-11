﻿# Test-ProcessAdminRights

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
