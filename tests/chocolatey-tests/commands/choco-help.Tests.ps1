param(
    # Which help command to test
    [string[]]$Command = @(
        "--help"
        "-?"
        "-help"
    ),

    # Commands that don't have full help
    [string[]]$SkipCommand = @(
        "unpackself"  # Out of spec
        "version"     # Deprecated
        "update"      # Deprecated
        "support"     # This should be tested seperately
    )
)
Import-Module helpers/common-helpers

BeforeDiscovery {
    $AllTopLevelCommands = (Invoke-Choco $Command[0]).Lines -match " \* (?<Command>\w+) -" -replace " \* (?<Command>\w+) -.+", '$1'
    $TopLevelCommands = $AllTopLevelCommands.Where{$_ -notin $SkipCommand}
}

Describe "choco help sections with command <_>" -ForEach $Command -Tag Chocolatey, HelpCommand {
    BeforeDiscovery {
        $helpArgument = $_
    }

    BeforeAll {
        $helpArgument = $_
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Top Level Help" {
        BeforeAll {
            $Output = Invoke-Choco $_ $helpArgument
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs the Top-Level Help" {
            $Output.Lines | Should -Contain "Commands"
            $Output.Lines | Should -Contain "Default Options and Switches"
        }
    }

    Context "choco <_> $helpArgument" -Foreach $TopLevelCommands {
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
}
