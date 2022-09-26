Import-Module helpers/common-helpers

Describe "choco outdated" -Tag Chocolatey, OutdatedCommand {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        # Pin all of the Chocolatey packages
        @(
            'chocolatey'
            'chocolatey.extension'
            'chocolatey-agent'
            'chocolatey-management-database'
            'chocolatey-management-service'
            'chocolatey-management-web'
        ) | ForEach-Object {
            $null = Invoke-Choco pin add -n $_
        }
        Invoke-Choco install upgradepackage --version 1.0.0 --confirm
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "outdated ignore-pinned uses correct enhanced exit codes" -Foreach @(
        @{ Argument = '' ; ExitCode = 2 }
        @{ Argument = '--ignore-pinned' ; ExitCode = 0 }
    )  -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan '1.1.0')) {
        BeforeAll {
            Disable-ChocolateyFeature -Name showNonElevatedWarnings
            Enable-ChocolateyFeature -Name allowGlobalConfirmation, useRememberedArgumentsForUpgrades, useEnhancedExitCodes
            $null = Invoke-Choco pin add -n upgradepackage
            $Output = Invoke-Choco outdated $Argument
        }

        It 'Exits with correct exit code (<ExitCode>)' {
            $Output.ExitCode | Should -Be $ExitCode -Because $Output.String
        }

        It 'Outputs properly' {
            $Output.Lines | Should -Not:($ExitCode -eq 0) -Contain 'upgradepackage|1.0.0|1.1.0|true'
        }
    }
}
