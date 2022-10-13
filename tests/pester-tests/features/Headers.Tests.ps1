Import-Module helpers/common-helpers

Describe "choco headers tests" -Tag Chocolatey, HeadersFeature {
    BeforeDiscovery {
        $isLicensed = Test-PackageIsEqualOrHigher "chocolatey.extension" "0.0.0"

        $Commands = @(
            @{
                Command = 'apikey'
                CommandLine = 'list'
                ExpectedHeaders = 'Source|ApiKey'
                SkipTest = $false
            }
            @{
                Command = 'config'
                CommandLine = 'list'
                ExpectedHeaders = 'Name|Value|Description'
                SkipTest = $false
            }
            @{
                Command = 'feature'
                CommandLine = 'list'
                ExpectedHeaders = 'Name|Enabled|Description'
                SkipTest = $false
            }
            @{
                Command = 'info'
                CommandLine = "chocolatey", "--local-only"
                ExpectedHeaders = 'Id|Version'
                SkipTest = $false
            }
            @{
                Command = 'license'
                CommandLine = 'info'
                ExpectedHeaders = 'Name|Type|ExpirationDate|NodeCount'
                SkipTest = $isLicensed
            }
            @{
                Command = 'list'
                ExpectedHeaders = 'Id|Version'
                SkipTest = $false
            }
            @{
                Command = 'outdated'
                ExpectedHeaders = 'Id|Version|AvailableVersion|Pinned'
                SkipTest = $false
            }
            @{
                # Needs to pin something...
                Command = 'pin'
                CommandLine = 'list'
                ExpectedHeaders = 'Id|Version'
                SkipTest = $false
            }
            @{
                Command = 'rule'
                CommandLine = 'list'
                ExpectedHeaders = 'Severity|Id|Summary|HelpUrl'
                SkipTest = $false
            }
            @{
                Command = 'search'
                CommandLine = 'windirstat'
                ExpectedHeaders = 'Id|Version'
                SkipTest = $false
            }
            @{
                Command = 'source'
                CommandLine = 'list'
                ExpectedHeaders = 'Name|Source|Disabled|UserName|Certificate|Priority|BypassProxy|AllowSelfService|AdminOnly'
                SkipTest = $false
            }
            @{
                Command = 'template'
                CommandLine = 'list'
                ExpectedHeaders = 'Name|Version'
                SkipTest = $false
            }
        )
    }
    BeforeAll {
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot

        # These are needed in order to test the choco pin list execution
        # as well as the choco apikey list as the headers will only be shown
        # when there is a pinned package
        $null = Invoke-Choco pin add --name="chocolatey"

        $null = Invoke-Choco apikey add --source "https://community.chocolatey.org/api/v2" --api-key "bob"
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Outputs headers for <Command> when '--include-headers' provided configured" -ForEach $Commands -Skip:($SkipTest) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot -NoSnapshotCopy

            $Output = Invoke-Choco $Command @CommandLine --limit-output --include-headers
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

    Context "Does not output headers for <Command> by default" -ForEach $Commands -Skip:($SkipTest) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot -NoSnapshotCopy

            $Output = Invoke-Choco $Command @CommandLine --limit-output
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

    Context "Includes headers when feature enabled" -Skip:($SkipTest) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            Enable-ChocolateyFeature -Name AlwaysIncludeHeaders
        }

        Context "Includes headers for <Command>" -ForEach $Commands {
            BeforeAll {
                $Output = Invoke-Choco $Command @CommandLine --limit-output
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
