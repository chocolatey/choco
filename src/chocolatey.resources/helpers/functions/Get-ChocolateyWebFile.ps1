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

function Get-ChocolateyWebFile {
<#
.SYNOPSIS
Downloads a file from the internets.

.DESCRIPTION
This will download a file from a url, tracking with a progress bar.
It returns the filepath to the downloaded file when it is complete.

.PARAMETER PackageName
The name of the package we want to download - this is arbitrary, call it whatever you want.
It's recommended you call it the same as your nuget package id.

.PARAMETER FileFullPath
This is the full path of the resulting file name.

.PARAMETER Url
This is the url to download the file from.

.PARAMETER Url64bit
OPTIONAL - If there is an x64 installer to download, please include it here. If not, delete this parameter

.PARAMETER Checksum
OPTIONAL (Right now) - This allows a checksum to be validated for files that are not local

.PARAMETER Checksum64
OPTIONAL (Right now) - This allows a checksum to be validated for files that are not local

.PARAMETER ChecksumType
OPTIONAL (Right now) - 'md5', 'sha1', 'sha256' or 'sha512' - defaults to 'md5'

.PARAMETER ChecksumType64
OPTIONAL (Right now) - 'md5', 'sha1', 'sha256' or 'sha512' - defaults to ChecksumType

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
      Cookie = 'requiredinfo=info';
      Referer = 'https://somelocation.com/';
    }
  }
  
  Get-ChocolateyWebFile 'package' 'https://somelocation.com/thefile.exe' -options $options

.PARAMETER GetOriginalFileName
OPTIONAL switch to allow Chocolatey to determine the original file name from the url
  
.EXAMPLE
Get-ChocolateyWebFile '__NAME__' 'C:\somepath\somename.exe' 'URL' '64BIT_URL_DELETE_IF_NO_64BIT'

.NOTES
This helper reduces the number of lines one would have to write to download a file to 1 line.
There is no error handling built into this method.

.LINK
Install-ChocolateyPackage
#>
param(
  [string] $packageName,
  [string] $fileFullPath,
  [string] $url,
  [string] $url64bit = '',
  [string] $checksum = '',
  [string] $checksumType = '',
  [string] $checksum64 = '',
  [string] $checksumType64 = $checksumType,
  [hashtable] $options = @{Headers=@{}},
  [switch]$getOriginalFileName
)
  Write-Debug "Running 'Get-ChocolateyWebFile' for $packageName with url:`'$url`', fileFullPath:`'$fileFullPath`', url64bit:`'$url64bit`', checksum: `'$checksum`', checksumType: `'$checksumType`', checksum64: `'$checksum64`', checksumType64: `'$checksumType64`'";

  $url32bit = $url;
  $checksum32 = $checksum
  $checksumType32 = $checksumType
  $bitWidth = 32
  if (Get-ProcessorBits 64) {
    $bitWidth = 64
  }
  Write-Debug "CPU is $bitWidth bit"

  $bitPackage = ''
  if ($url32bit -ne $url64bit -and $url64bit -ne $null -and $url64bit -ne '') { $bitPackage = '32 bit' }

  if ($bitWidth -eq 64 -and $url64bit -ne $null -and $url64bit -ne '') {
    Write-Debug "Setting url to '$url64bit' and bitPackage to $bitWidth"
    $bitPackage = '64 bit'
    $url = $url64bit;
    # only set if urls are different
    if ($url32bit -ne $url64bit) {
      $checksum = $checksum64
      if ($checkSumType64 -ne '') {
        $checksumType = $checksumType64
      }
    }
  }

  $forceX86 = $env:chocolateyForceX86;
  if ($forceX86) {
    Write-Debug "User specified -x86 so forcing 32 bit"
    if ($url32bit -ne $url64bit) { $bitPackage = '32 bit' }
    $url = $url32bit
    $checksum =  $checksum32
    $checksumType = $checksumType32
  }

  if ($getOriginalFileName) {
    try {
      $fileDirectory = [System.IO.Path]::GetDirectoryName($fileFullPath)
      $originalFileName = [System.IO.Path]::GetFileName($fileFullPath)
      $fileFullPath = Get-WebFileName -url $url -defaultName $originalFileName
      $fileFullPath = Join-Path $fileDirectory $fileFullPath
      $fileFullPath = [System.IO.Path]::GetFullPath($fileFullPath)
    } catch {
      Write-Host "Attempt to use original download file name failed for '$url'."
    }
  }

  try {
    $fileDirectory = $([System.IO.Path]::GetDirectoryName($fileFullPath))
    if (!(Test-Path($fileDirectory))) {
      [System.IO.Directory]::CreateDirectory($fileDirectory) | Out-Null
    }
  } catch {
    Write-Host "Attempt to create directory failed for '$fileFullPath'."
  }

  $headers = @{}
  if ($url.StartsWith('http')) {
    try {
      $headers = Get-WebHeaders $url -ErrorAction "Stop"
    } catch {
      if ($host.Version -lt (new-object 'Version' 3,0)) {
        Write-Debug "Converting Security Protocol to SSL3 only for Powershell v2"
        # this should last for the entire duration
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Ssl3
        try {
          $headers = Get-WebHeaders $url -ErrorAction "Stop"
        } catch {
          Write-Host "Attempt to get headers for $url failed.`n  $($_.Exception.Message)"
        }
      } else {
        Write-Host "Attempt to get headers for $url failed.`n  $($_.Exception.Message)"
      }
    }

    $needsDownload = $true
    if ($headers.Count -ne 0 -and $headers.ContainsKey("Content-Length")) {
      $fi = new-object System.IO.FileInfo($fileFullPath)
      # if the file already exists there is no reason to download it again.
      if ($fi.Exists -and $fi.Length -eq $headers["Content-Length"]) {
        Write-Debug "$($packageName)'s requested file has already been downloaded. Using cached copy at
  `'$fileFullPath`'."
        $needsDownload = $false
      }
    }

    if ($needsDownload) {
      Write-Host "Downloading $packageName $bitPackage
  from `'$url`'"
      Get-WebFile $url $fileFullPath -options $options
    }
  } elseif ($url.StartsWith('ftp')) {
    Write-Host "Ftp-ing $packageName
  from `'$url`'"
    Get-FtpFile $url $fileFullPath
  } else {
    if ($url.StartsWith('file:')) { $url = ([uri] $url).LocalPath }
    Write-Host "Copying $packageName
  from `'$url`'"
    Copy-Item $url -Destination $fileFullPath -Force
  }

  Start-Sleep 2 #give it a sec or two to finish up copying
  
  $fi = new-object System.IO.FileInfo($fileFullPath)
  # validate file exists
  if (!($fi.Exists)) { throw "Chocolatey expected a file to be downloaded to `'$fileFullPath`' but nothing exists at that location." }

  Get-VirusCheckValid -location $url -file $fileFullPath

  if ($headers.Count -ne 0) {
    # validate length is what we expected
    Write-Debug "Checking that `'$fileFullPath`' is the size we expect it to be."
    if ($headers.ContainsKey("Content-Length") -and ($fi.Length -ne $headers["Content-Length"]))  { throw "Chocolatey expected a file at `'$fileFullPath`' to be of length `'$($headers["Content-Length"])`' but the length was `'$($fi.Length)`'." }

    if ($headers.ContainsKey("X-Checksum-Sha1")) {
      $remoteChecksum = $headers["X-Checksum-Sha1"]
      Write-Debug "Verifying remote checksum of `'$remoteChecksum`' for `'$fileFullPath`'."
      Get-CheckSumValid -file $fileFullPath -checkSum $remoteChecksum -checksumType 'sha1'
    }
  }

  Write-Debug "Verifying package provided checksum of '$checksum' for '$fileFullPath'."
  Get-CheckSumValid -file $fileFullPath -checkSum $checksum -checksumType $checksumType

  return $fileFullPath
}
