Import-Module helpers/common-helpers

# https://github.com/chocolatey/choco/blob/master/src/chocolatey.tests.integration/scenarios/PinScenarios.cs

Describe "choco pin" -Tag Chocolatey, PinCommand {
    BeforeAll {
        $testPackageLocation = "$(Get-TempDirectory)ChocolateyTests\packages"
        Initialize-ChocolateyTestInstall -Source $testPackageLocation

        Invoke-Choco install installpackage --version 1.0.0 -confirm
        Invoke-Choco install upgradepackage --version 1.0.0 -confirm

        New-ChocolateyInstallSnapshot

        $listSuffix = ""
        if (Test-PackageIsEqualOrHigher "chocolatey.extension" "0.0.0") {
            # Licensed edition adds an extra `|` at the end
            $listSuffix = "|"
        }
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Listing Pins with no Pins" {
        BeforeAll {
            $Output = Invoke-Choco pin list --limitoutput
        }

        It "Exits with ExitCode 0" {
            $Output.ExitCode | Should -Be 0
        }

        It "Has no Pins listed" {
            $Output.Lines | Should -BeNullOrEmpty
        }
    }

    Context "Listing Pins with an existing Pin" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco pin add --name upgradepackage

            $Output = Invoke-Choco pin list --limitoutput
        }

        It "Exits with ExitCode 0" {
            $Output.ExitCode | Should -Be 0
        }

        It "Lists the new Pin" {
            $Output.Lines | Should -Contain "upgradepackage|1.0.0$listSuffix"
        }
    }

    Context "Listing Pins with existing Pins" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco pin add --name upgradepackage
            $null = Invoke-Choco pin add --name installpackage

            $Output = Invoke-Choco pin list --limitoutput
        }

        It "Exits with ExitCode 0" {
            $Output.ExitCode | Should -Be 0
        }

        It "Lists the Pins" {
            $Output.Lines | Should -Contain "upgradepackage|1.0.0$listSuffix"
            $Output.Lines | Should -Contain "installpackage|1.0.0$listSuffix"
        }
    }

    Context "Setting a Pin for an installed Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco pin add --name upgradepackage
            $CurrentPins = Invoke-Choco pin list --LimitOutput | ForEach-Object Lines
        }

        It "Exits with ExitCode 0" {
            $Output.ExitCode | Should -Be 0
        }

        It "Sets the Pin" {
            $Output.Lines | Should -Contain "Successfully added a pin for upgradepackage v1.0.0."
            $CurrentPins | Should -Contain "upgradepackage|1.0.0$listSuffix"
        }
    }

    Context "Setting a Pin for an already pinned Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco pin add --name upgradepackage

            $Output = Invoke-Choco pin add --name upgradepackage
            $CurrentPins = Invoke-Choco pin list --LimitOutput | ForEach-Object Lines
        }

        It "Exits with ExitCode 0" {
            $Output.ExitCode | Should -Be 0
        }

        It "Changes Nothing" {
            $Output.Lines | Should -Contain "Nothing to change. Pin already set or removed."
            $CurrentPins | Should -Contain "upgradepackage|1.0.0$listSuffix"
        }
    }

    Context "Setting a Pin for a non-installed Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco pin add --name whatisthis
            $CurrentPins = Invoke-Choco pin list --LimitOutput | ForEach-Object Lines
        }

        It "Exits with ExitCode 1" {
            $Output.ExitCode | Should -Be 1
        }

        It "Outputs a message indicating the failure" {
            $Output.Lines | Should -Contain "Unable to find package named 'whatisthis' to pin. Please check to ensure it is installed."
        }

        It "Hasn't pinned the package" {
            $CurrentPins | Should -Not -Match "whatisthis"
        }
    }

    Context "Removing a Pin for a pinned Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco pin add --name upgradepackage

            $Output = Invoke-Choco pin remove --name upgradepackage
            $CurrentPins = Invoke-Choco pin list --LimitOutput | ForEach-Object Lines
        }

        It "Exits with ExitCode 0" {
            $Output.ExitCode | Should -Be 0
        }

        It "Removed the Pin" {
            $Output.Lines | Should -Contain "Successfully removed a pin for upgradepackage v1.0.0."
            $CurrentPins | Should -Not -Match "^upgradepackage"
        }
    }

    Context "Removing a Pin for an unpinned Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco pin remove --name upgradepackage
        }

        It "Exits with ExitCode 0" {
            $Output.ExitCode | Should -Be 0
        }

        It "Changes Nothing" {
            $Output.Lines | Should -Contain "Nothing to change. Pin already set or removed."
        }
    }

    Context "Removing a Pin for a non-installed Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco pin remove -n whatisthis
        }

        It "Exits with ExitCode 1" {
            $Output.ExitCode | Should -Be 1
        }

        It "Outputs a message indicating the failure" {
            $Output.Lines | Should -Contain "Unable to find package named 'whatisthis' to pin. Please check to ensure it is installed."
        }
    }
}
