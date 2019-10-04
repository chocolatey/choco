# Install-ChocolateyShortcut

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyShortcut.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Creates a shortcut

## Syntax

~~~powershell
Install-ChocolateyShortcut `
  -ShortcutFilePath <String> `
  -TargetPath <String> `
  [-WorkingDirectory <String>] `
  [-Arguments <String>] `
  [-IconLocation <String>] `
  [-Description <String>] `
  [-WindowStyle <Int32>] `
  [-RunAsAdmin] `
  [-PinToTaskbar] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This adds a shortcut, at the specified location, with the option to specify
a number of additional properties for the shortcut, such as Working Directory,
Arguments, Icon Location, and Description.

## Notes

If this errors, as it may if being run under the local SYSTEM account with
particular folder that SYSTEM doesn't have, it will display a warning instead
of failing a package installation.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell

# This will create a new shortcut at the location of "C:\test.lnk" and
# link to the file located at "C:\text.exe"

Install-ChocolateyShortcut -ShortcutFilePath "C:\test.lnk" -TargetPath "C:\test.exe"
~~~

**EXAMPLE 2**

~~~powershell

# This will create a new shortcut at the location of "C:\notepad.lnk"
# and link to the Notepad application.  In addition, other properties
# are being set to specify the working directory, an icon to be used for
# the shortcut, along with a description and arguments.

Install-ChocolateyShortcut `
  -ShortcutFilePath "C:\notepad.lnk" `
  -TargetPath "C:\Windows\System32\notepad.exe" `
  -WorkingDirectory "C:\" `
  -Arguments "C:\test.txt" `
  -IconLocation "C:\test.ico" `
  -Description "This is the description"
~~~

**EXAMPLE 3**

~~~powershell

# Creates a new notepad shortcut on the root of c: that starts
# notepad.exe as Administrator. Shortcut is also pinned to taskbar.
# These parameters are available in 0.9.10+.

Install-ChocolateyShortcut `
  -ShortcutFilePath "C:\notepad.lnk" `
  -TargetPath "C:\Windows\System32\notepad.exe" `
  -WindowStyle 3 `
  -RunAsAdmin `
  -PinToTaskbar
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -ShortcutFilePath &lt;String&gt;
The full absolute path to where the shortcut should be created.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -TargetPath &lt;String&gt;
The full absolute path to the target for new shortcut.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -WorkingDirectory [&lt;String&gt;]
OPTIONAL - The full absolute path of the Working Directory that will be
used by the new shortcut.

As of v0.10.12, the directory will be created unless it contains environment
variable expansion like `%AppData%\FooBar`.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -Arguments [&lt;String&gt;]
OPTIONAL - Additonal arguments that should be passed along to the new
shortcut.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 4
Default Value          | 
Accept Pipeline Input? | false
 
###  -IconLocation [&lt;String&gt;]
OPTIONAL- The full absolute path to an icon file to be used for the new
shortcut.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 5
Default Value          | 
Accept Pipeline Input? | false
 
###  -Description [&lt;String&gt;]
OPTIONAL - A text description to be associated with the new description.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 6
Default Value          | 
Accept Pipeline Input? | false
 
###  -WindowStyle [&lt;Int32&gt;]
OPTIONAL - Type of windows target application should open with.
Available in 0.9.10+.
0 = Hidden, 1 = Normal Size, 3 = Maximized, 7 - Minimized.
Full list table 3.9 here: https://technet.microsoft.com/en-us/library/ee156605.aspx

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 7
Default Value          | 0
Accept Pipeline Input? | false
 
###  -RunAsAdmin
OPTIONAL - Set "Run As Administrator" checkbox for the created the
shortcut. Available in 0.9.10+.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | False
Accept Pipeline Input? | false
 
###  -PinToTaskbar
OPTIONAL - Pin the new shortcut to the taskbar. Available in 0.9.10+.

Property               | Value
---------------------- | -----
Aliases                | 
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

 * [[Install-ChocolateyDesktopLink|HelpersInstallChocolateyDesktopLink]]
 * [[Install-ChocolateyExplorerMenuItem|HelpersInstallChocolateyExplorerMenuItem]]
 * [[Install-ChocolateyPinnedTaskBarItem|HelpersInstallChocolateyPinnedTaskBarItem]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyShortcut -Full`.

View the source for [Install-ChocolateyShortcut](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyShortcut.ps1)
