# PowerShell Functions aka Helpers Reference

<!-- This documentation file is automatically generated from the files at $sourceFunctions using $($sourceLocation)GenerateDocs.ps1. Contributions are welcome at the original location(s). -->
## Main Functions

These functions call other functions and many times may be the only thing you need in your [[chocolateyInstall.ps1 file|ChocolateyInstallPS1]].

* [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
* [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]
* [[Install-ChocolateyPowershellCommand|HelpersInstallChocolateyPowershellCommand]]
* [[Install-ChocolateyVsixPackage|HelpersInstallChocolateyVsixPackage]]

## Error / Success Functions

* [[Write-ChocolateySuccess|HelpersWriteChocolateySuccess]] - **DEPRECATED**
* [[Write-ChocolateyFailure|HelpersWriteChocolateyFailure]] - **DEPRECATED**

You really don't need a try catch with Chocolatey PowerShell files anymore.

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

When you have a need to run Chocolatey without Administrative access required (non-default install location), you can run the following functions without administrative access.

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

 * [[Format-FileSize|HelpersFormatFileSize]]
 * [[Get-ChecksumValid|HelpersGetChecksumValid]]
 * [[Get-ChocolateyUnzip|HelpersGetChocolateyUnzip]]
 * [[Get-ChocolateyWebFile|HelpersGetChocolateyWebFile]]
 * [[Get-EnvironmentVariable|HelpersGetEnvironmentVariable]]
 * [[Get-EnvironmentVariableNames|HelpersGetEnvironmentVariableNames]]
 * [[Get-FtpFile|HelpersGetFtpFile]]
 * [[Get-OSArchitectureWidth|HelpersGetOSArchitectureWidth]]
 * [[Get-PackageParameters|HelpersGetPackageParameters]]
 * [[Get-ToolsLocation|HelpersGetToolsLocation]]
 * [[Get-UACEnabled|HelpersGetUACEnabled]]
 * [[Get-UninstallRegistryKey|HelpersGetUninstallRegistryKey]]
 * [[Get-VirusCheckValid|HelpersGetVirusCheckValid]]
 * [[Get-WebFile|HelpersGetWebFile]]
 * [[Get-WebFileName|HelpersGetWebFileName]]
 * [[Get-WebHeaders|HelpersGetWebHeaders]]
 * [[Install-BinFile|HelpersInstallBinFile]]
 * [[Install-ChocolateyDesktopLink|HelpersInstallChocolateyDesktopLink]]
 * [[Install-ChocolateyEnvironmentVariable|HelpersInstallChocolateyEnvironmentVariable]]
 * [[Install-ChocolateyExplorerMenuItem|HelpersInstallChocolateyExplorerMenuItem]]
 * [[Install-ChocolateyFileAssociation|HelpersInstallChocolateyFileAssociation]]
 * [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]
 * [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
 * [[Install-ChocolateyPath|HelpersInstallChocolateyPath]]
 * [[Install-ChocolateyPinnedTaskBarItem|HelpersInstallChocolateyPinnedTaskBarItem]]
 * [[Install-ChocolateyPowershellCommand|HelpersInstallChocolateyPowershellCommand]]
 * [[Install-ChocolateyShortcut|HelpersInstallChocolateyShortcut]]
 * [[Install-ChocolateyVsixPackage|HelpersInstallChocolateyVsixPackage]]
 * [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]
 * [[Install-Vsix|HelpersInstallVsix]]
 * [[Set-EnvironmentVariable|HelpersSetEnvironmentVariable]]
 * [[Set-PowerShellExitCode|HelpersSetPowerShellExitCode]]
 * [[Start-ChocolateyProcessAsAdmin|HelpersStartChocolateyProcessAsAdmin]]
 * [[Test-ProcessAdminRights|HelpersTestProcessAdminRights]]
 * [[Uninstall-BinFile|HelpersUninstallBinFile]]
 * [[Uninstall-ChocolateyEnvironmentVariable|HelpersUninstallChocolateyEnvironmentVariable]]
 * [[Uninstall-ChocolateyPackage|HelpersUninstallChocolateyPackage]]
 * [[Uninstall-ChocolateyZipPackage|HelpersUninstallChocolateyZipPackage]]
 * [[Update-SessionEnvironment|HelpersUpdateSessionEnvironment]]
 * [[Write-ChocolateyFailure|HelpersWriteChocolateyFailure]]
 * [[Write-ChocolateySuccess|HelpersWriteChocolateySuccess]]
 * [[Write-FileUpdateLog|HelpersWriteFileUpdateLog]]
 * [[Write-FunctionCallLogMessage|HelpersWriteFunctionCallLogMessage]]

## Variables

There are also a number of environment variables providing access to some values from the nuspec and other information that may be useful. They are accessed via `$env:variableName`.

* __chocolateyPackageFolder__ = the folder where Chocolatey has downloaded and extracted the NuGet package, typically `C:\ProgramData\chocolatey\lib\packageName`.
* __chocolateyPackageName__ (since 0.9.9.0) = The package name, which is equivalent to the `<id>` tag in the nuspec
* __chocolateyPackageVersion__ (since 0.9.9.0) = The package version, which is equivalent to the `<version>` tag in the nuspec

`chocolateyPackageVersion` may be particularly useful, since that would allow you in some cases to create packages for new releases of the updated software by only changing the `<version>` in the nuspec and not having to touch the `chocolateyInstall.ps1` at all. An example of this:

~~~powershell
$url = "http://www.thesoftware.com/downloads/thesoftware-$env:chocolateyPackageVersion.zip"

Install-ChocolateyZipPackage '$env:chocolateyPackageName' $url $binRoot
~~~
