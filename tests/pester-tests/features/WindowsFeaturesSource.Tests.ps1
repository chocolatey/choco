Import-Module helpers/common-helpers

Describe "Windows Features Source" -Tag Chocolatey, WindowsFeaturesSource {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "install all" {
        BeforeAll {
            $Output = Invoke-Choco install all --source=windowsfeatures
        }

        It 'Exits with exit code (1)' {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It 'Outputs exception' {
            $Output.Lines | Should -Contain "Alternative sources do not allow the use of the 'all' package name/keyword." -Because $Output.String
        }
    }
}