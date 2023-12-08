
$ErrorActionPreference = 'Stop';
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  destination   = "$toolsDir\extraction"
  file          = "$toolsDir\zip-log-disable-test.zip"
  disableLogging= $true
}

Get-ChocolateyUnzip @packageArgs
