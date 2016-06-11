﻿# Install-ChocolateyPowershellCommand

Installs a PowerShell Script as a command

## Syntax

~~~powershell
Install-ChocolateyPowershellCommand `
  [-PackageName <String>] `
  -PsFileFullPath <String> `
  [-Url <String>] `
  [-Url64bit <String>] `
  [-Checksum <String>] `
  [-ChecksumType <String>] `
  [-Checksum64 <String>] `
  [-ChecksumType64 <String>] `
  [-Options <Hashtable>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This will install a PowerShell script as a command on your system. Like
an executable can be run from a batch redirect, this will do the same,
calling PowerShell with this command and passing your arguments to it.
If you include a url, it will first download the PowerShell file.

## Notes

Chocolatey works best when the packages contain the software it is
managing and doesn't require downloads. However most software in the
Windows world requires redistribution rights and when sharing packages
publicly (like on the [community feed](https://chocolatey.org/packages)), maintainers may not have those
aforementioned rights. Chocolatey understands how to work with that,
hence this function. You are not subject to this limitation with
internal packages.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -PackageName [&lt;String&gt;]
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -PsFileFullPath &lt;String&gt;
Full file path to PowerShell file to turn into a command. If embedding
it in the package next to the install script, the path will be like
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\Script.ps1"`

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -Url [&lt;String&gt;]
This is the 32 bit url to download the resource from. This resource can
be used on 64 bit systems when a package has both a Url and Url64bit
specified if a user passes `--forceX86`. If there is only a 64 bit url
available, please remove do not use the paramter (only use Url64bit).
Will fail on 32bit systems if missing or if a user attempts to force
a 32 bit installation on a 64 bit system.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -Url64bit [&lt;String&gt;]
OPTIONAL - If there is a 64 bit resource available, use this
parameter. Chocolatey will automatically determine if the user is
running a 64 bit OS or not and adjust accordingly. Please note that
the 32 bit url will be used in the absence of this. This parameter
should only be used for 64 bit native software. If the original Url
contains both (which is quite rare), set this to '$url' Otherwise remove
this parameter.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

Property               | Value
---------------------- | -----
Aliases                | url64
Required?              | false
Position?              | 4
Default Value          | 
Accept Pipeline Input? | false
 
###  -Checksum [&lt;String&gt;]
OPTIONAL (Highly recommended) - The checksum hash value of the Url
resource. This allows a checksum to be validated for files that are not
local. The checksum type is covered by ChecksumType.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
###  -ChecksumType [&lt;String&gt;]
OPTIONAL - The type of checkum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to 'md5'.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
###  -Checksum64 [&lt;String&gt;]
OPTIONAL (Highly recommended) - The checksum hash value of the Url64bit
resource. This allows a checksum to be validated for files that are not
local. The checksum type is covered by ChecksumType64.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
###  -ChecksumType64 [&lt;String&gt;]
OPTIONAL - The type of checkum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to
ChecksumType parameter value.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
###  -Options [&lt;Hashtable&gt;]
OPTIONAL - Specify custom headers. Available in 0.9.10+.

Property               | Value
---------------------- | --------------
Aliases                | 
Required?              | false
Position?              | named
Default Value          | @{Headers=@{}}
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

$psFile = Join-Path $(Split-Path -Parent $MyInvocation.MyCommand.Definition) "Install-WindowsImage.ps1"
Install-ChocolateyPowershellCommand -PackageName 'installwindowsimage.powershell' -PSFileFullPath $psFile
~~~

**EXAMPLE 2**

~~~powershell

$psFile = Join-Path $(Split-Path -Parent $MyInvocation.MyCommand.Definition) `
 "Install-WindowsImage.ps1"
Install-ChocolateyPowershellCommand `
 -PackageName 'installwindowsimage.powershell' `
 -PSFileFullPath $psFile `
 -PSFileFullPath $psFile `
 -Url 'http://somewhere.com/downloads/Install-WindowsImage.ps1'
~~~

**EXAMPLE 3**

~~~powershell

$psFile = Join-Path $(Split-Path -Parent $MyInvocation.MyCommand.Definition) `
 "Install-WindowsImage.ps1"
Install-ChocolateyPowershellCommand `
 -PackageName 'installwindowsimage.powershell' `
 -PSFileFullPath $psFile `
 -Url 'http://somewhere.com/downloads/Install-WindowsImage.ps1' `
 -Url64 'http://somewhere.com/downloads/Install-WindowsImagex64.ps1'
~~~

## Links

 * [[Get-ChocolateyWebFile|HelpersGetChocolateyWebFile]]
 * [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]
 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
 * [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyPowershellCommand -Full`.
