$ErrorActionPreference = 'Stop'

$packagePath = Get-ChocolateyPath -PathType 'PackagePath'
$installPath = Get-ChocolateyPath -PathType 'InstallPath'
$toolsPath   = Get-ChocolateyPath -PathType 'ToolsPath'

Write-Host "Package Path in Uninstall Script: $packagePath"
Write-Host "Install Path in Uninstall Script: $installPath"
Write-Host "Tools Path in Uninstall Script: $toolsPath"