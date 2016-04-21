# Install-ChocolateyFileAssociation

Creates an association between a file extension and a executable

## Syntax

~~~powershell
Install-ChocolateyFileAssociation `
  [-Extension <String>] `
  [-Executable <String>]
~~~

## Description

Install-ChocolateyFileAssociation can associate a file extension
with a downloaded application. Once this command has created an
association, all invocations of files with the specified extension
will be opened via the executable specified.

## Notes

This command will assert UAC/Admin privileges on the machine.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -Extension [\<String\>]
The file extension to be associated.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -Executable [\<String\>]
The path to the application's executable to be associated.

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

# This will create an association between Sublime Text 2 and all .txt
# files. Any .txt file opened will by default open with Sublime Text 2.
$sublimeDir = (Get-ChildItem $env:ALLUSERSPROFILE\chocolatey\lib\sublimetext* | select $_.last)
$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
Install-ChocolateyFileAssociation ".txt" $sublimeExe
~~~


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyFileAssociation -Full`.
