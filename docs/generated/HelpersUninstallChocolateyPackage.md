# Uninstall-ChocolateyPackage

Uninstalls software from "Programs and Features".

## Syntax

~~~powershell
Uninstall-ChocolateyPackage `
  [-PackageName <String>] `
  [-FileType <String>] `
  [-SilentArgs <String>] `
  [-File <String>] `
  [-ValidExitCodes <Object>]
~~~

## Description

This will uninstall software from your machine (in Programs and
Features). This may not be necessary if Auto Uninstaller is turned on.

Choco 0.9.9+ automatically tracks registry changes for "Programs and
Features" of the underlying software's native installers when
installing packages. The "Automatic Uninstaller" (auto uninstaller)
service is a feature that can use that information to automatically
determine how to uninstall these natively installed applications. This
means that a package may not need an explicit chocolateyUninstall.ps1
to reverse the installation done in the install script.

With auto uninstaller turned off, a chocolateyUninstall.ps1 is required
to perform uninstall from "Programs and Features". In the absence of
chocolateyUninstall.ps1, choco uninstall only removes the package from
Chocolatey but does not remove the sofware from your system without
auto uninstaller.

## Notes

May not be required. Starting in 0.9.10+, the Automatic Uninstaller
(AutoUninstaller) is turned on by default.

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
 
###  -FileType [\<String\>]
This is the extension of the file. This should be either exe or msi.

Property               | Value
---------------------- | -------------
Aliases                | installerType
Required?              | false
Position?              | 2
Default Value          | exe
Accept Pipeline Input? | false
 
###  -SilentArgs [\<String\>]
Please include the notSilent tag in your chocolatey nuget package if you
are not setting up a silent package.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -File [\<String\>]
The full path to the native uninstaller to run.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 4
Default Value          | 
Accept Pipeline Input? | false
 
###  -ValidExitCodes [\<Object\>]
Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 5
Default Value          | @(0)
Accept Pipeline Input? | false
 


## Examples

 **EXAMPLE 1**

~~~powershell
Uninstall-ChocolateyPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

~~~

## Links

 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
 * [[Uninstall-ChocolateyZipPackage|HelpersUninstallChocolateyZipPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Uninstall-ChocolateyPackage -Full`.
