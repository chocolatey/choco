Describe 'Set-EnvironmentVariable helper function tests' -Tags Cmdlets, SetEnvironmentVariable {
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
            BeforeAll {
                Set-EnvironmentVariable -Name $Name -Value $Value -Scope $Scope
            }
            
            AfterAll {
                Set-EnvironmentVariable -Name $Name -Value "" -Scope $Scope
            }

            It 'sets the target environment variable in the proper scope' {
                [Environment]::GetEnvironmentVariable($Name, $Scope) | Should -BeExactly $Value
            }

            It 'propagates the change to the current process' {
                Get-Content "Env:\$Name" | Should -BeExactly $Value
            }
        }
    }
}