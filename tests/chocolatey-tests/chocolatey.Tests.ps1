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
            "\bin\chocolatey.exe"
            "\bin\cinst.exe"
            "\bin\clist.exe"
            "\bin\cpush.exe"
            "\bin\cuninst.exe"
            "\bin\cup.exe"
        )
        $PowerShellFiles = Get-ChildItem -Path $ChocolateyDirectoriesToCheck -Include "*.ps1", "*.psm1" -Recurse -ErrorAction Ignore
        # For certain test scenarios we run, there are additional files available in the bin directory.
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

        It "has <_> available on PATH" -ForEach @("choco.exe") {
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
    Context "File signing (<_.FullName>)" -ForEach @($PowerShellFiles; $ExecutableFiles; $StrongNamingKeyFiles) -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan "1.0.0"))) {
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
            $command = "try { `$ErrorActionPreference = 'Stop'; Import-Module $FileUnderTest } catch { $_ ; exit 1 }"
            $result = & powershell.exe -noprofile -ExecutionPolicy AllSigned -command $command *>&1
            $LastExitCode | Should -BeExactly $expectedErrors -Because $result
        }
    }

    Context "PowerShell script formatting (<_.FullName>)" -ForEach $PowerShellFiles {
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
    # These are skipped on Proxy tests because the proxy server we use doesn't allow
    # the Windows updates access this needs to install PowerShell 2 support
    Context "PowerShell v2 compatibility" -Skip:(-not $env:TEST_KITCHEN) -Tag ProxySkip {
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
            $command = 'try { $ErrorActionPreference = ''Stop''; Import-Module $env:ChocolateyInstall\helpers\chocolateyInstaller.psm1 } catch { $_ ; exit 1 }'
            $result = & powershell.exe -Version 2 -noprofile -command $command
            $LastExitCode | Should -BeExactly 0 -Because $result
        }

        It "Imports ChocolateyProfile module successfully in PowerShell v2" {
            $command = 'try { $ErrorActionPreference = ''Stop''; Import-Module $env:ChocolateyInstall\helpers\chocolateyProfile.psm1 } catch { $_ ; exit 1 }'
            $result = & powershell.exe -Version 2 -noprofile -command $command
            $LastExitCode | Should -BeExactly 0 -Because $result
        }

        Context "chocolateyScriptRunner.ps1" {
            BeforeAll {
                $Command = @'
& "$env:ChocolateyInstall\helpers\chocolateyScriptRunner.ps1" -packageScript '{0}' -installArguments '' -packageParameters '' -preRunHookScripts '{1}' -postRunHookScripts '{2}'
exit $error.count
'@
            'Write-Host "packageScript"' > packageScript.ps1
            'Write-Host "preRunHookScript"' > preRunHookScript.ps1
            'Write-Host "postRunHookScript"' > postRunHookScript.ps1
            }

            It "Handles just a packageScript" {
                $commandToExecute = $Command -f "$PWD/packageScript.ps1", $null, $null
                $output = & powershell.exe -Version 2 -noprofile -command $commandToExecute
                $LastExitCode | Should -BeExactly 0 -Because ($output -join ([Environment]::NewLine))
                $output | Should -Be @('packageScript') -Because ($output -join ([Environment]::NewLine))
            }

            It "Handles a packageScript with a preRunHookScript" {
                $commandToExecute = $Command -f "$PWD/packageScript.ps1", "$PWD/preRunHookScript.ps1", $null
                $output = & powershell.exe -Version 2 -noprofile -command $commandToExecute
                $LastExitCode | Should -BeExactly 0 -Because ($output -join ([Environment]::NewLine))
                $output | Should -Be @('preRunHookScript','packageScript') -Because ($output -join ([Environment]::NewLine))
            }

            It "Handles a packageScript with a preRunHookScript and postRunHookScript" {
                $commandToExecute = $Command -f "$PWD/packageScript.ps1", "$PWD/preRunHookScript.ps1", "$PWD/postRunHookScript.ps1"
                $output = & powershell.exe -Version 2 -noprofile -command $commandToExecute
                $LastExitCode | Should -BeExactly 0 -Because ($output -join ([Environment]::NewLine))
                $output | Should -Be @('preRunHookScript','packageScript', 'postRunHookScript') -Because ($output -join ([Environment]::NewLine))
            }

            It "Handles a packageScript with and postRunHookScript" {
                $commandToExecute = $Command -f "$PWD/packageScript.ps1", $null, "$PWD/postRunHookScript.ps1"
                $output = & powershell.exe -Version 2 -noprofile -command $commandToExecute
                $LastExitCode | Should -BeExactly 0 -Because ($output -join ([Environment]::NewLine))
                $output | Should -Be @('packageScript', 'postRunHookScript') -Because ($output -join ([Environment]::NewLine))
            }
        }
    }

    # This is skipped when not run in CI because it modifies the local system.
    Context 'License warning is worded properly' -Tag FossOnly,ListCommand,License -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0'))) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $null = Enable-ChocolateySource 'hermes-setup'
            $null = Invoke-Choco install chocolatey-license-business -y
            $Output = Invoke-Choco list
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
    Context 'PowerShell Profile comments updated correctly' -Tag ListCommand, Profile -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0'))) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            Remove-Item $Profile.CurrentUserCurrentHost -ErrorAction Ignore
            New-Item $Profile.CurrentUserCurrentHost -Force
            $chocolatey = (Invoke-Choco list chocolatey -r --exact).Lines | ConvertFrom-ChocolateyOutput -Command List
            Enable-ChocolateySource -Name local
            Enable-ChocolateySource -Name hermes-setup
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
    Context 'PowerShell Profile properly updated when Windows thinks a 5 byte file is signed' -Tag ListCommand, Profile -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.1.0'))) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            New-Item $Profile.CurrentUserCurrentHost -Force
            "" | Set-Content -Path $Profile.CurrentUserCurrentHost -Encoding UTF8
            $chocolatey = (Invoke-Choco list chocolatey -r --exact).Lines | ConvertFrom-ChocolateyOutput -Command List
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
    Context 'Ensure we <Removal> shims during upgrade' -Tag ListCommand, Shims -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0'))) -ForEach @(
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
            Restore-ChocolateyInstallSnapshot
            $chocolatey = (Invoke-Choco list chocolatey -r --exact).Lines | ConvertFrom-ChocolateyOutput -Command List

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

            Enable-ChocolateySource -Name local
            Enable-ChocolateySource -Name hermes-setup
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
            Restore-ChocolateyInstallSnapshot
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
    Context 'Get-FileEncoding works under <_>' -Tag PowerShell7 -ForEach @(
        'pwsh'
        'powershell'
    ) -Skip:((-not $env:TEST_KITCHEN) -or (-not (Test-ChocolateyVersionEqualOrHigherThan '1.1.0'))) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            Enable-ChocolateySource -Name hermes-setup
            $pwshInstall = Invoke-Choco install $_ -y
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
            $null = Invoke-Choco uninstall $_ -y --force-dependencies
            Remove-ChocolateyInstallSnapshot
        }

        It 'LastExitCode should be Success (0)' {
            & $_ -NoProfile -Command $Command
            $LASTEXITCODE | Should -Be 0
        }
    }

    # This is skipped when not run in CI because it modifies the local system.
    Context '.Net Registry is not set' -Skip:(-not $env:TEST_KITCHEN) {
        BeforeAll {
            $RegistryPath = 'SOFTWARE\Wow6432Node\Microsoft\NET Framework Setup\NDP\v4\Full\'
            Set-RegistryKeyOwner -Key $RegistryPath
            $OriginalRelease = Get-ItemPropertyValue -Path "HKLM:\$RegistryPath" -Name Release
            Remove-ItemProperty -Path "HKLM:\$RegistryPath" -Name Release
            $Output = Invoke-Choco help
        }

        AfterAll {
            New-ItemProperty -Path "HKLM:\$RegistryPath" -Name Release -Value $OriginalRelease
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Reports .NET Framework 4.8 is required" {
            $Output.Lines | Should -Contain '.NET 4.8 is not installed or may need a reboot to complete installation.'
        }
    }


    Context 'Chocolatey lib directory missing' {
        BeforeAll {
            New-ChocolateyInstallSnapshot
            Remove-Item -Path $env:ChocolateyInstall/lib/ -Recurse -Force
            $Output = Invoke-Choco list
        }

        AfterAll {
            Remove-ChocolateyInstallSnapshot
        }

        It 'Exits with success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Emits a warning about the missing directory' {
            $Output.Lines | Should -Contain "Directory '$($env:ChocolateyInstall)\lib' does not exist." -Because $Output.String
        }

        It 'Does not emit a NuGet error for the missing directory and fall over' {
            $Output.Lines | Should -Not -Contain "The path '$($env:ChocolateyInstall)\lib' for the selected source could not be resolved."
        }
    }
}
