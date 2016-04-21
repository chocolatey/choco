# Write-ChocolateyFailure

DEPRECATED - DO NOT USE.

## Syntax

~~~powershell
Write-ChocolateyFailure `
  [-PackageName <String>] `
  [-FailureMessage <String>]
~~~

## Description

Throws the error message as an error.

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

###  -PackageName [\<String\>]
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -FailureMessage [\<String\>]
The message to throw an error with.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 



## Links

 * [[Write-ChocolateySuccess|HelpersWriteChocolateySuccess]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Write-ChocolateyFailure -Full`.
