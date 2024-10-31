Describe 'Update-SessionEnvironment helper function tests' -Tag Cmdlets, UpdateSessionEnvironment {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
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