Import-Module helpers/common-helpers

Describe "choco version" -Tag Chocolatey, VersionCommand -Skip:(Test-ChocolateyVersionEqualOrHigherThan "1.0.0") {
    BeforeAll {
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Version" {
        BeforeAll {
            $Output = Invoke-Choco version
        }

        It "Exits successfully (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Reports that the command is deprecated" {
            $Output.String | Should -Match "command is deprecated"
        }

        # Issue: https://github.com/chocolatey/choco/issues/2048
        It "Tells the user about 'choco outdated'" -Skip:$((-not (Test-ChocolateyVersionEqualOrHigherThan "0.10.16-beta"))) {
            $Output.String | Should -Match "choco outdated"
        }
    }

    Context "Help Documentation (<_>)" -ForEach @("--help", "-?", "-help") {
        BeforeAll {
            $Output = Invoke-Choco version $_
        }

        It "Exits successfully (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Outputs Help for Version" {
            $Output.String | Should -Match "Version Command"
        }

        It "Outputs Options and Switches" {
            $Output.Lines | Should -Contain "Options and Switches"
        }

        It "Warns that the command is deprecated" {
            $Output.String | Should -Match "has been deprecated"
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
