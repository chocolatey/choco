Describe 'Uninstall-ChocolateyEnvironmentVariable helper function tests' -Tags UninstallChocolateyEnvironmentVariable, Cmdlets {
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

        It 'Removes an environment variable at the target <Scope>' {
            $Command = [scriptblock]::Create("Uninstall-ChocolateyEnvironmentVariable -Name 'testVariable' -Scope $Scope -WhatIf")

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
            $Command = [scriptblock]::Create("Uninstall-ChocolateyEnvironmentVariable -VariableName 'testVariable' -VariableType $Scope -WhatIf")

            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf[0] | Should -BeExactly "What if: Performing the operation ""Set $Scope environment variable"" on target ""testVariable""." -Because $results.Output
        }
    }

    Context 'Removing environment variables at the target <Scope>' -Tag VMOnly -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        BeforeAll {
            $Name = 'ChocolateyTestVariable'
        }

        BeforeEach {
            [Environment]::SetEnvironmentVariable($Name, 'C:\test\path', $Scope)
            [Environment]::SetEnvironmentVariable($Name, 'C:\test\path', 'Process')
        }

        AfterEach {
            [Environment]::SetEnvironmentVariable($Name, $null, $Scope)
            [Environment]::SetEnvironmentVariable($Name, $null, 'Process')
        }

        It 'Removes the environment variable from the target scope' {
            Uninstall-ChocolateyEnvironmentVariable -Name $Name -Scope $Scope
            [Environment]::GetEnvironmentVariable($Name, $Scope) | Should -BeNullOrEmpty
        }

        It 'Removes the environment variable from the current process' {
            Uninstall-ChocolateyEnvironmentVariable -Name $Name -Scope $Scope
            "Env:\$Name" | Should -Not -Exist
        }

        It 'Does not fail when the environment variable does not exist' {
            Uninstall-ChocolateyEnvironmentVariable -Name $Name -Scope $Scope
            { Uninstall-ChocolateyEnvironmentVariable -Name $Name -Scope $Scope } | Should -Not -Throw
        }
    }
}
