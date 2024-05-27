Describe 'Set-EnvironmentVariable helper function tests' -Tags Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    Context 'Sets an environment variable value at the target <Scope>' -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        BeforeDiscovery {
            $variables = @(
                @{ Name = "Test"; Value = "TestValue" }
                @{ Name = "Environment"; Value = "1234" }
                @{ Name = "Variable"; Value = "C:\test\path" }
            )
        }

        Describe 'Setting environment variable <Name>' -ForEach $variables {
            It 'Sets the target environment variable in the proper scope, as well as current process scope' {
                Set-EnvironmentVariable -Name $Name -Value $Value -Scope $Scope
                [Environment]::GetEnvironmentVariable($Name, $Scope) | Should -BeExactly $Value

                # We are explicitly checking the Process variable here.
                [Environment]::GetEnvironmentVariable($Name, 'Process') | Should -BeExactly $Value
            }
            
            AfterAll {
                Set-EnvironmentVariable -Name $Name -Value "" -Scope $Scope
            }
        }
    }
}