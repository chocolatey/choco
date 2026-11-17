Describe 'Get-WebFile helper function tests' -Tags Cmdlets, GetWebFile {
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

        $binaryContent = [byte[]]::new(64kb)
        [random]::new().NextBytes($binaryContent)

        $server = Start-TestWebServer -Routes @{
            '/binary.bin' = @{ BodyBytes = $binaryContent }
            '/named'      = @{
                Headers   = @{ 'Content-Disposition' = 'attachment; filename="installer.msi"' }
                BodyBytes = $binaryContent
            }
            '/text'       = @{ ContentType = 'text/plain'; Body = 'hello text' }
        }

        # A directory that is the PowerShell location for tests, which is deliberately different from the .NET current directory,
        # so that we can tell the two apart.
        $powerShellLocation = Join-Path $TestDrive 'powershell-location'
        $null = New-Item -Path $powerShellLocation -ItemType Directory
        [Environment]::CurrentDirectory | Should -Not -Be $powerShellLocation -Because 'the PowerShell location needs to differ from the .NET current directory for these tests to be meaningful'

        function Test-InPowerShellLocation {
            param($Name)

            (Test-Path -LiteralPath (Join-Path $powerShellLocation $Name)) -and -not (Test-Path -LiteralPath (Join-Path ([Environment]::CurrentDirectory) $Name))
        }
    }

    AfterAll {
        Stop-TestWebServer -Server $server

        foreach ($name in $proxyVariables) {
            [Environment]::SetEnvironmentVariable($name, $originalProxySettings[$name])
        }
    }

    BeforeEach {
        Push-Location $powerShellLocation
    }

    AfterEach {
        Pop-Location
        Get-ChildItem -Path $powerShellLocation | Remove-Item -Recurse -Force
    }

    Context 'Downloading to a file path' {
        It 'Downloads the file to an absolute path' {
            $target = Join-Path $TestDrive 'absolute.bin'

            Get-WebFile -Url "$($server.Url)/binary.bin" -FileName $target -Quiet

            $target | Should -Exist
            [System.IO.File]::ReadAllBytes($target) | Should -Be $binaryContent
        }

        It 'Downloads the file relative to the PowerShell location, not the .NET current directory' {
            Get-WebFile -Url "$($server.Url)/binary.bin" -FileName 'relative.bin' -Quiet

            Test-InPowerShellLocation 'relative.bin' | Should -BeTrue
            [System.IO.File]::ReadAllBytes((Join-Path $powerShellLocation 'relative.bin')) | Should -Be $binaryContent
        }

        It 'Accepts the path as -Path' {
            Get-WebFile -Url "$($server.Url)/binary.bin" -Path 'by-path.bin' -Quiet

            Test-InPowerShellLocation 'by-path.bin' | Should -BeTrue
        }

        It 'Creates folders that do not exist yet for a relative path' {
            Get-WebFile -Url "$($server.Url)/binary.bin" -FileName 'one\two\nested.bin' -Quiet

            Test-InPowerShellLocation 'one\two\nested.bin' | Should -BeTrue
        }

        It 'Creates folders that do not exist yet for an absolute path' {
            $target = Join-Path $TestDrive 'new-folder\absolute-nested.bin'

            Get-WebFile -Url "$($server.Url)/binary.bin" -FileName $target -Quiet

            $target | Should -Exist
        }

        It 'Overwrites a file that already exists' {
            Set-Content -Path 'existing.bin' -Value 'old contents'

            Get-WebFile -Url "$($server.Url)/binary.bin" -FileName 'existing.bin' -Quiet

            [System.IO.File]::ReadAllBytes((Join-Path $powerShellLocation 'existing.bin')) | Should -Be $binaryContent
        }
    }

    Context 'Downloading without a usable file name' {
        It 'Uses the name from the server when the path is a directory, in the PowerShell location' {
            $null = New-Item -Path 'target-directory' -ItemType Directory

            Get-WebFile -Url "$($server.Url)/named" -FileName 'target-directory' -Quiet

            Test-InPowerShellLocation 'installer.msi' | Should -BeTrue
        }

        It 'Uses the name from the server when no path is given' {
            Get-WebFile -Url "$($server.Url)/named" -Quiet

            Test-InPowerShellLocation 'installer.msi' | Should -BeTrue
        }
    }

    Context 'Output' {
        It 'Writes the name and size of the downloaded file to the host' {
            # Text written to the host is not captured from inside this process, so this is run in a separate process.
            $result = Invoke-TestPowerShell -ModulePath $modulePath -Command "Set-Location '$powerShellLocation'; Get-WebFile -Url '$($server.Url)/named' -FileName 'output.bin'"

            $result.Output | Should -Contain "Download of 'output.bin' (64 KB) completed."
        }

        It 'Writes nothing to the output stream when downloading to a file' {
            $output = Get-WebFile -Url "$($server.Url)/binary.bin" -FileName 'quiet.bin' -Quiet

            $output | Should -BeNullOrEmpty
        }

        It 'Writes the downloaded text to the output stream with -Passthru' {
            $content = Get-WebFile -Url "$($server.Url)/text" -Passthru -Quiet

            $content | Should -BeExactly 'hello text'
            $content | Should -BeOfType [string]
        }

        It 'Does not create a file when using -Passthru without a path' {
            $null = Get-WebFile -Url "$($server.Url)/text" -Passthru -Quiet

            Get-ChildItem -Path $powerShellLocation | Should -BeNullOrEmpty
        }
    }

    Context 'Downloading text instead of a file' {
        BeforeAll {
            $textTarget = Join-Path $powerShellLocation 'page.html'
            $warnings = Get-WebFile -Url "$($server.Url)/text" -FileName $textTarget -Quiet 3>&1 6>$null |
                Where-Object { $_ -is [System.Management.Automation.WarningRecord] }
            $checkFileWritten = Test-Path -LiteralPath "$textTarget.istext"
        }

        It 'Warns that the content is of a text type' {
            $warnings.Message | Should -Match 'page\.html is of content type text/plain'
        }

        It 'Writes a .istext file next to the download' {
            $checkFileWritten | Should -BeTrue
        }
    }

    Context 'Sending headers' {
        It 'Sends the Chocolatey user agent by default' {
            $server.Requests.Clear()
            Get-WebFile -Url "$($server.Url)/binary.bin" -FileName 'agent.bin' -Quiet

            $server.Requests[0].Headers['User-Agent'] | Should -BeExactly 'chocolatey command line'
        }

        It 'Sends the user agent it is given' {
            $server.Requests.Clear()
            Get-WebFile -Url "$($server.Url)/binary.bin" -FileName 'agent.bin' -UserAgent 'Pester-Agent/1.0' -Quiet

            $server.Requests[0].Headers['User-Agent'] | Should -BeExactly 'Pester-Agent/1.0'
        }

        It 'Sends the custom headers from the options' {
            $server.Requests.Clear()
            Get-WebFile -Url "$($server.Url)/binary.bin" -FileName 'headers.bin' -Quiet -Options @{
                Headers = @{
                    'X-Custom' = 'custom value'
                    'Referer'  = 'http://referrer.example.com/'
                    'Cookie'   = 'session=abc123'
                }
            }

            $request = $server.Requests[0]
            $request.Headers['X-Custom'] | Should -BeExactly 'custom value'
            $request.Headers['Referer'] | Should -BeExactly 'http://referrer.example.com/'
            $request.Headers['Cookie'] | Should -Match 'session=abc123'
        }
    }

    Context 'Copying a local file' {
        BeforeAll {
            $sourceFile = Join-Path $TestDrive 'source.txt'
            Set-Content -Path $sourceFile -Value 'local contents'
            $sourceUrl = ([System.Uri]$sourceFile).AbsoluteUri
        }

        It 'Copies the file to an absolute path' {
            $target = Join-Path $TestDrive 'copy-absolute.txt'

            Get-WebFile -Url $sourceUrl -FileName $target

            Get-Content $target | Should -BeExactly 'local contents'
        }

        It 'Copies the file relative to the PowerShell location, not the .NET current directory' {
            Get-WebFile -Url $sourceUrl -FileName 'copy-relative.txt'

            Test-InPowerShellLocation 'copy-relative.txt' | Should -BeTrue
        }

        It 'Creates folders that do not exist yet for the copy' {
            Get-WebFile -Url $sourceUrl -FileName 'copy\nested\copied.txt'

            Test-InPowerShellLocation 'copy\nested\copied.txt' | Should -BeTrue
        }

        It 'Overwrites a file that already exists' {
            Set-Content -Path 'copy-existing.txt' -Value 'old contents'

            Get-WebFile -Url $sourceUrl -FileName 'copy-existing.txt'

            Get-Content (Join-Path $powerShellLocation 'copy-existing.txt') | Should -BeExactly 'local contents'
        }

        It 'Copies into a directory when the path is a directory' {
            $null = New-Item -Path 'copy-directory' -ItemType Directory

            Get-WebFile -Url $sourceUrl -FileName 'copy-directory'

            Get-Content (Join-Path $powerShellLocation 'copy-directory\source.txt') | Should -BeExactly 'local contents'
        }

        It 'Leaves the file alone when it is its own destination' {
            { Get-WebFile -Url $sourceUrl -FileName $sourceFile -ErrorAction Stop } | Should -Not -Throw

            Get-Content $sourceFile | Should -BeExactly 'local contents'
        }

        It 'Does not make a web request' {
            $server.Requests.Clear()

            Get-WebFile -Url $sourceUrl -FileName 'no-request.txt'

            $server.Requests.Count | Should -Be 0
        }
    }

    Context 'Downloading a file that does not exist' {
        BeforeAll {
            # Reporting the failure asks the host to exit with the exit code, which would end the Pester run,
            # so this is done in a separate process.
            $missingTarget = Join-Path $TestDrive 'missing.bin'

            $result = Invoke-TestPowerShell -ModulePath $modulePath -Command @"
try {
    Get-WebFile -Url '$($server.Url)/missing' -FileName '$missingTarget' -ErrorAction Stop
}
catch {
    "ErrorType:`$(`$_.Exception.GetType().FullName)"
    "ErrorId:`$(`$_.FullyQualifiedErrorId)"
    "ErrorMessage:`$(`$_.Exception.Message)"
}
"ExitCodeVariable:`$env:ChocolateyExitCode"
"@
        }

        It 'Throws a terminating WebException' {
            $result.Output | Should -Contain 'ErrorType:System.Net.WebException'
        }

        It 'Reports the url and the status code' {
            ($result.Output -join "`n") | Should -Match "ErrorMessage:.*for url '$([regex]::Escape($server.Url))/missing'.*\(404\)"
        }

        It 'Uses the error id for a web exception from this command' {
            ($result.Output -join "`n") | Should -Match 'ErrorId:GetWebFileCommandError\.WebException,'
        }

        It 'Sets the Chocolatey exit code variable to 404' {
            $result.Output | Should -Contain 'ExitCodeVariable:404'
        }

        It 'Asks the host to exit with 404' {
            $result.ExitCode | Should -Be 404
        }

        It 'Does not create the file' {
            $missingTarget | Should -Not -Exist
        }
    }
}
