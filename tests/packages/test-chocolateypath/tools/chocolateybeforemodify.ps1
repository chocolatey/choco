$ErrorActionPreference = 'Stop'

$packagePath = Get-ChocolateyPath -PathType 'PackagePath'
$installPath = Get-ChocolateyPath -PathType 'InstallPath'

Write-Host "Package Path in Before Modify Script: $packagePath"
Write-Host "Install Path in Before Modify Script: $installPath"