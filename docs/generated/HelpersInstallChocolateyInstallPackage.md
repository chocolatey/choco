# Install-ChocolateyInstallPackage

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyInstallPackage.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

**NOTE:** Administrative Access Required.

Installs software into "Programs and Features". Use
Install-ChocolateyPackage when software must be downloaded first.

## Syntax

~~~powershell
Install-ChocolateyInstallPackage `
  -PackageName <String> `
  [-FileType <String>] `
  [-SilentArgs <String[]>] `
  [-File <String>] `
  [-File64 <String>] `
  [-ValidExitCodes <Object>] `
  [-UseOnlyPackageSilentArguments] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This will run an installer (local file) on your machine.

## Notes

This command will assert UAC/Admin privileges on the machine.

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

## Examples

 **EXAMPLE 1**

~~~powershell

$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'INSTALLER_EMBEDDED_IN_PACKAGE'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart"
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs
~~~

**EXAMPLE 2**

~~~powershell

$packageArgs = @{
  packageName   = 'bob'
  fileType      = 'exe'
  file          = '\\SHARE_LOCATION\to\INSTALLER_FILE'
  silentArgs    = "/S"
  validExitCodes= @(0)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs
~~~

**EXAMPLE 3**

~~~powershell

$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'someinstaller.msi'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart MSIPROPERTY=`"true`""
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs
~~~

**EXAMPLE 4**

~~~powershell

$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir 'someinstaller.msi'
$mstFileLocation = Join-Path $toolsDir 'transform.mst'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart TRANSFORMS=`"$mstFileLocation`""
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs
~~~

**EXAMPLE 5**

~~~powershell
Install-ChocolateyInstallPackage 'bob' 'exe' '/S' "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\bob.exe"

~~~

**EXAMPLE 6**

~~~powershell

Install-ChocolateyInstallPackage -PackageName 'bob' -FileType 'exe' `
  -SilentArgs '/S' `
  -File "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\bob.exe" `
  -ValidExitCodes @(0)
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
This is the extension of the file. This can be 'exe', 'msi', or 'msu'.
[Licensed editions](https://chocolatey.org/compare) of Chocolatey use this to automatically determine
silent arguments. If this is not provided, Chocolatey will
automatically determine this using the downloaded file's extension.

Property               | Value
---------------------- | --------------------------
Aliases                | installerType, installType
Required?              | false
Position?              | 2
Default Value          | exe
Accept Pipeline Input? | false
 
###  -SilentArgs [&lt;String[]&gt;]
OPTIONAL - These are the parameters to pass to the native installer,
including any arguments to make the installer silent/unattended.
Pro/Business Editions of Chocolatey will automatically determine the
installer type and merge the arguments with what is provided here.

Try any of the to get the silent installer -
`/s /S /q /Q /quiet /silent /SILENT /VERYSILENT`. With msi it is always
`/quiet`. Please pass it in still but it will be overridden by
Chocolatey to `/quiet`. If you don't pass anything it could invoke the
installer with out any arguments. That means a nonsilent installer.

Please include the `notSilent` tag in your Chocolatey package if you
are not setting up a silent/unattended package. Please note that if you
are submitting to the [community repository](https://chocolatey.org/packages), it is nearly a requirement
for the package to be completely unattended.

When you are using this with an MSI, it will set up the arguments as
follows: `"C:\Full\Path\To\msiexec.exe" /i "$fileFullPath" $silentArgs`,
where `$fileFullPath` is `$file` or `$file64`, depending on what has been
decided to be used. Previous to 0.10.4, it will be just `$file` as
passing `$file64` would not have been available yet.

When you use this with MSU, it is similar to MSI above in that it finds
the right executable to run.

When you use this with executable installers, the `$fileFullPath` will
be `$file` (or `$file64` starting with 0.10.4+) and expects to be a full
path to the file. If the file is in the package, see the parameters for
"File" and "File64" to determine how you can get that path at runtime in
a deterministic way. SilentArgs is everything you call against that
file, as in `"$fileFullPath" $silentArgs"`. An example would be
`"c:\path\setup.exe" /S`, where `$fileFullPath = "c:\path\setup.exe"`
and `$silentArgs = "/S"`.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -File [&lt;String&gt;]
Full file path to native installer to run. If embedding in the package,
you can get it to the path with
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\INSTALLER_FILE"`

In 0.10.1+, `FileFullPath` is an alias for File.

This can be a 32-bit or 64-bit file. This is mandatory in earlier versions
of Chocolatey, but optional if File64 has been provided.

Property               | Value
---------------------- | ------------
Aliases                | fileFullPath
Required?              | false
Position?              | 4
Default Value          | 
Accept Pipeline Input? | false
 
###  -File64 [&lt;String&gt;]
Full file path to a 64-bit native installer to run. Available in 0.10.4+.
If embedding in the package, you can get it to the path with
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\INSTALLER_FILE"`

Provide this when you want to provide both 32-bit and 64-bit
installers or explicitly only a 64-bit installer (which will cause a package
install failure on 32-bit systems).

Property               | Value
---------------------- | --------------
Aliases                | fileFullPath64
Required?              | false
Position?              | named
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
 * [[Uninstall-ChocolateyPackage|HelpersUninstallChocolateyPackage]]
 * [[Get-UninstallRegistryKey|HelpersGetUninstallRegistryKey]]
 * [[Start-ChocolateyProcessAsAdmin|HelpersStartChocolateyProcessAsAdmin]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyInstallPackage -Full`.

View the source for [Install-ChocolateyInstallPackage](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyInstallPackage.ps1)
