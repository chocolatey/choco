# Install-ChocolateyShortcut

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

## Inputs

None

## Outputs

None

## Parameters

###  -ShortcutFilePath &lt;String&gt;
The full absolute path to where the shortcut should be created.  This is mandatory.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -TargetPath &lt;String&gt;
The full absolute path to the target for new shortcut.  This is mandatory.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -WorkingDirectory [&lt;String&gt;]
The full absolute path of the Working Directory that will be used by
the new shortcut.  This is optional

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -Arguments [&lt;String&gt;]
Additonal arguments that should be passed along to the new shortcut.  This
is optional.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 4
Default Value          | 
Accept Pipeline Input? | false
 
###  -IconLocation [&lt;String&gt;]
The full absolute path to an icon file to be used for the new shortcut.  This
is optional.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 5
Default Value          | 
Accept Pipeline Input? | false
 
###  -Description [&lt;String&gt;]
A text description to be associated with the new description.  This is optional.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 6
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
  -WorkDirectory "C:\" `
  -Arguments "C:\test.txt" `
  -IconLocation "C:\test.ico" `
  -Description "This is the description"
~~~

## Links

 * [[Install-ChocolateyDesktopLink|HelpersInstallChocolateyDesktopLink]]
 * [[Install-ChocolateyExplorerMenuItem|HelpersInstallChocolateyExplorerMenuItem]]
 * [[Install-ChocolateyPinnedTaskBarItem|HelpersInstallChocolateyPinnedTaskBarItem]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyShortcut -Full`.
