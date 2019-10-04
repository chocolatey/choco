# Uninstall-ChocolateyPackage

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Uninstall-ChocolateyPackage.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Uninstalls software from "Programs and Features".

## Syntax

~~~powershell
Uninstall-ChocolateyPackage `
  -PackageName <String> `
  [-FileType <String>] `
  [-SilentArgs <String[]>] `
  [-File <String>] `
  [-ValidExitCodes <Object>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
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

## Examples

 **EXAMPLE 1**

~~~powershell
Uninstall-ChocolateyPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

~~~

**EXAMPLE 2**

~~~powershell

Uninstall-ChocolateyPackage -PackageName $packageName `
                                -FileType $installerType `
                                -SilentArgs "$silentArgs" `
                                -ValidExitCodes $validExitCodes `
                                -File "$file"
~~~ 

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
 
###  -FileType [&lt;String&gt;]
This is the extension of the file. This should be either exe or msi.

If what is provided is empty or null, Chocolatey will use 'exe'
starting in 0.10.1.

Property               | Value
---------------------- | -------------
Aliases                | installerType
Required?              | false
Position?              | 2
Default Value          | exe
Accept Pipeline Input? | false
 
###  -SilentArgs [&lt;String[]&gt;]
OPTIONAL - These are the parameters to pass to the native uninstaller,
including any arguments to make the uninstaller silent/unattended.
[Licensed editions](https://chocolatey.org/compare) of Chocolatey will automatically determine the
installer type and merge the arguments with what is provided here.

Try any of the to get the silent (unattended) uninstaller -
`/s /S /q /Q /quiet /silent /SILENT /VERYSILENT`. With msi it is always
`/quiet`. Please pass it in still but it will be overridden by
Chocolatey to `/quiet`. If you don't pass anything it could invoke the
installer with out any arguments. That means a nonsilent installer.

Please include the `notSilent` tag in your Chocolatey package if you
are not setting up a silent/unattended package. Please note that if you
are submitting to the [community repository](https://chocolatey.org/packages), it is nearly a requirement
for the package to be completely unattended.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -File [&lt;String&gt;]
The full path to the native uninstaller to run.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 4
Default Value          | 
Accept Pipeline Input? | false
 
###  -ValidExitCodes [&lt;Object&gt;]
Array of exit codes indicating success. Defaults to `@(0)`.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | @(0)
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


## Links

 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
 * [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]
 * [[Uninstall-ChocolateyZipPackage|HelpersUninstallChocolateyZipPackage]]
 * [[Get-UninstallRegistryKey|HelpersGetUninstallRegistryKey]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Uninstall-ChocolateyPackage -Full`.

View the source for [Uninstall-ChocolateyPackage](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Uninstall-ChocolateyPackage.ps1)
