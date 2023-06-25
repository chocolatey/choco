Describe "Ensuring Chocolatey Environment variables are correct (<_>)" -ForEach @(
    "config"
    "cli"
) -Tag EnvironmentVariables, Chocolatey {
    BeforeDiscovery {
        $TestedVariables = @(
            @{ Name = 'TEMP' ; Value = "C:\Temp\$PID" }
            @{ Name = 'TMP' ; Value = "C:\Temp\$PID" }
            @{ Name = 'ChocolateyPackageFolder' ; Value = '{0}\lib\test-environment' }
            @{ Name = 'ChocolateyPackageName' ; Value = 'test-environment' }
            @{ Name = 'ChocolateyPackageTitle' ; Value = 'test-environment (Install)' }
            @{ Name = 'ChocolateyPackageVersion' ; Value = '1.0.0' }
        )
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
        $cacheDir = "C:\Temp\$PID"
        switch ($_) {
            'config' {
                Invoke-Choco config set --name=cachelocation --value $cacheDir
            }
            'cli' {
                $cacheArg = "--cache-location='$cacheDir'"
            }
        }
        $Output = Invoke-Choco install test-environment --version 1.0.0 $cacheArg
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    It "Should exit with success (0)" {
        $Output.ExitCode | Should -Be 0 -Because $Output.String
    }

    It 'Should Output the expected value for <Name> environment variable' -ForEach $TestedVariables {
        $ExpectedLine = "$Name=$Value" -f $env:ChocolateyInstall
        $Output.Lines | Should -Contain $ExpectedLine -Because $Output.String
    }
}
