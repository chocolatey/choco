# Install-ChocolateyPackage

This will download a native installer from a url and install it on your machine. Has error handling built in. You do not need to surround this with try catch if it is the only thing in your [[chocolateyInstall.ps1|ChocolateyInstallPS1]].

**NOTE:** This command will assert UAC/Admin privileges on the machine.

## Usage

```powershell
Install-ChocolateyPackage $packageName $installerType $silentArgs $url $url64bit `
 -validExitCodes $validExitCodes -checksum $checksum -checksumType $checksumType `
 -checksum64 $checksum64 -checksumType64 $checksumType64
```

## Examples

```powershell
Install-ChocolateyPackage 'StExBar' 'msi' '/quiet' ` 
 'http://stexbar.googlecode.com/files/StExBar-1.8.3.msi' `
 'http://stexbar.googlecode.com/files/StExBar64-1.8.3.msi'

Install-ChocolateyPackage 'mono' 'exe' '/SILENT' `
 'http://ftp.novell.com/pub/mono/archive/2.10.2/windows-installer/5/mono-2.10.2-gtksharp-2.12.10-win32-5.exe'

Install-ChocolateyPackage 'mono' 'exe' '/SILENT' ` 
 'http://somehwere/something.exe' -validExitCodes @(0,21)

Install-ChocolateyPackage 'ruby.devkit' 'exe' '/SILENT' `
 'http://cdn.rubyinstaller.org/archives/devkits/DevKit-mingw64-32-4.7.2-20130224-1151-sfx.exe' `
 'http://cdn.rubyinstaller.org/archives/devkits/DevKit-mingw64-64-4.7.2-20130224-1432-sfx.exe' `
 -checksum '9383f12958aafc425923e322460a84de' -checksumType = 'md5' `
 -checksum64 'ce99d873c1acc8bffc639bd4e764b849'
```

## Parameters

* `-packageName`

    This is an arbitrary name.

    Example: `'7zip'`

* `-installerType`

    Pick only  one to leave here.

    Example: `'exe'` or `'msi'` or `'msu'`

* `-silentArgs`

    Silent and other arguments to pass to the native installer.

    Example: `'/S'`

    If there are no silent arguments, pass this as `''`

* `-url`

    The Url to the native installer.

    Example: `'http://stexbar.googlecode.com/files/StExBar-1.8.3.msi'`

* `-url64bit` _(optional)_

    If there is a 64 bit installer available, put the link next to the other url. Chocolatey will automatically determine if the user is running a 64bit machine or not and adjust accordingly.

    Example: `'http://stexbar.googlecode.com/files/StExBar64-1.8.3.msi'`

    Defaults to the 32bit url.

* `-validExitCodes` _(optional)_

    If there are other valid exit codes besides zero signifying a successful install, please pass `-validExitCodes` with the value, including 0 as long as it is still valid.

    Example: `-validExitCodes @(0,44)`

    Defaults to `@(0)`.

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

## See Also

* [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]] for installing a zip package.
* To add executables to the path see [[Get-ChocolateyBins|HelpersGetChocolateyBins]]

[[Function Reference|HelpersReference]]