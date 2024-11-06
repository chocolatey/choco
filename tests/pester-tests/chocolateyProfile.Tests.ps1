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

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "list" -Because $becauseCompletions
            $Completions[1] | Should -Be "add" -Because $becauseCompletions
            $Completions[2] | Should -Be "remove" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
        }

        It "Should correctly complete partial completions" -Skip:$ExportNotPresent {
            $Command = "choco ex"
            [array]$Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $Completions[0] | Should -Be "export"
        }

        It "Should list completions for Export" -Skip:$ExportNotPresent {
            $Command = "choco export "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "--include-version-numbers" -Because $becauseCompletions
            $Completions[1] | Should -Be "--output-file-path=''" -Because $becauseCompletions
        }

        It "Should list completions for Template" {
            $Command = "choco template "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "list" -Because $becauseCompletions
            $Completions[1] | Should -Be "info" -Because $becauseCompletions
            $Completions[2] | Should -Be "-?" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
        }

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

        It "Should list completions for cache" {
            $Command = "choco cache "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "list" -Because $becauseCompletions
            $Completions[1] | Should -Be "remove" -Because $becauseCompletions
            $Completions[2] | Should -Be "-?" -Because $becauseCompletions
            $Completions | Should -Contain "--expired" -Because $becauseCompletions
        }

        It "Should list completions for feature" {
            $Command = "choco feature "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "list" -Because $becauseCompletions
            $Completions[1] | Should -Be "get" -Because $becauseCompletions
            $Completions[2] | Should -Be "disable" -Because $becauseCompletions
            $Completions[3] | Should -Be "enable" -Because $becauseCompletions
            $Completions[4] | Should -Be "-?" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
        }

        It "Should list completions for config" {
            $Command = "choco config "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "list" -Because $becauseCompletions
            $Completions[1] | Should -Be "get" -Because $becauseCompletions
            $Completions[2] | Should -Be "set" -Because $becauseCompletions
            $Completions[3] | Should -Be "unset" -Because $becauseCompletions
            $Completions[4] | Should -Be "-?" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
            $Completions | Should -Contain "--value=''" -Because $becauseCompletions
        }

        It "Should list completions for source" {
            $Command = "choco source "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "list" -Because $becauseCompletions
            $Completions[1] | Should -Be "add" -Because $becauseCompletions
            $Completions[2] | Should -Be "remove" -Because $becauseCompletions
            $Completions[3] | Should -Be "disable" -Because $becauseCompletions
            $Completions[4] | Should -Be "enable" -Because $becauseCompletions
            $Completions | Should -Contain "-?" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--priority=''" -Because $becauseCompletions
            $Completions | Should -Contain "--bypass-proxy" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-self-service" -Because $becauseCompletions
        }

        It "Should list completions for apikey" {
            $Command = "choco apikey "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--api-key=''" -Because $becauseCompletions
            $Completions | Should -Contain "--remove" -Because $becauseCompletions
        }

        It "Should list completions for push" {
            $Command = "choco push "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--api-key=''" -Because $becauseCompletions
            $Completions | Should -Contain "--timeout=''" -Because $becauseCompletions
        }

        It "Should list completions for pack" {
            $Command = "choco pack "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
            $Completions | Should -Contain "--output-directory=''" -Because $becauseCompletions
        }

        It "Should list completions for new" {
            $Command = "choco new "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--template-name=''" -Because $becauseCompletions
            $Completions | Should -Contain "--output-directory=''" -Because $becauseCompletions
            $Completions | Should -Contain "--automaticpackage" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
            $Completions | Should -Contain "--maintainer=''" -Because $becauseCompletions
            $Completions | Should -Contain "packageversion=''" -Because $becauseCompletions
            $Completions | Should -Contain "maintainername=''" -Because $becauseCompletions
            $Completions | Should -Contain "maintainerrepo=''" -Because $becauseCompletions
            $Completions | Should -Contain "installertype=''" -Because $becauseCompletions
            $Completions | Should -Contain "url=''" -Because $becauseCompletions
            $Completions | Should -Contain "url64=''" -Because $becauseCompletions
            $Completions | Should -Contain "silentargs=''" -Because $becauseCompletions
            $Completions | Should -Contain "--use-built-in-template" -Because $becauseCompletions
        }

        It "Should list completions for uninstall" {
            $Command = "choco uninstall "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "-y" -Because $becauseCompletions
            $Completions | Should -Contain "-whatif" -Because $becauseCompletions
            $Completions | Should -Contain "--force-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--remove-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--all-versions" -Because $becauseCompletions
            $Completions | Should -Contain "--source='windowsfeatures'" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
            $Completions | Should -Contain "--uninstall-arguments=''" -Because $becauseCompletions
            $Completions | Should -Contain "--override-arguments" -Because $becauseCompletions
            $Completions | Should -Contain "--not-silent" -Because $becauseCompletions
            $Completions | Should -Contain "--params=''" -Because $becauseCompletions
            $Completions | Should -Contain "--package-parameters=''" -Because $becauseCompletions
            $Completions | Should -Contain "--exit-when-reboot-detected" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-detected-reboot" -Because $becauseCompletions
            $Completions | Should -Contain "--use-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-automation-scripts" -Because $becauseCompletions
            $Completions | Should -Contain "--use-autouninstaller" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-autouninstaller" -Because $becauseCompletions
            $Completions | Should -Contain "--fail-on-autouninstaller" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-autouninstaller-failure" -Because $becauseCompletions
            $Completions | Should -Contain "--stop-on-first-package-failure" -Because $becauseCompletions
        }

        It "Should list completions for list" {
            $Command = "choco list "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--id-only" -Because $becauseCompletions
            $Completions | Should -Contain "--pre" -Because $becauseCompletions
            $Completions | Should -Contain "--exact" -Because $becauseCompletions
            $Completions | Should -Contain "--by-id-only" -Because $becauseCompletions
            $Completions | Should -Contain "--id-starts-with" -Because $becauseCompletions
            $Completions | Should -Contain "--detailed" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--include-programs" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--page=''" -Because $becauseCompletions
            $Completions | Should -Contain "--page-size=''" -Because $becauseCompletions
        }
    }
}
