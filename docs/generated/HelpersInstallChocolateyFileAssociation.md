# Install-ChocolateyFileAssociation

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyFileAssociation.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

**NOTE:** Administrative Access Required.

Creates an association between a file extension and a executable.

## Syntax

~~~powershell
Install-ChocolateyFileAssociation `
  -Extension <String> `
  -Executable <String> `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
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

## Examples

 **EXAMPLE 1**

~~~powershell

# This will create an association between Sublime Text 2 and all .txt
# files. Any .txt file opened will by default open with Sublime Text 2.
$sublimeDir = (Get-ChildItem $env:ALLUSERSPROFILE\chocolatey\lib\sublimetext* | select $_.last)
$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
Install-ChocolateyFileAssociation ".txt" $sublimeExe
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -Extension &lt;String&gt;
The file extension to be associated.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -Executable &lt;String&gt;
The path to the application's executable to be associated.

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



[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyFileAssociation -Full`.

View the source for [Install-ChocolateyFileAssociation](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyFileAssociation.ps1)
