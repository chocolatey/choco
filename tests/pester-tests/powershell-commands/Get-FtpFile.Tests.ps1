Describe 'Get-FtpFile helper function tests' -Tags Cmdlets, GetFtpFile {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        $modulePath = "$testLocation\helpers\chocolateyInstaller.psm1"
        Import-Module $modulePath

        # A directory that is the PowerShell location for tests, which is deliberately different from the .NET current directory,
        # so that we can tell the two apart.
        $powerShellLocation = Join-Path $TestDrive 'powershell-location'
        $null = New-Item -Path $powerShellLocation -ItemType Directory
        [Environment]::CurrentDirectory | Should -Not -Be $powerShellLocation -Because 'the PowerShell location needs to differ from the .NET current directory for these tests to be meaningful'

        function Test-InPowerShellLocation {
            param($Name)

            (Test-Path -LiteralPath (Join-Path $powerShellLocation $Name)) -and -not (Test-Path -LiteralPath (Join-Path ([Environment]::CurrentDirectory) $Name))
        }

        $sourceFile = Join-Path $TestDrive 'source.txt'
        Set-Content -Path $sourceFile -Value 'local contents'
        $sourceUrl = ([System.Uri]$sourceFile).AbsoluteUri

        $binaryContent = [byte[]]::new(64kb)
        [random]::new().NextBytes($binaryContent)
        $binaryHash = [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::Create().ComputeHash($binaryContent)).Replace('-', '')

        # The server that needs a login, and the one that does not.
        $server = Start-TestFtpServer -UserName 'someone' -Password 'hunter2' -Files @{
            '/binary.bin'          = $binaryContent
            '/empty.txt'           = [byte[]]::new(0)
            '/folder/nested/a.txt' = 'nested ftp contents'
        }
        $anonymousServer = Start-TestFtpServer -Files @{
            '/public.txt' = 'public ftp contents'
        }
    }

    AfterAll {
        Stop-TestFtpServer $server
        Stop-TestFtpServer $anonymousServer
    }

    BeforeEach {
        Push-Location $powerShellLocation
        $server.Commands.Clear()
        $anonymousServer.Commands.Clear()
    }

    AfterEach {
        Pop-Location
        Get-ChildItem -Path $powerShellLocation | Remove-Item -Recurse -Force
    }

    Context 'Command' {
        It 'Is a cmdlet' {
            (Get-Command Get-FtpFile).CommandType | Should -Be 'Cmdlet'
        }

        It 'Requires a file name' {
            { Get-FtpFile -Url 'ftp://localhost:1/file.txt' -FileName '' } | Should -Throw
        }

        It 'Accepts the url, file name, user name and password as positional arguments' {
            # The address is not an FTP address, so the command stops before it connects to anything.
            { Get-FtpFile 'http://localhost:1/file.txt' 'file.txt' 'someone' 'hunter2' -ErrorAction Stop } | Should -Throw -ExceptionType ([System.ArgumentException])
        }
    }

    Context 'Without an address' {
        It 'Requires a url' {
            { Get-FtpFile -Url '' -FileName 'nothing.txt' -ErrorAction Stop } | Should -Throw
        }
    }
    
    Context 'Copying a local file' {
        It 'Copies the file to an absolute path' {
            $target = Join-Path $TestDrive 'copy-absolute.txt'

            Get-FtpFile -Url $sourceUrl -FileName $target

            Get-Content $target | Should -BeExactly 'local contents'
        }

        It 'Copies the file relative to the PowerShell location, not the .NET current directory' {
            Get-FtpFile -Url $sourceUrl -FileName 'copy-relative.txt'

            Test-InPowerShellLocation 'copy-relative.txt' | Should -BeTrue
        }

        It 'Creates folders that do not exist yet for the copy' {
            Get-FtpFile -Url $sourceUrl -FileName 'copy\nested\copied.txt'

            Test-InPowerShellLocation 'copy\nested\copied.txt' | Should -BeTrue
        }

        It 'Leaves the file alone when it is its own destination' {
            { Get-FtpFile -Url $sourceUrl -FileName $sourceFile -ErrorAction Stop } | Should -Not -Throw

            Get-Content $sourceFile | Should -BeExactly 'local contents'
        }
    }

    Context 'Downloading from an FTP server' {
        It 'Downloads the file to an absolute path' {
            $target = Join-Path $TestDrive 'download-absolute.bin'

            Get-FtpFile -Url "$($server.Url)/binary.bin" -FileName $target -UserName 'someone' -Password 'hunter2'

            (Get-FileHash -Path $target -Algorithm SHA256).Hash | Should -Be $binaryHash
        }

        It 'Downloads the file relative to the PowerShell location, not the .NET current directory' {
            Get-FtpFile -Url "$($server.Url)/binary.bin" -FileName 'download-relative.bin' -UserName 'someone' -Password 'hunter2'

            Test-InPowerShellLocation 'download-relative.bin' | Should -BeTrue
        }

        It 'Creates folders that do not exist yet for the download' {
            Get-FtpFile -Url "$($server.Url)/binary.bin" -FileName 'download\nested\file.bin' -UserName 'someone' -Password 'hunter2'

            Test-InPowerShellLocation 'download\nested\file.bin' | Should -BeTrue
        }

        It 'Downloads a file that is in a folder on the server' {
            Get-FtpFile -Url "$($server.Url)/folder/nested/a.txt" -FileName 'nested.txt' -UserName 'someone' -Password 'hunter2'

            Get-Content 'nested.txt' | Should -BeExactly 'nested ftp contents'
        }

        It 'Downloads an empty file' {
            Get-FtpFile -Url "$($server.Url)/empty.txt" -FileName 'empty.txt' -UserName 'someone' -Password 'hunter2'

            (Get-Item -LiteralPath (Join-Path $powerShellLocation 'empty.txt')).Length | Should -Be 0
        }

        It 'Accepts the url, file name, user name and password as positional arguments' {
            Get-FtpFile "$($server.Url)/folder/nested/a.txt" 'positional.txt' 'someone' 'hunter2'

            Get-Content 'positional.txt' | Should -BeExactly 'nested ftp contents'
        }

        It 'Logs in with the user name and password' {
            Get-FtpFile -Url "$($server.Url)/empty.txt" -FileName 'login.txt' -UserName 'someone' -Password 'hunter2'

            $server.Commands | Should -Contain 'USER someone'
            $server.Commands | Should -Contain 'PASS hunter2'
        }

        It 'Logs in anonymously when there is no user name' {
            Get-FtpFile -Url "$($anonymousServer.Url)/public.txt" -FileName 'public.txt'

            $anonymousServer.Commands | Should -Contain 'USER anonymous'
            Get-Content 'public.txt' | Should -BeExactly 'public ftp contents'
        }

        It 'Downloads the file in binary mode' {
            Get-FtpFile -Url "$($server.Url)/empty.txt" -FileName 'binary-mode.txt' -UserName 'someone' -Password 'hunter2'

            $server.Commands | Should -Contain 'TYPE I'
        }
    }

    Context 'Downloading from an FTP server that does not give the size of the file' {
        BeforeAll {
            $noSizeServer = Start-TestFtpServer -OmitSize -Files @{ '/unsized.txt' = 'unsized contents' }
        }

        AfterAll {
            Stop-TestFtpServer $noSizeServer
        }

        It 'Downloads the file' {
            Get-FtpFile -Url "$($noSizeServer.Url)/unsized.txt" -FileName 'unsized.txt'

            Get-Content 'unsized.txt' | Should -BeExactly 'unsized contents'
        }
    }

    Context 'Reporting a completed download' {
        BeforeAll {
            $completedTarget = Join-Path $TestDrive 'completed.bin'

            $result = Invoke-TestPowerShell -ModulePath $modulePath -Command "Get-FtpFile -Url '$($server.Url)/binary.bin' -FileName '$completedTarget' -UserName 'someone' -Password 'hunter2'"
        }

        It 'Writes the name and size of the file to the host' {
            $result.Output | Should -Contain "Download of 'completed.bin' (64 KB) completed."
        }

        It 'Exits normally' {
            $result.ExitCode | Should -Be 0
        }
    }

    Context 'Downloading a file that is not on the server' {
        BeforeAll {
            $missingTarget = Join-Path $TestDrive 'missing.txt'

            $result = Invoke-TestPowerShell -ModulePath $modulePath -Command @"
`$env:chocolateyRequestTimeout = '10000'
try {
    Get-FtpFile -Url '$($server.Url)/missing.txt' -FileName '$missingTarget' -UserName 'someone' -Password 'hunter2' -ErrorAction Stop
}
catch {
    "ErrorType:`$(`$_.Exception.GetType().FullName)"
    "ErrorMessage:`$(`$_.Exception.Message)"
}
"ExitCodeVariable:`$env:ChocolateyExitCode"
"@
        }

        It 'Throws a terminating WebException' {
            $result.Output | Should -Contain 'ErrorType:System.Net.WebException'
        }

        It 'Reports the url and the answer from the server' {
            $message = ($result.Output -join "`n")
            $message | Should -Match "for url '$([regex]::Escape($server.Url))/missing\.txt'\."
            $message | Should -Match '\(550\)'
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

    Context 'Logging in with a password the server does not accept' {
        BeforeAll {
            $rejectedTarget = Join-Path $TestDrive 'rejected.bin'

            $result = Invoke-TestPowerShell -ModulePath $modulePath -Command @"
`$env:chocolateyRequestTimeout = '10000'
try {
    Get-FtpFile -Url '$($server.Url)/binary.bin' -FileName '$rejectedTarget' -UserName 'someone' -Password 'wrong' -ErrorAction Stop
}
catch {
    "ErrorType:`$(`$_.Exception.GetType().FullName)"
    "ErrorMessage:`$(`$_.Exception.Message)"
}
"@
        }

        It 'Throws a terminating WebException that includes the answer from the server' {
            $result.Output | Should -Contain 'ErrorType:System.Net.WebException'
            ($result.Output -join "`n") | Should -Match '\(530\)'
        }

        It 'Asks the host to exit with 404' {
            $result.ExitCode | Should -Be 404
        }

        It 'Does not create the file' {
            $rejectedTarget | Should -Not -Exist
        }
    }

    Context 'Using an address that is not an FTP address' {
        BeforeAll {
            $caught = $null
            try {
                Get-FtpFile -Url 'http://localhost:1/file.txt' -FileName 'file.txt' -ErrorAction Stop
            }
            catch {
                $caught = $_
            }
        }

        It 'Throws a terminating ArgumentException that says why' {
            $caught.Exception | Should -BeOfType [System.ArgumentException]
            $caught.Exception.Message | Should -Match "The address 'http://localhost:1/file.txt' is not an FTP address\."
        }

        It 'Uses the error id for an unknown error from this command' {
            $caught.FullyQualifiedErrorId | Should -Match '^GetFtpFileCommandError\.UnknownError,'
        }

        It 'Does not create a file' {
            Get-ChildItem -Path $powerShellLocation | Should -BeNullOrEmpty
        }
    }

    Context 'Downloading from a server that cannot be reached' {
        BeforeAll {
            # Reporting the failure asks the host to exit with the exit code, which would end the Pester run,
            # so this is done in a separate process.
            $unreachableTarget = Join-Path $TestDrive 'unreachable.txt'

            $result = Invoke-TestPowerShell -ModulePath $modulePath -Command @"
`$env:chocolateyRequestTimeout = '3000'
try {
    Get-FtpFile -Url 'ftp://localhost:1/file.txt' -FileName '$unreachableTarget' -ErrorAction Stop
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

        It 'Reports the url' {
            ($result.Output -join "`n") | Should -Match "ErrorMessage:The remote file either doesn't exist, is unauthorized, or is forbidden for url 'ftp://localhost:1/file\.txt'\."
        }

        It 'Uses the error id for a web exception from this command' {
            ($result.Output -join "`n") | Should -Match 'ErrorId:GetFtpFileCommandError\.WebException,'
        }

        It 'Sets the Chocolatey exit code variable to 404' {
            $result.Output | Should -Contain 'ExitCodeVariable:404'
        }

        It 'Asks the host to exit with 404' {
            $result.ExitCode | Should -Be 404
        }

        It 'Does not create the file' {
            $unreachableTarget | Should -Not -Exist
        }
    }

    Context 'Using an explicit proxy' {
        BeforeAll {
            $result = Invoke-TestPowerShell -ModulePath $modulePath -Command @"
`$env:chocolateyProxyLocation = 'http://localhost:1'
`$env:chocolateyRequestTimeout = '3000'
Get-FtpFile -Url 'ftp://example.com/file.txt' -FileName '$(Join-Path $TestDrive 'proxied.txt')' -ErrorAction SilentlyContinue
"@
        }

        It 'Writes the proxy being used to the host' {
            $result.Output | Should -Contain "Using explicit proxy server 'http://localhost:1'."
        }
    }
}