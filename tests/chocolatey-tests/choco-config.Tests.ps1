Import-Module helpers/common-helpers

Describe "choco config" -Tag Chocolatey, ConfigCommand {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot

        $InitialConfiguration = ([xml](Get-Content $env:ChocolateyInstall\config\chocolatey.config)).chocolatey
        $expectedLicenseHeader = Get-ExpectedChocolateyHeader
    }

    BeforeDiscovery {
        $isLicensed = Test-PackageIsEqualOrHigher "chocolatey.extension" "0.0.0"

        $TestedFeatures = @(
            "checksumFiles"
            "autoUninstaller"
            "allowGlobalConfirmation"
            "failOnAutoUninstaller"
            "failOnStandardError"
            "allowEmptyChecksums"
            "allowEmptyChecksumsSecure"
            "powershellHost"
            "logEnvironmentValues"
            "virusCheck"
            "failOnInvalidOrMissingLicense"
            "ignoreInvalidOptionsSwitches"
            "usePackageExitCodes"
            "useEnhancedExitCodes"
            "exitOnRebootDetected"
            "useFipsCompliantChecksums"
            "showNonElevatedWarnings"
            "showDownloadProgress"
            "stopOnFirstPackageFailure"
            "useRememberedArgumentsForUpgrades"
            "ignoreUnfoundPackagesOnUpgradeOutdated"
            "skipPackageUpgradesWhenNotInstalled"
            "removePackageInformationOnUninstall"
            "logWithoutColor"
            "logValidationResultsOnWarnings"
            "usePackageRepositoryOptimizations"
        )

        $TestedConfigs = @(
            "cacheLocation"
            "containsLegacyPackageInstalls"
            "commandExecutionTimeoutSeconds"
            "proxy"
            "proxyUser"
            "proxyPassword"
            "webRequestTimeoutSeconds"
            "proxyBypassList"
            "proxyBypassOnLocal"
            "upgradeAllExceptions"
            "defaultTemplateName"
        )

        if ($isLicensed) {
            $TestedConfigs += @(
                "virusCheckMinimumPositives"
                "virusScannerType"
                "genericVirusScannerPath"
                "genericVirusScannerArgs"
                "genericVirusScannerValidExitCodes"
                "genericVirusScannerTimeoutInSeconds"
                "maximumDownloadRateBitsPerSecond"
                "serviceInstallsDefaultUserName"
                "serviceInstallsDefaultUserPassword"
                "backgroundServiceAllowedCommands"
                "centralManagementServiceUrl"
                "centralManagementReportPackagesTimerIntervalInSeconds"
                "centralManagementReceiveTimeoutInSeconds"
                "centralManagementSendTimeoutInSeconds"
                "centralManagementCertificateValidationMode"
                "centralManagementMaxReceiveMessageSizeInBytes"
                "centralManagementClientCommunicationSaltAdditivePassword"
                "centralManagementServiceCommunicationSaltAdditivePassword"
                "centralManagementDeploymentCheckTimerIntervalInSeconds"
            )
        }
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Listing all default configuration values" {
        BeforeAll {
            $Output = Invoke-Choco config list
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedLicenseHeader
        }

        It "Displays Settings section" {
            $Output.Lines | Should -Contain "Settings"
        }

        It "Displays Sources section" {
            $Output.Lines | Should -Contain "Sources"
        }

        It "Displays Features" {
            $Output.Lines | Should -Contain "Features"
        }

        It "Displays API Keys section" {
            $Output.Lines | Should -Contain "API Keys"
        }

        It "Displays note about choco <_>" -ForEach @(
            "source"
            "feature"
        ) {
            $Output.Lines | Should -Contain "NOTE: Use choco $_ to interact with $($_)s."
        }

        It "Displays note about choco apikey" {
            $Output.Lines | Should -Contain "NOTE: Api Keys are not shown through this command."
            $Output.Lines | Should -Contain "Use choco apikey to interact with API keys."
        }

        It "Displays Available Setting <_>" -ForEach @(
            "cacheLocation =  |"
            "containsLegacyPackageInstalls = true |"
            "commandExecutionTimeoutSeconds = 2700 |"
            "proxy =  |"
            "proxyUser =  |"
            "proxyPassword =  |"
            "webRequestTimeoutSeconds = 30 |"
            "proxyBypassList =  |"
            "proxyBypassOnLocal = true |"
            "upgradeAllExceptions =  |"
            "defaultTemplateName =  |"
        ) {
            $Output.String | Should -MatchExactly ([Regex]::Escape($_))
        }

        # Only community repository URL will be set on 0.10.16 and above
        # Issue: https://github.com/chocolatey/choco/issues/2231
        It "Displays Available Sources <Name> - <Source>" -ForEach @(
            @{
                Name   = "chocolatey"
                Source = if (Test-ChocolateyVersionEqualOrHigherThan "0.10.16-beta") { "https://community.chocolatey.org/api/v2/" } else { "https://chocolatey.org/api/v2/" }
            }
        ) {
            $Output.String | Should -MatchExactly "$Name( \[Disabled\])? - $([Regex]::Escape($Source))\s*\|"
        }

        It "Displays Available Feature <_>" -ForEach $TestedFeatures {
            $Output.String | Should -Match "\[[ x]\] $_ -"
        }
    }

    Context "Getting a configuration setting (<_>)" -ForEach $TestedConfigs {
        BeforeAll {
            $Output = Invoke-Choco config get $_
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedLicenseHeader
        }

        It "Displays value of config" {
            $Output.Lines | Should -Contain $InitialConfiguration.SelectSingleNode("//config/add[@key='$_']").Value
        }
    }

    Context "Getting a configuration setting by argument (<_>)" -ForEach $TestedConfigs {
        BeforeAll {
            $Output = Invoke-Choco config get --name $_
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedLicenseHeader
        }

        It "Displays value of config" {
            $Output.Lines | Should -Contain $InitialConfiguration.SelectSingleNode("//config/add[@key='$_']").Value
        }
    }

    Context "Getting a configuration setting that doesn't exist" {
        BeforeAll {
            $Output = Invoke-Choco config get noConfigValue
        }

        It "Exits with failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Outputs an error indicating that there's no config by that name" {
            $Output.Lines | Should -Contain "No configuration value by the name 'noConfigValue'"
        }
    }

    Context "Setting a configuration setting (cacheLocation)" {
        BeforeAll {
            $Output = Invoke-Choco config set cacheLocation "C:\temp\choco"

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $configs = @($ConfigFileContent.chocolatey.config.add)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedLicenseHeader
        }

        It "Displays config value updated" {
            $Output.Lines | Should -Contain "Updated cacheLocation = C:\temp\choco"
        }

        It "Updates value in config file" {
            $value = $configs.Where{ $_.key -eq "cacheLocation" }
            $value | Should -HaveCount 1
            $value.value | Should -Be "C:\temp\choco"
        }
    }

    Context "Setting a configuration setting not available by default (newConfiguration)" {
        BeforeAll {
            $Output = Invoke-Choco config set --name newConfiguration --value some-value

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $configs = @($ConfigFileContent.chocolatey.config.add)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedLicenseHeader
        }

        It "Displays config value Added" {
            $Output.Lines | Should -Contain "Added newConfiguration = some-value"
        }

        It "Adds new value in config file" {
            $value = $configs.Where{ $_.key -eq "newConfiguration" }
            $value | Should -HaveCount 1
            $value.value | Should -Be "some-value"
        }
    }

    Context "Unsetting configuration setting (cacheLocation)" {
        BeforeAll {
            $Output = Invoke-Choco config unset cacheLocation

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $configs = @($ConfigFileContent.chocolatey.config.add)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedLicenseHeader
        }

        It "Displays config value Added" {
            $Output.Lines | Should -Contain "Unset cacheLocation"
        }

        It "Updates value in config file" {
            $value = $configs.Where{ $_.key -eq "cacheLocation" }
            $value | Should -HaveCount 1
            $value.value | Should -BeNullOrEmpty
        }
    }

    Context "Unsetting configuration setting (newConfiguration)" {
        BeforeAll {
            # Ensure the configuration value is available
            Invoke-Choco config set newConfiguration some-value | Out-Null

            $Output = Invoke-Choco config unset --name newConfiguration

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $configs = @($ConfigFileContent.chocolatey.config.add)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedLicenseHeader
        }

        It "Displays config value Added" {
            $Output.Lines | Should -Contain "Unset newConfiguration"
        }

        It "Adds new value in config file" {
            $value = $configs.Where{ $_.key -eq "newConfiguration" }
            $value | Should -HaveCount 1
            $value.value | Should -BeNullOrEmpty
        }
    }

    Context "Unsetting a configuration that doesn't exist" {
        BeforeAll {
            $Output = Invoke-Choco config unset not-existing

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $configs = @($ConfigFileContent.chocolatey.config.add)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedLicenseHeader
        }

        It "Displays config value Added" {
            $Output.Lines | Should -Contain "Nothing to change. Config already set."
        }

        It "No value is added to file" {
            $value = $configs.Where{ $_.key -eq "not-existing" }
            $value | Should -HaveCount 0
        }
    }
}
