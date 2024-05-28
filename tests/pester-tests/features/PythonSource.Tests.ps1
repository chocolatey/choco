﻿Import-Module helpers/common-helpers

# This is skipped when not run in CI because it modifies the local system.
# This is skipped on Proxy as Python needs to reach out to pypi which our proxy server does not allow.
Describe "Python Source" -Tag Chocolatey, UpgradeCommand, PythonSource, ProxySkip -Skip:(-not $env:TEST_KITCHEN) {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
        Enable-ChocolateySource -Name hermes-setup
        $null = Invoke-Choco install python3
    }

    AfterAll {
        $null = Invoke-Choco uninstall python3 --remove-dependencies
        Remove-ChocolateyTestInstall
    }

    Context "upgrade <Argument>" -ForEach @(
        @{ Argument = 'all' ; ExitCode = 1 ; Count = 0 }
        @{ Argument = 'wheel' ; ExitCode = 0 ; Count = 1 }
    )  -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan '1.1.0')) {
        BeforeAll {
            # For some reason under kitchen-pester we don't have pip on the path. This might be due to our snapshotting...
            Import-Module $env:ChocolateyInstall/helpers/ChocolateyProfile.psm1
            Update-SessionEnvironment
            $Output = Invoke-Choco upgrade $Argument --source=python
        }

        It 'Exits with correct exit code (<ExitCode>)' {
            $Output.ExitCode | Should -Be $ExitCode -Because $Output.String
        }

        It 'Outputs properly' {
            $Output.Lines | Should -Not:($ExitCode -eq 0) -Contain "Alternative sources do not allow the use of the 'all' package name/keyword." -Because $Output.String
            $Output.Lines | Should  -Contain "Chocolatey upgraded $Count/$Count packages." -Because $Output.String
        }
    }

    Context "install all" {
        BeforeAll {
            # For some reason under kitchen-pester we don't have pip on the path. This might be due to our snapshotting...
            Import-Module $env:ChocolateyInstall/helpers/ChocolateyProfile.psm1
            Update-SessionEnvironment
            $Output = Invoke-Choco install all --source=python
        }

        It 'Exits with exit code (1)' {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It 'Outputs exception' {
            $Output.Lines | Should -Contain "Alternative sources do not allow the use of the 'all' package name/keyword." -Because $Output.String
        }
    }
}
