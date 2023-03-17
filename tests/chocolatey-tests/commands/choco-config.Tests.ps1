Import-Module helpers/common-helpers

Describe "choco config" -Tag Chocolatey, ConfigCommand {
    BeforeAll {
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot

        $InitialConfiguration = ([xml](Get-Content $env:ChocolateyInstall\config\chocolatey.config)).chocolatey
        $expectedLicenseHeader = Get-ExpectedChocolateyHeader
    }

    BeforeDiscovery {
        $isLicensed = Test-PackageIsEqualOrHigher "chocolatey.extension" "0.0.0"

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

        It "Does not display Sources section" {
            $Output.Lines | Should  -Not -Contain "Sources"
        }

        It "Does not display Features" {
            $Output.Lines | Should -Not -Contain "Features"
        }

        It "Does not display API Keys section" {
            $Output.Lines | Should -Not -Contain "API Keys"
        }

        It "Displays Available Setting <_>" -ForEach @(
            "cacheLocation =  \|"
            "containsLegacyPackageInstalls = true \|"
            "commandExecutionTimeoutSeconds = 2700 \|"
            "proxy = [^|]* \|"
            "proxyUser = [^|]* \|"
            "proxyPassword = [^|]* \|"
            "webRequestTimeoutSeconds = 30 \|"
            "proxyBypassList =  \|"
            "proxyBypassOnLocal = true \|"
            "upgradeAllExceptions =  \|"
            "defaultTemplateName =  \|"
        ) {
            $Output.String | Should -MatchExactly $_
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

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
