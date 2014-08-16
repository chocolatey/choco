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
OPTIONAL (Right now) - 'md5' or 'sha1' - defaults to 'md5'

.PARAMETER ChecksumType64
OPTIONAL (Right now) - 'md5' or 'sha1' - defaults to ChecksumType

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
  [string] $checksumType64 = $checksumType
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

  $bitPackage = 32
  if ($bitWidth -eq 64 -and $url64bit -ne $null -and $url64bit -ne '') {
    Write-Debug "Setting url to '$url64bit' and bitPackage to $bitWidth"
    $bitPackage = $bitWidth
    $url = $url64bit;
    # only set if urls are different
    if ($url32bit -ne $url64bit) {
      $checksum = $checksum64
    }

    $checksumType = $checksumType64
  }

  $forceX86 = $env:chocolateyForceX86;
  if ($forceX86) {
    Write-Debug "User specified -x86 so forcing 32 bit"
    $bitPackage = 32
    $url = $url32bit
    $checksum =  $checksum32
    $checksumType = $checksumType32
  }

  $headers = @{}
  if ($url.StartsWith('http')) {
    try {
      $headers = Get-WebHeaders $url
    } catch {
      if ($host.Version -lt (new-object 'Version' 3,0)) {
        Write-Debug "Converting Security Protocol to SSL3 only for Powershell v2"
        # this should last for the entire duration
        [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Ssl3
        $headers = Get-WebHeaders $url
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
      Write-Host "Downloading $packageName $bitPackage bit
  from `'$url`'"
      Get-WebFile $url $fileFullPath
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

  Write-Debug "Verifying package provided checksum of `'$checksum`' for `'$fileFullPath`'."
  Get-CheckSumValid -file $fileFullPath -checkSum $checksum -checksumType $checksumType

  # Virus check is not able to be performed, must note that.
  # $url is already set properly to the used location.
  #Write-Debug "Verifying downloaded file is not known to contain viruses. FilePath: `'$fileFullPath`'."
  #Get-VirusCheckValid -location $url -file $fileFullPath
}
