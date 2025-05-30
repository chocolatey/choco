Import-Module helpers/common-helpers

Describe "choco support" -Tag Chocolatey, SupportCommand {
    BeforeDiscovery {
        $HasLicensedExtension = Test-PackageIsEqualOrHigher -PackageName 'chocolatey.extension' -Version '6.0.0'
    }

    BeforeAll {
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Support" {
        BeforeAll {
            $Output = Invoke-Choco support
        }

        It "Exits successfully (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Reports support options" {
            $ExpectedOutput = if($HasLicensedExtension) {
                "Howdy, you have access to private support channels."
            } else {
                "Unfortunately, we are unable to provide private support for"
            }
            $Output.Lines | Should -Contain $ExpectedOutput -Because $Output.String
        }
    }

    Context "Help Documentation (<_>)" -ForEach @("--help", "-?", "-help") {
        BeforeAll {
            $Output = Invoke-Choco support $_
        }

        It "Exits successfully (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Outputs Help for Support" {
            $Output.String | Should -Match "Support Command" -Because $Output.String
        }

        It "Outputs Help for Support" {
            $ExpectedOutput = if($HasLicensedExtension) {
                "As a licensed customer, you can reach out to"
            } else {
                "As an open-source user of Chocolatey CLI, we are not able to"
            }
            $Output.Lines | Should -Contain $ExpectedOutput -Because $Output.String
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
