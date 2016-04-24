# http://poshcode.org/417
## Get-WebFile (aka wget for PowerShell)
##############################################################################################################
## Downloads a file or page from the web
## History:
## v3.6 - Add -Passthru switch to output TEXT files
## v3.5 - Add -Quiet switch to turn off the progress reports ...
## v3.4 - Add progress report for files which don't report size
## v3.3 - Add progress report for files which report their size
## v3.2 - Use the pure Stream object because StreamWriter is based on TextWriter:
##        it was messing up binary files, and making mistakes with extended characters in text
## v3.1 - Unwrap the filename when it has quotes around it
## v3   - rewritten completely using HttpWebRequest + HttpWebResponse to figure out the file name, if possible
## v2   - adds a ton of parsing to make the output pretty
##        added measuring the scripts involved in the command, (uses Tokenizer)
##############################################################################################################
function Get-WebFile {
param(
  $url = '', #(Read-Host "The URL to download"),
  $fileName = $null,
  $userAgent = 'chocolatey command line',
  [switch]$Passthru,
  [switch]$quiet,
  [hashtable] $options = @{Headers=@{}}
)
  Write-Debug "Running 'Get-WebFile' for $fileName with url:`'$url`', userAgent: `'$userAgent`' ";
  #if ($url -eq '' return)
  $req = [System.Net.HttpWebRequest]::Create($url);
  $defaultCreds = [System.Net.CredentialCache]::DefaultCredentials
  if ($defaultCreds -ne $null) {
    $req.Credentials = $defaultCreds
  }

  $webclient = new-object System.Net.WebClient
  if ($defaultCreds -ne $null) {
    $webClient.Credentials = $defaultCreds
  }

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
    $req.Proxy = $proxy
  
  } elseif (!$webclient.Proxy.IsBypassed($url))
  {
	# system proxy (pass through)
    $creds = [net.CredentialCache]::DefaultCredentials
    if ($creds -eq $null) {
      Write-Debug "Default credentials were null. Attempting backup method"
      $cred = get-credential
      $creds = $cred.GetNetworkCredential();
    }
    $proxyaddress = $webclient.Proxy.GetProxy($url).Authority
    Write-Host "Using system proxy server '$proxyaddress'."
    $proxy = New-Object System.Net.WebProxy($proxyaddress)
    $proxy.Credentials = $creds
    $req.Proxy = $proxy
  }

  $req.Accept = "*/*"
  $req.AllowAutoRedirect = $true
  $req.MaximumAutomaticRedirections = 20
  #$req.KeepAlive = $true

  #http://stackoverflow.com/questions/518181/too-many-automatic-redirections-were-attempted-error-message-when-using-a-httpw
  $req.CookieContainer = New-Object System.Net.CookieContainer
  if ($userAgent -ne $null) {
    Write-Debug "Setting the UserAgent to `'$userAgent`'"
    $req.UserAgent = $userAgent
  }

  if ($options.Headers.Count -gt 0) {
    Write-Debug "Setting custom headers"
    foreach ($item in $options.Headers.GetEnumerator()) {
      $uri = (new-object system.uri $url)
      Write-Debug($item.Key + ':' + $item.Value)
      switch ($item.Key) {
        'Accept' {$req.Accept = $item.Value}
        'Cookie' {$req.CookieContainer.SetCookies($uri, $item.Value)}
        'Referer' {$req.Referer = $item.Value}
        'User-Agent' {$req.UserAgent = $item.Value}
        Default {$req.Headers.Add($item.Key, $item.Value)}
      }
    }
  }

  try { 
   [System.Net.HttpWebResponse]$res = $req.GetResponse();

   try {
      $headers = @{}
      foreach ($key in $res.Headers) {
        $value = $res.Headers[$key];
        if ($value) {
          $headers.Add("$key","$value")
        }
      }

      if ($headers.ContainsKey("Content-Type")) {
        $contentType = $headers['Content-Type']
        if ($contentType -ne $null) {
          if ($contentType.ToLower().Contains("text/html") -or $contentType.ToLower().Contains("text/plain")) {
            Write-Warning "$fileName is of content type $contentType"
            Set-Content -Path "$fileName.istext" -Value "$fileName has content type $contentType" -Encoding UTF8 -Force
          }
        }
      } 
    } catch {
      # not able to get content-type header
      Write-Debug "Error getting content type - $($_.Exception.Message)"
    }

    if($fileName -and !(Split-Path $fileName)) {
      $fileName = Join-Path (Get-Location -PSProvider "FileSystem") $fileName
    }
    elseif((!$Passthru -and ($fileName -eq $null)) -or (($fileName -ne $null) -and (Test-Path -PathType "Container" $fileName)))
    {
      [string]$fileName = ([regex]'(?i)filename=(.*)$').Match( $res.Headers["Content-Disposition"] ).Groups[1].Value
      $fileName = $fileName.trim("\/""'")
      if(!$fileName) {
         $fileName = $res.ResponseUri.Segments[-1]
         $fileName = $fileName.trim("\/")
         if(!$fileName) {
            $fileName = Read-Host "Please provide a file name"
         }
         $fileName = $fileName.trim("\/")
         if(!([IO.FileInfo]$fileName).Extension) {
            $fileName = $fileName + "." + $res.ContentType.Split(";")[0].Split("/")[1]
         }
      }
      $fileName = Join-Path (Get-Location -PSProvider "FileSystem") $fileName
    }
    if($Passthru) {
      $encoding = [System.Text.Encoding]::GetEncoding( $res.CharacterSet )
      [string]$output = ""
    }

    if($res.StatusCode -eq 401 -or $res.StatusCode -eq 403 -or $res.StatusCode -eq 404) {
      $env:ChocolateyExitCode = $res.StatusCode
      throw "Remote file either doesn't exist, is unauthorized, or is forbidden for '$url'."
    }

    if($res.StatusCode -eq 200) {
      [long]$goal = $res.ContentLength
      $goalFormatted = Format-FileSize $goal
      $reader = $res.GetResponseStream()
    
      if ($fileName) {
        $fileDirectory = $([System.IO.Path]::GetDirectoryName($fileName))
        if (!(Test-Path($fileDirectory))) {
          [System.IO.Directory]::CreateDirectory($fileDirectory) | Out-Null
        }

        try {
          $writer = new-object System.IO.FileStream $fileName, "Create"
        } catch {
          throw $_.Exception
        }
      }
    
      [byte[]]$buffer = new-object byte[] 1048576
      [long]$total = [long]$count = [long]$iterLoop =0

      $originalEAP = $ErrorActionPreference
      $ErrorActionPreference = 'Stop'
      try {
        do
        {
          $count = $reader.Read($buffer, 0, $buffer.Length);
          if($fileName) {
            $writer.Write($buffer, 0, $count);
          }
        
          if($Passthru){
            $output += $encoding.GetString($buffer,0,$count)
          } elseif(!$quiet) {
            $total += $count
            $totalFormatted = Format-FileSize $total
            if($goal -gt 0 -and ++$iterLoop%10 -eq 0) {
              Write-Progress "Downloading $url to $fileName" "Saving $totalFormatted of $goalFormatted ($total/$goal)" -id 0 -percentComplete (($total/$goal)*100)
            }
          
            if ($total -eq $goal) {
              Write-Progress "Completed download of $url." "Completed download of $fileName ($goalFormatted)." -id 0 -Completed
            }
          }
        } while ($count -gt 0)
	    Write-Host ""
	    Write-Host "Download of $([System.IO.Path]::GetFileName($fileName)) ($goalFormatted) completed."
      } catch {
        throw $_.Exception
      } finally {
        $ErrorActionPreference = $originalEAP
      }

      $reader.Close()
      if($fileName) {
         $writer.Flush()
         $writer.Close()
      }
      if($Passthru){
         $output
      }
    }
  } catch {
    if ($req -ne $null) {
      $req.ServicePoint.MaxIdleTime = 0
      $req.Abort();
      # ruthlessly remove $req to ensure it isn't reused
      Remove-Variable req
      Start-Sleep 1
      [GC]::Collect()
    }
    
    Set-PowerShellExitCode 404
    throw "The remote file either doesn't exist, is unauthorized, or is forbidden for url '$url'. $($_.Exception.Message)"
  } finally {
    if ($res -ne $null) {
      $res.Close()
    }
  }
}

# this could be cleaned up with http://learn-powershell.net/2013/02/08/powershell-and-events-object-events/
