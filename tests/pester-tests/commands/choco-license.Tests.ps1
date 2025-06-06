Import-Module helpers/common-helpers

Describe "choco license" -Tag Chocolatey, LicenseCommand {
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

    Context "License (<_>)" -ForEach @("", "info", "bob") -Skip:($HasLicensedExtension) {
        BeforeAll {
            $Output = Invoke-Choco license $_
        }

        It "Exits successfully (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Reports available license" {
            $ExpectedOutput = if($HasLicensedExtension) {
                "Registered to:"
            } else {
                "No Chocolatey license found."
            }
            $Output.Lines | Should -Contain $ExpectedOutput -Because $Output.String
        }
    }

    Context "Help Documentation (<_>) - OSS" -ForEach @("--help", "-?", "-help") {
        BeforeAll {
            $Output = Invoke-Choco license $_
        }

        It "Exits successfully (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Outputs Help for License" {
            $Output.String | Should -Match "License Command" -Because $Output.String
        }

        It "Outputs help documentation for license command" {
            $Output.Lines | Should -Contain "Show information about the current Chocolatey CLI license." -Because $Output.String
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}