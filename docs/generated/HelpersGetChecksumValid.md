# Get-ChecksumValid

Checks a file's checksum versus a passed checksum and checksum type.

## Syntax

~~~powershell
Get-ChecksumValid `
  -File <String> `
  [-Checksum <String>] `
  [-ChecksumType <String>] [<CommonParameters>]
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

## Inputs

None

## Outputs

None

## Parameters

###  -File \<String\>
The full path to a binary file that is checksummed and compared to the
passed Checksum parameter value.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -Checksum [\<String\>]
The expected checksum hash value of the File resource. The checksum
type is covered by ChecksumType.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -ChecksumType [\<String\>]
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
 
### \<CommonParameters\>

This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see `about_CommonParameters` http://go.microsoft.com/fwlink/p/?LinkID=113216 .


## Examples

 **EXAMPLE 1**

~~~powershell
Get-CheckSumValid -File $fileFullPath -CheckSum $checksum -ChecksumType $checksumType

~~~

## Links

 * [[Get-ChocolateyWebFile|HelpersGetChocolateyWebFile]]
 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-ChecksumValid -Full`.
