$ErrorActionPreference = 'Stop'

$toolsDir = Split-Path $MyInvocation.MyCommand.Definition
$filePath = Join-Path $toolsDir "shimwithbinfile1.bat"

Install-Binfile "shimwithbinfile1" "$filePath"
Write-Output "$env:PackageName $env:PackageVersion Installed"
