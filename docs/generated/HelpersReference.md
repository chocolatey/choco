# Chocolatey Package Functions aka Helpers Reference

## Main Functions

These functions call other functions and many times may be the only thing you need in your [[chocolateyInstall.ps1 file|ChocolateyInstallPS1]].

* [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
* [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]
* [[Install-ChocolateyPowershellCommand|HelpersInstallChocolateyPowershellCommand]]
* [[Install-ChocolateyVsixPackage|HelpersInstallChocolateyVsixPackage]]

## Error / Success Functions

* [[Write-ChocolateySuccess|HelpersWriteChocolateySuccess]]  - **DEPRECATED**
* [[Write-ChocolateyFailure|HelpersWriteChocolateyFailure]]  - **DEPRECATED**

You really don't need a try catch with chocolatey powershell files anymore.

## More Functions

### Administrative Access Functions

When creating packages that need to run one of the following commands below, one should add the tag `admin` to the nuspec.

* [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
* [[Start-ChocolateyProcessAsAdmin|HelpersStartChocolateyProcessAsAdmin]]
* [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]
* [[Install-ChocolateyPath|HelpersInstallChocolateyPath]] - when specifying machine path
* [[Install-ChocolateyEnvironmentVariable|HelpersInstallChocolateyEnvironmentVariable]] - when specifying machine path
* [[Install-ChocolateyExplorerMenuItem|HelpersInstallChocolateyExplorerMenuItem]]
* [[Install-ChocolateyFileAssociation|HelpersInstallChocolateyFileAssociation]]

### Non-Administrator Safe Functions

Some folks expressed a desire to have Chocolatey not run as administrator to reach continuous integration and developers that are not administrators on their machines.

These are the functions from above as one list.

* [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]
* [[Install-ChocolateyPowershellCommand|HelpersInstallChocolateyPowershellCommand]]
* [[Write-ChocolateySuccess|HelpersWriteChocolateySuccess]]
* [[Write-ChocolateyFailure|HelpersWriteChocolateyFailure]]
* [[Get-ChocolateyWebFile|HelpersGetChocolateyWebFile]]
* [[Get-ChocolateyUnzip|HelpersGetChocolateyUnzip]]
* [[Install-ChocolateyPath|HelpersInstallChocolateyPath]] - when specifying user path
* [[Install-ChocolateyEnvironmentVariable|HelpersInstallChocolateyEnvironmentVariable]] - when specifying user path
* [[Install-ChocolateyDesktopLink|HelpersInstallChocolateyDesktopLink]] - **DEPRECATED** - see [[Install-ChocolateyShortcut|HelpersInstallChocolateyShortcut]]
* [[Install-ChocolateyPinnedTaskBarItem|HelpersInstallChocolateyPinnedTaskBarItem]]
* [[Install-ChocolateyShortcut|HelpersInstallChocolateyShortcut]] - v0.9.9+
* [[Update-SessionEnvironment|HelpersUpdateSessionEnvironment]]

## Complete List (alphabetical order)

* __Get-BinRoot__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-BinRoot.ps1)\]  - this is a horribly named function that doesn't do what new folks think it does. It gets you the `tools` root folder, which by default is set to `c:\tools`, not the `$env:ChocolateyInstall\bin` folder. 

* __Get-CheckSumValid__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-CheckSumValid.ps1)\]

* __Get-ChocolateyUnzip__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-ChocolateyUnzip.ps1)\]

* __Get-ChocolateyWebFile__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-ChocolateyWebFile.ps1)\]

* __Get-EnvironmentVariable__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-EnvironmentVariable.ps1)\]

* __Get-EnvironmentVariableNames__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-EnvironmentVariableNames.ps1)\]
* __Get-FtpFile__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-FtpFile.ps1)\]

* __Get-ProcessorBits__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-ProcessorBits.ps1)\]

* __Get-UACEnabled__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-UACEnabled.ps1)\]

* __Get-VirusCheckValid__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-VirusCheckValid.ps1)\]
:warning: not implemented!

* __Get-WebFile__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-WebFile.ps1)\]

* __Get-WebHeaders__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Get-WebHeaders.ps1)\]

* __Install-ChocolateyDesktopLink__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyDesktopLink.ps1)\]

* __Install-ChocolateyEnvironmentVariable__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyEnvironmentVariable.ps1)\]

* __Install-ChocolateyExplorerMenuItem__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyExplorerMenuItem.ps1)\]

* __Install-ChocolateyFileAssociation__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyFileAssociation.ps1)\]

* __Install-ChocolateyInstallPackage__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyInstallPackage.ps1)\]

* __Install-ChocolateyPackage__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyPackage.ps1)\]

* __Install-ChocolateyPath__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyPath.ps1)\]

* __Install-ChocolateyPinnedTaskBarItem__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyPinnedTaskBarItem.ps1)\]

* __Install-ChocolateyPowershellCommand__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyPowershellCommand.ps1)\]

* __Install-ChocolateyShortcut__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyShortcut.ps1)\]

* __Install-ChocolateyVsixPackage__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyVsixPackage.ps1)\]

* __Install-ChocolateyZipPackage__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Install-ChocolateyZipPackage.ps1)\]

* __Set-EnvironmentVariable__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Set-EnvironmentVariable.ps1)\]

* __Start-ChocolateyProcessAsAdmin__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Start-ChocolateyProcessAsAdmin.ps1)\]

* __Test-ProcessAdminRights__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Test-ProcessAdminRights.ps1)\]

* __Uninstall-ChocolateyPackage__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Uninstall-ChocolateyPackage.ps1)\]

* __Uninstall-ChocolateyZipPackage__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/UnInstall-ChocolateyZipPackage.ps1)\]

* __Update-SessionEnvironment__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Update-SessionEnvironment.ps1)\]

* __Write-ChocolateyFailure__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Write-ChocolateyFailure.ps1)\]

* __Write-ChocolateySuccess__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Write-ChocolateySuccess.ps1)\]

* __Write-FileUpdateLog__ \[[src](https://github.com/chocolatey/choco/blob/master/src/chocolatey.resources/helpers/functions/Write-FileUpdateLog.ps1)\]

## Variables

There are also a number of environment variables providing access to some values from the nuspec and other information that may be useful. They are accessed via `$env:variableName`.

* __chocolateyPackageFolder__ = the folder where Chocolatey has downloaded and extracted the NuGet package, typically `C:\ProgramData\chocolatey\lib\packageName`.
* __chocolateyPackageName__ (since 0.9.9) = The package name, which is equivalent to the `<id>` tag in the nuspec 
* __chocolateyPackageVersion__ (since 0.9.9) = The package version, which is equivalent to the `<version>` tag in the nuspec
 
`chocolateyPackageVersion` may be particularly useful, since that would allow you in some cases to create packages for new releases of the updated software by only changing the `<version>` in the nuspec and not having to touch the `chocolateyInstall.ps1` at all. An example of this:
```
$url = "http://www.thesoftware.com/downloads/thesoftware-$env:chocolateyPackageVersion.zip"

Install-ChocolateyZipPackage '$env:chocolateyPackageName' $url $binRoot
```

Remember, if you want to use `chocolateyPackageName` or `chocolateyPackageVersion` you should declare a dependency on Chocolatey 0.9.9 in the nuspec
```xml
<dependencies>
  <!-- Chocolatey 0.9.9 required in order to access the chocolateyPackageName and chocolateyPackageVersion environment variables -->
  <dependency id="chocolatey" version="0.9.9" />
</dependencies>
```