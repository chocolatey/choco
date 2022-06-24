Import-Module helpers/common-helpers

# This is skipped when not run in CI because it modifies the local system.
Describe "Python Source" -Tag Chocolatey, UpgradeCommand, PythonSource -Skip:(-not $env:TEST_KITCHEN) {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
        # TODO: Internalize Python and most dependencies. (KB perhaps not internalized due to excessive size...)
        $null = Invoke-Choco install python3 --source https://community.chocolatey.org/api/v2/
    }

    AfterAll {
        $null = Invoke-Choco uninstall python3 --remove-dependencies
        Remove-ChocolateyTestInstall
    }

    Context "upgrade <Argument>" -Foreach @(
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
            $Output.ExitCode | Should -Be $ExitCode
        }

        It 'Outputs properly' {
            $Output.Lines | Should -Not:($ExitCode -eq 0) -Contain 'The all keyword is not available for alternate sources'
            $Output.Lines | Should  -Contain "Chocolatey upgraded $Count/$Count packages."
        }
    }
}
