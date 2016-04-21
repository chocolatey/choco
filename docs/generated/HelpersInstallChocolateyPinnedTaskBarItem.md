# Install-ChocolateyPinnedTaskBarItem

Creates an item in the task bar linking to the provided path.

## Syntax

~~~powershell
Install-ChocolateyPinnedTaskBarItem `
  [-TargetFilePath <String>]
~~~



## Aliases

None

## Inputs

None

## Outputs

None

## Parameters
 


## Examples

 **EXAMPLE 1**

~~~powershell

# This will create a Visual Studio task bar icon.
Install-ChocolateyPinnedTaskBarItem -TargetFilePath "${env:ProgramFiles(x86)}\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe"
~~~


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyPinnedTaskBarItem -Full`.
