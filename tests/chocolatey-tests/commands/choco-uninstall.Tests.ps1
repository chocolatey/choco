Import-Module helpers/common-helpers

Describe "choco uninstall" -Tag Chocolatey, UninstallCommand {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Uninstalling a side-by-side Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco upgrade $PackageUnderTest --confirm --allowmultipleversions

            $Output = Invoke-Choco uninstall $PackageUnderTest --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Removed a package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0" | Should -Not -Exist
            "$env:ChocolateyInstall\lib-bad\$($PackageUnderTest).1.0.0" | Should -Not -Exist
        }

        It "Outputs a warning message that installed side by side package is deprecated" {
            $Output.Lines | Should -Contain "$PackageUnderTest has been installed as a side by side installation." -Because $Output.String
            $Output.Lines | Should -Contain "Side by side installations are deprecated and is pending removal in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it uninstalled the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 1/1 packages." -Because $Output.String
        }
    }

    Context "Uninstalling package that makes use of new Get Chocolatey Path helper" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            Enable-ChocolateySource -Name 'local'

            $null = Invoke-Choco install test-chocolateypath -y

            $Output = Invoke-Choco uninstall test-chocolateypath -y
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Outputs message <_>" -ForEach @(
            'Package Path in Before Modify Script: <installPath>\lib\test-chocolateypath'
            'Install Path in Before Modify Script: <installPath>'
            'Package Path in Uninstall Script: <installPath>\lib\test-chocolateypath'
            'Install Path in Uninstall Script: <installPath>'
        ) {
            $Output.Lines | Should -Contain ($_ -replace '<installPath>',$env:ChocolateyInstall) -Because $Output.String
        }
    }
}
