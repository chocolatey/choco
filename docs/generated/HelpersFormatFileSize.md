﻿# Format-FileSize

DO NOT USE. Not part of the public API.

## Syntax

~~~powershell
Format-FileSize `
  -Size <Double> `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Formats file size into a human readable format.

## Notes

Available in 0.9.10+.

This function is not part of the API.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -Size &lt;Double&gt;
The size of a file in bytes.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 0
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


## Examples

 **EXAMPLE 1**

~~~powershell
Format-FileSize -Size $fileSizeBytes

~~~

## Links

 * [[Get-WebFile|HelpersGetWebFile]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Format-FileSize -Full`.
