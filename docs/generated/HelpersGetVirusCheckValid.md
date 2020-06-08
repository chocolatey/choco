# Get-VirusCheckValid

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-VirusCheckValid.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Used in Pro/Business editions. Runtime virus check against downloaded
resources.

## Syntax

~~~powershell
Get-VirusCheckValid `
  [-Url <String>] `
  [-File <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Run a runtime malware check against downloaded resources prior to
allowing Chocolatey to execute a file. This is available in 0.9.10+ only
in Pro / Business editions.

## Notes

Only [licensed editions](https://chocolatey.org/compare) of Chocolatey provide runtime malware protection.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -Url [&lt;String&gt;]
Not used

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -File [&lt;String&gt;]
The full file path to the file to verify against anti-virus scanners.

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
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
### &lt;CommonParameters&gt;

This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see `about_CommonParameters` http://go.microsoft.com/fwlink/p/?LinkID=113216 .



[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-VirusCheckValid -Full`.

View the source for [Get-VirusCheckValid](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-VirusCheckValid.ps1)
