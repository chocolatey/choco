# Install-Vsix

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-Vsix.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

DO NOT USE. Not part of the public API.

## Syntax

~~~powershell
Install-Vsix `
  -Installer <String> `
  -InstallFile <String> `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Installs a VSIX package into a particular version of Visual Studio.

## Notes

This is not part of the public API. Please use
Install-ChocolateyVsixPackage instead.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -Installer &lt;String&gt;
The path to the VSIX installer

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -InstallFile &lt;String&gt;
The VSIX file that is being installed.

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


## Links

 * [[Install-ChocolateyVsixPackage|HelpersInstallChocolateyVsixPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-Vsix -Full`.

View the source for [Install-Vsix](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-Vsix.ps1)
