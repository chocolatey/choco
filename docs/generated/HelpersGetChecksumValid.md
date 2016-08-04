# Get-ChecksumValid

Checks a file's checksum versus a passed checksum and checksum type.

## Syntax

~~~powershell
Get-ChecksumValid `
  -File <String> `
  [-Checksum <String>] `
  [-ChecksumType <String>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Makes a determination if a file meets an expected checksum. This
function is usually used when comparing a file that is downloaded from
an official distribution point. If the checksum fails to
match, this function throws an error.

## Notes

This uses the checksum.exe tool available separately at
https://chocolatey.org/packages/checksum.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell
Get-CheckSumValid -File $fileFullPath -CheckSum $checksum -ChecksumType $checksumType

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

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | md5
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
