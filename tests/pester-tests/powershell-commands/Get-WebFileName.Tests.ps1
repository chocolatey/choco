Describe 'Get-WebFileName helper function tests' -Tags Cmdlets, GetWebFileName {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        $modulePath = "$testLocation\helpers\chocolateyInstaller.psm1"
        Import-Module $modulePath

        # Test machines don't usually have proxy settings set, but it doesn't hurt to check and
        # restore any that may have been clobbered in the course of the tests.
        $proxyVariables = @(
            'chocolateyProxyLocation'
            'chocolateyProxyBypassOnLocal'
            'chocolateyProxyBypassList'
            'chocolateyProxyUser'
            'chocolateyProxyPassword'
        )
        $originalProxySettings = @{}
        foreach ($name in $proxyVariables) {
            $originalProxySettings[$name] = [Environment]::GetEnvironmentVariable($name)
            [Environment]::SetEnvironmentVariable($name, $null)
        }

        $server = Start-TestWebServer -Routes @{
            '/named'                 = @{
                Headers = @{ 'Content-Disposition' = 'attachment; filename="installer.msi"' }
                Body    = 'content'
            }
            '/unquoted'              = @{
                Headers = @{ 'Content-Disposition' = 'attachment; filename=setup.exe' }
                Body    = 'content'
            }
            '/redirect'              = @{
                Status  = 302
                Headers = @{ Location = '/final/setup.exe' }
            }
            '/final/setup.exe'       = @{ Body = 'content' }
            '/downloads/file.zip'    = @{ Body = 'content' }
            '/downloads/noextension' = @{ Body = 'content' }
            '/downloads/'            = @{ Body = 'content' }
        }
    }

    AfterAll {
        Stop-TestWebServer -Server $server

        foreach ($name in $proxyVariables) {
            [Environment]::SetEnvironmentVariable($name, $originalProxySettings[$name])
        }
    }

    Context 'Determining the file name from the server' {
        It 'Uses the file name from the Content-Disposition header' {
            Get-WebFileName -Url "$($server.Url)/named" -DefaultName 'default.bin' | Should -BeExactly 'installer.msi'
        }

        It 'Uses an unquoted file name from the Content-Disposition header' {
            Get-WebFileName -Url "$($server.Url)/unquoted" -DefaultName 'default.bin' | Should -BeExactly 'setup.exe'
        }

        It 'Follows redirects and uses the name in the final url' {
            Get-WebFileName -Url "$($server.Url)/redirect" -DefaultName 'default.bin' | Should -BeExactly 'setup.exe'
        }

        It 'Uses the name in the url when the server does not name the file' {
            Get-WebFileName -Url "$($server.Url)/downloads/file.zip" -DefaultName 'default.bin' | Should -BeExactly 'file.zip'
        }

        It 'Uses the last segment of the url even when it has no extension' {
            Get-WebFileName -Url "$($server.Url)/downloads/noextension" -DefaultName 'default.bin' | Should -BeExactly 'noextension'
        }

        It 'Uses the default name when the url ends in a slash' {
            Get-WebFileName -Url "$($server.Url)/downloads/" -DefaultName 'default.bin' | Should -BeExactly 'default.bin'
        }
    }

    Context 'Falling back to the default name' {
        It 'Uses the default name when the server responds with an error, without an error being raised' {
            { Get-WebFileName -Url "$($server.Url)/missing" -DefaultName 'default.bin' -ErrorAction Stop } | Should -Not -Throw
            Get-WebFileName -Url "$($server.Url)/missing" -DefaultName 'default.bin' | Should -BeExactly 'default.bin'
        }

        It 'Uses the default name when the server cannot be reached' {
            Get-WebFileName -Url 'http://localhost:1/unreachable' -DefaultName 'default.bin' | Should -BeExactly 'default.bin'
        }

        It 'Uses the default name when the url is empty' {
            Get-WebFileName -Url '' -DefaultName 'default.bin' | Should -BeExactly 'default.bin'
        }

        It 'Uses the default name for an <Url> address without making a request' -ForEach @(
            @{ Url = 'ftp://example.com/files/tool.zip' }
            @{ Url = 'FTP://example.com/files/tool.zip' }
        ) {
            $server.Requests.Clear()

            Get-WebFileName -Url $Url -DefaultName 'default.bin' | Should -BeExactly 'default.bin'
            $server.Requests.Count | Should -Be 0
        }
    }

    Context 'Local file addresses' {
        It 'Uses the name of the local file' {
            Get-WebFileName -Url 'file:///C:/downloads/installer.msi' -DefaultName 'default.bin' | Should -BeExactly 'installer.msi'
        }
    }

    Context 'Parameters and output' {
        It 'Returns the name as a single string' {
            $name = Get-WebFileName -Url "$($server.Url)/named" -DefaultName 'default.bin'

            @($name).Count | Should -Be 1
            $name | Should -BeOfType [string]
        }

        It 'Accepts the url and default name as positional arguments' {
            Get-WebFileName "$($server.Url)/named" 'default.bin' | Should -BeExactly 'installer.msi'
        }

        It 'Accepts the url as -Uri' {
            Get-WebFileName -Uri "$($server.Url)/named" -DefaultName 'default.bin' | Should -BeExactly 'installer.msi'
        }

        It 'Requires a default name' {
            { Get-WebFileName -Url "$($server.Url)/named" -DefaultName $null } | Should -Throw
        }

        It 'Sends the Chocolatey user agent by default' {
            $server.Requests.Clear()
            $null = Get-WebFileName -Url "$($server.Url)/named" -DefaultName 'default.bin'

            $server.Requests[0].Headers['User-Agent'] | Should -BeExactly 'chocolatey command line'
        }

        It 'Sends the user agent it is given' {
            $server.Requests.Clear()
            $null = Get-WebFileName -Url "$($server.Url)/named" -DefaultName 'default.bin' -UserAgent 'Pester-Agent/1.0'

            $server.Requests[0].Headers['User-Agent'] | Should -BeExactly 'Pester-Agent/1.0'
        }
    }

    Context 'Debug output' {
        BeforeAll {
            $originalDebugPreference = $DebugPreference
            $DebugPreference = 'Continue'
            try {
                $records = Get-WebFileName -Url "$($server.Url)/named" -DefaultName 'default.bin' 5>&1 |
                    Where-Object { $_ -is [System.Management.Automation.DebugRecord] } |
                    ForEach-Object { $_.Message }
            }
            finally {
                $DebugPreference = $originalDebugPreference
            }
        }

        It 'Says which header the name was taken from' {
            $records | Should -Contain "Using header 'Content-Disposition' to determine file name."
        }

        It 'Says which name was determined' {
            $records | Should -Contain "File name determined from url is 'installer.msi'"
        }
    }

    Context 'Using an explicit proxy' {
        BeforeAll {
            $proxySettings = "`$env:chocolateyProxyLocation = 'http://localhost:1/'; `$env:chocolateyProxyBypassOnLocal = 'true'"
            $command = "Get-WebFileName -Url '$($server.Url)/named' -DefaultName 'default.bin'"

            $quietResult = Invoke-TestPowerShell -ModulePath $modulePath -Command "$proxySettings`n$command"
            $debugResult = Invoke-TestPowerShell -ModulePath $modulePath -DebugOutput -Command "$proxySettings`n$command"
        }

        It 'Does not write the proxy being used to the host' {
            $quietResult.Output | Should -Not -Contain "Using explicit proxy server 'http://localhost:1/'."
        }

        It 'Writes the proxy being used to the debug stream' {
            $debugResult.Output | Should -Contain "DEBUG: Using explicit proxy server 'http://localhost:1/'."
        }

        It 'Bypasses the proxy for a local address when asked to' {
            $quietResult.Output | Should -Contain 'installer.msi'
        }
    }
}
