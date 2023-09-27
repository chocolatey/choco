Describe "choco info" -Tag Chocolatey, InfoCommand {
    BeforeDiscovery {
        $licensedProxyFixed = Test-PackageIsEqualOrHigher 'chocolatey.extension' 2.2.0-beta -AllowMissingPackage
    }

    BeforeAll {
        Remove-NuGetPaths
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Displays <Title> with value <Value>" -ForEach $infoItems  {
            $Output.Lines | Should -Contain "${Title}: $Value"
        }

        It "Displays published date in format M/DD/YYYY" {
            $Output.String | Should -Match "Published: (?<month>\d{1,2})\/(?<day>\d{1,2})\/(?<year>\d{4})"
        }

        # TODO> The identifier is incorrectly outputted instead of the title
        It "Displays the title of the package" {
            $line = $Output.Lines | Select-String "Title"
            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "Title: Mvc Music Store Web" -Because $Output.String
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
            $Output.ExitCode | Should -Be $ExitCode -Because $Output.String
        }

        It "Displays no packages could be found" {
            $Output.Lines | Should -Contain "0 packages found."
        }
    }

    Context "Listing package information when more than one package ID is provided" {
        BeforeAll {
            $Output = Invoke-Choco info foo bar
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Reports a package ID is required" {
            $Output.Lines | Should -Contain 'Only a single package name can be passed to the choco info command.'
        }
    }

    Context "Listing package information when no package ID is provided" {
        BeforeAll {
            $Output = Invoke-Choco info
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Reports a package ID is required" {
            $Output.Lines | Should -Contain 'A single package name is required to run the choco info command.'
        }
    }

    # Issue: https://gitlab.com/chocolatey/collaborators/choco-licensed/-/issues/530 (NOTE: Proxy bypassing also works on Chocolatey FOSS)
    # These are skipped on Proxy tests because the proxy server can't be bypassed in that test environment.
    Context "Listing package information when using proxy and proxy bypass list in config" -Tag ProxySkip -Skip:(!$licensedProxyFixed) {
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Displays the package mvcmusicstore-db 1.2.0" {
            $Output.Lines | Should -Contain "mvcmusicstore-db 1.2.0"
        }

        It "Displays <Title> with value <Value>" -ForEach $infoItems {
            $Output.Lines | Should -Contain "${Title}: $Value"
        }
    }

    # Issue: https://gitlab.com/chocolatey/collaborators/choco-licensed/-/issues/530 (NOTE: Proxy bypassing also works on Chocolatey FOSS)
    # These are skipped on Proxy tests because the proxy server can't be bypassed in that test environment.
    Context "Listing package information when using proxy and proxy bypass list on command" -Tag ProxySkip -Skip:(!$licensedProxyFixed) {
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Displays the package mvcmusicstore-db 1.2.0" {
            $Output.Lines | Should -Contain "mvcmusicstore-db 1.2.0"
        }

        It "Displays <Title> with value <Value>" -ForEach $infoItems {
            $Output.Lines | Should -Contain "${Title}: $Value"
        }
    }

    Context "Listing package information when invalid package source is being used" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $InvalidSource = "https://invalid.chocolatey.org/api/v2/"
            $null = Invoke-Choco source add -n "invalid" -s $InvalidSource

            $Output = Invoke-Choco info chocolatey
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Outputs warning about unable to load service index' {
            $Output.Lines | Should -Contain "Unable to load the service index for source $InvalidSource."
        }

        It 'Output information about the package' {
            $Output.String | Should -Match "Title: Chocolatey "
        }
    }

    Context "Listing package information for non-normalized exact package version" -ForEach @(
        @{ ExpectedPackageVersion = '1.0.0' ; SearchVersion = '1' }
        @{ ExpectedPackageVersion = '1.0.0' ; SearchVersion = '1.0' }
        @{ ExpectedPackageVersion = '1.0.0' ; SearchVersion = '1.0.0' }
        @{ ExpectedPackageVersion = '4.0.1' ; SearchVersion = '4.0.1' }
        @{ ExpectedPackageVersion = '1.0.0' ; SearchVersion = '01.0.0.0' }
        @{ ExpectedPackageVersion = '4.0.1' ; SearchVersion = '004.0.01.0' }
        @{ ExpectedPackageVersion = '4.0.1' ; SearchVersion = '0000004.00000.00001.0000' }
    ) -Tag VersionNormalization {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $PackageUnderTest = 'nonnormalizedversions'

            $Output = Invoke-Choco info $PackageUnderTest --version $SearchVersion
        }

        It "Should exit with success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Should find and report the normalized package version" {
            $Output.Lines | Should -Contain "$PackageUnderTest $ExpectedPackageVersion" -Because $Output.String
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
