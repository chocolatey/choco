$ErrorActionPreference = 'Stop'

$packagePath = Get-ChocolateyPath -pathType 'PackagePath'
$installPath = Get-ChocolateyPath -pathType 'InstallPath'

Write-Host "Package Path in Uninstall Script: $packagePath"
Write-Host "Install Path in Uninstall Script: $installPath"