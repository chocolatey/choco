# Install-ChocolateyDesktopLink

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyDesktopLink.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

DEPRECATED - This adds a shortcut on the desktop to the specified file path.

## Syntax

~~~powershell
Install-ChocolateyDesktopLink `
  -TargetFilePath <String> `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Determines the desktop folder and creates a shortcut to the specified
file path. Will not throw errors if it fails.

It is recommended you use `Install-ChocolateyShorctut` instead of this
method as this has been deprecated.

## Notes

Deprecated in favor of [[`Install-ChocolateyShortcut`|HelpersInstallChocolateyShortcut]].
If this errors, such as it will if being installed under the local
SYSTEM account, it will display a warning instead of failing a package
installation.

Will not throw an error if it fails.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell

# This will create a new Desktop Shortcut pointing at the NHibernate
# Profiler exe.
Install-ChocolateyDesktopLink -TargetFilePath "C:\tools\NHibernatProfiler\nhprof.exe"
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -TargetFilePath &lt;String&gt;
This is the location to the application/executable file that you want to
add a shortcut to on the desktop.  This is mandatory.

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


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyDesktopLink -Full`.

View the source for [Install-ChocolateyDesktopLink](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyDesktopLink.ps1)
