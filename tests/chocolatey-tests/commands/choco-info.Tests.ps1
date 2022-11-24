Import-Module helpers/common-helpers

Describe "choco info" -Tag Chocolatey, InfoCommand {
    BeforeDiscovery {
        $licensedProxyFixed = Test-PackageIsEqualOrHigher 'chocolatey.extension' 2.2.0-beta -AllowMissingPackage
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Listing package information when package can be found" {
        BeforeDiscovery {
            $infoItems = @(
                @{ Title = "Tags"; Value = "mvcmusicstore-web SPACE_SEPARATED" }
                @{ Title = "Summary"; Value = "Mvc Music Store Website" }
                @{ Title = "Description"; Value = "This is the code that releases the website" }
            )
        }

        BeforeAll {
            $Output = Invoke-Choco info mvcmusicstore-web
            $Output.Lines = $Output.Lines
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays <Title> with value <Value>" -ForEach $infoItems {
            $Output.Lines | Should -Contain "${Title}: $Value"
        }

        It "Displays published date in format M/DD/YYYY" -Tag ExpectBroken {
            $Output.String | Should -Match "Published: (?<month>\d{1,2})\/(?<day>\d{1,2})\/(?<year>\d{4})"
        }

        It "Displays the title of the package" {
            $line = $Output.Lines | Select-String "Title"
            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "Title: Mvc Music Store Web"
        }
    }

    Context "Listing package information when package can not be found (Enhanced Exit Code: <Enhanced>)" -ForEach @(
        @{
            Enhanced = $false
            ExitCode = 0
        }
        @{
            Enhanced = $true
            ExitCode = 2
        }
    ) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            if ($Enhanced) {
                Enable-ChocolateyFeature useEnhancedExitCodes
            }
            else {
                Disable-ChocolateyFeature useEnhancedExitCodes
            }

            $Output = Invoke-Choco info unavailable
        }

        It "Exists with Failure (<ExitCode>)" {
            $Output.ExitCode | Should -Be $ExitCode
        }

        It "Displays no packages could be found" {
            $Output.Lines | Should -Contain "0 packages found."
        }
    }

    # Issue: https://gitlab.com/chocolatey/collaborators/choco-licensed/-/issues/530 (NOTE: Proxy bypassing also works on Chocolatey FOSS)
    Context "Listing package information when using proxy and proxy bypass list in config" -Skip:(!$licensedProxyFixed) {
        BeforeDiscovery {
            $infoItems = @(
                @{ Title = "Tags"; Value = "mvcmusicstore db" }
                @{ Title = "Summary"; Value = "Mvc Music Store Database" }
                @{ Title = "Description"; Value = "This is the code that releases the database" }
                @{ Title = "Release Notes"; Value = "v1.2.0 - Updated Migration" }
            )
        }

        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $null = Invoke-Choco config set --name=proxy --value="https://invalid.chocolatey.org/"
            $null = Invoke-Choco config set --name=proxyBypassList --value="hermes.chocolatey.org"

            $Output = Invoke-Choco info mvcmusicstore-db
            $Output.Lines = $Output.Lines
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays the package mvcmusicstore-db 1.2.0" {
            $Output.Lines | Should -Contain "mvcmusicstore-db 1.2.0"
        }

        It "Displays <Title> with value <Value>" -ForEach $infoItems {
            $Output.Lines | Should -Contain "${Title}: $Value"
        }
    }

    # Issue: https://gitlab.com/chocolatey/collaborators/choco-licensed/-/issues/530 (NOTE: Proxy bypassing also works on Chocolatey FOSS)
    Context "Listing package information when using proxy and proxy bypass list on command" -Skip:(!$licensedProxyFixed) {
        BeforeDiscovery {
            $infoItems = @(
                @{ Title = "Tags"; Value = "mvcmusicstore db" }
                @{ Title = "Summary"; Value = "Mvc Music Store Database" }
                @{ Title = "Description"; Value = "This is the code that releases the database" }
                @{ Title = "Release Notes"; Value = "v1.2.0 - Updated Migration" }
            )
        }

        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $null = Invoke-Choco config set --name=proxy --value="https://invalid.chocolatey.org/"

            $Output = Invoke-Choco info mvcmusicstore-db "--proxy-bypass-list=hermes.chocolatey.org"
            $Output.Lines = $Output.Lines
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays the package mvcmusicstore-db 1.2.0" {
            $Output.Lines | Should -Contain "mvcmusicstore-db 1.2.0"
        }

        It "Displays <Title> with value <Value>" -ForEach $infoItems {
            $Output.Lines | Should -Contain "${Title}: $Value"
        }
    }

    Context "Listing package information about local side by side installed package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $null = Invoke-Choco install 'isdependency' --confirm --sxs

            $Output = Invoke-Choco info 'isdependency' --local-only
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs a warning message that installed side by side package is deprecated" {
            $Output.Lines | Should -Contain "isdependency has been installed as a side by side installation." -Because $Output.String
            $Output.Lines | Should -Contain "Side by side installations are deprecated and is pending removal in v2.0.0." -Because $Output.String
        }
    }
}
