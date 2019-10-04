# Install-ChocolateyVsixPackage

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyVsixPackage.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Downloads and installs a VSIX package for Visual Studio

## Syntax

~~~powershell
Install-ChocolateyVsixPackage `
  -PackageName <String> `
  [-VsixUrl <String>] `
  [-VsVersion <Int32>] `
  [-Checksum <String>] `
  [-ChecksumType <String>] `
  [-Options <Hashtable>] `
  [-File <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

VSIX packages are Extensions for the Visual Studio IDE. The Visual
Studio Gallery at  http://visualstudiogallery.msdn.microsoft.com/ is the
public extension feed and hosts thousands of extensions. You can locate
a VSIX Url by finding the download link of Visual Studio extensions on
the Visual Studio Gallery.

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

# This downloads the AutoWrockTestable VSIX from the Visual Studio
# Gallery and installs it to the latest version of VS.

Install-ChocolateyVsixPackage -PackageName "MyPackage" `
  -VsixUrl http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix
~~~

**EXAMPLE 2**

~~~powershell

# This downloads the AutoWrockTestable VSIX from the Visual Studio
# Gallery and installs it to Visual Studio 2012 (v11.0).

Install-ChocolateyVsixPackage -PackageName "MyPackage" `
  -VsixUrl http://visualstudiogallery.msdn.microsoft.com/ea3a37c9-1c76-4628-803e-b10a109e7943/file/73131/1/AutoWrockTestable.vsix `
  -VsVersion 11
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -PackageName &lt;String&gt;
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

In 0.10.4+, `Name` is an alias for PackageName.

Property               | Value
---------------------- | -----
Aliases                | name
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -VsixUrl [&lt;String&gt;]
The URL of the package to be installed.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

In 0.10.4+, `Url` is an alias for VsixUrl.

Property               | Value
---------------------- | -----
Aliases                | url
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -VsVersion [&lt;Int32&gt;]
The major version number of Visual Studio where the
package should be installed. This is optional. If not
specified, the most recent Visual Studio installation
will be targetted.

NOTE: For Visual Studio 2015, the VsVersion is 14. It can be determined
by looking at the folders under Program Files / Program Files (x86).

In 0.10.4+, `VisualStudioVersion` is an alias for VsVersion.

Property               | Value
---------------------- | -------------------
Aliases                | visualStudioVersion
Required?              | false
Position?              | 3
Default Value          | 0
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
Will be used for VsixUrl if VsixUrl is empty. Available in 0.10.7+.

This parameter provides compatibility, but should not be used directly
and not with the community package repository until January 2018.

Property               | Value
---------------------- | ------------
Aliases                | fileFullPath
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

 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
 * [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]
 * [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyVsixPackage -Full`.

View the source for [Install-ChocolateyVsixPackage](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyVsixPackage.ps1)
