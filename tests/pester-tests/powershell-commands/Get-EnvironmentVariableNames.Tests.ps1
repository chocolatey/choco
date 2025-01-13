Describe 'Get-EnvironmentVariable helper function tests' -Tags Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    Context 'Gets the named environment variable value at the target scope' -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        It 'Gets the complete list of environment variables in the <Scope> scope"' {
            $expectedValue = [Environment]::GetEnvironmentVariables($Scope).Keys | Sort-Object
            Get-EnvironmentVariableNames -Scope $Scope | Sort-Object | Should -Be $expectedValue
        }
    }
}
