$ErrorActionPreference = 'Stop'

$packagePath = Get-ChocolateyPath -PathType 'PackagePath'
$installPath = Get-ChocolateyPath -PathType 'InstallPath'

Write-Host "Package Path in Install Script: $packagePath"
Write-Host "Install Path in Install Script: $installPath"