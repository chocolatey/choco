Import-Module helpers/common-helpers

# This is skipped when not run in CI because it modifies the local system.
Describe "Cygwin Source" -Tag Chocolatey, CygwinSource -Skip:(-not $env:TEST_KITCHEN) {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
        Enable-ChocolateySource -Name hermes-setup
        $null = Invoke-Choco install cygwin
    }

    AfterAll {
        $null = Invoke-Choco uninstall cygwin --remove-dependencies
        Remove-ChocolateyTestInstall
    }

    Context "install all" {
        BeforeAll {
            $Output = Invoke-Choco install all --source=cygwin
        }

        It 'Exits with exit code (1)' {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It 'Outputs exception' {
            $Output.Lines | Should -Contain "Alternative sources do not allow the use of the 'all' package name/keyword." -Because $Output.String
        }
    }
}