# Get-UninstallRegistryKey

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
0.9.10, please add the following to your nuspec:

~~~xml
<dependencies>
  <dependency id="chocolatey-uninstall.extension" />
</dependencies>
~~~

## Aliases

`Get-InstallRegistryKey`


## Examples

 **EXAMPLE 1**

~~~powershell

# Software name in Programs and Features is "Gpg4Win (2.3.0)"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Gpg4win*"
$key.DisplayName
~~~

**EXAMPLE 2**

~~~powershell

# Software name is "Launchy 2.5"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Launchy*"
$key.UninstallString
~~~

**EXAMPLE 3**

~~~powershell

# Software name is "Mozilla Firefox"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Mozilla Firefox"
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

If the display name contains a version number, such as "Launchy 2.5", 
it is recommended you use a fuzzy search "Launchy*" (the wildcard '*')
as if the version is upgraded or autoupgraded, suddenly the uninstall
script will stop working and it may not be clear as to what went wrong
at first.

Property               | Value
---------------------- | --------------
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | true (ByValue)
 
###  -IgnoredArguments [&lt;Object[]&gt;]
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
