$ErrorActionPreference = 'Stop'

$packagePath = Get-ChocolateyPath -pathType 'PackagePath'
$installPath = Get-ChocolateyPath -pathType 'InstallPath'

Write-Host "Package Path in Before Modify Script: $packagePath"
Write-Host "Install Path in Before Modify Script: $installPath"