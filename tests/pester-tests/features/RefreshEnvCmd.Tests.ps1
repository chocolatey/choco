Describe "Ensuring RefreshEnv.cmd updates environment variables" -Tag EnvironmentVariables, RefreshEnv, Chocolatey {
    BeforeDiscovery {
        $testVariables = @(
            @{
                Name = "test"
                Value = "test%20value"
            }
            @{
                Name = "zsdblamp"
                Value = '"&1'
            }
            @{
                Name = "zsdblpercent"
                Value = '"%1'
            }
            @{
                Name = "zsdblpipe"
                Value = '"|1'
            }
            @{
                Name = "zsnoneamp"
                Value = "&1"
            }
            @{
                Name = "zspercent"
                Value = "%1"
            }
            @{
                Name = "zsnonepipe"
                Value = "|1"
            }
            @{
                Name = "zssingleamp"
                Value = "'&1"
            }
            @{
                Name = "zssinglepercent"
                Value = "'%1"
            }
            @{
                Name = "zssinglepipe"
                Value = "'|1"
            }
        )
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "When refreshing environment variables with special characters (<Name>=<Value>)" -ForEach $testVariables {
        BeforeAll {
            [System.Environment]::SetEnvironmentVariable($Name, $Value, 'User')

            $Output = & "cmd.exe" "/c" "`"$env:ChocolateyInstall\bin\RefreshEnv.cmd`" & set"
        }

        AfterAll {
            [System.Environment]::SetEnvironmentVariable($Name, $null, 'User')
        }

        It "Should have expected value in refreshed variable" {
            $Output | Should -Contain "$Name=$Value" -Because ($Output -join "`n")
        }
    }
}
