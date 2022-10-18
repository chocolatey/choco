$ErrorActionPreference = 'Stop'

$packagePath = Get-ChocolateyPath -PathType 'PackagePath'
$installPath = Get-ChocolateyPath -PathType 'InstallPath'
$toolsPath   = Get-ChocolateyPath -PathType 'ToolsPath'

Write-Host "Package Path in Install Script: $packagePath"
Write-Host "Install Path in Install Script: $installPath"
Write-Host "Tools Path in Install Script: $toolsPath"