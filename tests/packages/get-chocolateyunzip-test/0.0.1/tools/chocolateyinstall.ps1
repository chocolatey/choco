$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$zipFileLocation = Join-Path $toolsDir "test.zip"

if ((Get-Location) -eq $null)
{
  Write-Warning "Working Directory not set. Setting to '$env:ChocolateyInstall'"
  Set-Location $env:ChocolateyInstall #See https://github.com/chocolatey/choco/issues/1781
}
if ([string]::IsNullOrEmpty((Get-Location)))
{
  Write-Warning "Working Directory is an empty string. Setting to '$env:ChocolateyInstall'"
  Set-Location $env:ChocolateyInstall #See https://github.com/chocolatey/choco/issues/1781
}

Write-Debug "Working Directory is now '$(Get-Location)'"

Get-ChocolateyUnzip -FileFullPath $zipFileLocation -Destination $toolsDir
