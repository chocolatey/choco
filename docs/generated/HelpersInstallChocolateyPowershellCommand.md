# Install-ChocolateyPowershellCommand

This will install a PowerShell script as a command on your system. Like an executable can be run from a batch redirect, this will do the same, calling PowerShell with this command and passing your arguments to it. If you include a url, it will first download the PowerShell file. Has error handling built in. You do not need to surround this with try catch if it is the only thing in your [[chocolateyInstall.ps1|ChocolateyInstallPS1]].

## Usage

```powershell
Install-ChocolateyPowershellCommand $packageName $psFileFullPath $url $url64 `
 -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 `
 -checksumType64 $checksumType64
```

## Examples

```powershell
$psFile = Join-Path $(Split-Path -parent $MyInvocation.MyCommand.Definition) `
 "Install-WindowsImage.ps1"
Install-ChocolateyPowershellCommand 'installwindowsimage.powershell' $psFile `
 'http://somewhere.com/downloads/Install-WindowsImage.ps1'
```

```powershell
$psFile = Join-Path $(Split-Path -parent $MyInvocation.MyCommand.Definition) `
 "Install-WindowsImage.ps1" 
Install-ChocolateyPowershellCommand 'installwindowsimage.powershell' $psFile ` 
 'http://somewhere.com/downloads/Install-WindowsImage.ps1' `
 'http://somewhere.com/downloads/Install-WindowsImagex64.ps1'
```

```powershell
$psFile = Join-Path $(Split-Path -parent $MyInvocation.MyCommand.Definition) `
 "Install-WindowsImage.ps1" 
Install-ChocolateyPowershellCommand 'installwindowsimage.powershell' $psFile
```

## Parameters

* `-packageName

    This is an arbitrary name.

    Example: `'installwindowsimage.powershell'`

* `-psFileFullPath`

    The full path and name of the file to save or an existing file if $url is not included. This should include the name of the file and extension.

    Example: `Join-Path $(Split-Path -parent $MyInvocation.MyCommand.Definition) 'Install-WindowsImage.ps1'`

* `-url` _(optional)_

    The Url to the file.

    Example: `http://somewhere.com/downloads/Install-WindowsImage.ps1`

* `-url64bit` _(optional)_

    If there is a 64 bit installer available, put the link next to the other url. Chocolatey will automatically determine if the user is running a 64bit machine or not and adjust accordingly.

    Example: `'http://somewhere.com/downloads/Install-WindowsImagex64.ps1'`

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

[[Function Reference|HelpersReference]]