Describe 'Update-SessionEnvironment helper function tests' -Tag UpdateSessionEnvironment, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }
    
    Context 'Unit tests' -Tag WhatIf {
        It 'refreshes the current session environment variables' {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'")
            $Command = [scriptblock]::Create("Update-SessionEnvironment -WhatIf")
            
            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results | Should -BeExactly 'What if: Performing the operation "refresh all environment variables" on target "current process".'
        }
    }

    Context 'Refreshing environment' {
        BeforeAll {
            [Environment]::SetEnvironmentVariable("Test", "user-value", "User")
            [Environment]::SetEnvironmentVariable("Test2", "machine-value", "Machine")
            $env:Test3 = "process-value"
        }

        It 'successfully refreshes values into the process' {
            Update-SessionEnvironment
            $env:Test | Should -BeExactly 'user-value'
            $env:Test2 | Should -BeExactly 'machine-value'
            $env:Test3 | Should -BeExactly 'process-value'
        }

        AfterAll {
            [Environment]::SetEnvironmentVariable("Test", [string]::Empty, "User")
            [Environment]::SetEnvironmentVariable("Test2", [string]::Empty, "Machine")
            $env:Test3 = ''
        }
    }
}