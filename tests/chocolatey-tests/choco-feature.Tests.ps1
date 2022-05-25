param(
    # The command to test.
    [string[]]$Command = @(
        "feature"
        "features"
    )
)

Import-Module helpers/common-helpers

Describe "choco <_>" -ForEach $Command -Tag Chocolatey, FeatureCommand {
    BeforeDiscovery {
        $CurrentFeatures = ([xml](Get-Content $env:ChocolateyInstall\config\chocolatey.config)).chocolatey.features.feature
    }

    BeforeAll {
        $CommandUnderTest = $_
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Listing Features" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            Invoke-Choco $_ disable "--name=logValidationResultsOnWarnings"

            $Output = Invoke-Choco $_
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Lists available features" {
            # Skip "Chocolatey vVersion" line
            $lines = $Output.Lines[1..($Output.Lines.Count - 1)] | Where-Object {
                # Filter out unofficial disclaimer and blank lines
                $_ -and ($_ -notmatch "^\s*(Chocolatey|If you|now be in)")
            }

            $lines | Should -Match "^\[(?<enabled>[x ])\] (?<feature>\w+) - (?<description>.+)$"
        }

        It "Lists '<_.Name>' feature" -TestCases $CurrentFeatures {
            # Using just $Name in this case do not work
            ($Output.Lines -match "\[[x ]\] $($_.Name) -").Count | Should -Be 1
        }

        # This test have a will fail on licensed extension due to the
        # configuration file will not yet have the necessary configuration
        # values to compare with populated.
        It "Contains no features not in the $($CurrentFeatures.Count) features listed in the config file" -TestCases @{Names = $CurrentFeatures.Name; Command = $_ } -Tag FossOnly {
            $data = Invoke-Choco $Command --limitoutput
            $featureNames = ($data.Lines | ConvertFrom-ChocolateyOutput -Command "Feature").Name
            $featureNames | Should -BeIn $Names
        }
    }

    Context "Adjusting Feature Settings" {
        BeforeDiscovery {
            # Get the features this way so we're working with the entire list, and not just what was in the config file initially.
            # We additionally want to ignore any presence of removed features
            # as these are not intended to work as expected, even when present.
            $FeaturesToTest = (Invoke-Choco feature list -r).Lines | ConvertFrom-ChocolateyOutput -Command Feature | Where-Object Name -ne 'scriptsCheckLastExitCode'
        }

        BeforeAll {
            Restore-ChocolateyInstallSnapshot
        }

        Context "Enabling <_.Name> feature" -Foreach $FeaturesToTest {
            BeforeAll {
                $Name = $_.Name
                # Disable feature before trying to enable it.
                $null = Invoke-Choco $CommandUnderTest disable --name $Name
                $Output = Invoke-Choco $CommandUnderTest enable --name $Name
            }

            It 'Exits with Success (0)' {
                $Output.ExitCode | Should -Be 0 -Because $Output.String
            }

            It 'Outputs correctly' {
                $Output.Lines | Should -Contain "Enabled $Name"
            }
        }

        Context "Disabling <_.Name> feature" -Foreach $FeaturesToTest {
            BeforeAll {
                $Name = $_.Name
                # Enable feature before trying to enable it.
                $null = Invoke-Choco $CommandUnderTest enable --name $Name
                $Output = Invoke-Choco $CommandUnderTest disable --name $Name
            }

            It 'Exits with Success (0)' {
                $Output.ExitCode | Should -Be 0 -Because $Output.String
            }

            It 'Outputs correctly' {
                $Output.Lines | Should -Contain "Disabled $Name"
            }
        }
    }

    Context "Disabling usePackageRepositoryOptimizations" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco $_ enable --name usePackageRepositoryOptimizations

            $Output = Invoke-Choco $_ disable --name usePackageRepositoryOptimizations

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs a message indicating that it disabled the feature" {
            $Output.Lines | Should -Contain "Disabled usePackageRepositoryOptimizations"
        }

        It "Disables the feature in the file" {
            $ConfigFileContent.chocolatey.features.feature.Where{ $_.Name -eq 'usePackageRepositoryOptimizations' }.enabled | Should -Be "false"
        }
    }

    Context "Enabling showDownloadProgress" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco $_ disable --name showDownloadProgress

            $Output = Invoke-Choco $_ enable --name showDownloadProgress

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs a message indicating that it enabled the feature" {
            $Output.Lines | Should -Contain "Enabled showDownloadProgress"
        }

        It "Enables the feature in the file" {
            $ConfigFileContent.chocolatey.features.feature.Where{ $_.Name -eq 'showDownloadProgress' }.enabled | Should -Be "true"
        }
    }

    Context "Disabling failOnInvalidOrMissingLicense" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco $_ enable --name failOnInvalidOrMissingLicense

            $Output = Invoke-Choco $_ disable --name failOnInvalidOrMissingLicense

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs a message indicating that it disabled the feature" {
            $Output.Lines | Should -Contain "Disabled failOnInvalidOrMissingLicense"
        }

        It "Disables the feature in the file" {
            $ConfigFileContent.chocolatey.features.feature.Where{ $_.Name -eq 'failOnInvalidOrMissingLicense' }.enabled | Should -Be "false"
        }
    }

    Context "Enabling a non-existant feature" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco $_ enable --name nonExistingFeature
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Outputs a message indicating it didn't find the feature in question" {
            $Output.String | Should -Match "Feature 'nonExistingFeature' not found"
        }
    }

    Context "Disabling a non-existant feature" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco $_ disable --name nonExistingFeature
            $TestedExitCode = $LastExitCode
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Outputs a message indicating it didn't find the feature in question" {
            $Output.String | Should -Match "Feature 'nonExistingFeature' not found"
        }
    }
}
