Describe 'Get-ToolsLocation helper function tests' -Tags Cmdlets, GetToolsLocation {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        $systemDrive = $env:SystemDrive

        # The command reads and writes this in the process and in the user's environment, so what the machine running the tests
        # has is put aside, and put back afterwards.
        $variableNames = 'ChocolateyToolsLocation'
        $originalValues = @{}
        foreach ($name in $variableNames) {
            $originalValues[$name] = @{
                User    = [Environment]::GetEnvironmentVariable($name, 'User')
                Process = [Environment]::GetEnvironmentVariable($name, 'Process')
            }
        }

        function Set-TestEnvironment {
            # Clears the variable, then sets it if given. A variable is set in the user's environment and the process, as
            # it would be after a session starts with it set.
            param(
                [hashtable]$Variables = @{}
            )

            foreach ($name in $variableNames) {
                [Environment]::SetEnvironmentVariable($name, [string]::Empty, 'User')
                Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
            }

            foreach ($name in $Variables.Keys) {
                [Environment]::SetEnvironmentVariable($name, $Variables[$name], 'User')
                Set-Item "Env:\$name" $Variables[$name]
            }
        }

        function Get-UserVariable {
            param([string]$Name)

            [Environment]::GetEnvironmentVariable($Name, 'User')
        }

        function Get-DebugMessages {
            param([scriptblock]$Script)

            & {
                $DebugPreference = 'Continue'
                & $Script
            } 5>&1 |
                Where-Object { $_ -is [System.Management.Automation.DebugRecord] } |
                ForEach-Object { $_.Message }
        }
    }

    AfterAll {
        foreach ($name in $variableNames) {
            [Environment]::SetEnvironmentVariable($name, $originalValues[$name].User, 'User')

            if ($null -eq $originalValues[$name].Process) {
                Remove-Item "Env:\$name" -ErrorAction SilentlyContinue
            }
            else {
                Set-Item "Env:\$name" $originalValues[$name].Process
            }
        }
    }

    Context 'Command' {
        BeforeAll {
            # On the system drive, so the location is on a drive that exists wherever the tests run.
            Set-TestEnvironment @{ ChocolateyToolsLocation = "$systemDrive\tools" }
        }

        It 'Is a cmdlet' {
            (Get-Command Get-ToolsLocation).CommandType | Should -Be 'Cmdlet'
        }

        It 'Writes the location as a single string' {
            $result = @(Get-ToolsLocation)

            $result.Count | Should -Be 1
            $result[0] | Should -BeOfType [string]
            $result[0] | Should -BeExactly "$systemDrive\tools"
        }

        It 'Can be piped into another command' {
            Get-ToolsLocation | Join-Path -ChildPath 'package' | Should -BeExactly "$systemDrive\tools\package"
        }

        It 'Accepts arguments that do not apply' {
            { Get-ToolsLocation 'ignored' -Unrelated 'value' -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Writes the call to the debug stream' {
            $messages = Get-DebugMessages { $null = Get-ToolsLocation }

            $messages | Should -Contain 'Running Get-ToolsLocation'
        }
    }

    Context 'When ChocolateyToolsLocation is set to a path with a drive' {
        BeforeAll {
            Set-TestEnvironment @{ ChocolateyToolsLocation = 'D:\tools' }

            $result = Get-ToolsLocation
        }

        It 'Returns that location' {
            $result | Should -BeExactly 'D:\tools'
        }

        It 'Does not change the user environment' {
            Get-UserVariable 'ChocolateyToolsLocation' | Should -BeExactly 'D:\tools'
        }
    }

    Context 'When ChocolateyToolsLocation is set to a path without a drive' {
        BeforeAll {
            Set-TestEnvironment @{ ChocolateyToolsLocation = 'sometools' }

            $result = Get-ToolsLocation
        }

        It 'Puts the location on the system drive' {
            $result | Should -BeExactly "$systemDrive\sometools"
        }

        It 'Saves the location with the drive to the user environment' {
            Get-UserVariable 'ChocolateyToolsLocation' | Should -BeExactly "$systemDrive\sometools"
        }

        It 'Updates the current process' {
            $env:ChocolateyToolsLocation | Should -BeExactly "$systemDrive\sometools"
        }
    }

    Context 'When nothing is set' {
        BeforeAll {
            Set-TestEnvironment

            $result = Get-ToolsLocation
        }

        It 'Returns the tools folder of the system drive' {
            $result | Should -BeExactly "$systemDrive\tools"
        }

        It 'Saves the location to the user environment' {
            Get-UserVariable 'ChocolateyToolsLocation' | Should -BeExactly "$systemDrive\tools"
        }

        It 'Updates the current process' {
            $env:ChocolateyToolsLocation | Should -BeExactly "$systemDrive\tools"
        }
    }

    Context 'When it is called again' {
        BeforeAll {
            Set-TestEnvironment

            $first = Get-ToolsLocation
            $second = Get-ToolsLocation
        }

        It 'Returns the same location' {
            $first | Should -BeExactly "$systemDrive\tools"
            $second | Should -BeExactly "$systemDrive\tools"
        }
    }
}
