param(
    # Which help command to test
    [string[]]$HelpOptions = @(
        "--help"
        "-?"
        "-help"
    ),

    # Commands that don't have full help
    [string[]]$SkipCommand = @(
        "unpackself"  # Out of spec
        "support"     # This should be tested separately
    )
)
Import-Module helpers/common-helpers

BeforeDiscovery {
    $AllTopLevelCommands = (Invoke-Choco $HelpOptions[0]).Lines -match "\* (?<Command>\w+) -" -replace "\* (?<Command>\w+) -.+", '${Command}'
    $TopLevelCommands = $AllTopLevelCommands.Where{ $_ -notin $SkipCommand }
}

Describe "choco help sections with option <_>" -ForEach $HelpOptions -Tag Chocolatey, HelpCommand {
    BeforeDiscovery {
        $helpArgument = $_
    }

    BeforeAll {
        Remove-NuGetPaths
        $helpArgument = $_
        Initialize-ChocolateyTestInstall

        # We're just testing help output here, we don't need to copy config/package files
        New-ChocolateyInstallSnapshot -NoSnapshotCopy
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Top Level Help" {
        BeforeAll {
            $Output = Invoke-Choco $_
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs the Top-Level Help" {
            $Output.Lines | Should -Contain "Commands"
            $Output.Lines | Should -Contain "Default Options and Switches"
        }
    }

    Context "choco <_> $helpArgument" -ForEach $TopLevelCommands {
        BeforeDiscovery {
            $comandsWithoutExitCodes = @(
                "help"
                "download"
                "synchronize"
                "sync"
                "optimize"
            )
        }

        BeforeAll {
            $Output = Invoke-Choco $_ $helpArgument
        }

        It "'choco <_> $helpArgument' exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs help for <_>" {
            $Output.Lines | Should -Not -BeNullOrEmpty
        }

        It "Outputs Usage for <_>" -Skip:$($_ -match "help") {
            $Output.Lines | Should -Contain "Usage"
        }

        It "Outputs Examples for <_>" -Skip:$($_ -match "help") {
            $Output.Lines | Should -Contain "Examples"
        }

        It "Outputs Exit Codes for <_>" -Skip:$($_ -in $comandsWithoutExitCodes) {
            $Output.Lines | Should -Contain "Exit Codes"
        }

        It "Outputs Options and Switches for <_>" {
            $Output.Lines | Should -Contain "Options and Switches"
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
