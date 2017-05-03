## Get-Proxy
###########################################################################################
## Get proxy
## History:
## v1.0 - initial version
## Usage:
##    if (!$webclient.Proxy.IsBypassed($url))
##    {
##      $req.proxy = Get-Proxy $webclient $url
##    }
###########################################################################################

function Get-Proxy {
param(
  $webclient = '',
  $url = ''
 )
    $proxyaddress = [Environment]::GetEnvironmentVariable('HTTP_PROXY') -replace 'http://', ''
    Write-Debug "HTTP_PROXY=http://$proxyaddress"
    if ($proxyaddress -eq $null) {
        $proxyaddress = $webclient.Proxy.GetProxy($url).Authority
        Write-host "Using this proxyserver: $proxyaddress"
        $creds = [net.CredentialCache]::DefaultCredentials
        if ($creds -eq $null) {
          Write-Debug "Default credentials were null. Attempting backup method"
          $cred = get-credential
          $creds = $cred.GetNetworkCredential();
        }
    } else {
        $p = $proxyaddress.Split("@")
        $proxyaddress = $p[1]
        $creds = $p[0].Split(":")
        $creds = new-object System.Net.NetworkCredential($creds[0],$creds[1])
    }
  $proxy = New-Object System.Net.WebProxy($proxyaddress)
  $proxy.credentials = $creds
  $proxy
}
