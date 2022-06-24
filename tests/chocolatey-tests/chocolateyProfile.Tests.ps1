Import-Module helpers/common-helpers
Import-Module "$env:ChocolateyInstall\helpers\chocolateyProfile.psm1"

Describe "Chocolatey Profile" -Tag Chocolatey, Profile, Environment {
    # Because we're not modifying the install in any way, there is no need to Initialize-ChocolateyTestInstall
    BeforeDiscovery {
        $ExportNotPresent = $true
        if (Test-ChocolateyVersionEqualOrHigherThan -Version "0.10.16-beta") {
            $ExportNotPresent = $false
        }
    }

    Context "Tab Completion" {
        It "Should Exist" {
            Test-Path Function:\TabExpansion | Should -BeTrue
        }

        It "Should have overridden TabExpansion with a new scriptblock including ChocolateyTabExpansion" {
            [bool]((Get-Command TabExpansion).ScriptBlock -match "ChocolateyTabExpansion") | Should -BeTrue
        }

        It "Should list completions for Top Level Commands" {
            $Command = "choco "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            # These should be first
            $Completions[0] | Should -Be "-?"
            $Completions[1] | Should -Be "search"

            # TODO: Determine why these lines are commented out. Remove if not needed, get working otherwise.
            <#  Completions don't contain aliases, etc
            foreach ($Command in $(
                $(choco --help) -match " \* (?<Command>\w+) -" -replace " \* (?<Command>\w+) -.+", '$1'
            )) {
                $Completions | Should -Contain $Command
            }
            #>
        }

        It "Should list completions for Pin" {
            $Command = "choco pin "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $Completions[0] | Should -Be "list"
            $Completions[1] | Should -Be "add"
            $Completions[2] | Should -Be "remove"
        }

        It "Should correctly complete partial completions" -Skip:$ExportNotPresent {
            $Command = "choco ex"
            [array]$Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $Completions[0] | Should -Be "export"
        }

        It "Should list completions for Export" -Skip:$ExportNotPresent {
            $Command = "choco export "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $Completions[0] | Should -Be "--include-version-numbers"
            $Completions[1] | Should -Be "--output-file-path=''"
        }

        It "Should list completions for Template" {
            $Command = "choco template "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $Completions[0] | Should -Be "list"
            $Completions[1] | Should -Be "info"
            $Completions[2] | Should -Be "-?"
            $Completions[3] | Should -Be "--name=''"
        }
    }
}
