$ErrorActionPreference = 'Stop'

$packagePath = Get-ChocolateyPath -PathType 'PackagePath'
$installPath = Get-ChocolateyPath -PathType 'InstallPath'
$toolsPath   = Get-ChocolateyPath -PathType 'ToolsPath'

Write-Host "Package Path in Before Modify Script: $packagePath"
Write-Host "Install Path in Before Modify Script: $installPath"
Write-Host "Tools Path in Before Modify Script: $toolsPath"