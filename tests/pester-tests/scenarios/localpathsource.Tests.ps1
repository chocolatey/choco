Describe "Actions from the root of a drive" -Tag Scenario {
        BeforeAll {
        Initialize-ChocolateyTestInstall
        Push-Location '\'
        Invoke-Choco new roottest --version 1.0.0
        Remove-Item roottest/tools/*.ps1 -ErrorAction SilentlyContinue
        Invoke-Choco pack roottest/roottest.nuspec
    }

    AfterAll {
        Remove-ChocolateyTestInstall
        Remove-Item roottest, roottest.1.0.0.nupkg -Force -Recurse -ErrorAction SilentlyContinue
        Pop-Location
    }

    Context "Searching with <_> '.' source at the root of a drive" -ForEach @('find', 'search') {
        BeforeAll {
            $Output = Invoke-Choco $_ --source="'.'"
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Does not output message about being unable to parse a source' {
            $Output.Lines | Should -Not -Contain "Source '.' is unable to be parsed" -Because $Output.String
        }

        It "Finds the package expected" {
            $Output.Lines | Should -Contain "roottest 1.0.0" -Because $Output.String
        }
    }

    Context "Installing from '.' source using <_> at the root of a drive" -ForEach @( 'install', 'upgrade' ) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $Output = Invoke-Choco $_ roottest --source="'.'"
        }

        AfterAll {
            Remove-ChocolateyInstallSnapshot
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Does not output message about being unable to parse a source' {
            $Output.Lines | Should -Not -Contain "Source '.' is unable to be parsed" -Because $Output.String
        }

        It "Finds the package expected" {
            $Output.Lines | Should -Contain "roottest v1.0.0" -Because $Output.String
        }

        It "Successfully updates the package" {
            $Action = $_.TrimEnd('e') + 'ed'
            $Output.Lines | Should -Contain "Chocolatey $Action 1/1 packages." -Because $Output.String
        }
    }
}
