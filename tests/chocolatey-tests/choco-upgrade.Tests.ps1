Import-Module helpers/common-helpers

Describe "choco upgrade" -Tag Chocolatey, UpgradeCommand {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Upgrading a side-by-side Package (non-existing)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $Output = Invoke-Choco upgrade $PackageUnderTest --confirm --allowmultipleversions
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed a package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0" | Should -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Outputs a warning message about side by side installs are deprecated" {
            $Output.Lines | Should -Contain "Upgrading the same package with multiple versions is deprecated and will be removed in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it upgraded the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages." -Because $Output.String
        }
    }

    Context "Switching a normal Package to a side-by-side Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco install $PackageUnderTest --confirm

            $Output = Invoke-Choco upgrade $PackageUnderTest --confirm --force --allowmultipleversions
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0" | Should -Exist
        }

        It "Removed the previous version of the package from the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Not -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Outputs a warning message about side by side installs are deprecated" {
            $Output.Lines | Should -Contain "Upgrading the same package with multiple versions is deprecated and will be removed in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it upgraded the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages." -Because $Output.String
        }
    }

    Context "Switching a side-by-side Package to a normal Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco install $PackageUnderTest --confirm --allowmultipleversion

            $Output = Invoke-Choco upgrade $PackageUnderTest --confirm --force
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Does not output a warning message about side by side installs are deprecated" {
            $Output.Lines | Should -Not -Contain "Upgrading the same package with multiple versions is deprecated and will be removed in v2.0.0." -Because $Output.String
        }

        It "Does not output a warning message that installed side by side package is deprecated" {
            $Output.Lines | Should -Not -Contain "installpackage has been installed as a side by side installation." -Because $Output.String
            $Output.Lines | Should -Not -Contain "Side by side installations are deprecated and is pending removal in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it upgraded the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages." -Because $Output.String
        }
    }
}
