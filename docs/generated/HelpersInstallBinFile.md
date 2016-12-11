# Install-BinFile

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-BinFile.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Creates a shim (or batch redirect) for a file that is on the PATH.

## Syntax

~~~powershell
Install-BinFile `
  -Name <String> `
  -Path <String> `
  [-UseStart] `
  [-Command <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Chocolatey installs have the folder `$($env:ChocolateyInstall)\bin`
included in the PATH environment variable. Chocolatey automatically
shims executables in package folders that are not explicitly ignored,
putting them into the bin folder (and subsequently onto the PATH).

When you have other files you want to shim to add them to the PATH or
if you want to handle the shimming explicitly, use this function.

If you do use this function, ensure you also add `Uninstall-BinFile` to
your `chocolateyUninstall.ps1` script as Chocolatey will not
automatically clean up shims created with this function.

## Notes

Not normally needed for exe files in the package folder, those are
automatically discovered and added as shims after the install script
completes.

## Aliases

`Add-BinFile`
`Generate-BinFile`


## Inputs

None

## Outputs

None

## Parameters

###  -Name &lt;String&gt;
The name of the redirect file, will have .exe appended to it.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -Path &lt;String&gt;
The path to the original file. Can be relative from
`$($env:ChocolateyInstall)\bin` back to your file or a full path to the
file.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -UseStart
This should be passed if the shim should not wait on the action to
complete. This is usually the case with GUI apps, you don't want the
command shell blocked waiting for the GUI app to be shut back down.

Property               | Value
---------------------- | -----
Aliases                | isGui
Required?              | false
Position?              | named
Default Value          | False
Accept Pipeline Input? | false
 
###  -Command [&lt;String&gt;]
OPTIONAL - This is any additional command arguments you want passed
every time to the command. This is not normally used, but may be
necessary if you are calling something and then your application. For
example if you are calling Java with your JAR, the command would be the
JAR file plus any other options to start Java appropriately.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
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


## Links

 * [[Uninstall-BinFile|HelpersUninstallBinFile]]
 * [[Install-ChocolateyShortcut|HelpersInstallChocolateyShortcut]]
 * [[Install-ChocolateyPath|HelpersInstallChocolateyPath]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-BinFile -Full`.

View the source for [Install-BinFile](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-BinFile.ps1)
