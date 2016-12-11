# Get-ChecksumValid

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-ChecksumValid.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Checks a file's checksum versus a passed checksum and checksum type.

## Syntax

~~~powershell
Get-ChecksumValid `
  -File <String> `
  [-Checksum <String>] `
  [-ChecksumType <String>] `
  [-OriginalUrl <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Makes a determination if a file meets an expected checksum signature.
This function is usually used when comparing a file that is downloaded
from an official distribution point. If the checksum fails to match the
expected output, this function throws an error.

Checksums have been used for years as a means of verification. A
checksum hash is a unique value or signature that corresponds to the
contents of a file. File names and extensions can be altered without
changing the checksum signature. However if you changed the contents of
the file, even one character, the checksum will be different.

Checksums are used to provide as a means of cryptographically ensuring
the contents of a file have not been changed. While some cryptographic
algorithms, including MD5 and SHA1, are no longer considered secure
against attack, the goal of a checksum algorithm is to make it
extremely difficult (near impossible with better algorithms) to alter
the contents of a file (whether by accident or for malicious reasons)
and still result in the same checksum signature.

When verifying a checksum using a secure algorithm, if the checksum
matches the expected signature, the contents of the file are identical
to what is expected.

## Notes

This uses the checksum.exe tool available separately at
https://chocolatey.org/packages/checksum.

Options that affect checksum verification:

* `--ignore-checksums` - skips checksumming
* `--allow-empty-checksums` - skips checksumming when the package is missing a checksum
* `--allow-empty-checksums-secure` - skips checksumming when the package is missing a checksum for secure (HTTPS) locations
* `--require-checksums` - requires checksums for both non-secure and secure locations
* `--download-checksum`, `--download-checksum-type` - allows user to pass their own checksums
* `--download-checksum-x64`, `--download-checksum-type-x64` - allows user to pass their own checksums

Features that affect checksum verification:

* `checksumFiles` - when turned off, skips checksumming
* `allowEmptyChecksums` - when turned on, skips checksumming when the package is missing a checksum
* `allowEmptyChecksumsSecure` - when turned on, skips checksumming when the package is missing a checksum for secure (HTTPS) locations

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell
Get-ChecksumValid -File $fileFullPath -CheckSum $checksum -ChecksumType $checksumType

~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -File &lt;String&gt;
The full path to a binary file that is checksummed and compared to the
passed Checksum parameter value.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -Checksum [&lt;String&gt;]
The expected checksum hash value of the File resource. The checksum
type is covered by ChecksumType.

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
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -ChecksumType [&lt;String&gt;]
The type of checkum that the file is validated with - 'md5', 'sha1',
'sha256' or 'sha512' - defaults to 'md5'.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | md5
Accept Pipeline Input? | false
 
###  -OriginalUrl [&lt;String&gt;]
Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 4
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
 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-ChecksumValid -Full`.

View the source for [Get-ChecksumValid](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-ChecksumValid.ps1)
