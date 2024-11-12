Describe 'Set-EnvironmentVariable helper function tests' -Tags SetEnvironmentVariable, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    Context 'Unit tests' -Tags WhatIf -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        It 'Sets an environment variable value at the target <Scope>' {
            $testVariableName = 'testVariable'
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'")
            $Command = [scriptblock]::Create("Set-EnvironmentVariable -Name $testVariableName -Value 'TEST' -Scope $Scope -WhatIf")
            
            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf[0] | Should -BeExactly "What if: Performing the operation ""Set $Scope environment variable"" on target ""testVariable""." -Because $results.Output

            if ($Scope -ne 'Process') {
                $results.WhatIf[1] | Should -BeExactly 'What if: Performing the operation "Notify system of changes" on target "Environment variables".' -Because $results.Output
                $results.WhatIf[2] | Should -BeExactly 'What if: Performing the operation "Refresh all environment variables" on target "Current process".' -Because $results.Output
            }
        }
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