# Install-ChocolateyZipPackage

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyZipPackage.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Downloads file from a url and unzips it on your machine. Use
Get-ChocolateyUnzip when local or embedded file.

## Syntax

~~~powershell
Install-ChocolateyZipPackage `
  -PackageName <String> `
  [-Url <String>] `
  -UnzipLocation <String> `
  [-Url64bit <String>] `
  [-SpecificFolder <String>] `
  [-Checksum <String>] `
  [-ChecksumType <String>] `
  [-Checksum64 <String>] `
  [-ChecksumType64 <String>] `
  [-Options <Hashtable>] `
  [-File <String>] `
  [-File64 <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This will download a file from a url and unzip it on your machine.
If you are embedding the file(s) directly in the package (or do not need
to download a file first), use Get-ChocolateyUnzip instead.

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

## Examples

 **EXAMPLE 1**

~~~powershell
Install-ChocolateyZipPackage -PackageName 'gittfs' -Url 'https://github.com/downloads/spraints/git-tfs/GitTfs-0.11.0.zip' -UnzipLocation $gittfsPath

~~~

**EXAMPLE 2**

~~~powershell

Install-ChocolateyZipPackage -PackageName 'sysinternals' `
 -Url 'http://download.sysinternals.com/Files/SysinternalsSuite.zip' `
 -UnzipLocation "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
~~~

**EXAMPLE 3**

~~~powershell

Install-ChocolateyZipPackage -PackageName 'sysinternals' `
 -Url 'http://download.sysinternals.com/Files/SysinternalsSuite.zip' `
 -UnzipLocation "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)" `
 -Url64 'http://download.sysinternals.com/Files/SysinternalsSuitex64.zip'
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -PackageName &lt;String&gt;
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
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
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -UnzipLocation &lt;String&gt;
This is the full path to a location to unzip the contents to, most
likely your script folder. If unzipping to your package folder, the path
will be like
`"$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\\file.exe"`

Property               | Value
---------------------- | -----------
Aliases                | destination
Required?              | true
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
 
###  -SpecificFolder [&lt;String&gt;]
Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
###  -Checksum [&lt;String&gt;]
The checksum hash value of the Url resource. This allows a checksum to
be validated for files that are not local. The checksum type is covered
by ChecksumType.

**NOTE:** Checksums in packages are meant as a measure to validate the
originally intended file that was used in the creation of a package is
the same file that is received at a future date. Since this is used for
other steps in the process related to the [community repository](https://chocolatey.org/packages), it
ensures that the file a user receives is the same file a maintainer
and a moderator (if applicable), plus any moderation review has
intended for you to receive with this package. If you are looking at a
remote source that uses the same url for updates, you will need to
ensure the package also stays updated in line with those remote
resource updates. You should look into [automatic packaging](https://chocolatey.org/docs/automatic-packages)
to help provide that functionality.

**NOTE:** To determine checksums, you can get that from the original
site if provided. You can also use the [checksum tool available on
the [community feed](https://chocolatey.org/packages)](https://chocolatey.org/packages/checksum) (`choco install checksum`)
and use it e.g. `checksum -t sha256 -f path\to\file`. Ensure you
provide checksums for all remote resources used.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
###  -ChecksumType [&lt;String&gt;]
The type of checkum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to 'md5'.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
###  -Checksum64 [&lt;String&gt;]
OPTIONAL if no Url64bit - The checksum hash value of the Url64bit
resource. This allows a checksum to be validated for files that are not
local. The checksum type is covered by ChecksumType64.

**NOTE:** Checksums in packages are meant as a measure to validate the
originally intended file that was used in the creation of a package is
the same file that is received at a future date. Since this is used for
other steps in the process related to the [community repository](https://chocolatey.org/packages), it
ensures that the file a user receives is the same file a maintainer
and a moderator (if applicable), plus any moderation review has
intended for you to receive with this package. If you are looking at a
remote source that uses the same url for updates, you will need to
ensure the package also stays updated in line with those remote
resource updates. You should look into [automatic packaging](https://chocolatey.org/docs/automatic-packages)
to help provide that functionality.

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

The recommendation is to use at least SHA256.

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
 
###  -File [&lt;String&gt;]
Will be used for Url if Url is empty. Available in 0.10.7+.

This parameter provides compatibility, but should not be used directly
and not with the community package repository until January 2018.

Property               | Value
---------------------- | ------------
Aliases                | fileFullPath
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
###  -File64 [&lt;String&gt;]
Will be used for Url64bit if Url64bit is empty. Available in 0.10.7+.

This parameter provides compatibility, but should not be used directly
and not with the community package repository until January 2018.

Property               | Value
---------------------- | --------------
Aliases                | fileFullPath64
Required?              | false
Position?              | named
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

 * [[Get-ChocolateyWebFile|HelpersGetChocolateyWebFile]]
 * [[Get-ChocolateyUnzip|HelpersGetChocolateyUnzip]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyZipPackage -Full`.

View the source for [Install-ChocolateyZipPackage](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyZipPackage.ps1)
