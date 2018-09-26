# Install-ChocolateyPackage

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyPackage.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

**NOTE:** Administrative Access Required.

Installs software into "Programs and Features" based on a remote file
download. Use Install-ChocolateyInstallPackage when local or embedded
file.

Building packages for an organization or for use internally? You want to
use Install-ChocolateyINSTALLPackage instead of this method (see links
below).

## Syntax

~~~powershell
Install-ChocolateyPackage `
  -PackageName <String> `
  [-FileType <String>] `
  [-SilentArgs <String[]>] `
  [-Url <String>] `
  [-Url64bit <String>] `
  [-ValidExitCodes <Object>] `
  [-Checksum <String>] `
  [-ChecksumType <String>] `
  [-Checksum64 <String>] `
  [-ChecksumType64 <String>] `
  [-Options <Hashtable>] `
  [-File <String>] `
  [-File64 <String>] `
  [-UseOnlyPackageSilentArguments] `
  [-UseOriginalLocation] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This will download a native installer from a url and install it on your
machine. Has error handling built in.

If you are embedding the file(s) directly in the package (or do not need
to download a file first), use Install-ChocolateyInstallPackage instead.

Building packages for an organization or for use internally? You want to
use Install-ChocolateyINSTALLPackage instead of this method
(see links below).

## Notes

This command will assert UAC/Admin privileges on the machine.

This is a native installer wrapper function. A "true" package will
contain all the run time files and not an installer. That could come
pre-zipped and require unzipping in a PowerShell script. Chocolatey
works best when the packages contain the software it is managing. Most
software in the Windows world comes as installers and Chocolatey
understands how to work with that, hence this wrapper function.

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

$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://somewhere.com/file.msi'
$url64      = 'https://somewhere.com/file-x64.msi'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  url           = $url
  url64bit      = $url64
  silentArgs    = "/qn /norestart"
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
  checksum      = '12345'
  checksumType  = 'sha256'
  checksum64    = '123356'
  checksumType64= 'sha256'
}

Install-ChocolateyPackage @packageArgs
~~~

**EXAMPLE 2**

~~~powershell

$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://somewhere.com/file.msi'
$url64      = 'https://somewhere.com/file-x64.msi'


$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  url           = $url
  url64bit      = $url64
  silentArgs    = "/qn /norestart MSIPROPERTY=`"true`""
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
  checksum      = '12345'
  checksumType  = 'sha256'
  checksum64    = '123356'
  checksumType64= 'sha256'
}

Install-ChocolateyPackage @packageArgs
~~~

**EXAMPLE 3**

~~~powershell

$packageName= 'bob'
$toolsDir   = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$url        = 'https://somewhere.com/file.msi'
$url64      = 'https://somewhere.com/file-x64.msi'
$urlTransform = 'https://somewhere.com/file.mst'
$mstFileLocation = Join-Path $toolsDir 'transform.mst'

Get-ChocolateyWebFile -PackageName 'bob' `
                      -Url $urlTransform -FileFullPath $mstFileLocation `
                      -Checksum '1234' -ChecksumType 'sha256'

$packageArgs = @{
  packageName   = $packageName
  fileType      = 'msi'
  file          = $fileLocation
  silentArgs    = "/qn /norestart TRANSFORMS=`"$mstFileLocation`""
  validExitCodes= @(0, 3010, 1641)
  softwareName  = 'Bob*'
}

Install-ChocolateyInstallPackage @packageArgs
~~~

**EXAMPLE 4**

~~~powershell

Install-ChocolateyPackage 'StExBar' 'msi' '/quiet' `
 'http://stexbar.googlecode.com/files/StExBar-1.8.3.msi' `
 'http://stexbar.googlecode.com/files/StExBar64-1.8.3.msi'
~~~

**EXAMPLE 5**

~~~powershell

Install-ChocolateyPackage 'mono' 'exe' '/SILENT' `
 'http://somehwere/something.exe' -ValidExitCodes @(0,21)
~~~

**EXAMPLE 6**

~~~powershell

Install-ChocolateyPackage 'ruby.devkit' 'exe' '/SILENT' `
 'http://cdn.rubyinstaller.org/archives/devkits/DevKit-mingw64-32-4.7.2-20130224-1151-sfx.exe' `
 'http://cdn.rubyinstaller.org/archives/devkits/DevKit-mingw64-64-4.7.2-20130224-1432-sfx.exe' `
 -checksum '9383f12958aafc425923e322460a84de' -checksumType 'md5' `
 -checksum64 'ce99d873c1acc8bffc639bd4e764b849'
~~~

**EXAMPLE 7**

~~~powershell
Install-ChocolateyPackage 'bob' 'exe' '/S' 'https://somewhere/bob.exe' 'https://somewhere/bob-x64.exe'

~~~

**EXAMPLE 8**

~~~powershell

$options =
@{
  Headers = @{
    Accept = 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8';
    'Accept-Charset' = 'ISO-8859-1,utf-8;q=0.7,*;q=0.3';
    'Accept-Language' = 'en-GB,en-US;q=0.8,en;q=0.6';
    Cookie = 'requiredinfo=info';
    Referer = 'https://somelocation.com/';
  }
}

Install-ChocolateyPackage -PackageName 'package' -FileType 'exe' -SilentArgs '/S' 'https://somelocation.com/thefile.exe' -Options $options
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
 
###  -FileType [&lt;String&gt;]
This is the extension of the file. This can be 'exe', 'msi', or 'msu'.
[Licensed editions](https://chocolatey.org/compare) of Chocolatey use this to automatically determine
silent arguments. If this is not provided, Chocolatey will
automatically determine this using the downloaded file's extension.

Property               | Value
---------------------- | --------------------------
Aliases                | installerType, installType
Required?              | false
Position?              | 2
Default Value          | exe
Accept Pipeline Input? | false
 
###  -SilentArgs [&lt;String[]&gt;]
OPTIONAL - These are the parameters to pass to the native installer,
including any arguments to make the installer silent/unattended.
[Licensed editions](https://chocolatey.org/compare) of Chocolatey will automatically determine the
installer type and merge the arguments with what is provided here.

Try any of the to get the silent (unattended) installer -
`/s /S /q /Q /quiet /silent /SILENT /VERYSILENT`. With msi it is always
`/quiet`. Please pass it in still but it will be overridden by
Chocolatey to `/quiet`. If you don't pass anything it could invoke the
installer with out any arguments. That means a nonsilent installer.

Please include the `notSilent` tag in your Chocolatey package if you
are not setting up a silent/unattended package. Please note that if you
are submitting to the [community repository](https://chocolatey.org/packages), it is nearly a requirement
for the package to be completely unattended.

When you are using this with an MSI, it will set up the arguments as
follows:
`"C:\Full\Path\To\msiexec.exe" /i "$downloadedFileFullPath" $silentArgs`,
where `$downloadedfileFullPath` is `$url` or `$url64`, depending on what
has been decided to be used.

When you use this with MSU, it is similar to MSI above in that it finds
the right executable to run.

When you use this with executable installers, the
`$downloadedFileFullPath` will also be `$url`/`$url64` SilentArgs is
everything you call against that file, as in
`"$fileFullPath" $silentArgs"`. An example would be
`"c:\path\setup.exe" /S`, where
`$downloadedfileFullPath = "c:\path\setup.exe"` and `$silentArgs = "/S"`.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
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
Position?              | 4
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
Position?              | 5
Default Value          | 
Accept Pipeline Input? | false
 
###  -ValidExitCodes [&lt;Object&gt;]
Array of exit codes indicating success. Defaults to `@(0)`.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | @(0)
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
 
###  -UseOnlyPackageSilentArguments
Do not allow choco to provide/merge additional silent arguments and only
use the ones available with the package. Available in 0.9.10+.

Property               | Value
---------------------- | ------------------------
Aliases                | useOnlyPackageSilentArgs
Required?              | false
Position?              | named
Default Value          | False
Accept Pipeline Input? | false
 
###  -UseOriginalLocation
Do not download the resources. This is typically passed if Url/Url64bit
are pointed to local files or files on a share and those files should
be used in place. Available in 0.10.1+.

NOTE: You can also use `Install-ChocolateyInstallPackage` for the same
functionality (see links).

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | False
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
 * [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]
 * [[Get-UninstallRegistryKey|HelpersGetUninstallRegistryKey]]
 * [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyPackage -Full`.

View the source for [Install-ChocolateyPackage](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyPackage.ps1)
