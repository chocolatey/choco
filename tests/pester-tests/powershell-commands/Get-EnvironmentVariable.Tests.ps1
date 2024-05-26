Describe 'Get-EnvironmentVariable helper function tests' -Tags Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    Context 'Gets the named environment variable value at the <Scope> scope' -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        BeforeDiscovery {
            $variables = [Environment]::GetEnvironmentVariables($Scope).Keys | ForEach-Object { @{ Name = $_ } }
        }

        It 'Gets the environment variable "<Name>"' -TestCases $variables {
            $expectedValue = [Environment]::GetEnvironmentVariable($Name, $Scope)
            Get-EnvironmentVariable -Name $Name -Scope $Scope | Should -BeExactly $expectedValue
        }
    }

    Context 'Can retrieve the PATH variable without expanding environment names for the <Scope> scope' {
        It 'Retrieves the <Scope> PATH value with un-expanded environment names' {
            Get-EnvironmentVariable -Name 'PATH' -Scope 'Machine' | Should -Match '%[^%;\]+%'
        }
    }
}