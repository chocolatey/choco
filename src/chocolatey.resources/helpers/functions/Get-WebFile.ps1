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
  [switch]$quiet
)
  Write-Debug "Running 'Get-WebFile' for $fileName with url:`'$url`', userAgent: `'$userAgent`' ";
  #if ($url -eq '' return)
  $req = [System.Net.HttpWebRequest]::Create($url);
  $defaultCreds = [System.Net.CredentialCache]::DefaultCredentials
  if ($defaultCreds -ne $null) {
    $req.Credentials = $defaultCreds
  }

  # check if a proxy is required
  $webclient = new-object System.Net.WebClient
  if ($defaultCreds -ne $null) {
    $webClient.Credentials = $defaultCreds
  }

  if (!$webclient.Proxy.IsBypassed($url))
  {
    $creds = [net.CredentialCache]::DefaultCredentials
    if ($creds -eq $null) {
      Write-Debug "Default credentials were null. Attempting backup method"
      $cred = get-credential
      $creds = $cred.GetNetworkCredential();
    }
    $proxyaddress = $webclient.Proxy.GetProxy($url).Authority
    Write-host "Using this proxyserver: $proxyaddress"
    $proxy = New-Object System.Net.WebProxy($proxyaddress)
    $proxy.credentials = $creds
    $req.proxy = $proxy
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

  $res = $req.GetResponse();

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

  if($res.StatusCode -eq 200) {
    [long]$goal = $res.ContentLength
    $reader = $res.GetResponseStream()
    if($fileName) {
       $writer = new-object System.IO.FileStream $fileName, "Create"
    }
    [byte[]]$buffer = new-object byte[] 1048576
    [long]$total = [long]$count = [long]$iterLoop =0
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
          if($goal -gt 0 -and ++$iterLoop%10 -eq 0) {
             Write-Progress "Downloading $url to $fileName" "Saving $total of $goal" -id 0 -percentComplete (($total/$goal)*100)
          }
          if ($total -eq $goal) {
            Write-Progress "Completed download of $url." "Completed a total of $total bytes of $fileName" -id 0 -Completed
          }
       }
    } while ($count -gt 0)

    $reader.Close()
    if($fileName) {
       $writer.Flush()
       $writer.Close()
    }
    if($Passthru){
       $output
    }
  }
  $res.Close();
}

# this could be cleaned up with http://learn-powershell.net/2013/02/08/powershell-and-events-object-events/
