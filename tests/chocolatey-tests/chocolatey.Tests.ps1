Import-Module helpers/common-helpers

Describe "Ensuring Chocolatey is correctly installed" -Tag Environment, Chocolatey {
    BeforeDiscovery {
        $ChocolateyDirectoriesToCheck = @(
            "$env:ChocolateyInstall\helpers"
            "$env:ChocolateyInstall\extensions\chocolatey"
            "$env:ChocolateyInstall\bin"
        )
        $StrongNamingKeyFilesToCheck = @(
            "$env:ChocolateyInstall\choco.exe"
            "$env:ChocolateyInstall\extensions\chocolatey\chocolatey.licensed.dll"
        )
        $RemovedShims = @(
            "\bin\cpack.exe"
            "\bin\cver.exe"
        )
        $PowerShellFiles = Get-ChildItem -Path $ChocolateyDirectoriesToCheck -Include "*.ps1", "*.psm1" -Recurse -ErrorAction Ignore
        # For certain test scenarious we run, there are additional files available in the bin directory.
        # These files should not be tested as part of the signing check.
        $ExecutableFiles = Get-ChildItem -Path $ChocolateyDirectoriesToCheck -Include "*.exe", "*.dll" -Recurse -ErrorAction Ignore | Where-Object Name -NotMatch 'driver\.exe$'
        $StrongNamingKeyFiles = Get-ChildItem -Path $StrongNamingKeyFilesToCheck -ErrorAction Ignore
    }

    BeforeAll {
        # TODO: Both this thumbprint and strong name key should be in an environment variable. Update when new kitchen-pester is available. - https://github.com/chocolatey/choco/issues/2692
        $ChocolateyThumbprint = '83AC7D88C66CB8680BCE802E0F0F5C179722764B'
        $ChocolateyStrongNameKey = '79d02ea9cad655eb'
        # These lines are part of testing the issue
        # https://github.com/chocolatey/choco/issues/2233
        # to see if the nuget.config is not created by
        # chocolatey.
        $path = "$env:APPDATA\NuGet\nuget.config"
        if (Test-Path $path) {
            Remove-Item $path
        }
        $null = Invoke-Choco outdated
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context 'Chocolatey' {
        It 'exists in $env:ChocolateyInstall folder' {
            Join-Path $env:ChocolateyInstall -ChildPath "choco.exe" | Should -Exist
        }

        It "has <_> available on PATH" -ForEach @("choco.exe"; "cinst.exe"; "cpush.exe") {
            $executable = $_
            $pathList = $env:PATH -split ";"
            $found = $false
            foreach ($path in $pathList) {
                $found = Test-Path (Join-Path $path $executable)
                if ($found) {
                    break
                }
            }

            $found | Should -BeTrue
        }

        It 'has Chocolatey in the /lib folder' {
            Join-Path $env:ChocolateyInstall "lib/chocolatey" | Should -Exist
        }

        It 'has added choco.exe to the bin folder' {
            Join-Path $env:ChocolateyInstall "bin/choco.exe" | Should -Exist
        }

        It 'has added the bin folder to PATH' {
            $env:PATH.Split(';') | Should -Contain (Join-Path $env:ChocolateyInstall "bin")
        }

        It "Outputs the version when run with --version" {
            $Output = Invoke-Choco --version
            $script:CurrentVersion = $Output.String
            $Output.ExitCode | Should -Be 0
            $LastExitCode | Should -Be 0
            ($Output.String -split '-' | Select-Object -First 1) -as [version] | Should -BeTrue
        }

        # Only FossOnly for this one, as we do not want to get the version
        # from an existing --version output
        It "Displays the current version and instructions for help when run without arguments" -Tag FossOnly {
            if (-not $script:CurrentVersion) {
                $script:CurrentVersion = Invoke-Choco --version | ForEach-Object String
            }

            # We can not use `Invoke-Choco` in this case
            # as it sets the --allow-unofficial argument
            # that causes the expected output to not show up
            $choco = Get-ChocoPath

            $Output = & $choco

            $LastExitCode | Should -Be 1

            $Output | Should -Contain "Chocolatey v$($script:CurrentVersion)"
            $Output | Should -Contain "Please run 'choco -?' or 'choco <command> -?' for help menu."
        }
    }

    # This is skipped when not run in CI because it modifies the local system.
    # Issue: https://github.com/chocolatey/choco/issues/2233
    It "Does not create nuget configuration file in application data" -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan "0.10.16-beta"))) {
        $path | Should -Not -Exist
    }

    # This is skipped when not run in CI because it requires signed executables.
    Context "File signing (<_.FullName>)" -Foreach @($PowerShellFiles; $ExecutableFiles; $StrongNamingKeyFiles) -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan "1.0.0"))) {
        BeforeAll {
            $FileUnderTest = $_
            $SignerCert = (Get-AuthenticodeSignature (Get-ChocoPath)).SignerCertificate
            $Cert = "$PWD\cert.cer"
            # Write out the certificate
            [IO.File]::WriteAllBytes($Cert, $SignerCert.export([security.cryptography.x509certificates.x509contenttype]::cert))
            # Trust the certificate
            Import-Certificate -FilePath $Cert -CertStoreLocation 'Cert:\CurrentUser\TrustedPublisher\'
            Remove-Item -Path $Cert -Force -ErrorAction Ignore
        }

        AfterAll {
            Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
        }

        It "Should be signed with our certificate" -Skip:($_.Name -like 'package*.exe') {
            $authenticodeSignature = Get-AuthenticodeSignature $FileUnderTest
            $authenticodeSignature.Status | Should -Be 'Valid'
            $authenticodeSignature.SignerCertificate.Thumbprint | Should -Be $ChocolateyThumbprint
        }

        It "Should be strongly named with our strong name key" -Skip:($_ -notin $StrongNamingKeyFilesToCheck) {
            $Token = [System.Reflection.AssemblyName]::GetAssemblyName($FileUnderTest.FullName).FullName -replace ".+PublicKeyToken=(?<Token>\w+)", '$1'
            $Token | Should -Be $ChocolateyStrongNameKey
        }

        # This is FossOnly for now as there are some undetermined errors here that do not seem to present inside of Chocolatey. https://gitlab.com/chocolatey/build-automation/chocolatey-test-kitchen/-/issues/39
        It "Should be able to run the script in AllSigned mode" -Skip:($_ -notin $PowerShellFiles) -Tag FossOnly {
            $expectedErrors = 0
            $command = "Import-Module $FileUnderTest -ErrorAction SilentlyContinue; exit `$error.count"
            & powershell.exe -noprofile -ExecutionPolicy AllSigned -command $command 2>$null
            $LastExitCode | Should -BeExactly $expectedErrors
        }
    }

    Context "PowerShell script formatting (<_.FullName>)" -Foreach $PowerShellFiles {
        BeforeAll {
            $FileUnderTest = $_
        }

        It "Should have a Byte Order Mark" {
            $encoding = Test-ByteOrderMark -Path $FileUnderTest
            $encoding | Should -Be ([System.Text.Encoding]::UTF8)
        }

        It "Should have 'CRLF' Line Endings" {
            (Get-Content $FileUnderTest -Raw) -match '([^\r]\n|\r[^\n])' | Should -BeFalse
        }
    }

    # These tests are not a true test of PowerShell v2 compatibility as -Version 2 does not guarantee that things run exactly as in a PowerShell 2 instance, but it is as close as we can get in a testing environment.
    # Full proper testing on v2 would require a VM with only v2 installed.
    # This is skipped when not run in CI because it modifies the local system.
    Context "PowerShell v2 compatibility" -Skip:(-not $env:TEST_KITCHEN) {
        BeforeAll {
            # TODO: This doesn't work on client OSes (might be Install-WindowsOptionalFeature). Make sure this works on both server and client.
            Install-WindowsFeature powershell-v2
            # TODO: This doesn't work on Windows server.
            # $null = Invoke-Choco install dotnet3.5
            # $null = Invoke-Choco install MicrosoftWindowsPowerShellV2 -s windowsfeatures
        }

        # This is Foss only as PowerShell running under version 2 doesn't have .net available and can't import the Licensed DLL.
        # Tests on Windows 7 show no issues with running Chocolatey under Windows 7 with PowerShell v2 aside from issues surrounding TLS versions that we cannot resolve without an upgrade to Windows 7.
        It "Imports ChocolateyInstaller module successfully in PowerShell v2" -Tag FossOnly {
            $command = 'Import-Module $env:ChocolateyInstall\helpers\chocolateyInstaller.psm1;exit $error.count'
            & powershell.exe -Version 2 -noprofile -command $command
            $LastExitCode | Should -BeExactly 0
        }

        It "Imports ChocolateyProfile module successfully in PowerShell v2" {
            $command = 'Import-Module $env:ChocolateyInstall\helpers\chocolateyProfile.psm1;exit $error.count'
            & powershell.exe -Version 2 -noprofile -command $command
            $LastExitCode | Should -BeExactly 0
        }
    }

    # This is skipped when not run in CI because it modifies the local system.
    Context 'License warning is worded properly' -Tag FossOnly -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0'))) {
        BeforeAll {
            $null = Invoke-Choco install chocolatey-license-business -y
            $Output = Invoke-Choco list -lo
        }

        AfterAll {
            $null = Invoke-Choco uninstall chocolatey-license-business -y
        }

        It 'Should display warning' {
            $Output.Lines | Should -Contain 'A valid chocolatey license was found, but the chocolatey.licensed.dll assembly could not be loaded:'
            $Output.Lines | Should -Contain 'Ensure that the chocolatey.licensed.dll exists at the following path:'
            $Output.Lines | Should -Contain 'To resolve this, install the Chocolatey Licensed Extension package with'
            $Output.Lines | Should -Contain '`choco install chocolatey.extension`'
        }
    }

    # This is skipped when not run in CI because it modifies the local system.
    Context 'PowerShell Profile comments updated correctly' -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0'))) {
        BeforeAll {
            Remove-Item $Profile.CurrentUserCurrentHost -ErrorAction Ignore
            New-Item $Profile.CurrentUserCurrentHost -Force
            $chocolatey = (Invoke-Choco list chocolatey -lo -r --exact).Lines | ConvertFrom-ChocolateyOutput -Command List
            $null = Invoke-Choco install chocolatey -f --version $chocolatey.Version
        }

        AfterAll {
            Remove-Item $Profile.CurrentUserCurrentHost -ErrorAction Ignore
        }

        It 'should update profile successfully' {
            $ProfileContents = Get-Content $Profile.CurrentUserCurrentHost -Raw
            $expectedLines = @(
                '# Import the Chocolatey Profile that contains the necessary code to enable'
                '# tab-completions to function for `choco`.'
                '# Be aware that if you are missing these lines from your profile, tab completion'
                '# for `choco` will not function.'
                '# See https://ch0.co/tab-completion for details.'
            ) -join '\r?\n'
            $ProfileContents | Should -Match $expectedLines
        }
    }

    # This is skipped when not run in CI because it modifies the local system.
    Context 'PowerShell Profile properly updated when Windows thinks a 5 byte file is signed' -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.1.0'))) {
        BeforeAll {
            New-Item $Profile.CurrentUserCurrentHost -Force
            "" | Set-Content -Path $Profile.CurrentUserCurrentHost -Encoding UTF8
            $chocolatey = (Invoke-Choco list chocolatey -lo -r --exact).Lines | ConvertFrom-ChocolateyOutput -Command List
        }

        AfterAll {
            Remove-Item $Profile.CurrentUserCurrentHost -ErrorAction Ignore
        }

        It 'should not report the profile being signed' {
            $ProfileFile = Get-ChildItem $Profile.CurrentUserCurrentHost
            $ProfileFile.Length | Should -Be 5 -Because 'The Profile should have been set to mostly empty UTF8BOM file...'
            $ProfileSignature = Get-AuthenticodeSignature $Profile.CurrentUserCurrentHost
            if ($ProfileSignature.Status -ne 'Valid') {
                Set-ItResult -Skipped -Because 'Windows is not detecting the profile as signed.'
            }
            $output = Invoke-Choco install chocolatey -f --version $chocolatey.Version
            $output.Lines | Should -Not -Contain 'WARNING: Not setting tab completion: File is Authenticode signed at'
        }
    }

    # This is skipped when not run in CI because it requires signed executables
    Context 'Ensure we <Removal> shims during upgrade' -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0'))) -Foreach @(
        @{
            RemovedShims = $RemovedShims
            Signed       = $true
            Removal      = "remove signed"
        }
        @{
            RemovedShims = $RemovedShims
            Signed       = $false
            Removal      = "keep unsigned"
        }
    ) {
        BeforeAll {
            $chocolatey = (Invoke-Choco list chocolatey -lo -r --exact).Lines | ConvertFrom-ChocolateyOutput -Command List

            foreach ($shim in $RemovedShims) {
                $shimToRemove = "$env:ChocolateyInstall$shim"
                Remove-Item $shimToRemove -ErrorAction Ignore
                if ($signed) {
                    Copy-Item -Path $env:ChocolateyInstall/bin/choco.exe -Destination $shimToRemove
                }
                else {
                    New-Item $shimToRemove -Force -ItemType File
                }
            }

            $Output = Invoke-Choco install chocolatey -f --version $chocolatey.Version --no-progress
        }

        It 'should exit Success (0)' {
            $Output.ExitCode | Should -Be 0
        }

        It 'should <removal> shim <_> on upgrade' -ForEach $RemovedShims {
            "$env:ChocolateyInstall$_" | Should -Not:$signed -Exist -Because $Output.String
        }

        It 'should report keeping <_> during upgrade' -ForEach $RemovedShims -Skip:$signed {
            $Output.Lines | Should -Contain "WARNING: Shim found in $env:ChocolateyInstall$_, but was not signed. Ignoring Removal..." -Because $Output.String
        }
    }

    Context 'Ensure a corrupted config file does not cause errors' -Tag ConfigFile -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan '1.1.0')) {
        BeforeAll {
            $ChocolateyConfigLocation = "$env:ChocolateyInstall/config/chocolatey.config"
            $BadContent = "<chocolatey></chocolatey>BadFile"
            # Make sure we have a chocolatey config file
            $null = Invoke-Choco outdated
            $BadContent | Out-File -FilePath $ChocolateyConfigLocation
            $Output = Invoke-Choco outdated
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0
        }

        It 'Should remove the invalid configuration file' {
            $ChocolateyConfigLocation | Should -Not -FileContentMatch "$BadContent"
        }
    }

    # This is skipped when not run in CI because it modifies the local system.
    Context 'Get-FileEncoding works under <_>' -Tag PowerShell7 -Foreach @(
        'pwsh'
        'powershell'
    ) -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.1.0'))) {
        BeforeAll {
            New-ChocolateyInstallSnapshot
            # TODO: Internalize pwsh and powershell packages...
            $pwshInstall = Invoke-Choco install $_ -y -s https://community.chocolatey.org/api/v2/
            $ChocoUnzipped = "$(Get-TempDirectory)$(New-Guid)"
            $modulePath = "$ChocoUnzipped/tools/chocolateySetup.psm1"

            if (Test-Path $ChocoUnzipped) {
                Remove-Item $ChocoUnzipped -Force -Recurse
            }

            Expand-ZipArchive -Source $env:ChocolateyInstall/lib/chocolatey/chocolatey.nupkg -Destination $ChocoUnzipped

            if (-not (Test-Path $modulePath)) {
                throw "Something has happened that the module doesn't exist at $modulePath. Please see directory contents for more information: $(Get-ChildItem $ChocoUnzipped -Recurse -Force | ForEach-Object { $_.FullName } | Out-String)"
            }

            $Command = @"
            Import-Module $modulePath -Force
            & (Get-Module chocolateySetup) {
                Get-FileEncoding $modulePath
            }
            exit `$error.Count
"@
            Import-Module $env:ChocolateyInstall/helpers/ChocolateyProfile.psm1
            Update-SessionEnvironment
        }

        AfterAll {
            if (Test-Path $ChocoUnzipped) {
                Remove-Item $ChocoUnzipped -Force -Recurse
            }
            $null = Invoke-Choco uninstall $_ -y -s https://community.chocolatey.org/api/v2/ --force-dependencies
            Remove-ChocolateyInstallSnapshot
        }

        It 'LastExitCode should be Success (0)' {
            & $_ -NoProfile -Command $Command
            $LASTEXITCODE | Should -Be 0
        }
    }
}
