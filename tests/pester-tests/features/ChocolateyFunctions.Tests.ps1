# These tests install packages that exercise the Chocolatey PowerShell functions
# TODO: Move some of the install tests into this file
Describe 'Chocolatey PowerShell functions' {
    BeforeAll {
        Initialize-ChocolateyTestInstall
    }

    Context 'Get-ChocolateyConfigValue' -ForEach @{
        ConfigValues = @(
            @{ Name = 'addedTextValue' ; Value = 'SomeTextValue' }
            @{ Name = 'addedNumberValue' ; Value = 123456 }
            @{ Name = 'commandExecutionTimeoutSeconds' ; Value = 2795 }
            @{ Name = 'cacheLocation' ; Value = $null }
            @{ Name = 'nonExistentKey' ; Value = $null }
        )
    } -Tag Get-ChocolateyConfigValue {

        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            foreach ($Value in $ConfigValues) {
                if ($null -ne $Value.Value) {
                    Invoke-Choco config set --name $Value.Name --value $Value.Value
                }
            }

            $Output = Invoke-Choco install getconfig --confirm
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Outputs the expected value for config entry (<Name>)' -ForEach $ConfigValues {
            # Trim the output as Lines is trimmed.
            $Output.Lines | Should -Contain "${Name}: $Value".Trim() -Because $Output.String
        }
    }
}
