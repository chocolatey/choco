
$ErrorActionPreference = 'Stop';
$toolsDir = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"

$packageArgs = @{
    packageName    = $env:ChocolateyPackageName
    destination    = "$toolsDir\extraction"
    file           = "$toolsDir\zip-log-disable-test.zip"
    disableLogging = $true
}

Get-ChocolateyUnzip @packageArgs
