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

        It "Should list completions for all Top Level Commands, sorted alphabetically, but not aliases or unpackself" {
            $Command = "choco "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $sortedCompletions = $Completions | Sort-Object -Property { $_ -replace '[^a-z](.*$)', '$1--' }
            $differences = Compare-Object -ReferenceObject $sortedCompletions -DifferenceObject $Completions -SyncWindow 0
            $differences | Should -BeNullOrEmpty -Because ($differences | Format-Table | Out-String)

            # These are not provided by tab completion as they are either command aliases or not intended to be used
            # by end-users.
            $missingFromTabCompletion = @(
                'features'
                'find'
                'setapikey'
                'sources'
                'synchronize'
                'templates'
                'unpackself'
            )

            # Fail the test if any choco command listed by `choco --help` isn't either in the missing list or the
            # tab completion.
            $unaccountedForCommands = @(choco --help) -match " \* \w+ -" -replace " \* (?<Command>\w+) -.+", '${Command}' |
                Where-Object { $_ -notin $missingFromTabCompletion } |
                Where-Object { $_ -notin $Completions }

            $unaccountedForCommands | Should -HaveCount 0 -Because "expected all un-excluded commands to be present in tab completion, but the following were missing: $($unaccountedForCommands -join ', ')"
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
