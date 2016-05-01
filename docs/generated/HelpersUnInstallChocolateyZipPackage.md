# UnInstall-ChocolateyZipPackage

Uninstalls a previous installed zip package

## Syntax

~~~powershell
UnInstall-ChocolateyZipPackage `
  [-PackageName <String>] `
  [-ZipFileName <String>]
~~~

## Description

This will uninstall a zip file if installed via Install-ChocolateyZipPackage

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
 
###  -ZipFileName [\<String\>]
This is the zip filename originally installed.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 


## Examples

 **EXAMPLE 1**

~~~powershell
UnInstall-ChocolateyZipPackage '__NAME__' 'filename.zip'

~~~

## Links

 * [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]
 * [[Uninstall-ChocolateyPackage|HelpersUninstallChocolateyPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help UnInstall-ChocolateyZipPackage -Full`.
