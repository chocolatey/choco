# Install-ChocolateyPinnedTaskBarItem

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyPinnedTaskBarItem.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Creates an item in the task bar linking to the provided path.

## Syntax

~~~powershell
Install-ChocolateyPinnedTaskBarItem `
  -TargetFilePath <String> `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~


## Notes

Does not work with SYSTEM, but does not error. It warns with the error
message.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell

# This will create a Visual Studio task bar icon.
Install-ChocolateyPinnedTaskBarItem -TargetFilePath "${env:ProgramFiles(x86)}\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe"
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -TargetFilePath &lt;String&gt;
The path to the application that should be launched when clicking on the
task bar icon.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
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

 * [[Install-ChocolateyShortcut|HelpersInstallChocolateyShortcut]]
 * [[Install-ChocolateyExplorerMenuItem|HelpersInstallChocolateyExplorerMenuItem]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyPinnedTaskBarItem -Full`.

View the source for [Install-ChocolateyPinnedTaskBarItem](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyPinnedTaskBarItem.ps1)
