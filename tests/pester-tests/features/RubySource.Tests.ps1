Import-Module helpers/common-helpers

# This is skipped when not run in CI because it modifies the local system.
Describe "Ruby Source" -Tag Chocolatey, RubySource -Skip:(-not $env:TEST_KITCHEN) {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
        Enable-ChocolateySource -Name hermes-setup
        $null = Invoke-Choco install ruby.portable
    }

    AfterAll {
        $null = Invoke-Choco uninstall ruby.portable --remove-dependencies
        Remove-ChocolateyTestInstall
    }

    Context "install all" {
        BeforeAll {
            $Output = Invoke-Choco install all --source=ruby
        }

        It 'Exits with exit code (1)' {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It 'Outputs exception' {
            $Output.Lines | Should -Contain "Alternative sources do not allow the use of the 'all' package name/keyword." -Because $Output.String
        }
    }
}