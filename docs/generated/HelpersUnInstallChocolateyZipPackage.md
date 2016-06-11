﻿# Uninstall-ChocolateyZipPackage

Uninstalls a previous installed zip package

## Syntax

~~~powershell
Uninstall-ChocolateyZipPackage `
  -PackageName <String> `
  -ZipFileName <String> `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This will uninstall a zip file if installed via Install-ChocolateyZipPackage.
This is not necessary if the files are unzipped to the package location.

## Notes

This helper reduces the number of lines one would have to remove the
files extracted from a previously installed zip file.
This method has error handling built into it.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -PackageName &lt;String&gt;
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -ZipFileName &lt;String&gt;
This is the zip filename originally installed.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
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


## Examples

 **EXAMPLE 1**

~~~powershell
Uninstall-ChocolateyZipPackage '__NAME__' 'filename.zip'

~~~

## Links

 * [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]
 * [[Uninstall-ChocolateyPackage|HelpersUninstallChocolateyPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Uninstall-ChocolateyZipPackage -Full`.
