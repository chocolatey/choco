# Write-ChocolateyFailure

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Write-ChocolateyFailure.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

DEPRECATED - DO NOT USE.

## Syntax

~~~powershell
Write-ChocolateyFailure `
  [-PackageName <String>] `
  [-FailureMessage <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
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

###  -PackageName [&lt;String&gt;]
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -FailureMessage [&lt;String&gt;]
The message to throw an error with.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -IgnoredArguments [&lt;Object[]&gt;]
Allows splatting with arguments that do not apply. Do not use directly.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
### &lt;CommonParameters&gt;

This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see `about_CommonParameters` http://go.microsoft.com/fwlink/p/?LinkID=113216 .


## Links

 * [[Write-ChocolateySuccess|HelpersWriteChocolateySuccess]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Write-ChocolateyFailure -Full`.

View the source for [Write-ChocolateyFailure](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Write-ChocolateyFailure.ps1)
