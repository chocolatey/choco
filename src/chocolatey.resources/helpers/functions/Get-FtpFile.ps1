﻿## Get-FtpFile
##############################################################################################################
## Downloads a file from ftp
## Some code from http://stackoverflow.com/questions/265339/whats-the-best-way-to-automate-secure-ftp-in-powershell
## Additional functionality emulated from http://poshcode.org/417 (Get-WebFile)
## Written by Stephen C. Austin, Pwnt & Co. http://pwnt.co
##############################################################################################################
## Additional functionality added by Chocolatey Team / Chocolatey Contributors
##  - Proxy
##  - Better error handling
##  - Inline documentation
##  - Cmdlet conversion
##  - Closing request/response and cleanup
##  - Request / ReadWriteResponse Timeouts
##############################################################################################################
function Get-FtpFile {
<#
.SYNOPSIS
Downloads a file from a File Transfter Protocol (FTP) location.

.DESCRIPTION
This will download a file from an FTP location, saving the file to the
FileName location specified.

.NOTES
This is a low-level function and not recommended for use in package
scripts. It is recommended you call `Get-ChocolateyWebFile` instead.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Url
This is the url to download the file from.

.PARAMETER FileName
This is the full path to the file to create. If FTPing to the
package folder next to the install script, the path will be like
`"$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\\file.exe"`

.PARAMETER UserName
The user account to connect to FTP with.

.PARAMETER Password
The password for the user account on the FTP server.

.PARAMETER Quiet
Silences the progress output.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.LINK
Get-ChocolateyWebFile

.LINK
Get-WebFile
#>
param(
  [parameter(Mandatory=$false, Position=0)][string] $url = '',
  [parameter(Mandatory=$true, Position=1)][string] $fileName = $null,
  [parameter(Mandatory=$false, Position=2)][string] $username = $null,
  [parameter(Mandatory=$false, Position=3)][string] $password = $null,
  [parameter(Mandatory=$false)][switch] $quiet,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-Debug "Running 'Get-FtpFile' for $fileName with url:'$url', userName: '$userName', password: '$password'";

  if ($url -eq $null -or $url -eq '') {
    Write-Warning "Url parameter is empty, Get-FtpFile has nothing to do."
    return
  }

  if ($fileName -eq $null -or $fileName -eq '') {
    Write-Warning "FileName parameter is empty, Get-FtpFile cannot save the output."
    return
  }

  # Create a FTPWebRequest object to handle the connection to the ftp server
  $ftprequest = [System.Net.FtpWebRequest]::create($url)

  # check if a proxy is required
  $explicitProxy = $env:chocolateyProxyLocation
  $explicitProxyUser = $env:chocolateyProxyUser
  $explicitProxyPassword = $env:chocolateyProxyPassword
  if ($explicitProxy -ne $null) {
    # explicit proxy
	  $proxy = New-Object System.Net.WebProxy($explicitProxy, $true)
	  if ($explicitProxyPassword -ne $null) {
	    $passwd = ConvertTo-SecureString $explicitProxyPassword -AsPlainText -Force
	    $proxy.Credentials = New-Object System.Management.Automation.PSCredential ($explicitProxyUser, $passwd)
	  }

  	Write-Host "Using explicit proxy server '$explicitProxy'."
    $ftprequest.Proxy = $proxy
  }

  # set the request's network credentials for an authenticated connection
  $ftprequest.Credentials = New-Object System.Net.NetworkCredential($username, $password)

  $ftprequest.Method = [System.Net.WebRequestMethods+Ftp]::DownloadFile
  $ftprequest.UseBinary = $true
  $ftprequest.KeepAlive = $false

  # use the default request timeout of 100000
  if ($env:chocolateyRequestTimeout -ne $null -and $env:chocolateyRequestTimeout -ne '') {
    $ftprequest.Timeout =  $env:chocolateyRequestTimeout
  }
  if ($env:chocolateyResponseTimeout -ne $null -and $env:chocolateyResponseTimeout -ne '') {
    $ftprequest.ReadWriteTimeout =  $env:chocolateyResponseTimeout
  }

  try {
    # send the ftp request to the server
    $ftpresponse = $ftprequest.GetResponse()
    [int]$goal = $ftpresponse.ContentLength
    $goalFormatted = Format-FileSize $goal

    # get a download stream from the server response
    $reader = $ftpresponse.GetResponseStream()

    # create the target file on the local system and the download buffer
    $writer = New-Object IO.FileStream ($fileName,[IO.FileMode]::Create)
    [byte[]]$buffer = New-Object byte[] 1024
    [int]$total = [int]$count = 0

    $originalEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Stop'
    try {
      # loop through the download stream and send the data to the target file
      do {
        $count = $reader.Read($buffer, 0, $buffer.Length);
        $writer.Write($buffer, 0, $count);
        if(!$quiet) {
          $total += $count
          $totalFormatted = Format-FileSize $total
          if($goal -gt 0) {
            Write-Progress "Downloading $url to $fileName" "Saving $totalFormatted of $goalFormatted ($total/$goal)" -id 0 -percentComplete (($total/$goal)*100)
          } else {
            Write-Progress "Downloading $url to $fileName" "Saving $total bytes..." -id 0 -Completed
          }
          if ($total -eq $goal) {
            Write-Progress "Completed download of $url." "Completed a total of $total bytes of $fileName" -id 0 -Completed
          }
        }
      } while ($count -ne 0)
      Write-Host ""
      Write-Host "Download of $([System.IO.Path]::GetFileName($fileName)) ($goalFormatted) completed."
    } catch {
      throw $_.Exception
    } finally {
        $ErrorActionPreference = $originalEAP
    }

    $reader.Close()
    if ($fileName) {
      $writer.Flush()
      $writer.Close()
    }

  } catch {
    if ($ftprequest -ne $null) {
      $ftprequest.ServicePoint.MaxIdleTime = 0
      $ftprequest.Abort();
      # ruthlessly remove $ftprequest to ensure it isn't reused
      Remove-Variable ftprequest
      Start-Sleep 1
      [GC]::Collect()
    }

    Set-PowerShellExitCode 404
    throw "The remote file either doesn't exist, is unauthorized, or is forbidden for url '$url'. $($_.Exception.Message)"
  } finally {
    if ($ftpresponse -ne $null) {
      $ftpresponse.Close()
    }
  }
}
