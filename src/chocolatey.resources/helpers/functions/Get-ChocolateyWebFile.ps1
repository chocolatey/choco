# Copyright © 2017 Chocolatey Software, Inc.
# Copyright © 2015 - 2017 RealDimensions Software, LLC
# Copyright © 2011 - 2015 RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
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
contains both (which is quite rare), set this to '$url' Otherwise
remove this parameter.

Prefer HTTPS when available. Can be HTTP, FTP, or File URIs.

In 0.10.1+, `Url64` is an alias for Url64bit.

.PARAMETER Checksum
The checksum hash value of the Url resource. This allows a checksum to
be validated for files that are not local. The checksum type is covered
by ChecksumType.

**NOTE:** Checksums in packages are meant as a measure to validate the
originally intended file that was used in the creation of a package is
the same file that is received at a future date. Since this is used for
other steps in the process related to the community repository, it
ensures that the file a user receives is the same file a maintainer
and a moderator (if applicable), plus any moderation review has
intended for you to receive with this package. If you are looking at a
remote source that uses the same url for updates, you will need to
ensure the package also stays updated in line with those remote
resource updates. You should look into [automatic packaging](https://chocolatey.org/docs/automatic-packages)
to help provide that functionality.

.PARAMETER ChecksumType
The type of checkum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to 'md5'.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

.PARAMETER Checksum64
OPTIONAL if no Url64bit - The checksum hash value of the Url64bit
resource. This allows a checksum to be validated for files that are not
local. The checksum type is covered by ChecksumType64.

**NOTE:** Checksums in packages are meant as a measure to validate the
originally intended file that was used in the creation of a package is
the same file that is received at a future date. Since this is used for
other steps in the process related to the community repository, it
ensures that the file a user receives is the same file a maintainer
and a moderator (if applicable), plus any moderation review has
intended for you to receive with this package. If you are looking at a
remote source that uses the same url for updates, you will need to
ensure the package also stays updated in line with those remote
resource updates. You should look into [automatic packaging](https://chocolatey.org/docs/automatic-packages)
to help provide that functionality.

**NOTE:** To determine checksums, you can get that from the original
site if provided. You can also use the [checksum tool available on
the community feed](https://chocolatey.org/packages/checksum) (`choco install checksum`)
and use it e.g. `checksum -t sha256 -f path\to\file`. Ensure you
provide checksums for all remote resources used.

.PARAMETER ChecksumType64
OPTIONAL - The type of checkum that the file is validated with - valid
values are 'md5', 'sha1', 'sha256' or 'sha512' - defaults to
ChecksumType parameter value.

MD5 is not recommended as certain organizations need to use FIPS
compliant algorithms for hashing - see
https://support.microsoft.com/en-us/kb/811833 for more details.

The recommendation is to use at least SHA256.

.PARAMETER Options
OPTIONAL - Specify custom headers. Available in 0.9.10+.

.PARAMETER GetOriginalFileName
OPTIONAL switch to allow Chocolatey to determine the original file name
from the url resource. Available in 0.9.10+.

.PARAMETER ForceDownload
OPTIONAL switch to force download of file every time, even if the file
already exists.

Available in 0.10.1+.

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
  [parameter(Mandatory=$false, Position=3)]
  [alias("url64")][string] $url64bit = '',
  [parameter(Mandatory=$false)][string] $checksum = '',
  [parameter(Mandatory=$false)][string] $checksumType = '',
  [parameter(Mandatory=$false)][string] $checksum64 = '',
  [parameter(Mandatory=$false)][string] $checksumType64 = $checksumType,
  [parameter(Mandatory=$false)][hashtable] $options = @{Headers=@{}},
  [parameter(Mandatory=$false)][switch] $getOriginalFileName,
  [parameter(Mandatory=$false)][switch] $forceDownload,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  # url overrides
  $urlOverride = $env:ChocolateyUrlOverride
  $url64bitOverride = $env:ChocolateyUrl64bitOverride
  if ($urlOverride -ne $null -and $urlOverride -ne '') { $url = $urlOverride }
  if ($url64bitOverride -ne $null -and $url64bitOverride -ne '') { $url64bit = $url64bitOverride }

  if ($url -ne $null) { $url = $url.Replace("//","/").Replace(":/","://") }
  if ($url64bit -ne $null) { $url64bit = $url64bit.Replace("//","/").Replace(":/","://") }

  $url32bit = $url

  # allow user provided values for checksumming
  $checksum32Override = $env:chocolateyChecksum32
  $checksumType32Override = $env:chocolateyChecksumType32
  $checksum64Override = $env:chocolateyChecksum64
  $checksumType64Override = $env:chocolateyChecksumType64
  if ($checksum32Override -ne $null -and $checksum32Override -ne '') { $checksum = $checksum32Override }
  if ($checksumType32Override -ne $null -and $checksumType32Override -ne '') { $checksumType = $checksumType32Override }
  if ($checksum64Override -ne $null -and $checksum64Override -ne '') { $checksum64 = $checksum64Override }
  if ($checksumType64Override -ne $null -and $checksumType64Override -ne '') { $checksumType64 = $checksumType64Override }

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
    $url = $url64bit
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

  # determine if the url can be SSL/TLS
  if ($url.StartsWith('http:')) {
    try {
      $httpsUrl = $url.Replace("http://", "https://")
      Get-WebHeaders -Url $httpsUrl -ErrorAction "Stop" | Out-Null
      $url = $httpsUrl
      Write-Warning "Url has SSL/TLS available, switching to HTTPS for download"
    } catch {
      Write-Debug "Url does not have HTTPS available"
    }
  }

  if ($getOriginalFileName) {
    try {
      $fileFullPath = $fileFullPath -replace '\\chocolatey\\chocolatey\\', '\chocolatey\'
      $fileDirectory = [System.IO.Path]::GetDirectoryName($fileFullPath)
      $originalFileName = [System.IO.Path]::GetFileName($fileFullPath)
      $fileFullPath = Get-WebFileName -Url $url -DefaultName $originalFileName
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

  $urlIsRemote = $true
  $headers = @{}
  if ($url.StartsWith('http')) {
    try {
      $headers = Get-WebHeaders -Url $url -ErrorAction "Stop"
    } catch {
      if ($PSVersionTable.PSVersion -lt (New-Object 'Version' 3,0)) {
        Write-Debug "Converting Security Protocol to SSL3 only for Powershell v2"
        # this should last for the entire duration
        $originalProtocol = [System.Net.ServicePointManager]::SecurityProtocol
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Ssl3
        try {
          $headers = Get-WebHeaders -Url $url -ErrorAction "Stop"
        } catch {
          Write-Host "Attempt to get headers for $url failed.`n  $($_.Exception.Message)"
          [System.Net.ServicePointManager]::SecurityProtocol = $originalProtocol
        }
      } else {
        Write-Host "Attempt to get headers for $url failed.`n  $($_.Exception.Message)"
      }
    }

    $needsDownload = $true
    $fiCached = New-Object System.IO.FileInfo($fileFullPath)
    if ($fiCached.Exists -and -not ($forceDownload)) {
      if ($checksum -ne $null -and $checksum -ne '') {
          try {
            Write-Host "File appears to be downloaded already. Verifying with package checksum to determine if it needs to be redownloaded."
            Get-ChecksumValid -file $fileFullPath -checkSum $checksum -checksumType $checksumType -originalUrl $url -ErrorAction "Stop"
            $needsDownload = $false
          } catch {
            Write-Debug "Existing file failed checksum. Will be redownloaded from url."
          }
      }
      elseif ($headers.Count -ne 0 -and $headers.ContainsKey("Content-Length")) {
        # if the file already exists there is no reason to download it again.
        if ($fiCached.Length -eq $headers["Content-Length"]) { $needsDownload = $false }
      }
    }

    if ($needsDownload) {
      Write-Host "Downloading $packageName $bitPackage
  from `'$url`'"
      Get-WebFile -Url $url -FileName $fileFullPath -Options $options
    } else {
      Write-Debug "$($packageName)'s requested file has already been downloaded. Using cached copy at
 '$fileFullPath'."
    }
  } elseif ($url.StartsWith('ftp')) {
    Write-Host "Ftp-ing $packageName
  from '$url'"
    Get-FtpFile -Url $url -FileName $fileFullPath
  } else {
    if ($url.StartsWith('file:')) { $url = ([uri] $url).LocalPath }
    Write-Host "Copying $packageName
  from `'$url`'"
    Copy-Item $url -Destination $fileFullPath -Force
    $urlIsRemote = $false
  }

  Start-Sleep 2 #give it a sec or two to finish up copying

  $fi = New-Object System.IO.FileInfo($fileFullPath)
  # validate file exists
  if (!($fi.Exists)) { throw "Chocolatey expected a file to be downloaded to `'$fileFullPath`' but nothing exists at that location." }

  Get-VirusCheckValid -Location $url -File $fileFullPath

  if ($headers.Count -ne 0 -and ($checksum -eq $null -or $checksum -eq '')) {
    # validate length is what we expected
    Write-Debug "Checking that '$fileFullPath' is the size we expect it to be."
    if ($headers.ContainsKey("Content-Length") -and ($fi.Length -ne $headers["Content-Length"]))  { throw "Chocolatey expected a file at '$fileFullPath' to be of length '$($headers["Content-Length"])' but the length was '$($fi.Length)'." }

    if ($headers.ContainsKey("X-Checksum-Sha1")) {
      $remoteChecksum = $headers["X-Checksum-Sha1"]
      Write-Debug "Verifying remote checksum of '$remoteChecksum' for '$fileFullPath'."
      Get-ChecksumValid -File $fileFullPath -Checksum $remoteChecksum -ChecksumType 'sha1' -OriginalUrl $url
    }
  }

  #skip requirement for embedded files if checksum is not provided
  if ($urlIsRemote -or ($checksum -ne $null -and $checksum -ne '')) {
    Write-Debug "Verifying package provided checksum of '$checksum' for '$fileFullPath'."
    Get-ChecksumValid -File $fileFullPath -Checksum $checksum -ChecksumType $checksumType -OriginalUrl $url
  }

  return $fileFullPath
}
