# Copyright © 2011 - Present RealDimensions Software, LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
#
# You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

Function Configure-Proxy {
<#
.SYNOPSIS
DO NOT USE. Not part of the public API.

.DESCRIPTION
Helper function for handling proxy.

.NOTES
This function is not part of the API.

.INPUTS
None

.OUTPUTS
Returns pair: $true and IWebProxy according to environmental variables
and current settings of system, or $false and $null if proxy is not required.

.PARAMETER DefaultProxy
Default proxy from e.g. current WebClient, or null.

.PARAMETER Url
Url for which the proxy is being configured.

.EXAMPLE
Configure-Proxy -DefaultProxy $oldProxy 'http://url.com'

.LINK
Get-FtpFile

.LINK
Get-WebFile

.LINK
Get-WebFileName

.LINK
Get-WebHeaders
#>
param (
  [Parameter(Mandatory=$false, Position=0)][System.Net.IWebProxy] $defaultProxy = $null,
  [Parameter(Mandatory=$false, Position=1)][string] $url = ''
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  $ignoreProxy = $env:chocolateyIgnoreProxy
  if ($ignoreProxy -ne $null -and $ignoreProxy -eq 'true') {
    Write-Host "Ignoring proxy"
    return $true, $null
  }

  $explicitProxy = $env:chocolateyProxyLocation
  $explicitProxyUser = $env:chocolateyProxyUser
  $explicitProxyPassword = $env:chocolateyProxyPassword
  if ($explicitProxy -ne $null) {
    $proxy = New-Object System.Net.WebProxy($explicitProxy, $true)
    if ($explicitProxyPassword -ne $null) {
      $passwd = ConvertTo-SecureString $explicitProxyPassword -AsPlainText -Force
      $proxy.Credentials = New-Object System.Management.Automation.PSCredential($explicitProxyUser, $passwd)
    }
      
    Write-Host "Using explicit proxy server '$explicitProxy'."
    return $true, $proxy
  }
  
  if ($defaultProxy -ne $null -and !$defaultProxy.IsBypassed($url))
  {
    $creds = [Net.CredentialCache]::DefaultCredentials
    if ($creds -eq $null) {
      Write-Debug "Default credentials were null. Attempting backup method"
      $cred = Get-Credential
      $creds = $cred.GetNetworkCredential();
    }
    $proxyAddress = $defaultProxy.GetProxy($url).Authority
    Write-Host "Using system proxy server '$proxyaddress'."
    $proxy = New-Object System.Net.WebProxy($proxyAddress)
    $proxy.Credentials = $creds
    return $true, $proxy
  }
  
  return $false, $null
}