Import-Module helpers/common-helpers

Describe "choco rule" -Tag Chocolatey, RuleCommand {
    BeforeDiscovery {
    }

    BeforeAll {
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Running without subcommand specified" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco rule
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Displays the rules expected" {
            $Output.Lines | Should -Contain 'CHCR0001: A required element is missing or has no content in the package nuspec file.' -Because $Output.String
            $Output.Lines | Should -Contain 'CHCR0002: Enabling license acceptance requires a license url.' -Because $Output.String
            $Output.Lines | Should -Contain 'CHCU0001: The specified content of the element is not of the expected type and can not be accepted.' -Because $Output.String
            $Output.Lines | Should -Contain 'CHCU0002: Unsupported element is used.' -Because $Output.String
        }
    }

    Context "Running with list subcommand" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco rule list
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Displays the rules expected" {
            $Output.Lines | Should -Contain 'CHCR0001: A required element is missing or has no content in the package nuspec file.' -Because $Output.String
            $Output.Lines | Should -Contain 'CHCR0002: Enabling license acceptance requires a license url.' -Because $Output.String
            $Output.Lines | Should -Contain 'CHCU0001: The specified content of the element is not of the expected type and can not be accepted.' -Because $Output.String
            $Output.Lines | Should -Contain 'CHCU0002: Unsupported element is used.' -Because $Output.String
        }
    }

    Context "Running with get subcommand specified with no additional parameters" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco rule get
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Displays error with correct format" {
            $Output.Lines | Should -Contain "A Rule Name (-n|--name) is required when getting information for a specific rule." -Because $Output.String
        }
    }

    Context "Running with get subcommand specified with --name parameter" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco rule get --name CHCU0001
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Displays rule information" {
            $Output.Lines | Should -Contain "Name: CHCU0001 | Severity: Error" -Because $Output.String
            $Output.Lines | Should -Contain "Summary: The specified content of the element is not of the expected type and can not be accepted." -Because $Output.String
            $Output.Lines | Should -Contain "Help URL:" -Because $Output.String
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}