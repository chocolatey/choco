# Copyright 2011 - Present RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Install-ChocolateyPowershellCommand {
param(
  [string] $packageName,
  [string] $psFileFullPath,
  [string] $url ='',
  [alias("url64")][string] $url64bit = '',
  [string] $checksum = '',
  [string] $checksumType = '',
  [string] $checksum64 = '',
  [string] $checksumType64 = ''
)
  Write-Debug "Running 'Install-ChocolateyPowershellCommand' for $packageName with psFileFullPath:`'$psFileFullPath`', url: `'$url`', url64bit:`'$url64bit`', checkSum: `'$checksum`', checksumType: `'$checksumType`', checkSum64: `'$checksum64`', checksumType64: `'$checksumType64`' ";
  
  if ($url -ne '') {
  Get-ChocolateyWebFile $packageName $psFileFullPath $url $url64bit -checksum $checksum -checksumType $checksumType -checksum64 $checksum64 -checksumType64 $checksumType64
  }

  if ($env:chocolateyPackageName -ne $null -and $env:chocolateyPackageName -eq $env:ChocolateyInstallDirectoryPackage) {
    Write-Warning "Install Directory override not available for PowerShell command packages."
  }
  
  $nugetPath = $(Split-Path -parent $helpersPath)
  $nugetExePath = Join-Path $nuGetPath 'bin'
  
  $cmdName = [System.IO.Path]::GetFileNameWithoutExtension($psFileFullPath)
  $packageBatchFileName = Join-Path $nugetExePath "$($cmdName).bat"
  
  Write-Host "Adding $packageBatchFileName and pointing it to powershell command $psFileFullPath"
"@echo off
powershell -NoProfile -ExecutionPolicy unrestricted -Command ""& `'$psFileFullPath`'  %*"""| Out-File $packageBatchFileName -encoding ASCII

}
