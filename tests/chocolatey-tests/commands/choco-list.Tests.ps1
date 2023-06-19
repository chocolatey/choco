Import-Module helpers/common-helpers

Describe "choco list" -Tag Chocolatey, ListCommand {
    BeforeAll {
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall -Source $PSScriptRoot\testpackages
        Invoke-Choco install installpackage --version 1.0.0 --confirm
        Invoke-Choco install upgradepackage --version 1.0.0 --confirm
        $VersionRegex = "[^v]\d+\.\d+\.\d+"
        # Ensure that we remove any compatibility package before running the tests
        $null = Invoke-Choco uninstall chocolatey-compatibility.extension -y --force
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Listing local packages" {
        BeforeAll {
            $Output = Invoke-Choco list
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Should contain packages and version with a space between them" {
            $Output.Lines | Should -Contain "upgradepackage 1.0.0"
        }

        It "Should not contain pipe-delimited packages and versions" {
            $Output.Lines | Should -Not -Contain "upgradepackage|1.0.0"
        }

        It "Should contain a summary" {
            $Output.String | Should -Match "\d+ packages installed"
        }
    }

    Context "Listing local packages (limiting output)" {
        BeforeAll {
            $Output = Invoke-Choco list --LimitOutput
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Should not contain packages and version with a space between them" {
            $Output.Lines | Should -Not -Contain "upgradepackage 1.0.0"
        }

        It "Should contain pipe-delimited packages and versions" {
            $Output.Lines | Should -Contain "upgradepackage|1.0.0"
        }

        It "Should not contain a summary" {
            $Output.String | Should -Not -Match "\d+ packages installed"
        }
    }

    Context "Listing local packages (limiting output, ID only)" {
        BeforeAll {
            $Output = Invoke-Choco list --IdOnly
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Should contain package name(s)" {
            $Output.Lines | Should -Contain "upgradepackage"
        }

        It "Should not contain any version numbers" {
            $Output.String | Should -Not -Match $VersionRegex
        }
    }

    Context "Listing local packages with paging" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install hasdependency --confirm

            $Output = Invoke-Choco list dependency --page 1 --page-size 2 --id-only
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Should contain package isexactversiondependency" {
            $Output.Lines | Should -Contain isexactversiondependency
        }

        It "Should not contain package <_>" -Foreach @('isdependency', 'hasdependency') {
            $Output.Lines | Should -Not -Contain $_
        }
    }

    Context "Listing local packages with unsupported argument errors out" -ForEach @('-l', '-lo', '--lo', '--local', '--localonly', '--local-only', '--order-by-popularity', '-a', '--all', '--allversions', '--all-versions', '-li', '-il', '-lai', '-lia', '-ali', '-ail', '-ial', '-ila') {
        BeforeAll {
            $Output = Invoke-Choco list $_
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Should output expected error message" {
            $Output.Lines | Should -Contain "Invalid argument $_. This argument has been removed from the list command and cannot be used." -Because $Output.String
        }
    }

    Context "Listing local packages with unsupported argument and --limit-output allows listing packages" -Foreach @('-l', '-lo', '--lo', '--local', '--localonly', '--local-only', '--order-by-popularity', '-a', '--all', '--allversions', '--all-versions', '-li', '-il', '-lai', '-lia', '-ali', '-ail', '-ial', '-ila') {
        BeforeAll {
            $Output = Invoke-Choco list $_ --limit-output
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Should not output the error message" {
            $Output.Lines | Should -Not -Contain "Invalid argument $_. This argument has been removed from the list command and cannot be used." -Because $Output.String
        }
    }
}
