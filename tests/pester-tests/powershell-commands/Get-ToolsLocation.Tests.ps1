Describe 'Get-ToolsLocation helper function tests' -Tags Cmdlets, GetToolsLocation {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        $systemDrive = $env:SystemDrive

        # The command reads and writes these in the process and in the user's environment, so what the machine running the tests
        # has is put aside, and put back afterwards.
        $variableNames = 'ChocolateyToolsLocation', 'ChocolateyBinRoot', 'chocolatey_bin_root'
        $originalValues = @{}
        foreach ($name in $variableNames) {
            $originalValues[$name] = @{
                User    = [Environment]::GetEnvironmentVariable($name, 'User')
                Process = [Environment]::GetEnvironmentVariable($name, 'Process')
            }
        }

        function Set-TestEnvironment {
            # Clears the three variables, then sets the ones given. A variable is set in the user's environment and the process, as
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

    Context 'When ChocolateyToolsLocation is set along with the older variables' {
        BeforeAll {
            Set-TestEnvironment @{
                ChocolateyToolsLocation = 'D:\tools'
                ChocolateyBinRoot       = 'E:\bin'
                chocolatey_bin_root     = 'F:\old'
            }

            $result = Get-ToolsLocation
        }

        It 'Returns the tools location' {
            $result | Should -BeExactly 'D:\tools'
        }

        It 'Leaves the older variables as they are' {
            Get-UserVariable 'ChocolateyBinRoot' | Should -BeExactly 'E:\bin'
            Get-UserVariable 'chocolatey_bin_root' | Should -BeExactly 'F:\old'
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

    Context 'When only the older ChocolateyBinRoot is set' {
        BeforeAll {
            Set-TestEnvironment @{ ChocolateyBinRoot = 'E:\bin' }

            $result = Get-ToolsLocation
        }

        It 'Returns the value of ChocolateyBinRoot' {
            $result | Should -BeExactly 'E:\bin'
        }

        It 'Saves it as the tools location' {
            Get-UserVariable 'ChocolateyToolsLocation' | Should -BeExactly 'E:\bin'
        }

        It 'Removes ChocolateyBinRoot from the user environment' {
            Get-UserVariable 'ChocolateyBinRoot' | Should -BeNullOrEmpty
        }
    }

    Context 'When the older ChocolateyBinRoot has no drive' {
        BeforeAll {
            Set-TestEnvironment @{ ChocolateyBinRoot = 'oldbin' }

            $result = Get-ToolsLocation
        }

        It 'Puts the location on the system drive' {
            $result | Should -BeExactly "$systemDrive\oldbin"
        }

        It 'Saves the location with the drive as the tools location' {
            Get-UserVariable 'ChocolateyToolsLocation' | Should -BeExactly "$systemDrive\oldbin"
        }
    }

    Context 'When only the oldest chocolatey_bin_root is set' {
        BeforeAll {
            Set-TestEnvironment @{ chocolatey_bin_root = 'F:\old' }

            $result = Get-ToolsLocation
        }

        It 'Returns the value of chocolatey_bin_root' {
            $result | Should -BeExactly 'F:\old'
        }

        It 'Saves it as the tools location' {
            Get-UserVariable 'ChocolateyToolsLocation' | Should -BeExactly 'F:\old'
        }

        It 'Removes both older variables from the user environment' {
            Get-UserVariable 'chocolatey_bin_root' | Should -BeNullOrEmpty
            Get-UserVariable 'ChocolateyBinRoot' | Should -BeNullOrEmpty
        }
    }

    Context 'When both older variables are set' {
        BeforeAll {
            Set-TestEnvironment @{
                ChocolateyBinRoot   = 'E:\bin'
                chocolatey_bin_root = 'F:\old'
            }

            $result = Get-ToolsLocation
        }

        It 'Returns the value of ChocolateyBinRoot' {
            $result | Should -BeExactly 'E:\bin'
        }

        It 'Saves it as the tools location' {
            Get-UserVariable 'ChocolateyToolsLocation' | Should -BeExactly 'E:\bin'
        }

        It 'Removes both older variables from the user environment' {
            Get-UserVariable 'chocolatey_bin_root' | Should -BeNullOrEmpty
            Get-UserVariable 'ChocolateyBinRoot' | Should -BeNullOrEmpty
        }
    }

    Context 'When it is called again' {
        BeforeAll {
            Set-TestEnvironment @{ ChocolateyBinRoot = 'E:\bin' }

            $first = Get-ToolsLocation
            $second = Get-ToolsLocation
        }

        It 'Returns the same location' {
            $first | Should -BeExactly 'E:\bin'
            $second | Should -BeExactly 'E:\bin'
        }
    }
}
