﻿# Get-EnvironmentVariableNames

Gets all environment variable names.

## Syntax

~~~powershell
Get-EnvironmentVariableNames `
  [-Scope {Process | User | Machine}]
~~~

## Description

Provides a list of environment variable names based on the scope. This
can be used to loop through the list and generate names.

## Notes

Process dumps the current environment variable names in memory /
session. The other scopes refer to the registry values.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters
 


## Examples

 **EXAMPLE 1**

~~~powershell
Get-EnvironmentVariableNames -Scope Machine

~~~

## Links

 * [[Get-EnvironmentVariable|HelpersGetEnvironmentVariable]]
 * [[Set-EnvironmentVariable|HelpersSetEnvironmentVariable]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-EnvironmentVariableNames -Full`.
