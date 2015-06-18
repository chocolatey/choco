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

function Install-ChocolateyZipPackage {
<#
.SYNOPSIS
Downloads and unzips a package

.DESCRIPTION
This will download a file from a url and unzip it on your machine.

.PARAMETER PackageName
The name of the package we want to download - this is arbitrary, call it whatever you want.
It's recommended you call it the same as your nuget package id.

.PARAMETER Url
This is the url to download the file from.

.PARAMETER Url64bit
OPTIONAL - If there is an x64 installer to download, please include it here. If not, delete this parameter

.PARAMETER UnzipLocation
This is a location to unzip the contents to, most likely your script folder.

.PARAMETER Checksum
OPTIONAL (Right now) - This allows a checksum to be validated for files that are not local

.PARAMETER Checksum64
OPTIONAL (Right now) - This allows a checksum to be validated for files that are not local

.PARAMETER ChecksumType
OPTIONAL (Right now) - 'md5' or 'sha1' - defaults to 'md5'

.PARAMETER ChecksumType64
OPTIONAL (Right now) - 'md5' or 'sha1' - defaults to ChecksumType

.PARAMETER options
OPTIONAL - Specify custom headers

Example:
-------- 
  $options =
  @{
    Headers = @{
      Accept = 'text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8';
      'Accept-Charset' = 'ISO-8859-1,utf-8;q=0.7,*;q=0.3';
      'Accept-Language' = 'en-GB,en-US;q=0.8,en;q=0.6';
      Cookie = 'products.download.email=ewilde@gmail.com';
      Referer = 'http://submain.com/download/ghostdoc/';
    }
  }

  Get-ChocolateyWebFile 'ghostdoc' 'http://submain.com/download/GhostDoc_v4.0.zip' -options $options

.EXAMPLE
Install-ChocolateyZipPackage '__NAME__' 'URL' "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

.OUTPUTS
None

.NOTES
This helper reduces the number of lines one would have to write to download and unzip a file to 1 line.
This method has error handling built into it.

.LINK
  Get-ChocolateyWebFile
  Get-ChocolateyUnzip
#>
param(
  [string] $packageName,
  [string] $url,
  [string] $unzipLocation,
  [alias("url64")][string] $url64bit = '',
  [string] $specificFolder ="",
  [string] $checksum = '',
  [string] $checksumType = '',
  [string] $checksum64 = '',
  [string] $checksumType64 = '',
  [hashtable] $options = @{Headers=@{}}
)
  Write-Debug "Running 'Install-ChocolateyZipPackage' for $packageName with url:`'$url`', unzipLocation: `'$unzipLocation`', url64bit: `'$url64bit`', specificFolder: `'$specificFolder`', checksum: `'$checksum`', checksumType: `'$checksumType`', checksum64: `'$checksum64`', checksumType64: `'$checksumType64`' ";

  $fileType = 'zip'

  $chocTempDir = Join-Path $env:TEMP "chocolatey"
  $tempDir = Join-Path $chocTempDir "$packageName"
  if (![System.IO.Directory]::Exists($tempDir)) {[System.IO.Directory]::CreateDirectory($tempDir) | Out-Null}
  $file = Join-Path $tempDir "$($packageName)Install.$fileType"
  
  Get-ChocolateyWebFile $packageName $file $url $url64bit -checkSum $checkSum -checksumType $checksumType -checkSum64 $checkSum64 -checksumType64 $checksumType64 -options $options
  Get-ChocolateyUnzip "$file" $unzipLocation $specificFolder $packageName
}
