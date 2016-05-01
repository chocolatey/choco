# Install-ChocolateyInstallPackage

Installs software into "Programs and Features". Use
Install-ChocolateyPackage when software must be downloaded first.

## Syntax

~~~powershell
Install-ChocolateyInstallPackage `
  -PackageName <String> `
  [-FileType <String>] `
  [-SilentArgs <String>] `
  -File <String> `
  [-ValidExitCodes <Object>] `
  [-UseOnlyPackageSilentArguments] [<CommonParameters>]
~~~

## Description

This will run an installer (local file) on your machine.

## Notes

If you are embedding files into a package, ensure that you have the
rights to redistribute those files if you are sharing this package
publicly (like on the [community feed](https://chocolatey.org/packages)). Otherwise, please use
Install-ChocolateyPackage to download those resources from their
official distribution points.

This is a native installer wrapper function. A "true" package will
contain all the run time files and not an installer. That could come
pre-zipped and require unzipping in a PowerShell script. Chocolatey
works best when the packages contain the software it is managing. Most
software in the Windows world comes as installers and Chocolatey
understands how to work with that, hence this wrapper function.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -PackageName \<String\>
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -FileType [\<String\>]
This is the extension of the file. This can be 'exe', 'msi', or 'msu'.
Licensed versions of Chocolatey use this to automatically determine
silent arguments. If this is not provided, Chocolatey will
automatically determine this using the downloaded file's extension.

Property               | Value
---------------------- | --------------------------
Aliases                | installerType, installType
Required?              | false
Position?              | 2
Default Value          | exe
Accept Pipeline Input? | false
 
###  -SilentArgs [\<String\>]
OPTIONAL - These are the parameters to pass to the native installer.
Licensed versions of Chocolatey will automatically determine the
installer type and merge the arguments with what is provided here.

Try any of the to get the silent installer -
`/s /S /q /Q /quiet /silent /SILENT /VERYSILENT`. With msi it is always
`/quiet`. Please pass it in still but it will be overridden by
Chocolatey to `/quiet`. If you don't pass anything it could invoke the
installer with out any arguments. That means a nonsilent installer.

Please include the `notSilent` tag in your Chocolatey package if you
are not setting up a silent package. Please note that if you are
submitting to the [community repository](https://chocolatey.org/packages), it is nearly a requirement for
the package to be completely unattended.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -File \<String\>
Full file path to native installer to run. If embedding in the package,
you can get it to the path with
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\INSTALLER_FILE"`

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 4
Default Value          | 
Accept Pipeline Input? | false
 
###  -ValidExitCodes [\<Object\>]
Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | @(0)
Accept Pipeline Input? | false
 
###  -UseOnlyPackageSilentArguments
Do not allow choco to provide/merge additional silent arguments and
only use the ones available with the package. Available in 0.9.10+.

Property               | Value
---------------------- | ------------------------
Aliases                | useOnlyPackageSilentArgs
Required?              | false
Position?              | named
Default Value          | False
Accept Pipeline Input? | false
 
### \<CommonParameters\>

This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see `about_CommonParameters` http://go.microsoft.com/fwlink/p/?LinkID=113216 .


## Examples

 **EXAMPLE 1**

~~~powershell
Install-ChocolateyInstallPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

~~~

## Links

 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyInstallPackage -Full`.
