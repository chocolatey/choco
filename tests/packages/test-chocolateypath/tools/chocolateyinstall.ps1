$ErrorActionPreference = 'Stop'

$packagePath = Get-ChocolateyPath -pathType 'PackagePath'
$installPath = Get-ChocolateyPath -pathType 'InstallPath'

Write-Host "Package Path in Install Script: $packagePath"
Write-Host "Install Path in Install Script: $installPath"