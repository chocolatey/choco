Import-Module helpers/common-helpers

Describe "hooks tests" -Tag Chocolatey, Hooks {
    BeforeDiscovery {
        $Flags = @(
            @{ Flag = '' ; RunsHooks = $true ; Command = 'install' }
            @{ Flag = '--skip-powershell' ; RunsHooks = $false ; Command = 'install' }
            @{ Flag = '--skip-hooks' ; RunsHooks = $false ; Command = 'install' }

            @{ Flag = '' ; RunsHooks = $true ; Command = 'uninstall' }
            @{ Flag = '--skip-powershell' ; RunsHooks = $false ; Command = 'uninstall' }
            @{ Flag = '--skip-hooks' ; RunsHooks = $false ; Command = 'uninstall' }

            @{ Flag = '' ; RunsHooks = $true ; Command = 'upgrade' }
            @{ Flag = '--skip-powershell' ; RunsHooks = $false ; Command = 'upgrade' }
            @{ Flag = '--skip-hooks' ; RunsHooks = $false ; Command = 'upgrade' }

        )
    }
    BeforeAll {
        Initialize-ChocolateyTestInstall
        # Add the hooks package to the test install so that it is available in each snapshot.
        $null = Invoke-Choco install scriptpackage.hook --version 1.0.0
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context '<Command> hooks with flag: <Flag>' -ForEach $Flags {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $Package = 'upgradepackage'

            if ($Command -ne 'install') {
                $ver = if ($Command -eq 'upgrade') {
                    '1.0.0'
                }
                else {
                    '1.1.0'
                }
                $null = Invoke-Choco install $Package --version $ver --no-progress
            }

            $Output = Invoke-Choco $Command $Package $Flag --no-progress
        }

        # Uninstall/Upgrade exit -1: https://github.com/chocolatey/choco/issues/2822
        It "Exits with Success (0,-1)" {
            $Output.ExitCode | Should -BeIn @(0, -1) -Because $Output.String
        }

        It "Should execute hooks (<RunsHooks>)" {
            $Version = '1.1.0'

            $Messages = @(
                if ($Command -eq 'uninstall') {
                    "pre-uninstall-all.ps1 hook ran for $Package $Version"
                    "post-uninstall-all.ps1 hook ran for $Package $Version"
                    "pre-uninstall-$Package.ps1 hook ran for $Package $Version"
                    "post-uninstall-$Package.ps1 hook ran for $Package $Version"
                    "pre-beforemodify-all.ps1 hook ran for $Package $Version"
                    "post-beforemodify-all.ps1 hook ran for $Package $Version"
                    "pre-beforemodify-$Package.ps1 hook ran for $Package $Version"
                    "post-beforemodify-$Package.ps1 hook ran for $Package $Version"
                }
                else {
                    "pre-install-all.ps1 hook ran for $Package $Version"
                    "post-install-all.ps1 hook ran for $Package $Version"
                    "pre-install-$Package.ps1 hook ran for $Package $Version"
                    "post-install-$Package.ps1 hook ran for $Package $Version"
                }

                if ($Command -eq 'upgrade') {
                    "pre-beforemodify-all.ps1 hook ran for $Package 1.0.0"
                    "pre-beforemodify-$Package.ps1 hook ran for $Package 1.0.0"
                }
            )

            foreach ($msg in $Messages) {
                if ($RunsHooks) {
                    $Output.Lines | Should -Contain $msg -Because $Output.String
                }
                else {
                    $Output.Lines | Should -Not -Contain $msg -Because $Output.String
                }
            }

            $Output.Lines | Should -Not -Contain "pre-$Command-doesnotexist.ps1 hook ran for $Package $Version" -Because $Output.String
        }
    }
}
