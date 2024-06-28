Import-Module helpers/common-helpers

Describe "choco headers tests" -Tag Chocolatey, HeadersFeature {
    BeforeDiscovery {
        $Commands = @(
            @{
                Command = 'list'
                CommandLine = '--local-only'
                ExpectedHeaders = 'PackageID|Version'
            }
            @{
                #
                Command = 'info'
                CommandLine = 'chocolatey --local-only'
                ExpectedHeaders = 'PackageID|Version'
            }
            @{
                # Needs to pin something...
                Command = 'pin'
                ExpectedHeaders = 'PackageID|Version'
            }
            @{
                Command = 'outdated'
                ExpectedHeaders = 'PackageName|CurrentVersion|AvailableVersion|Pinned'
            }
            @{
                Command = 'source'
                ExpectedHeaders = 'SourceId|Location|Disabled|UserName|Certificate|Priority|BypassProxy|AllowSelfService|AdminOnly'
            }
            @{
                Command = 'config'
                ExpectedHeaders = 'Name|Value|Description'
            }
            @{
                Command = 'feature'
                ExpectedHeaders = 'FeatureName|Enabled|Description'
            }
            @{
                Command = 'apikey'
                ExpectedHeaders = 'Source|Key'
            }
            @{
                Command = 'template'
                ExpectedHeaders = 'TemplateName|Version'
            }
        )
    }
    BeforeAll {
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Outputs headers for <Command> when '--display-headers' provided configured" -ForEach $Commands {
        BeforeAll {
            $Output = Invoke-Choco $Command $CommandLine --limit-output --display-headers
        }

        It 'Exits success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Displays appropriate header" {
            # Some commands won't output anything but the header. In that case we want just the lines instead of indexing into it.
            $ActualOutput = if ($Output.Lines.Count -gt 1) {
                $Output.Lines[0]
            }
            else {
                $Output.Lines
            }

            $ActualOutput | Should -Be $ExpectedHeaders
        }

    }

    Context "Does not output headers for <Command> by default" -ForEach $Commands {
        BeforeAll {
            $Output = Invoke-Choco $Command $CommandLine --limit-output
        }

        It 'Exits success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Does not display header" {
            # Some commands won't output anything but the header. In that case we want just the lines instead of indexing into it.
            $ActualOutput = if ($Output.Lines.Count -gt 1) {
                $Output.Lines[0]
            }
            else {
                $Output.Lines
            }

            $ActualOutput | Should -Not -Be $ExpectedHeaders
        }

    }

    Context "Outputs headers when feature enabled" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            Enable-ChocolateyFeature -Name AlwaysDisplayHeaders
        }

        Context "Outputs headers for <Command>" -ForEach $Commands {
            BeforeAll {
                $Output = Invoke-Choco $Command $CommandLine --limit-output
            }

            It 'Exits success (0)' {
                $Output.ExitCode | Should -Be 0 -Because $Output.String
            }

            It "Displays appropriate header" {
                # Some commands won't output anything but the header. In that case we want just the lines instead of indexing into it.
                $ActualOutput = if ($Output.Lines.Count -gt 1) {
                    $Output.Lines[0]
                }
                else {
                    $Output.Lines
                }

                $ActualOutput | Should -Be $ExpectedHeaders
            }
        }
    }
}
