Import-Module helpers/common-helpers

Describe "choco headers tests" -Tag Chocolatey, HeadersFeature {
    BeforeDiscovery {
        $isLicensed = Test-PackageIsEqualOrHigher "chocolatey.extension" "0.0.0"

        $Commands = @(
            @{
                CommandArguments = 'apikey', 'list'
                ExpectedHeaders = 'Source|ApiKey'
            }
            @{
                CommandArguments = 'config', 'list'
                ExpectedHeaders = 'Name|Value|Description'
            }
            @{
                CommandArguments = 'feature', 'list'
                ExpectedHeaders = 'Name|Enabled|Description'
            }
            @{
                CommandArguments = 'info', 'chocolatey', '--local-only', '--pre'
                ExpectedHeaders = 'Id|Version'
            }
            @{
                CommandArguments = 'list'
                ExpectedHeaders = 'Id|Version'
            }
            @{
                CommandArguments = 'outdated', '--ignore-unfound', '--ignore-pinned'
                ExpectedHeaders = 'Id|Version|AvailableVersion|Pinned'
            }
            @{
                CommandArguments = 'pin', 'list'
                ExpectedHeaders = 'Id|Version'
            }
            @{
                CommandArguments = 'rule', 'list'
                ExpectedHeaders = 'Severity|Id|Summary|HelpUrl'
            }
            @{
                CommandArguments = 'search', 'windirstat'
                ExpectedHeaders = 'Id|Version'
            }
            @{
                CommandArguments = 'source', 'list'
                ExpectedHeaders = 'Name|Source|Disabled|UserName|Certificate|Priority|BypassProxy|AllowSelfService|AdminOnly'
            }
            @{
                CommandArguments = 'template', 'list'
                ExpectedHeaders = 'Name|Version'
            }
        )

        if ($isLicensed) {
            $Commands += @{
                CommandArguments = 'license', 'info'
                ExpectedHeaders = 'Name|Type|ExpirationDate|NodeCount'
            }
        }
    }
    BeforeAll {
        Initialize-ChocolateyTestInstall

        # These are needed in order to test the choco pin list execution
        # as well as the choco apikey list as the headers will only be shown
        # when there is a pinned package. Finally, we need to install an
        # outdated package, so that the headers show in the choco outdated
        # command.
        $null = Invoke-Choco pin add --name="chocolatey"

        $null = Invoke-Choco apikey add --source "https://test.com/api/add/" --api-key "test-api-key"

        $null = Invoke-Choco install upgradepackage --version 1.0.0 --confirm

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Outputs headers for <CommandArguments> when '--include-headers' provided configured" -ForEach $Commands {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot -NoSnapshotCopy

            [string[]]$chocoArgs = @($CommandArguments) + @('--limit-output', '--include-headers')
            $Output = Invoke-Choco @chocoArgs
        }

        It 'Exits success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Includes appropriate header" {
            # The headers are _only_ output if there is content to show, so grab the first line
            # when there is actual content
            $HeaderRow = if ($Output.Lines.Count -gt 1) {
                $Output.Lines[0]
            }

            $HeaderRow | Should -Be $ExpectedHeaders -Because $Output.String
        }
    }

    Context "Does not output headers for <CommandArguments> by default" -ForEach $Commands {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot -NoSnapshotCopy

            [string[]]$chocoArgs = @($CommandArguments) + @('--limit-output')
            $Output = Invoke-Choco @chocoArgs
        }

        It 'Exits success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Does not include header" {
            # The headers are _only_ output if there is content to show, so grab the first line
            # when there is actual content
            $HeaderRow = if ($Output.Lines.Count -gt 1) {
                $Output.Lines[0]
            }

            $HeaderRow | Should -Not -Be $ExpectedHeaders -Because $Output.String
        }
    }

    Context "Includes headers when feature enabled" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            Enable-ChocolateyFeature -Name AlwaysIncludeHeaders
        }

        Context "Includes headers for <CommandArguments>" -ForEach $Commands {
            BeforeAll {
                [string[]]$chocoArgs = @($CommandArguments) + @('--limit-output')
                $Output = Invoke-Choco @chocoArgs
            }

            It 'Exits success (0)' {
                $Output.ExitCode | Should -Be 0 -Because $Output.String
            }

            It "Includes appropriate header" {
                # The headers are _only_ output if there is content to show, so grab the first line
                # when there is actual content
                $HeaderRow = if ($Output.Lines.Count -gt 1) {
                    $Output.Lines[0]
                }

                $HeaderRow | Should -Be $ExpectedHeaders -Because $Output.String
            }
        }
    }
}
