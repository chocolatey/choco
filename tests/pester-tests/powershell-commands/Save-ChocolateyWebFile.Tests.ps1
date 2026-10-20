Describe 'Save-ChocolateyWebFile helper function tests' -Tags Cmdlets, SaveChocolateyWebFile, GetChocolateyWebFile {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        $modulePath = "$testLocation\helpers\chocolateyInstaller.psm1"
        Import-Module $modulePath

        # Requests go directly to the local server, whatever proxy the machine running the tests has configured,
        # and the variables that change what is downloaded are not set by the machine.
        $managedVariables = @(
            'chocolateyProxyLocation', 'chocolateyProxyBypassOnLocal', 'chocolateyProxyBypassList', 'chocolateyProxyUser', 'chocolateyProxyPassword'
            'ChocolateyUrlOverride', 'ChocolateyUrl64bitOverride', 'chocolateyChecksum32', 'chocolateyChecksumType32', 'chocolateyChecksum64', 'chocolateyChecksumType64'
            'chocolateyForceX86', 'chocolateyAllowEmptyChecksums', 'chocolateyAllowEmptyChecksumsSecure', 'chocolateyIgnoreChecksums'
        )
        $originalValues = @{}
        foreach ($name in $managedVariables) {
            $originalValues[$name] = [Environment]::GetEnvironmentVariable($name)
            [Environment]::SetEnvironmentVariable($name, $null)
        }

        function Get-Sha256 {
            param([byte[]]$Bytes)

            $algorithm = [System.Security.Cryptography.SHA256]::Create()
            try {
                [BitConverter]::ToString($algorithm.ComputeHash($Bytes)).Replace('-', '').ToLowerInvariant()
            }
            finally {
                $algorithm.Dispose()
            }
        }

        $random = [Random]::new()
        $content32 = [byte[]]::new(8kb)
        $random.NextBytes($content32)
        $content64 = [byte[]]::new(12kb)
        $random.NextBytes($content64)
        $checksum32 = Get-Sha256 $content32
        $checksum64 = Get-Sha256 $content64

        $server = Start-TestWebServer -Routes @{
            '/file32.bin' = @{ BodyBytes = $content32 }
            '/file64.bin' = @{ BodyBytes = $content64 }
            '/named'      = @{
                Headers   = @{ 'Content-Disposition' = 'attachment; filename="installer.msi"' }
                BodyBytes = $content32
            }
        }
        $url32 = "$($server.Url)/file32.bin"
        $url64 = "$($server.Url)/file64.bin"

        $is64Bit = Get-OSArchitectureWidth 64

        function Get-RequestCount {
            param($Path)

            @($server.Requests | Where-Object { $_.Path -eq $Path }).Count
        }
    }

    AfterAll {
        Stop-TestWebServer -Server $server

        foreach ($name in $managedVariables) {
            [Environment]::SetEnvironmentVariable($name, $originalValues[$name])
        }
    }

    Context 'Command' {
        It 'Is a cmdlet' {
            (Get-Command Save-ChocolateyWebFile).CommandType | Should -Be 'Cmdlet'
        }

        It 'Has the previous name, Get-ChocolateyWebFile, as an alias' -Tag FossOnly {
            (Get-Alias -Name 'Get-ChocolateyWebFile').Definition | Should -BeExactly 'Save-ChocolateyWebFile'
        }

        It 'Requires a package name and a file path' {
            { Save-ChocolateyWebFile -PackageName 'package' -Path '' -Url $url32 } | Should -Throw
            { Save-ChocolateyWebFile -PackageName '' -Path (Join-Path $TestDrive 'x.bin') -Url $url32 } | Should -Throw
        }
    }

    Context 'Previous names' {
        BeforeAll {
            $env:chocolateyAllowEmptyChecksums = 'true'
        }

        AfterAll {
            $env:chocolateyAllowEmptyChecksums = $null
        }

        It 'Can still be called by its previous name, Get-ChocolateyWebFile' -Tag FossOnly {
            $target = Join-Path $TestDrive 'previous-command-name.bin'

            Get-ChocolateyWebFile 'package' $target $url32 | Should -BeExactly $target

            [System.IO.File]::ReadAllBytes($target) | Should -Be $content32
        }

        It 'Accepts the previous name of the path, FileFullPath' {
            $target = Join-Path $TestDrive 'previous-path-name.bin'

            Save-ChocolateyWebFile -PackageName 'package' -FileFullPath $target -Url $url32 | Should -BeExactly $target

            [System.IO.File]::ReadAllBytes($target) | Should -Be $content32
        }

        It 'Accepts the previous name of the download switch, ForceDownload' {
            $target = Join-Path $TestDrive 'previous-force-name.bin'
            $null = Save-ChocolateyWebFile 'package' $target $url32
            $server.Requests.Clear()

            $null = Save-ChocolateyWebFile 'package' $target $url32 -ForceDownload

            Get-RequestCount '/file32.bin' | Should -Be 2
        }

        It 'Accepts the previous name of the original file name switch, GetOriginalFileName' {
            $folder = Join-Path $TestDrive 'previous-original-name'

            $result = Save-ChocolateyWebFile 'package' (Join-Path $folder 'default.bin') "$($server.Url)/named" -GetOriginalFileName

            $result | Should -BeExactly (Join-Path $folder 'installer.msi')
        }
    }
    Context 'Downloading a file' {
        BeforeAll {
            $target = Join-Path $TestDrive 'download\file32.bin'
            $server.Requests.Clear()

            $result = Save-ChocolateyWebFile 'package' $target $url32 -Checksum $checksum32 -ChecksumType sha256
        }

        It 'Returns the path of the file as a single string' {
            @($result).Count | Should -Be 1
            $result | Should -BeOfType [string]
            $result | Should -BeExactly $target
        }

        It 'Creates the folder and downloads the file' {
            [System.IO.File]::ReadAllBytes($target) | Should -Be $content32
        }

        It 'Makes a request for the headers and one for the file' {
            Get-RequestCount '/file32.bin' | Should -Be 2
        }
    }

    Context 'Verifying the checksum' {
        It 'Fails when the checksum is not the checksum of the file' {
            $target = Join-Path $TestDrive 'bad-checksum.bin'
            $badChecksum = 'a' * 64

            { Save-ChocolateyWebFile 'package' $target $url32 -Checksum $badChecksum -ChecksumType sha256 -ErrorAction Stop } | Should -Throw
        }

        It 'Fails when there is no checksum and empty checksums are not allowed' {
            $target = Join-Path $TestDrive 'no-checksum.bin'

            { Save-ChocolateyWebFile 'package' $target $url32 -ErrorAction Stop } | Should -Throw -ExpectedMessage '*Empty checksums are no longer allowed*'
        }

        It 'Uses the checksum type for the checksum' {
            $target = Join-Path $TestDrive 'md5-checksum.bin'
            $md5 = (Get-FileHash -InputStream ([System.IO.MemoryStream]::new($content32)) -Algorithm MD5).Hash.ToLowerInvariant()

            { Save-ChocolateyWebFile 'package' $target $url32 -Checksum $md5 -ChecksumType md5 -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Uses md5 when no checksum type is given' {
            $target = Join-Path $TestDrive 'default-type.bin'
            $md5 = (Get-FileHash -InputStream ([System.IO.MemoryStream]::new($content32)) -Algorithm MD5).Hash.ToLowerInvariant()

            { Save-ChocolateyWebFile 'package' $target $url32 -Checksum $md5 -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Accepts the name of a checksum type in any case' {
            $target = Join-Path $TestDrive 'type-case.bin'

            { Save-ChocolateyWebFile 'package' $target $url32 -Checksum $checksum32 -ChecksumType SHA256 -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Rejects a checksum type that is an empty string' {
            $target = Join-Path $TestDrive 'empty-type.bin'

            { Save-ChocolateyWebFile 'package' $target $url32 -Checksum $checksum32 -ChecksumType '' -ErrorAction Stop } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            $target | Should -Not -Exist
        }

        It 'Rejects a 64 bit checksum type that is an empty string' {
            $target = Join-Path $TestDrive 'empty-type-64.bin'

            { Save-ChocolateyWebFile 'package' $target $url32 -Checksum $checksum32 -ChecksumType sha256 -ChecksumType64 '' -ErrorAction Stop } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
            $target | Should -Not -Exist
        }

        It 'Rejects a checksum type that is not one of the supported types' {
            $target = Join-Path $TestDrive 'bad-type.bin'

            { Save-ChocolateyWebFile 'package' $target $url32 -Checksum $checksum32 -ChecksumType sha384 -ErrorAction Stop } | Should -Throw -ExceptionType ([System.Management.Automation.ParameterBindingException])
        }
        Context 'When empty checksums are allowed' {
            BeforeAll {
                $env:chocolateyAllowEmptyChecksums = 'true'
            }

            AfterAll {
                $env:chocolateyAllowEmptyChecksums = $null
            }

            It 'Downloads a file without a checksum' {
                $target = Join-Path $TestDrive 'allowed-empty.bin'

                Save-ChocolateyWebFile 'package' $target $url32 | Should -BeExactly $target

                [System.IO.File]::ReadAllBytes($target) | Should -Be $content32
            }
        }
    }

    Context 'Reusing a file that was already downloaded' {
        BeforeAll {
            $env:chocolateyAllowEmptyChecksums = 'true'
        }

        AfterAll {
            $env:chocolateyAllowEmptyChecksums = $null
        }

        It 'Does not download the file again when the length is the same' {
            $target = Join-Path $TestDrive 'reuse.bin'
            $null = Save-ChocolateyWebFile 'package' $target $url32
            $server.Requests.Clear()

            $null = Save-ChocolateyWebFile 'package' $target $url32

            Get-RequestCount '/file32.bin' | Should -Be 1 -Because 'only the request for the headers is made'
        }

        It 'Downloads the file again when the length is different' {
            $target = Join-Path $TestDrive 'replace.bin'
            Set-Content -Path $target -Value 'old'

            $null = Save-ChocolateyWebFile 'package' $target $url32

            [System.IO.File]::ReadAllBytes($target) | Should -Be $content32
        }

        It 'Downloads the file again when a download is forced' {
            $target = Join-Path $TestDrive 'force.bin'
            $null = Save-ChocolateyWebFile 'package' $target $url32
            $server.Requests.Clear()

            $null = Save-ChocolateyWebFile 'package' $target $url32 -Force

            Get-RequestCount '/file32.bin' | Should -Be 2
        }

        It 'Does not download the file again when it matches the checksum' {
            $target = Join-Path $TestDrive 'reuse-checksum.bin'
            $null = Save-ChocolateyWebFile 'package' $target $url32 -Checksum $checksum32 -ChecksumType sha256
            $server.Requests.Clear()

            $null = Save-ChocolateyWebFile 'package' $target $url32 -Checksum $checksum32 -ChecksumType sha256

            Get-RequestCount '/file32.bin' | Should -Be 1
        }

        It 'Downloads the file again when it does not match the checksum' {
            $target = Join-Path $TestDrive 'replace-checksum.bin'
            [System.IO.File]::WriteAllBytes($target, $content32[0..99])

            $null = Save-ChocolateyWebFile 'package' $target $url32 -Checksum $checksum32 -ChecksumType sha256

            [System.IO.File]::ReadAllBytes($target) | Should -Be $content32
        }
    }

    Context 'Using the name that the server gives the file' {
        BeforeAll {
            $env:chocolateyAllowEmptyChecksums = 'true'
        }

        AfterAll {
            $env:chocolateyAllowEmptyChecksums = $null
        }

        It 'Saves the file under the name from the server, in the same folder' {
            $folder = Join-Path $TestDrive 'original-name'

            $result = Save-ChocolateyWebFile 'package' (Join-Path $folder 'default.bin') "$($server.Url)/named" -UseOriginalFileName

            $result | Should -BeExactly (Join-Path $folder 'installer.msi')
            [System.IO.File]::ReadAllBytes($result) | Should -Be $content32
        }

        It 'Keeps the given name without the switch' {
            $target = Join-Path $TestDrive 'given-name.bin'

            Save-ChocolateyWebFile 'package' $target "$($server.Url)/named" | Should -BeExactly $target
        }
    }

    Context 'Choosing between a 32 bit and a 64 bit file' {
        BeforeAll {
            $env:chocolateyAllowEmptyChecksums = $null
        }

        AfterEach {
            $env:chocolateyForceX86 = $null
        }

        It 'Downloads the 64 bit file and verifies its checksum on a 64 bit system' -Skip:(-not $is64Bit) {
            $target = Join-Path $TestDrive 'bits-64.bin'

            $null = Save-ChocolateyWebFile 'package' $target $url32 -Url64bit $url64 -Checksum $checksum32 -ChecksumType sha256 -Checksum64 $checksum64

            [System.IO.File]::ReadAllBytes($target) | Should -Be $content64
        }

        It 'Accepts the 64 bit address by its alias Url64' -Skip:(-not $is64Bit) {
            $target = Join-Path $TestDrive 'bits-alias.bin'

            $null = Save-ChocolateyWebFile 'package' $target $url32 -Url64 $url64 -Checksum $checksum32 -ChecksumType sha256 -Checksum64 $checksum64

            [System.IO.File]::ReadAllBytes($target) | Should -Be $content64
        }

        It 'Uses the checksum type of the 32 bit file for the 64 bit file when no other is given' -Skip:(-not $is64Bit) {
            $target = Join-Path $TestDrive 'bits-type.bin'

            { Save-ChocolateyWebFile 'package' $target $url32 -Url64bit $url64 -Checksum $checksum32 -ChecksumType sha256 -Checksum64 $checksum64 -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Downloads the 32 bit file when 32 bit is forced' {
            $env:chocolateyForceX86 = 'true'
            $target = Join-Path $TestDrive 'bits-forced.bin'

            $null = Save-ChocolateyWebFile 'package' $target $url32 -Url64bit $url64 -Checksum $checksum32 -ChecksumType sha256 -Checksum64 $checksum64

            [System.IO.File]::ReadAllBytes($target) | Should -Be $content32
        }

        It 'Downloads from the address in the override variable' {
            $env:ChocolateyUrlOverride = $url64
            try {
                $target = Join-Path $TestDrive 'override.bin'

                $null = Save-ChocolateyWebFile 'package' $target $url32 -Checksum $checksum64 -ChecksumType sha256

                [System.IO.File]::ReadAllBytes($target) | Should -Be $content64
            }
            finally {
                $env:ChocolateyUrlOverride = $null
            }
        }

        It 'Says that the package does not support the architecture when there is no address' {
            { Save-ChocolateyWebFile 'package' (Join-Path $TestDrive 'none.bin') '' -ErrorAction Stop } | Should -Throw -ExpectedMessage 'This package does not support  architecture.'
        }
    }

    Context 'Addresses' {
        BeforeAll {
            $env:chocolateyAllowEmptyChecksums = 'true'
            $sourceFile = Join-Path $TestDrive 'source.txt'
            Set-Content -Path $sourceFile -Value 'local contents'
        }

        AfterAll {
            $env:chocolateyAllowEmptyChecksums = $null
        }

        It 'Downloads from an address with an upper case scheme' {
            $target = Join-Path $TestDrive 'upper.bin'

            $null = Save-ChocolateyWebFile 'package' $target ($url32 -replace '^http', 'HTTP')

            [System.IO.File]::ReadAllBytes($target) | Should -Be $content32
        }

        It 'Copies a local file from a file address, without needing a checksum' {
            $target = Join-Path $TestDrive 'copied-from-uri.txt'

            Save-ChocolateyWebFile 'package' $target ([System.Uri]$sourceFile).AbsoluteUri | Should -BeExactly $target

            Get-Content $target | Should -BeExactly 'local contents'
        }

        It 'Copies a local file from a path' {
            $target = Join-Path $TestDrive 'copied-from-path.txt'

            $null = Save-ChocolateyWebFile 'package' $target $sourceFile

            Get-Content $target | Should -BeExactly 'local contents'
        }

        It 'Verifies the checksum of a local file when the package gives one' {
            $target = Join-Path $TestDrive 'copied-checksum.txt'

            { Save-ChocolateyWebFile 'package' $target $sourceFile -Checksum ('a' * 64) -ChecksumType sha256 -ErrorAction Stop } | Should -Throw
        }

        It 'Stops before doing anything when the scheme is not supported' {
            $target = Join-Path $TestDrive 'unsupported.bin'
            $caught = $null
            try {
                Save-ChocolateyWebFile 'package' $target 'ftps://example.com/file.bin' -ErrorAction Stop
            }
            catch {
                $caught = $_
            }

            $caught.Exception | Should -BeOfType [System.NotSupportedException]
            $caught.Exception.Message | Should -Match "the scheme 'ftps' is not supported"
            $target | Should -Not -Exist
        }
    }

    Context 'Output' {
        BeforeAll {
            $outputTarget = Join-Path $TestDrive 'output.bin'

            $result = Invoke-TestPowerShell -ModulePath $modulePath -DebugOutput -Command @"
`$env:chocolateyAllowEmptyChecksums = 'true'
Save-ChocolateyWebFile 'my-package' '$outputTarget' '$url32' | ForEach-Object { "Returned:`$_" }
"@
        }

        It 'Says what is being downloaded and from where' {
            $result.Output | Should -Contain "  from '$url32'"
            ($result.Output -join "`n") | Should -Match '(?m)^Downloading my-package\s*$'
        }

        It 'Says that the file was downloaded' {
            $result.Output | Should -Contain "Download of 'output.bin' (8 KB) completed."
        }

        It 'Returns the path' {
            $result.Output | Should -Contain "Returned:$outputTarget"
        }

        It 'Writes the CPU width in the debug output' {
            $result.Output | Should -Contain "DEBUG: CPU is $(if ($is64Bit) { 64 } else { 32 }) bit"
        }

        It 'Says that the address does not have HTTPS available, in the debug output' {
            $result.Output | Should -Contain 'DEBUG: Url does not have HTTPS available'
        }
    }

    Context 'Downloading a file that does not exist' {
        BeforeAll {
            # Reporting the failure asks the host to exit with the exit code, which would end the Pester run,
            # so this is done in a separate process.
            $missingTarget = Join-Path $TestDrive 'missing.bin'

            $result = Invoke-TestPowerShell -ModulePath $modulePath -Command @"
try {
    Save-ChocolateyWebFile 'package' '$missingTarget' '$($server.Url)/missing' -ErrorAction Stop
}
catch {
    "ErrorType:`$(`$_.Exception.GetType().FullName)"
    "ErrorId:`$(`$_.FullyQualifiedErrorId)"
}
"ExitCodeVariable:`$env:ChocolateyExitCode"
"@
        }

        It 'Says that the headers could not be retrieved, and carries on to the download' {
            ($result.Output -join "`n") | Should -Match "Attempt to get headers for $([regex]::Escape($server.Url))/missing failed\."
        }

        It 'Throws a terminating WebException' {
            $result.Output | Should -Contain 'ErrorType:System.Net.WebException'
        }

        It 'Uses the error id for a web exception from this command' {
            ($result.Output -join "`n") | Should -Match 'ErrorId:SaveChocolateyWebFileCommandError\.WebException,'
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
