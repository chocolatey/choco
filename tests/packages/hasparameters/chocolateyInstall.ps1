<#
    .Synopsis
        Some example script.
#>
[CmdletBinding()]
param(
    [string]$ValidStringParameter,

    $AnotherValidParameter
)

$ErrorActionPreference = 'Stop'
$toolsDir = Split-Path $MyInvocation.MyCommand.Definition -Parent

$packageArgs = @{
    PackageName    = $env:ChocolateyPackageName
    FileFullPath   = Join-Path $toolsDir "some.zip"
    Destination    = $toolsDir
    Checksum       = '3E08376FD0AECA1F851FDE0C08E18CA2D797F6A4C7A449670BF4D1270303C8F6'
    ChecksumType   = 'sha256'
}

Get-ChocolateyUnzip @packageArgs