# Get-PackageParameters

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-PackageParameters.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Parses a string and returns a hash table array of those values for use
in package scripts.

## Syntax

~~~powershell
Get-PackageParameters `
  [-Parameters <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This looks at a string value and parses it into a hash table array for
use in package scripts. By default this will look at
`$env:ChocolateyPackageParameters` (`--params="'/ITEM:value'"`) and
`$env:ChocolateyPackageParametersSensitive`
(`--package-parameters-sensitive="'/PASSWORD:value'"` in commercial
editions).

Learn more about using this at https://chocolatey.org/docs/how-to-parse-package-parameters-argument

## Notes

Available in 0.10.8+. If you need compatibility with older versions,
take a dependency on the `chocolatey-core.extension` package which
also provides this functionality. If you are pushing to the community
package repository (https://chocolatey.org/packages), you are required
to take a dependency on the core extension until January 2018. How to
do this is explained at https://chocolatey.org/docs/how-to-parse-package-parameters-argument#step-3---use-core-community-extension.

The differences between this and the `chocolatey-core.extension` package
functionality is that the extension function can only do one string at a
time and it only looks at `$env:ChocolateyPackageParameters` by default.
It also only supports splitting by `:`, with this function you can
either split by `:` or `=`. For compatibility with the core extension,
build all docs with `/Item:Value`.

## Aliases

`Get-PackageParametersBuiltIn`


## Examples

 **EXAMPLE 1**

~~~powershell

# The default way of calling, uses `$env:ChocolateyPackageParameters`
# and `$env:ChocolateyPackageParametersSensitive` - this is typically
# how things are passed in from choco.exe
$pp = Get-PackageParameters
~~~

**EXAMPLE 2**

~~~powershell

# see https://chocolatey.org/docs/how-to-parse-package-parameters-argument
# command line call: `choco install <pkg_id> --params "'/LICENSE:value'"`
$pp = Get-PackageParameters
# Read-Host, PromptForChoice, etc are not blocking calls with Chocolatey.
# Chocolatey has a custom PowerShell host that will time these calls
# after 30 seconds, allowing headless operation to continue but offer
# prompts to users to ask questions during installation.
if (!$pp['LICENSE']) { $pp['LICENSE'] = Read-Host 'License key?' }
# set a default if not passed
if (!$pp['LICENSE']) { $pp['LICENSE'] = '1234' }
~~~

**EXAMPLE 3**

~~~powershell

$pp = Get-PackageParameters
if (!$pp['UserName']) { $pp['UserName'] = "$env:UserName" }
# Requires Choocolatey v0.10.8+ for Read-Host -AsSecureString
if (!$pp['Password']) { $pp['Password'] = Read-Host "Enter password for $($pp['UserName']):" -AsSecureString}
# fail the install/upgrade if not value is not determined
if (!$pp['Password']) { throw "Package needs Password to install, that must be provided in params or in prompt." }
~~~

**EXAMPLE 4**

~~~powershell

# Pass in your own values
Get-PackageParameters -Parameters "/Shortcut /InstallDir:'c:\program files\xyz' /NoStartup" | set r
if ($r.Shortcut) {... }
Write-Host $r.InstallDir
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -Parameters [&lt;String&gt;]
OPTIONAL - Specify a string to parse. If not set, will use
`$env:ChocolateyPackageParameters` and
`$env:ChocolateyPackageParametersSensitive` to parse values from.

Parameters should be passed as "/NAME:value" or "/NAME=value". For
compatibility with `chocolatey-core.extension`, use `:`.

For example `-Parameters "/ITEM1:value /ITEM2:value with spaces"

NOTE: In 0.10.9+, to maintain compatibility with the prior art of the
chocolatey-core.extension method, quotes and apostrophes surrounding
parameter values will be removed. When the param is used, those items
can be added back if desired, but it's most important to ensure that
existing packages are compatible on upgrade.

Property               | Value
---------------------- | ------
Aliases                | params
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -IgnoredArguments [&lt;Object[]&gt;]
Allows splatting with arguments that do not apply and future expansion.
Do not use directly.

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
 * [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-PackageParameters -Full`.

View the source for [Get-PackageParameters](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-PackageParameters.ps1)
