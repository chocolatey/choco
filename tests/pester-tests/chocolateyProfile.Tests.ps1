Import-Module helpers/common-helpers
Import-Module "$env:ChocolateyInstall\helpers\chocolateyProfile.psm1"

Describe "Chocolatey Profile" -Tag Chocolatey, Profile, Environment {
    # Because we're not modifying the install in any way, there is no need to Initialize-ChocolateyTestInstall
    BeforeDiscovery {
        $isLicensed = Test-PackageIsEqualOrHigher "chocolatey.extension" "0.0.0"

        $ExportNotPresent = $true
        if (Test-ChocolateyVersionEqualOrHigherThan -Version "0.10.16-beta") {
            $ExportNotPresent = $false
        }
    }

    Context "Tab Completion" -Tag TabCompletions {
        BeforeAll {
            Initialize-ChocolateyTestInstall

            # These are needed in order to test the tab completions for some
            # Chocolatey operations
            $null = Invoke-Choco pin add --name="chocolatey"

            $null = Invoke-Choco apikey add --source "https://test.com/api/add/" --api-key "test-api-key"

            $null = Invoke-Choco install upgradepackage --version 1.0.0 --confirm

            New-ChocolateyInstallSnapshot
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

        It "Should correctly complete partial completions" -Skip:$ExportNotPresent {
            $Command = "choco ex"
            [array]$Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $Completions[0] | Should -Be "export"
        }

        It "Should list completions for apikey" {
            $Command = "choco apikey "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "add" -Because $becauseCompletions
            $Completions[1] | Should -Be "list" -Because $becauseCompletions
            $Completions[2] | Should -Be "remove" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--api-key=''" -Because $becauseCompletions
        }

        It "Should list completions for cache" {
            $Command = "choco cache "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "list" -Because $becauseCompletions
            $Completions[1] | Should -Be "remove" -Because $becauseCompletions
            $Completions | Should -Contain "--expired" -Because $becauseCompletions
        }

        It "Should list completions for config" {
            $Command = "choco config "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "get" -Because $becauseCompletions
            $Completions[1] | Should -Be "list" -Because $becauseCompletions
            $Completions[2] | Should -Be "set" -Because $becauseCompletions
            $Completions[3] | Should -Be "unset" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
            $Completions | Should -Contain "--value=''" -Because $becauseCompletions
        }

        It "Should list completions for export" -Skip:$ExportNotPresent {
            $Command = "choco export "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "--include-version" -Because $becauseCompletions
            $Completions[1] | Should -Be "--output-file-path=''" -Because $becauseCompletions
        }

        It "Should list completions for feature" {
            $Command = "choco feature "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "disable" -Because $becauseCompletions
            $Completions[1] | Should -Be "enable" -Because $becauseCompletions
            $Completions[2] | Should -Be "get" -Because $becauseCompletions
            $Completions[3] | Should -Be "list" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
        }

        It "Should list completions for info" {
            $Command = "choco info "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--cert=''" -Because $becauseCompletions
            $Completions | Should -Contain "--certpassword=''" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
            $Completions | Should -Contain "--local-only" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
        }

        It "Should list completions for install" {
            $Command = "choco install "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--allow-downgrade" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-empty-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-empty-checksums-secure" -Because $becauseCompletions
            $Completions | Should -Contain "--apply-args-to-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--apply-package-parameters-to-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--cert=''" -Because $becauseCompletions
            $Completions | Should -Contain "--certpassword=''" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-type=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-x64=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-type-x64=''" -Because $becauseCompletions
            $Completions | Should -Contain "--exit-when-reboot-detected" -Because $becauseCompletions
            $Completions | Should -Contain "--force-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--forcex86" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-checksum" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-detected-reboot" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
            $Completions | Should -Contain "--install-arguments=''" -Because $becauseCompletions
            $Completions | Should -Contain "--not-silent" -Because $becauseCompletions
            $Completions | Should -Contain "--override-arguments" -Because $becauseCompletions
            $Completions | Should -Contain "--package-parameters=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--pin" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--require-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-hooks" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-scripts" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--stop-on-first-failure" -Because $becauseCompletions
            $Completions | Should -Contain "--use-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
        }

        It "Should list completions for license" {
            $Command = "choco license "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "info" -Because $becauseCompletions
            $Completions | Should -Contain "--accept-license" -Because $becauseCompletions
        }

        It "Should list completions for list" {
            $Command = "choco list "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--by-id-only" -Because $becauseCompletions
            $Completions | Should -Contain "--by-tag-only" -Because $becauseCompletions
            $Completions | Should -Contain "--detail" -Because $becauseCompletions
            $Completions | Should -Contain "--exact" -Because $becauseCompletions
            $Completions | Should -Contain "--id-only" -Because $becauseCompletions
            $Completions | Should -Contain "--id-starts-with" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-pinned" -Because $becauseCompletions
            $Completions | Should -Contain "--include-programs" -Because $becauseCompletions
            $Completions | Should -Contain "--page=''" -Because $becauseCompletions
            $Completions | Should -Contain "--page-size=''" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
        }

        It "Should list completions for new" {
            $Command = "choco new "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--automaticpackage" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-x64=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-type=''" -Because $becauseCompletions
            $Completions | Should -Contain "--maintainer=''" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
            $Completions | Should -Contain "--output-directory=''" -Because $becauseCompletions
            $Completions | Should -Contain "--template=''" -Because $becauseCompletions
            $Completions | Should -Contain "--use-built-in-template" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
        }

        It "Should list completions for outdated" {
            $Command = "choco outdated "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--cert=''" -Because $becauseCompletions
            $Completions | Should -Contain "--certpassword=''" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-pinned" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-unfound" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
        }

        It "Should list completions for pack" {
            $Command = "choco pack "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--output-directory=''" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
        }

        It "Should list completions for pin" {
            $Command = "choco pin "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "add" -Because $becauseCompletions
            $Completions[1] | Should -Be "list" -Because $becauseCompletions
            $Completions[2] | Should -Be "remove" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
        }

        It "Should list completions for push" {
            $Command = "choco push "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--api-key=''" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
        }

        It "Should list completions for rule" {
            $Command = "choco rule "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "get" -Because $becauseCompletions
            $Completions[1] | Should -Be "list" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
        }

        It "Should list completions for search" {
            $Command = "choco search "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--all-versions" -Because $becauseCompletions
            $Completions | Should -Contain "--approved-only" -Because $becauseCompletions
            $Completions | Should -Contain "--by-id-only" -Because $becauseCompletions
            $Completions | Should -Contain "--by-tag-only" -Because $becauseCompletions
            $Completions | Should -Contain "--cert=''" -Because $becauseCompletions
            $Completions | Should -Contain "--certpassword=''" -Because $becauseCompletions
            $Completions | Should -Contain "--detail" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--download-cache-only" -Because $becauseCompletions
            $Completions | Should -Contain "--exact" -Because $becauseCompletions
            $Completions | Should -Contain "--id-only" -Because $becauseCompletions
            $Completions | Should -Contain "--id-starts-with" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
            $Completions | Should -Contain "--include-programs" -Because $becauseCompletions
            $Completions | Should -Contain "--not-broken" -Because $becauseCompletions
            $Completions | Should -Contain "--order-by-popularity" -Because $becauseCompletions
            $Completions | Should -Contain "--page=''" -Because $becauseCompletions
            $Completions | Should -Contain "--page-size=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
        }

        It "Should list completions for source" {
            $Command = "choco source "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "add" -Because $becauseCompletions
            $Completions[1] | Should -Be "disable" -Because $becauseCompletions
            $Completions[2] | Should -Be "enable" -Because $becauseCompletions
            $Completions[3] | Should -Be "list" -Because $becauseCompletions
            $Completions[4] | Should -Be "remove" -Because $becauseCompletions
            $Completions | Should -Contain "--admin-only" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-self-service" -Because $becauseCompletions
            $Completions | Should -Contain "--bypass-proxy" -Because $becauseCompletions
            $Completions | Should -Contain "--cert=''" -Because $becauseCompletions
            $Completions | Should -Contain "--certpassword=''" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--priority=''" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
        }

        It "Should list completions for support" {
            $Command = "choco support "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--accept-license" -Because $becauseCompletions
        }

        It "Should list completions for template" {
            $Command = "choco template "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions[0] | Should -Be "info" -Because $becauseCompletions
            $Completions[1] | Should -Be "list" -Because $becauseCompletions
            $Completions | Should -Contain "--name=''" -Because $becauseCompletions
        }

        It "Should list completions for uninstall" {
            $Command = "choco uninstall "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--all-versions" -Because $becauseCompletions
            $Completions | Should -Contain "--apply-args-to-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--apply-package-parameters-to-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--exit-when-reboot-detected" -Because $becauseCompletions
            $Completions | Should -Contain "--fail-on-autouninstaller" -Because $becauseCompletions
            $Completions | Should -Contain "--force-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-autouninstaller-failure" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-detected-reboot" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--not-silent" -Because $becauseCompletions
            $Completions | Should -Contain "--override-arguments" -Because $becauseCompletions
            $Completions | Should -Contain "--package-parameters=''" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-autouninstaller" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-hooks" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-scripts" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--stop-on-first-failure" -Because $becauseCompletions
            $Completions | Should -Contain "--uninstall-arguments=''" -Because $becauseCompletions
            $Completions | Should -Contain "--use-autouninstaller" -Because $becauseCompletions
            $Completions | Should -Contain "--use-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
        }

        It "Should list completions for upgrade" {
            $Command = "choco upgrade "
            $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

            $becauseCompletions = ($Completions -Join ", ")

            $Completions | Should -Contain "--allow-downgrade" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-empty-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--allow-empty-checksums-secure" -Because $becauseCompletions
            $Completions | Should -Contain "--apply-args-to-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--apply-package-parameters-to-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--cert=''" -Because $becauseCompletions
            $Completions | Should -Contain "--certpassword=''" -Because $becauseCompletions
            $Completions | Should -Contain "--disable-repository-optimizations" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-x64=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-type=''" -Because $becauseCompletions
            $Completions | Should -Contain "--download-checksum-type-x64=''" -Because $becauseCompletions
            $Completions | Should -Contain "--except=''" -Because $becauseCompletions
            $Completions | Should -Contain "--exclude-prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--exit-when-reboot-detected" -Because $becauseCompletions
            $Completions | Should -Contain "--fail-on-not-installed" -Because $becauseCompletions
            $Completions | Should -Contain "--fail-on-unfound" -Because $becauseCompletions
            $Completions | Should -Contain "--forcex86" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-dependencies" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-detected-reboot" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-pinned" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-remembered-arguments" -Because $becauseCompletions
            $Completions | Should -Contain "--ignore-unfound" -Because $becauseCompletions
            $Completions | Should -Contain "--include-configured-sources" -Because $becauseCompletions
            $Completions | Should -Contain "--install-arguments=''" -Because $becauseCompletions
            $Completions | Should -Contain "--install-if-not-installed" -Because $becauseCompletions
            $Completions | Should -Contain "--not-silent" -Because $becauseCompletions
            $Completions | Should -Contain "--override-arguments" -Because $becauseCompletions
            $Completions | Should -Contain "--package-parameters=''" -Because $becauseCompletions
            $Completions | Should -Contain "--password=''" -Because $becauseCompletions
            $Completions | Should -Contain "--pin" -Because $becauseCompletions
            $Completions | Should -Contain "--prerelease" -Because $becauseCompletions
            $Completions | Should -Contain "--require-checksums" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-hooks" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-if-not-installed" -Because $becauseCompletions
            $Completions | Should -Contain "--skip-scripts" -Because $becauseCompletions
            $Completions | Should -Contain "--source=''" -Because $becauseCompletions
            $Completions | Should -Contain "--stop-on-first-failure" -Because $becauseCompletions
            $Completions | Should -Contain "--use-package-exit-codes" -Because $becauseCompletions
            $Completions | Should -Contain "--use-remembered-arguments" -Because $becauseCompletions
            $Completions | Should -Contain "--user=''" -Because $becauseCompletions
            $Completions | Should -Contain "--version=''" -Because $becauseCompletions
        }

        # This is marked Internal as all the tests run `choco.exe` to determine the tab expansion and unofficial builds will not provide the tab completion.
        Context "Requires Official Build" -Tag Internal {
            It "Should list completions for apikey remove" {
                $Command = "choco apikey remove --source='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--source='https://test.com/api/add/'" -Because $becauseCompletions
            }

            It "Should list completions for feature enable" {
                $Command = "choco feature enable --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='allowEmptyChecksums'" -Because $becauseCompletions
                $Completions | Should -Contain "--name='useRememberedArgumentsForUpgrades'" -Because $becauseCompletions

                if ($isLicensed) {
                    $Completions | Should -Contain "--name='adminOnlyExecutionForAllChocolateyCommands'" -Because $becauseCompletions
                }
            }

            It "Should list completions for feature disable" {
                $Command = "choco feature disable --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='allowEmptyChecksums'" -Because $becauseCompletions
                $Completions | Should -Contain "--name='useRememberedArgumentsForUpgrades'" -Because $becauseCompletions

                if ($isLicensed) {
                    $Completions | Should -Contain "--name='adminOnlyExecutionForAllChocolateyCommands'" -Because $becauseCompletions
                }
            }

            It "Should list completions for feature get" {
                $Command = "choco feature get --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='allowEmptyChecksums'" -Because $becauseCompletions
                $Completions | Should -Contain "--name='useRememberedArgumentsForUpgrades'" -Because $becauseCompletions

                if ($isLicensed) {
                    $Completions | Should -Contain "--name='adminOnlyExecutionForAllChocolateyCommands'" -Because $becauseCompletions
                }
            }

            It "Should list completions for config get" {
                $Command = "choco config get --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='cacheLocation'" -Because $becauseCompletions
                $Completions | Should -Contain "--name='webRequestTimeoutSeconds'" -Because $becauseCompletions

                if ($isLicensed) {
                    $Completions | Should -Contain "--name='virusScannerType'" -Because $becauseCompletions
                }
            }

            It "Should list completions for config set" {
                $Command = "choco config set --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='cacheLocation'" -Because $becauseCompletions
                $Completions | Should -Contain "--name='webRequestTimeoutSeconds'" -Because $becauseCompletions

                if ($isLicensed) {
                    $Completions | Should -Contain "--name='virusScannerType'" -Because $becauseCompletions
                }
            }

            It "Should list completions for config unset" {
                $Command = "choco config unset --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='cacheLocation'" -Because $becauseCompletions
                $Completions | Should -Contain "--name='webRequestTimeoutSeconds'" -Because $becauseCompletions

                if ($isLicensed) {
                    $Completions | Should -Contain "--name='virusScannerType'" -Because $becauseCompletions
                }
            }

            It "Should list completions for pin add" {
                $Command = "choco pin add --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='upgradepackage'" -Because $becauseCompletions
            }

            It "Should list completions for pin remove" {
                $Command = "choco pin remove --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='chocolatey'" -Because $becauseCompletions
            }

            It "Should list completions for rule get" {
                $Command = "choco rule get --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='CHCU0001'" -Because $becauseCompletions
            }

            It "Should list completions for source disable" {
                $Command = "choco source disable --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='chocolatey'" -Because $becauseCompletions
            }

            It "Should list completions for source enable" {
                $Command = "choco source enable --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='chocolatey'" -Because $becauseCompletions
            }

            It "Should list completions for source remove" {
                $Command = "choco source remove --name='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--name='chocolatey'" -Because $becauseCompletions
            }

            It "Should list versions for <_> isdependency --version=" -ForEach @('install', 'upgrade') {
                $Command = "choco $_ isdependency --version="
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--version='2.1.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='2.0.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.1.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.0.1'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.0.0'" -Because $becauseCompletions
            }

            It "Should list versions for <_> isdependency --version='" -ForEach @('install', 'upgrade') {
                $Command = "choco $_ isdependency --version='"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn $Command.Length).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--version='2.1.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='2.0.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.1.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.0.1'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.0.0'" -Because $becauseCompletions
            }

            It "Should list versions for <_> isdependency --version=''" -ForEach @('install', 'upgrade') {
                $Command = "choco $_ isdependency --version=''"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn ($Command.Length - 1)).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--version='2.1.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='2.0.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.1.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.0.1'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.0.0'" -Because $becauseCompletions
            }

            It "Should list versions for <_> isdependency --version='' without moving cursor" -ForEach @('install', 'upgrade') {
                $Command = "choco $_ isdependency --version=''"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn ($Command.Length)).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--version='2.1.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='2.0.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.1.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.0.1'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='1.0.0'" -Because $becauseCompletions
            }

            It "Should list 2.x versions for <_> isdependency --version='2" -ForEach @('install', 'upgrade') {
                $Command = "choco $_ isdependency --version='2"
                $Completions = (TabExpansion2 -inputScript $Command -cursorColumn ($Command.Length)).CompletionMatches.CompletionText

                $becauseCompletions = ($Completions -Join ", ")

                $Completions | Should -Contain "--version='2.1.0'" -Because $becauseCompletions
                $Completions | Should -Contain "--version='2.0.0'" -Because $becauseCompletions
                $Completions | Should -Not -Contain "--version='1.1.0'" -Because $becauseCompletions
                $Completions | Should -Not -Contain "--version='1.0.1'" -Because $becauseCompletions
                $Completions | Should -Not -Contain "--version='1.0.0'" -Because $becauseCompletions
            }
        }
    }
}
