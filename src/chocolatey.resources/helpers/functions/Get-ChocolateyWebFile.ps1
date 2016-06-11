﻿# Copyright 2011 - Present RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
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

.NOTES
Chocolatey works best when the packages contain the software it is
managing and doesn't require downloads. However most software in the
Windows world requires redistribution rights and when sharing packages
publicly (like on the community feed), maintainers may not have those
aforementioned rights. Chocolatey understands how to work with that,
hence this function. You are not subject to this limitation with
internal packages.

.PARAMETER PackageName
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

.PARAMETER FileFullPath
This is the full path of the resulting file name.

.PARAMETER Url
This is the 32 bit url to download the resource from. This resource can
be used on 64 bit systems when a package has both a Url and Url64bit
specified if a user passes `--forceX86`. If there is only a 64 bit url
available, please remove do not use the paramter (only use Url64bit).
Will fail on 32bit systems if missing or if a user attempts to force
a 32 bit installation on a 64 bit system.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

.PARAMETER Url64bit
OPTIONAL - If there is a 64 bit resource available, use this
parameter. Chocolatey will automatically determine if the user is
running a 64 bit OS or not and adjust accordingly. Please note that
the 32 bit url will be used in the absence of this. This parameter
should only be used for 64 bit native software. If the original Url
contains both (which is quite rare), set this to '$url' Otherwise remove
this parameter.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

.PARAMETER Checksum
OPTIONAL (Highly recommended) - The checksum hash value of the Url
resource. This allows a checksum to be validated for files that are not
local. The checksum type is covered by ChecksumType.

.PARAMETER ChecksumType
OPTIONAL - The type of checkum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to 'md5'.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

.PARAMETER Checksum64
OPTIONAL (Highly recommended) - The checksum hash value of the Url64bit
resource. This allows a checksum to be validated for files that are not
local. The checksum type is covered by ChecksumType64.

.PARAMETER ChecksumType64
OPTIONAL - The type of checkum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to
ChecksumType parameter value.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

.PARAMETER Options
OPTIONAL - Specify custom headers. Available in 0.9.10+.

.PARAMETER GetOriginalFileName
OPTIONAL switch to allow Chocolatey to determine the original file name
from the url resource.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Get-ChocolateyWebFile '__NAME__' 'C:\somepath\somename.exe' 'URL' '64BIT_URL_DELETE_IF_NO_64BIT'

.EXAMPLE
>
# Download from an HTTPS location
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
Get-ChocolateyWebFile -PackageName 'bob' -FileFullPath "$toolsDir\bob.exe" -Url 'https://somewhere/bob.exe'

.EXAMPLE
>
# Download from FTP
$toolsDir = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
Get-ChocolateyWebFile -PackageName 'bob' -FileFullPath "$toolsDir\bob.exe" -Url 'ftp://somewhere/bob.exe'

.EXAMPLE
>
# Download from a file share
$toolsDir = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
Get-ChocolateyWebFile -PackageName 'bob' -FileFullPath "$toolsDir\bob.exe" -Url 'file:///\\fileshare\location\bob.exe'

.EXAMPLE
>
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

Get-ChocolateyWebFile -PackageName 'package' -FileFullPath "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\thefile.exe" -Url 'https://somelocation.com/thefile.exe' -Options $options

.LINK
Install-ChocolateyPackage

.LINK
Get-WebFile

.LINK
Get-WebFileName

.LINK
Get-FtpFile
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $packageName,
  [parameter(Mandatory=$true, Position=1)][string] $fileFullPath,
  [parameter(Mandatory=$false, Position=2)][string] $url = '',
  [parameter(Mandatory=$false, Position=3)][string] $url64bit = '',
  [parameter(Mandatory=$false)][string] $checksum = '',
  [parameter(Mandatory=$false)][string] $checksumType = '',
  [parameter(Mandatory=$false)][string] $checksum64 = '',
  [parameter(Mandatory=$false)][string] $checksumType64 = $checksumType,
  [parameter(Mandatory=$false)][hashtable] $options = @{Headers=@{}},
  [parameter(Mandatory=$false)][switch] $getOriginalFileName,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
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

  # If we're on 32 bit or attempting to force 32 bit and there is no
  # 32 bit url, we need to throw an error.
  if ($url -eq $null -or $url -eq '') {
    throw "This package does not support $bitWidth bit architecture."
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
