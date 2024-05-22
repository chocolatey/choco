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

        It "Should list completions for upgrade" {
            $Command = "choco upgrade "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "-y" -Because $becauseCompletions
            $Completions | Should -Contain "-whatif" -Because $becauseCompletions
            $Completions | Should -Contain "--pre" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
            $Completions | Should -Contain "--except=''" -Because $becauseCompletions
            $Completions | Should -Contain "--params=''" -Because $becauseCompletions
            $Completions | Should -Contain "--install-arguments=''" -Because $becauseCompletions
            $Completions | Should -Contain "--override-arguments" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--source='windowsfeatures'" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--forcex86" -Because $becauseCompletions
            $Completions | Should -Contain "--not-silent" -Because $becauseCompletions
            $Completions | Should -Contain "--package-parameters=''" -Because $becauseCompletions
            $Completions | Should -Contain "--exit-when-reboot-detected" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-detected-reboot" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-downgrade" -Because $becauseCompletions
            $Completions | Should -Contain "--require-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--use-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-automation-scripts" -Because $becauseCompletions
            $Completions | Should -Contain "--fail-on-unfound" -Because $becauseCompletions
            $Completions | Should -Contain "--fail-on-not-installed" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-empty-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-empty-checksums-secure" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-type=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-x64=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-type-x64=''" -Because $becauseCompletions
            $Completions | Should -Contain "--exclude-prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--stop-on-first-package-failure" -Because $becauseCompletions
            $Completions | Should -Contain "--use-remembered-options" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-remembered-options" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-when-not-installed" -Because $becauseCompletions
            $Completions | Should -Contain "--install-if-not-installed" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-package-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--pin" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-pinned" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
        }

        It "Should list completions for rule" {
            $Command = "choco rule "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
        }

        It "Should list completions for search" {
            $Command = "choco search "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--id-only" -Because $becauseCompletions
            $Completions | Should -Contain "--pre" -Because $becauseCompletions
            $Completions | Should -Contain "--exact" -Because $becauseCompletions
            $Completions | Should -Contain "--by-id-only" -Because $becauseCompletions
            $Completions | Should -Contain "--id-starts-with" -Because $becauseCompletions
            $Completions | Should -Contain "--detailed" -Because $becauseCompletions
            $Completions | Should -Contain "--approved-only" -Because $becauseCompletions
            $Completions | Should -Contain "--not-broken" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--include-programs" -Because $becauseCompletions
            $Completions | Should -Contain "--page=''" -Because $becauseCompletions
            $Completions | Should -Contain "--page-size=''" -Because $becauseCompletions
            $Completions | Should -Contain "--order-by-popularity" -Because $becauseCompletions
            $Completions | Should -Contain "--download-cache-only" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-package-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
        }

        It "Should list completions for info" {
            $Command = "choco info "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--local-only" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--cert=''" -Because $becauseCompletions
            $Completions | Should -Contain "--certpassword=''" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-package-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
        }

        It "Should list completions for install" {
            $Command = "choco install "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "-y" -Because $becauseCompletions
            $Completions | Should -Contain "-whatif" -Because $becauseCompletions
            $Completions | Should -Contain "--pre" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
            $Completions | Should -Contain "--params=''" -Because $becauseCompletions
            $Completions | Should -Contain "--install-arguments=''" -Because $becauseCompletions
            $Completions | Should -Contain "--override-arguments" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--source='windowsfeatures'" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--forcex86" -Because $becauseCompletions
            $Completions | Should -Contain "--not-silent" -Because $becauseCompletions
            $Completions | Should -Contain "--package-parameters=''" -Because $becauseCompletions
            $Completions | Should -Contain "--exit-when-reboot-detected" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-detected-reboot" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-downgrade" -Because $becauseCompletions
            $Completions | Should -Contain "--force-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--require-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--use-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-automation-scripts" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-empty-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-empty-checksums-secure" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-type=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-x64=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-type-x64=''" -Because $becauseCompletions
            $Completions | Should -Contain "--stop-on-first-package-failure" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-package-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--pin" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
        }

        It "Should list completions for outdated" {
            $Command = "choco outdated "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-pinned" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-unfound" -Because $becauseCompletions
            $Completions | Should -Contain "--pre" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-package-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
        }

        }
    }
}
