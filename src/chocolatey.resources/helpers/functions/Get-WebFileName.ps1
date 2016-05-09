# Copyright 2011 - Present RealDimensions Software, LLC
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
#
# Based on http://stackoverflow.com/a/13571471/18475
function Get-WebFileName {
param(
  [string]$url = '',
  [string]$defaultName,
  $userAgent = 'chocolatey command line'
)

  Write-Debug "Running 'Get-WebFileName' to determine name with url:'$url', defaultName:'$defaultName'";

  $originalFileName = $defaultName
  $fileName = $null

  if ($url -eq $null -or $url -eq '') {
    Write-Debug "Url was null, using default name."
    return $originalFileName
  }

  $request = [System.Net.HttpWebRequest]::Create($url)
  if ($request -eq $null) { 
    $request.Close()
    Write-Debug "Request was null, using default name."
    return $originalFileName
  }

  $defaultCreds = [System.Net.CredentialCache]::DefaultCredentials
  if ($defaultCreds -ne $null) {
    $request.Credentials = $defaultCreds
  }

  $client = New-Object System.Net.WebClient
  if ($defaultCreds -ne $null) {
    $client.Credentials = $defaultCreds
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

    Write-Debug "Using explicit proxy server '$explicitProxy'."
    $request.Proxy = $proxy
  
  } elseif (!$client.Proxy.IsBypassed($url))
  {
    # system proxy (pass through)
    $creds = [Net.CredentialCache]::DefaultCredentials
    if ($creds -eq $null) {
      Write-Debug "Default credentials were null. Attempting backup method"
      $cred = Get-Credential
      $creds = $cred.GetNetworkCredential();
    }
    $proxyAddress = $client.Proxy.GetProxy($url).Authority
    Write-Debug "Using system proxy server '$proxyaddress'."
    $proxy = New-Object System.Net.WebProxy($proxyAddress)
    $proxy.Credentials = $creds
    $request.Proxy = $proxy
  }

  $request.Method = "GET"
  $request.Accept = '*/*'
  $request.AllowAutoRedirect = $true
  $request.MaximumAutomaticRedirections = 20
  #$request.KeepAlive = $true
  $request.Timeout = 20000

  #http://stackoverflow.com/questions/518181/too-many-automatic-redirections-were-attempted-error-message-when-using-a-httpw
  $request.CookieContainer = New-Object System.Net.CookieContainer
  $request.UserAgent = $userAgent
  
  try
  {
    [System.Net.HttpWebResponse]$response = $request.GetResponse()
    if ($response -eq $null) {
      $response.Close()
      Write-Debug "Response was null, using default name."
      return $originalFileName
    }

    [string]$header = $response.Headers['Content-Disposition']
    [string]$headerLocation = $response.Headers['Location']
    
    # start with content-disposition header
    if ($header -ne '') {
      $fileHeaderName = 'filename='
      $index = $header.LastIndexOf($fileHeaderName, [StringComparison]::OrdinalIgnoreCase)
      if ($index -gt -1) {
        Write-Debug "Using header 'Content-Disposition' to determine file name."
        $fileName = $header.Substring($index + $fileHeaderName.Length).Replace('"', '')
      }
    }
    
    # If empty, check location header next
    if ($fileName -eq $null -or  $fileName -eq '') {
      if ($headerLocation -ne '') {
        Write-Debug "Using header 'Location' to determine file name."
        $fileName = [System.IO.Path]::GetFileName($headerLocation)
      }
    }

    #$containsQuery = [System.IO.Path]::GetFileName($url).Contains('?')
    #$containsEquals = [System.IO.Path]::GetFileName($url).Contains('=')

    # Next comes using the response url value
    if ($fileName -eq $null -or  $fileName -eq '') {
      $responseUrl = $response.ResponseUri.ToString()
      if (!$responseUrl.Contains('?')) {
        Write-Debug "Using response url to determine file name. '$responseUrl'"
        $fileName = [System.IO.Path]::GetFileName($responseUrl) 
      }
    }

    # Next comes using the request url value
    if ($fileName -eq $null -or  $fileName -eq '') {
      $requestUrl = $url
      $extension = [System.IO.Path]::GetExtension($requestUrl)
      if (!$requestUrl.Contains('?') -and $extension -ne $null -and $extension -ne '') {
        Write-Debug "Using request url to determine file name. ' $requestUrl'"
        $fileName = [System.IO.Path]::GetFileName($requestUrl) 
      }
    }

    [System.Text.RegularExpressions.Regex]$containsABadCharacter = New-Object Regex("[" + [System.Text.RegularExpressions.Regex]::Escape([System.IO.Path]::GetInvalidFileNameChars()) + "]", [System.Text.RegularExpressions.RegexOptions]::IgnorePatternWhitespace);

    # when all else fails, default the name
    if ($fileName -eq $null -or  $fileName -eq '' -or $containsABadCharacter.IsMatch($fileName)) {
      Write-Debug "File name is null or illegal. Using $originalFileName instead."
      $fileName = $originalFileName
    }
    
    Write-Debug "File name determined from url is '$fileName'"
    
    return $fileName
  } catch {
    if ($request -ne $null) {
      $request.ServicePoint.MaxIdleTime = 0
      $request.Abort();
      # ruthlessly remove $request to ensure it isn't reused
      Remove-Variable request
      Start-Sleep 1
      [GC]::Collect()
    }
    
    Write-Debug "Url request/response failed - file name will be '$originalFileName':  $($_)"
    
    return $originalFileName
  } finally {
   if ($response -ne $null) {
      $response.Close();
    }
  }
}