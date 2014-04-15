function Get-WebHeaders {
param(
  $url = '',
  $userAgent = 'chocolatey command line'
)
  Write-Debug "Running 'Get-WebHeaders' with url:`'$url`', userAgent: `'$userAgent`'";
  if ($url -eq '') { return }

  $request = [System.Net.HttpWebRequest]::Create($url);
  #to check if a proxy is required
  $client = New-Object System.Net.WebClient
  if (!$client.Proxy.IsBypassed($url))
  {
    $creds = [Net.CredentialCache]::DefaultCredentials
    if ($creds -eq $null) {
      Write-Debug "Default credentials were null. Attempting backup method"
      $cred = Get-Credential
      $creds = $cred.GetNetworkCredential();
    }
    $proxyAddress = $client.Proxy.GetProxy($url).Authority
    Write-host "Using this proxyserver: $proxyAddress"
    $proxy = New-Object System.Net.WebProxy($proxyAddress)
    $proxy.credentials = $creds
    $request.proxy = $proxy
  }

  #http://stackoverflow.com/questions/518181/too-many-automatic-redirections-were-attempted-error-message-when-using-a-httpw
  $request.CookieContainer = New-Object System.Net.CookieContainer
  if ($userAgent -ne $null) {
    Write-Debug "Setting the UserAgent to `'$userAgent`'"
    $request.UserAgent = $userAgent
  }

  $response = $request.GetResponse();

  $headers = @{}
  Write-Debug "Web Headers Received:"
  foreach ($key in $response.Headers) {
    $value = $response.Headers[$key];
    if ($value) {
      $headers.Add("$key","$value")
      Write-Debug "  `'$key`':`'$value`'"
    }
  }
  $response.Close();

  $headers
}
