Describe 'Install-ChocolateyEnvironmentVariable helper function tests' -Tags InstallChocolateyEnvironmentVariable, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    AfterAll {
        Remove-Module "chocolateyInstaller" -Force
    }

    Context 'Unit tests' -Tags WhatIf -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        BeforeAll {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1' -Global")
        }

        It 'Sets an environment variable value at the target <Scope>' {
            $Command = [scriptblock]::Create("Install-ChocolateyEnvironmentVariable -Name 'testVariable' -Value 'TEST' -Scope $Scope -WhatIf")

            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf[0] | Should -BeExactly "What if: Performing the operation ""Set $Scope environment variable"" on target ""testVariable""." -Because $results.Output

            if ($Scope -eq 'Process') {
                $results.WhatIf | Should -HaveCount 1 -Because $results.Output
            }
            else {
                $results.WhatIf[1] | Should -BeExactly 'What if: Performing the operation "Notify system of changes" on target "Environment variables".' -Because $results.Output
                $results.WhatIf[2] | Should -BeExactly 'What if: Performing the operation "Refresh all environment variables" on target "Current process".' -Because $results.Output
                $results.WhatIf[3] | Should -BeExactly 'What if: Performing the operation "Set Process environment variable" on target "testVariable".' -Because $results.Output
            }
        }

        It 'Accepts the legacy parameter names' {
            $Command = [scriptblock]::Create("Install-ChocolateyEnvironmentVariable -VariableName 'testVariable' -VariableValue 'TEST' -VariableType $Scope -WhatIf")

            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf[0] | Should -BeExactly "What if: Performing the operation ""Set $Scope environment variable"" on target ""testVariable""." -Because $results.Output
        }
    }

    Context 'Setting and removing environment variables at the target <Scope>' -Tag VMOnly -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        BeforeAll {
            $Name = 'ChocolateyTestVariable'
            $Value = 'C:\test\path'
        }

        AfterEach {
            [Environment]::SetEnvironmentVariable($Name, $null, $Scope)
            [Environment]::SetEnvironmentVariable($Name, $null, 'Process')
        }

        It 'Sets the environment variable in the target scope' {
            Install-ChocolateyEnvironmentVariable -Name $Name -Value $Value -Scope $Scope
            [Environment]::GetEnvironmentVariable($Name, $Scope) | Should -BeExactly $Value
        }

        It 'Sets the environment variable in the current process' {
            Install-ChocolateyEnvironmentVariable -Name $Name -Value $Value -Scope $Scope
            Get-Content "Env:\$Name" | Should -BeExactly $Value
        }

        It 'Removes the environment variable when the value is <Description>' -ForEach @(
            @{ Description = 'null'; NewValue = $null }
            @{ Description = 'an empty string'; NewValue = '' }
        ) {
            Install-ChocolateyEnvironmentVariable -Name $Name -Value $Value -Scope $Scope
            Install-ChocolateyEnvironmentVariable -Name $Name -Value $NewValue -Scope $Scope

            [Environment]::GetEnvironmentVariable($Name, $Scope) | Should -BeNullOrEmpty
            "Env:\$Name" | Should -Not -Exist
        }
    }
}
