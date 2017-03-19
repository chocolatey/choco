# Get-UninstallRegistryKey

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-UninstallRegistryKey.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Retrieve registry key(s) for system-installed applications from an
exact or wildcard search.

## Syntax

~~~powershell
Get-UninstallRegistryKey `
  -SoftwareName <String> `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This function will attempt to retrieve a matching registry key for an
already installed application, usually to be used with a
chocolateyUninstall.ps1 automation script.

The function also prevents `Get-ItemProperty` from failing when
handling wrongly encoded registry keys.

## Notes

Available in 0.9.10+. If you need to maintain compatibility with pre
0.9.10, please add the following to your nuspec (check for minimum
version):

~~~xml
<dependencies>
  <dependency id="chocolatey-core.extension" version="1.1.0" />
</dependencies>
~~~

## Aliases

`Get-InstallRegistryKey`


## Examples

 **EXAMPLE 1**

~~~powershell

# Version match: Software name is "Gpg4Win (2.3.0)"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Gpg4win (*)"
$key.UninstallString
~~~

**EXAMPLE 2**

~~~powershell

# Fuzzy match: Software name is "Launchy 2.5"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Launchy*"
$key.UninstallString
~~~

**EXAMPLE 3**

~~~powershell

# Exact match: Software name in Programs and Features is "VLC media player"
[array]$key = Get-UninstallRegistryKey -SoftwareName "VLC media player"
$key.UninstallString
~~~

**EXAMPLE 4**

~~~powershell

#  Version match: Software name is "SketchUp 2016"
# Note that the similar software name "SketchUp Viewer" would not be matched.
[array]$key = Get-UninstallRegistryKey -SoftwareName "SketchUp [0-9]*"
$key.UninstallString
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -SoftwareName &lt;String&gt;
Part or all of the Display Name as you see it in Programs and Features.
It should be enough to be unique.
The syntax follows the rules of the PowerShell `-like` operator, so the
`*` character is interpreted as a wildcard, which matches any (zero or
more) characters.

If the display name contains a version number, such as "Launchy (2.5)",
it is recommended you use a fuzzy search `"Launchy (*)"` (the wildcard
`*`) so if Launchy auto-updates or is updated outside of Chocolatey, the
uninstall script will not fail.

Take care not to abuse fuzzy/glob pattern searches. Be conscious of
programs that may have shared or common root words to prevent
overmatching. For example, "SketchUp*" would match two keys with
software names "SketchUp 2016" and "SketchUp Viewer" that are different
programs released by the same company.

Property               | Value
---------------------- | --------------
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | true (ByValue)
 
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

 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
 * [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]
 * [[Uninstall-ChocolateyPackage|HelpersUninstallChocolateyPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-UninstallRegistryKey -Full`.

View the source for [Get-UninstallRegistryKey](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-UninstallRegistryKey.ps1)
