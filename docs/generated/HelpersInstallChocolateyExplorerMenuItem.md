# Install-ChocolateyExplorerMenuItem

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyExplorerMenuItem.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

**NOTE:** Administrative Access Required.

Creates a windows explorer context menu item that can be associated with
a command

## Syntax

~~~powershell
Install-ChocolateyExplorerMenuItem `
  -MenuKey <String> `
  [-MenuLabel <String>] `
  [-Command <String>] `
  [-Type <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Install-ChocolateyExplorerMenuItem can add an entry in the context menu
of Windows Explorer. The menu item is given a text label and a command.
The command can be any command accepted on the windows command line. The
menu item can be applied to either folder items or file items.

Because this command accesses and edits the root class registry node, it
will be elevated to admin.

## Notes

This command will assert UAC/Admin privileges on the machine.

Chocolatey will automatically add the path of the file or folder clicked
to the command. This is done simply by appending a %1 to the end of the
command.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell

# This will create a context menu item in Windows Explorer when any file
# is right clicked. The menu item will appear with the text "Open with
# Sublime Text 2" and will invoke sublime text 2 when selected.
$sublimeDir = (Get-ChildItem $env:ALLUSERSPROFILE\chocolatey\lib\sublimetext* | select $_.last)
$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
Install-ChocolateyExplorerMenuItem "sublime" "Open with Sublime Text 2" $sublimeExe
~~~

**EXAMPLE 2**

~~~powershell

# This will create a context menu item in Windows Explorer when any
# folder is right clicked. The menu item will appear with the text
# "Open with Sublime Text 2" and will invoke sublime text 2 when selected.
$sublimeDir = (Get-ChildItem $env:ALLUSERSPROFILE\chocolatey\lib\sublimetext* | select $_.last)
$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
Install-ChocolateyExplorerMenuItem "sublime" "Open with Sublime Text 2" $sublimeExe "directory"
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -MenuKey &lt;String&gt;
A unique string to identify this menu item in the registry

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -MenuLabel [&lt;String&gt;]
The string that will be displayed in the context menu

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -Command [&lt;String&gt;]
A command line command that will be invoked when the menu item is
selected

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -Type [&lt;String&gt;]
Specifies if the menu item should be applied to a folder or a file

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 4
Default Value          | file
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


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyExplorerMenuItem -Full`.

View the source for [Install-ChocolateyExplorerMenuItem](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyExplorerMenuItem.ps1)
