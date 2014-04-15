function Install-ChocolateyPowershellCommand {
param(
  [string] $packageName,
  [string] $psFileFullPath,
  [string] $url ='',
  [string] $url64bit = '',
  [string] $checksum = '',
  [string] $checksumType = '',
  [string] $checksum64 = '',
  [string] $checksumType64 = ''
)
  Write-Debug "Running 'Install-ChocolateyPowershellCommand' for $packageName with psFileFullPath:`'$psFileFullPath`', url: `'$url`', url64bit:`'$url64bit`', checkSum: `'$checksum`', checksumType: `'$checksumType`', checkSum64: `'$checksum64`', checksumType64: `'$checksumType64`' ";

  try {

    if ($url -ne '') {
      Get-ChocolateyWebFile $packageName $psFileFullPath $url $url64bit -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 -checksumType64 $checksumType64
    }

    $nugetPath = $(Split-Path -parent $(Split-Path -parent $helpersPath))
    $nugetExePath = Join-Path $nuGetPath 'bin'

    $cmdName = [System.IO.Path]::GetFileNameWithoutExtension($psFileFullPath)
    $packageBatchFileName = Join-Path $nugetExePath "$($cmdName).bat"

    Write-Host "Adding $packageBatchFileName and pointing it to powershell command $psFileFullPath"
"@echo off
powershell -NoProfile -ExecutionPolicy unrestricted -Command ""& `'$psFileFullPath`'  %*"""| Out-File $packageBatchFileName -encoding ASCII

    Write-ChocolateySuccess $packageName
  } catch {
    Write-ChocolateyFailure $packageName $($_.Exception.Message)
    throw
  }
}
