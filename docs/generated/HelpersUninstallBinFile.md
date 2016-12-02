# Uninstall-BinFile

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Uninstall-BinFile.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Removes a shim (or batch redirect) for a file.

## Syntax

~~~powershell
Uninstall-BinFile `
  -Name <String> `
  [-Path <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Chocolatey installs have the folder `$($env:ChocolateyInstall)\bin`
included in the PATH environment variable. Chocolatey automatically
shims executables in package folders that are not explicitly ignored,
putting them into the bin folder (and subsequently onto the PATH).

When you have other files you have shimmed, you need to use this
function to remove them from the bin folder.

## Notes

Not normally needed for exe files in the package folder, those are
automatically discovered and the shims removed.

## Aliases

`Remove-BinFile`


## Inputs

None

## Outputs

None

## Parameters

###  -Name &lt;String&gt;
The name of the redirect file without ".exe" appended to it.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -Path [&lt;String&gt;]
The path to the original file. Can be relative from
`$($env:ChocolateyInstall)\bin` back to your file or a full path to the
file.

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


## Links

 * [[Install-BinFile|HelpersInstallBinFile]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Uninstall-BinFile -Full`.

View the source for [Uninstall-BinFile](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Uninstall-BinFile.ps1)
