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

    Context "Upgrade package with (<Command>) specified" -ForEach @(
        @{ Command = '--pin' ; Contains = $true }
        @{ Command = '' ; Contains = $false }
    ) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Package = 'upgradepackage'
            $null = Invoke-Choco install $Package --version 1.0.0 --confirm
            $null = Invoke-Choco upgrade $Package $Command --confirm
            $Output = Invoke-Choco pin list
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Output should include pinned package" {
            if ($Contains) {
                $Output.String | Should -Match "$Package|1.1.0"
            }
            else {
                $Output.String | Should -Not -Match "$Package|1.1.0"
            }
        }
    }

    Context "Upgrading packages while remembering arguments with only one package using arguments" -Tag Internal {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Enable-ChocolateyFeature useRememberedArgumentsForUpgrades
            $null = Invoke-Choco install curl --package-parameters="'/CurlOnlyParam'" --version="7.77.0" --ia="'/CurlIAParam'" --x86 -y
            $null = Invoke-Choco install wget --version=1.21.1 -y

            $Output = Invoke-Choco upgrade all -y --debug
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0
        }

        It 'Outputs running curl script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*curl\\tools" } | Select-Object -Last 1

            $line | Should -Not -BeNullOrEmpty
            $line | Should -MatchExactly "\/CurlIAParam"
            $line | Should -MatchExactly "\/CurlOnlyParam"
            $line | Should -Match "-forceX86"
        }

        It 'Outputs running wget script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*wget\\tools" }

            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "installArguments:? ''"
            $line | Should -Match "packageParameters:? ''"
            $line | Should -Not -Match "-forceX86"
        }
    }

    # We exclude this test when running CCM, as it will install and remove
    # the firefox package which is used through other tests that will be affected.
    Context "Upgrading packages while remembering arguments with multiple packages using arguments" -Tag CCMExcluded, Internal, VMOnly {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Enable-ChocolateyFeature useRememberedArgumentsForUpgrades
            $null = Invoke-Choco install curl --package-parameters="'/CurlOnlyParam'" --version="7.77.0" --ia="'/CurlIAParam'" --forcex86 -y
            $null = Invoke-Choco install wget --version=1.21.1 -y --forcex86
            $null = Invoke-Choco install firefox --version=99.0.1 --package-parameters="'/l=eu'" -y --ia="'/RemoveDistributionDir=true'"

            $Output = Invoke-Choco upgrade all -y --debug
        }

        AfterAll {
            $null = Invoke-Choco uninstall firefox -y
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0
        }

        It 'Outputs running curl script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*curl\\tools" } | Select-Object -Last 1

            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "installArguments:? '/CurlIAParam'"
            $line | Should -Match "packageParameters:? '/CurlOnlyParam'"
            $line | Should -Match "-forceX86"
        }

        It 'Outputs running wget script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*wget\\tools" } | Select-Object -Last 1

            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "installArguments:? ''"
            $line | Should -Match "packageParameters:? ''"
            $line | Should -Match "-forceX86"
        }

        It 'Outputs firefox using eu as language locale' {
            $Output.Lines | Should -Contain "Using locale 'eu'..." -Because $Output.String
        }

        It 'Outputs running firefox script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*firefox\\tools" }

            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "installArguments:? '\/RemoveDistributionDir=true'"
            $line | Should -Match "packageParameters:? '\/l=eu'"
            $line | Should -Not -Match "-forceX86"
        }
    }


    Context "Upgrading multiple packages at once" {

        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage", "packagewithscript"

            $Output = Invoke-Choco upgrade @PackageUnderTest --confirm
        }

        It "Installs successfully and exits with success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the packages to the lib directory" {
            $PackageUnderTest | ForEach-Object {
                "$env:ChocolateyInstall\lib\$_" | Should -Exist
            }
        }
    }
}
