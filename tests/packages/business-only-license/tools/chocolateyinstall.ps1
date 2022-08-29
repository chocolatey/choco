$ErrorActionPreference = 'Stop'

$LicensedCommandsRegistered = Get-Command "Invoke-ValidateChocolateyLicense" -EA SilentlyContinue
if (!$LicensedCommandsRegistered) {
  Write-Warning "Package Requires Commercial License - Installation cannot continue as Package Builder use require endpoints to be licensed with Chocolatey Licensed Extension v3.0.0+ (chocolatey.extension). Please see error below for details and correction instructions."
  throw "This package requires a commercial edition of Chocolatey as it was built/internalized with commercial features. Please install the license and install/upgrade to Chocolatey Licensed Extension v3.0.0+ as per https://docs.chocolatey.org/en-us/licensed-extension/setup."
}

Invoke-ValidateChocolateyLicense -RequiredLicenseTypes @('Business')

$toolsDir     = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$fileLocation = Join-Path $toolsDir '7z1900-x64.exe'

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  softwareName  = '7-Zip*'
  file          = $fileLocation
  fileType      = 'exe'
  silentArgs    = "/S"
  
  validExitCodes= @(0)
  url           = ""
  checksum      = '0F5D4DBBE5E55B7AA31B91E5925ED901FDF46A367491D81381846F05AD54C45E'
  checksumType  = 'sha256'
  url64bit      = ""
  checksum64    = ''
  checksumType64= 'sha256'
  destination   = $toolsDir
}

Install-ChocolateyInstallPackage @packageArgs
