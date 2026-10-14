Describe 'Get-WebHeaders helper function tests' -Tags Cmdlets, GetWebHeaders {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        $modulePath = "$testLocation\helpers\chocolateyInstaller.psm1"
        Import-Module $modulePath

        # Requests go directly to the local server, whatever proxy the machine running the tests has configured.
        $proxyVariables = 'chocolateyProxyLocation', 'chocolateyProxyBypassOnLocal', 'chocolateyProxyBypassList', 'chocolateyProxyUser', 'chocolateyProxyPassword'
        $originalProxySettings = @{}
        foreach ($name in $proxyVariables) {
            $originalProxySettings[$name] = [Environment]::GetEnvironmentVariable($name)
            [Environment]::SetEnvironmentVariable($name, $null)
        }

        $server = Start-TestWebServer -Routes @{
            '/file.zip' = @{
                ContentType = 'application/zip'
                Headers     = @{ 'X-Custom' = 'custom value'; 'Cache-Control' = 'no-cache, no-store' }
                Body        = 'zip file contents'
            }
        }
    }

    AfterAll {
        Stop-TestWebServer -Server $server

        foreach ($name in $proxyVariables) {
            [Environment]::SetEnvironmentVariable($name, $originalProxySettings[$name])
        }
    }

    Context 'Requesting a file that exists' {
        BeforeAll {
            $server.Requests.Clear()
            $headers = Get-WebHeaders -Url "$($server.Url)/file.zip" -ErrorAction Stop
        }

        It 'Returns a single Hashtable' {
            @($headers).Count | Should -Be 1
            $headers | Should -BeOfType [System.Collections.Hashtable]
        }

        It 'Writes System.Collections.Hashtable when the result is written as text' {
            # Package install tests assert this text as the output from writing the headers.
            "$headers" | Should -BeExactly 'System.Collections.Hashtable'
        }

        It 'Includes the Content-Length header that callers check for' {
            $headers.ContainsKey('Content-Length') | Should -BeTrue
            $headers['Content-Length'] | Should -Be 'zip file contents'.Length
        }

        It 'Includes the Content-Type header' {
            $headers['Content-Type'] | Should -BeExactly 'application/zip'
        }

        It 'Includes custom response headers' {
            $headers['X-Custom'] | Should -BeExactly 'custom value'
        }

        It 'Looks up header names regardless of casing' {
            $headers['content-type'] | Should -BeExactly 'application/zip'
            $headers.ContainsKey('CONTENT-LENGTH') | Should -BeTrue
        }

        It 'Requests the url given' {
            $server.Requests.Count | Should -BeGreaterThan 0
            $server.Requests[0].Path | Should -BeExactly '/file.zip'
        }
    }

    Context 'User agent' {
        It 'Sends the Chocolatey user agent by default' {
            $server.Requests.Clear()
            $null = Get-WebHeaders -Url "$($server.Url)/file.zip"

            $server.Requests[0].Headers['User-Agent'] | Should -BeExactly 'chocolatey command line'
        }

        It 'Sends the user agent it is given' {
            $server.Requests.Clear()
            $null = Get-WebHeaders -Url "$($server.Url)/file.zip" -UserAgent 'Pester-Agent/1.0'

            $server.Requests[0].Headers['User-Agent'] | Should -BeExactly 'Pester-Agent/1.0'
        }

        It 'Accepts the user agent as the second positional argument' {
            $server.Requests.Clear()
            $null = Get-WebHeaders "$($server.Url)/file.zip" 'Positional-Agent/2.0'

            $server.Requests[0].Headers['User-Agent'] | Should -BeExactly 'Positional-Agent/2.0'
        }
    }

    Context 'Debug output' {
        BeforeAll {
            $originalDebugPreference = $DebugPreference
            $DebugPreference = 'Continue'
            try {
                $records = Get-WebHeaders -Url "$($server.Url)/file.zip" 5>&1 |
                    Where-Object { $_ -is [System.Management.Automation.DebugRecord] } |
                    ForEach-Object { $_.Message }
            }
            finally {
                $DebugPreference = $originalDebugPreference
            }
        }

        It 'Writes the request headers' {
            $records | Should -Contain 'Request Headers:'
        }

        It 'Writes the response headers' {
            $records | Should -Contain 'Response Headers:'
            $records | Should -Contain "  'X-Custom':'custom value'"
        }

        It 'Writes the user agent with its parts separated by spaces' {
            $records | Should -Contain "  'User-Agent':'chocolatey command line'"
        }
    }

    Context 'Requesting without an address' {
        It 'Returns an empty Hashtable when no url is given' {
            $headers = Get-WebHeaders

            $headers | Should -BeOfType [System.Collections.Hashtable]
            $headers.Count | Should -Be 0
        }

        It 'Returns an empty Hashtable when the url is empty' {
            $headers = Get-WebHeaders -Url ''

            $headers | Should -BeOfType [System.Collections.Hashtable]
            $headers.Count | Should -Be 0
        }
    }

    Context 'Requesting a file that does not exist' {
        BeforeAll {
            $env:ChocolateyExitCode = $null
            $caught = $null
            try {
                Get-WebHeaders -Url "$($server.Url)/missing" -ErrorAction Stop
            }
            catch {
                $caught = $_
            }
        }

        It 'Throws a terminating WebException' {
            $caught | Should -Not -BeNullOrEmpty
            $caught.Exception | Should -BeOfType [System.Net.WebException]
        }

        It 'Reports the url and the status code' {
            $caught.Exception.Message | Should -Match "for url '$([regex]::Escape($server.Url))/missing'"
            $caught.Exception.Message | Should -Match '\(404\)'
        }

        It 'Uses the error id for a web exception from this command' {
            $caught.FullyQualifiedErrorId | Should -Match '^GetWebHeadersCommandError\.WebException,'
        }

        It 'Does not set the Chocolatey exit code' {
            $env:ChocolateyExitCode | Should -BeNullOrEmpty
        }
    }

    Context 'Using an explicit proxy' {
        BeforeAll {
            $script = @'
$env:chocolateyProxyLocation = 'http://localhost:1/'
$env:chocolateyProxyBypassOnLocal = 'true'
Get-WebHeaders -Url '{0}/file.zip' | Out-Null
'@ -f $server.Url
            $result = Invoke-TestPowerShell -ModulePath $modulePath -Command $script
        }

        It 'Writes the proxy being used to the host' {
            $result.Output | Should -Contain "Using explicit proxy server 'http://localhost:1/'."
        }
    }

    Context 'Requesting from a server that cannot be reached' {
        It 'Throws a terminating error' {
            { Get-WebHeaders -Url 'http://localhost:1/unreachable' -ErrorAction Stop } | Should -Throw -ExceptionType ([System.Net.WebException])
        }
    }
}
