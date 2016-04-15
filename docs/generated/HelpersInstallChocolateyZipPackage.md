# Install-ChocolateyZipPackage

This will download a file from a url and unzip it on your machine. Has error handling built in. You do not need to surround this with try catch if it is the only thing in your [[chocolateyInstall.ps1|ChocolateyInstallPS1]].

## Usage

```powershell
Install-ChocolateyZipPackage $packageName $url $unzipLocation $url64 -checksum `
 $checksum -checksumType $checksumType -checksum64 $checksum64 ` 
 -checksumType64 $checksumType64
```

## Examples

```powershell
Install-ChocolateyZipPackage 'gittfs' `
 'https://github.com/downloads/spraints/git-tfs/GitTfs-0.11.0.zip' $gittfsPath

Install-ChocolateyZipPackage 'sysinternals' `
 'http://download.sysinternals.com/Files/SysinternalsSuite.zip' `
 "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

Install-ChocolateyZipPackage 'sysinternals' `
 'http://download.sysinternals.com/Files/SysinternalsSuite.zip' `
 "$(Split-Path -parent $MyInvocation.MyCommand.Definition)" `
 'http://download.sysinternals.com/Files/SysinternalsSuitex64.zip'
```

## Parameters

* `-packageName`

    This is an arbitrary name.

    Example: `'7zip'`

* `-url`

    The Url to the zip file.

    Example: `'https://github.com/downloads/spraints/git-tfs/GitTfs-0.11.0.zip'`

* `-unzipLocation`

    Where to unzip contents of the downloaded zip file.

    Example: `"$(Split-Path -parent $MyInvocation.MyCommand.Definition)"` - will install it to the tools folder of your package.

* `-url64bit` _(optional)_

    If there is a 64 bit installer available, put the link next to the other url. Chocolatey will automatically determine if the user is running a 64bit machine or not and adjust accordingly.

    Example: `'http://stexbar.googlecode.com/files/StExBar64-1.8.3.zip'`

    Defaults to the 32bit url.

* `-checksum` _(optional)_

    This allows the file being downloaded to be validated. Can be an MD5 or SHA1 hash.

    Example: `-checksum 'C67962F064924F3C7B95D69F88E745C0'`

    Defaults to ``.

* `-checksumType` _(optional)_

    This allows the file being downloaded to be validated. Can be an MD5 or SHA1 hash.

    Example: `-checksumType 'sha1'`

    Defaults to `md5`.

* `-checksum64` _(optional)_

    This allows the x64 file being downloaded to be validated. Can be an MD5 or SHA1 hash.

    Example: `-checksum64 'C67962F064924F3C7B95D69F88E745C0'`

    Defaults to ``.

* `-checksumType64` _(optional)_

    This allows the file being downloaded to be validated. Can be an MD5 or SHA1 hash.

    Example: `-checksumType64 'sha1'`

    Defaults to checksumType's value.

[[Helper Reference|HelpersReference]]