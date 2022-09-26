param(
    # The command to test.
    [string[]]$Command = @(
        "source"
        "sources"
    )
)

Import-Module helpers/common-helpers

Describe "choco <_>" -ForEach $Command -Tag Chocolatey, SourceCommand {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot

        $CurrentCommand = $_
        $expectedHeader = Get-ExpectedChocolateyHeader
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Add single unauthenticated source" -ForEach @(
        @{
            SourceName = "dummy-long"
            Name       = "--name"
            Source     = "--source"
        }
        @{
            SourceName = "dummy-short"
            Name       = "-n"
            Source     = "-s"
        }
    ) {
        BeforeAll {
            $Output = Invoke-Choco $CurrentCommand add $_.Name $_.SourceName $_.Source "https://test.com/api"

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $sources = @($ConfigFileContent.chocolatey.sources.source)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays source added" {
            $Output.Lines | Should -Contain "Added $($_.SourceName) - https://test.com/api (Priority 0)"
        }

        It "Updates configuration with source" {
            $sourceName = $_.SourceName
            $config = $sources.Where{ $_.id -eq $sourceName }
            $config | Should -HaveCount 1
            $config.id | Should -Be $sourceName
            $config.value | Should -Be "https://test.com/api"
            $config.disabled | Should -Be "false" # We can not use ps $false here
            $config.bypassProxy | Should -Be "false"
            $config.adminOnly | Should -Be "false"
            $config.priority | Should -Be 0
            $config.user | Should -BeNullOrEmpty
        }
    }

    Context "Add single unauthenticated source with priority" {
        BeforeAll {
            $Output = Invoke-Choco $CurrentCommand add --name "dummy-long" --source "https://priority.test.com/api" --priority 1

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $sources = @($ConfigFileContent.chocolatey.sources.source)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays source added" {
            $Output.Lines | Should -Contain "Updated dummy-long - https://priority.test.com/api (Priority 1)"
        }

        It "Updates configuration with source" {
            $config = $sources.Where{ $_.id -eq "dummy-long" }
            $config | Should -HaveCount 1
            $config.id | Should -Be "dummy-long"
            $config.value | Should -Be "https://priority.test.com/api"
            $config.disabled | Should -Be "false" # We can not use ps $false here
            $config.bypassProxy | Should -Be "false"
            $config.adminOnly | Should -Be "false"
            $config.priority | Should -Be 1
            $config.user | Should -BeNullOrEmpty
        }
    }

    Context "Add single authenticated source with user+pass" -ForEach @(
        @{
            SourceName = "dummy-auth-long"
            User       = "--user"
            Pass       = "--password"
        }
        @{
            SourceName = "dummy-auth-short"
            User       = "-u"
            Pass       = "-p"
        }
    ) {
        BeforeAll {
            $Output = Invoke-Choco $CurrentCommand add --name $_.SourceName --source "https://auth.test.com/api" $_.User "test-kitchen" $_.Pass "dummy-password"

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $sources = @($ConfigFileContent.chocolatey.sources.source)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays source added" {
            $Output.Lines | Should -Contain "Added $($_.SourceName) - https://auth.test.com/api (Priority 0)"
        }

        It "Updates configuration with source" {
            $sourceName = $_.SourceName
            $config = $sources.Where{ $_.id -eq $sourceName }
            $config | Should -HaveCount 1
            $config.id | Should -Be $sourceName
            $config.value | Should -Be "https://auth.test.com/api"
            $config.disabled | Should -Be "false" # We can not use ps $false here
            $config.bypassProxy | Should -Be "false"
            $config.adminOnly | Should -Be "false"
            $config.priority | Should -Be 0
            $config.user | Should -Be "test-kitchen"
            $config.password | Should -Not -BeNullOrEmpty # Value is encoded
        }
    }

    Context "Add single authenticated source with certificate" -ForEach @(
        @{
            SourceName = "dummy-cert-auth-long"
            Cert       = "--cert"
            Pass       = "--certpassword"
        }
        @{
            SourceName = "dummy-cert-auth-short"
            Cert       = "--cert"
            Pass       = "--cp"
        }
    ) {
        BeforeAll {
            $CertPath = "$PSScriptRoot\TestCertificate.pfx"
            $Output = Invoke-Choco $CurrentCommand add --name $_.SourceName --source "https://auth.test.com/api" $_.Cert "`"$CertPath`"" $_.Pass "TestPassword"

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $sources = @($ConfigFileContent.chocolatey.sources.source)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays source added" {
            $Output.Lines | Should -Contain "Added $($_.SourceName) - https://auth.test.com/api (Priority 0)"
        }

        It "Updates configuration with source" {
            $sourceName = $_.SourceName
            $config = $sources.Where{ $_.id -eq $sourceName }
            $config | Should -HaveCount 1
            $config.id | Should -Be $sourceName
            $config.value | Should -Be "https://auth.test.com/api"
            $config.disabled | Should -Be "false" # We can not use ps $false here
            $config.bypassProxy | Should -Be "false"
            $config.adminOnly | Should -Be "false"
            $config.priority | Should -Be 0
            $config.user | Should -BeNullOrEmpty "test-kitchen"
            $config.password | Should -BeNullOrEmpty
            $config.certificate | Should -Be $CertPath
            $config.certificatePassword | Should -Not -BeNullOrEmpty # Value is encoded
        }
    }

    Context "Removing named source" {
        BeforeAll {
            Invoke-Choco $CurrentCommand add --name "dummy-removal" --source "https://test.com/api"

            $Output = Invoke-Choco $CurrentCommand remove --name "dummy-removal"

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $sources = @($ConfigFileContent.chocolatey.sources.source)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays source removed" {
            $Output.Lines | Should -Contain "Removed dummy-removal"
        }

        It "Removes source from configuration" {
            $config = $sources.Where{ $_.id -eq "dummy-removal" }
            $config | Should -BeNullOrEmpty
        }
    }

    Context "Add local directory source" {
        BeforeAll {
            $Output = Invoke-Choco $CurrentCommand add --name "dummy-local" --source "C:\packages"

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $sources = @($ConfigFileContent.chocolatey.sources.source)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays source added" {
            $Output.Lines | Should -Contain "Added dummy-local - C:\packages (Priority 0)"
        }

        It "Updates configuration with source" {
            $config = $sources.Where{ $_.id -eq "dummy-local" }
            $config | Should -HaveCount 1
            $config.id | Should -Be "dummy-local"
            $config.value | Should -Be "C:\packages"
            $config.disabled | Should -Be "false" # We can not use ps $false here
            $config.bypassProxy | Should -Be "false"
            $config.adminOnly | Should -Be "false"
            $config.priority | Should -Be 0
            $config.user | Should -BeNullOrEmpty
            $config.password | Should -BeNullOrEmpty
        }
    }

    Context "Adding source with invalid argument" {
        BeforeAll {
            $Output = Invoke-Choco $CurrentCommand add --name "dummy-invalid" --source "C:\packages" --user "test-kitchen" --pass "no matter"
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays source added" {
            $Output.Lines | Should -Contain "A single sources command must be listed. Please see the help menu for those commands"
        }
    }

    Context "Lists configured sources" {
        BeforeAll {
            # Prepare by ensuring some sources are added
            Invoke-Choco $CurrentCommand add --name "dummy-remote" --source "https://remote.test.com/api"
            Invoke-Choco $CurrentCommand add --name "dummy-local" --source "C:\packages\Local"
            Invoke-Choco $CurrentCommand add --name "dummy-auth" --source "https://auth.test.com/api" --user "test-kitchen" --password "test-password"
            Invoke-Choco $CurrentCommand add --name "dummy-priority" --source "https://pri.test.com/api" --priority 5

            $Output = Invoke-Choco $CurrentCommand list
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays expected sources" {
            $Output.Lines | Should -Contain "dummy-remote - https://remote.test.com/api | Priority 0|Bypass Proxy - False|Self-Service - False|Admin Only - False."
            $Output.Lines | Should -Contain "dummy-local - C:\packages\Local | Priority 0|Bypass Proxy - False|Self-Service - False|Admin Only - False."
            $Output.Lines | Should -Contain "dummy-auth - https://auth.test.com/api (Authenticated)| Priority 0|Bypass Proxy - False|Self-Service - False|Admin Only - False."
            $Output.Lines | Should -Contain "dummy-priority - https://pri.test.com/api | Priority 5|Bypass Proxy - False|Self-Service - False|Admin Only - False."
        }
    }

    Context "Removing missing source" {
        BeforeAll {
            # Make sure the source is removed
            Invoke-Choco $CurrentCommand remove --name "not-existing"

            $Output = Invoke-Choco $CurrentCommand remove --name "not-existing"
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays message about no change made" {
            $Output.Lines | Should -Contain "Nothing to change. Config already set."
        }
    }

    Context "Disabling existing source" {
        BeforeAll {
            # Ensure source is enabled
            Invoke-Choco $CurrentCommand enable --name "chocolatey"

            $Output = Invoke-Choco $CurrentCommand disable --name "chocolatey"

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $sources = @($ConfigFileContent.chocolatey.sources.source)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays source disabled" {
            $Output.Lines | Should -Contain "Disabled chocolatey"
        }

        It "Updates configuration with source" {
            $config = $sources.Where{ $_.id -eq "chocolatey" }
            $config | Should -HaveCount 1
            $config.id | Should -Be "chocolatey"
            $config.disabled | Should -Be "true" # We can not use ps $false here
        }
    }

    Context "Enabling existing source" {
        BeforeAll {
            # Ensure source is disabled
            Invoke-Choco $CurrentCommand disable --name "chocolatey"

            $Output = Invoke-Choco $CurrentCommand enable --name "chocolatey"

            [xml]$ConfigFileContent = Get-Content $env:ChocolateyInstall\config\chocolatey.config
            $sources = @($ConfigFileContent.chocolatey.sources.source)
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays source enabled" {
            $Output.Lines | Should -Contain "Enabled chocolatey"
        }

        It "Updates configuration with source" {
            $config = $sources.Where{ $_.id -eq "chocolatey" }
            $config | Should -HaveCount 1
            $config.id | Should -Be "chocolatey"
            $config.disabled | Should -Be "false" # We can not use ps $false here
        }
    }

    Context "Disabling missing source" {
        BeforeAll {
            # Ensure source is not available
            Invoke-Choco $CurrentCommand remove --name "not-existing"

            $Output = Invoke-Choco $CurrentCommand disable --name "not-existing"
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays no change made" {
            $Output.Lines | Should -Contain "Nothing to change. Config already set."
        }
    }

    Context "Enabling missing source" {
        BeforeAll {
            # Ensure source is not available
            Invoke-Choco $CurrentCommand remove --name "not-existing"

            $Output = Invoke-Choco $CurrentCommand enable --name "not-existing"
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays no change made" {
            $Output.Lines | Should -Contain "Nothing to change. Config already set."
        }
    }
}
