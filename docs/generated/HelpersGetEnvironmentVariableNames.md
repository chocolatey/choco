# Get-EnvironmentVariableNames

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-EnvironmentVariableNames.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

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

## Examples

 **EXAMPLE 1**

~~~powershell
Get-EnvironmentVariableNames -Scope Machine

~~~ 

## Inputs

None

## Outputs

None

## Parameters
 


## Links

 * [[Get-EnvironmentVariable|HelpersGetEnvironmentVariable]]
 * [[Set-EnvironmentVariable|HelpersSetEnvironmentVariable]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-EnvironmentVariableNames -Full`.

View the source for [Get-EnvironmentVariableNames](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-EnvironmentVariableNames.ps1)
