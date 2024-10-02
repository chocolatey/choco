Describe 'Get-EnvironmentVariable helper function tests' -Tags Cmdlets, GetEnvironmentVariable {
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

    Context 'Can retrieve the PATH variable without expanding environment names for the Machine scope' {
        BeforeAll {
            Install-ChocolateyPath -Path "%systemroot%\test" -PathType Machine
        }

        AfterAll {
            Uninstall-ChocolateyPath -Path "%systemroot%\test" -PathType Machine
        }

        It 'Retrieves the Machine PATH value with un-expanded environment names' {
            # We expect there to be an entry similar to the following: "%SystemRoot%\system32", since this
            # is there by default in a Windows install
            Get-EnvironmentVariable -Name 'PATH' -Scope 'Machine' -PreserveVariables | Should -Match '%systemroot%\\test'
        }
    }
}