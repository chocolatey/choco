# Write-ChocolateySuccess

DEPRECATED - DO NOT USE.

## Syntax

~~~powershell
Write-ChocolateySuccess `
  [-PackageName <String>]
~~~

## Description

Writes a success message for a package.

## Notes

This has been deprecated and is no longer useful as of 0.9.9. Instead
please just use `throw $_.Exception` when catching errors. Although
try/catch is no longer necessary unless you want to do some error
handling.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters
 



## Links

 * [[Write-ChocolateyFailure|HelpersWriteChocolateyFailure]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Write-ChocolateySuccess -Full`.
