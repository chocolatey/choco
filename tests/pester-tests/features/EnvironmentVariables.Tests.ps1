# This test is covering a number of different things
# 1. Verify that the TEMP/TMP variables are set with the cachelocation value when
# it is set, either in the chocolatey.config file or via command line
# 2. Verify that some of the expected environment variables are present during a
# package operation, i.e. ChocolateyPackageName
# 3. Verify that the ChocolateyPreviousPackageVersion environment variable is set
# during an upgrade operation. We are testing here for the normalized package
# version, even though the installed package version is not normalized.
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
            @{ Name = 'ChocolateyPreviousPackageVersion' ; Value = '0.9.0' }
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
        $null = Invoke-Choco install test-environment --version 0.9 $cacheArg
        $Output = Invoke-Choco upgrade test-environment --version 1.0.0 $cacheArg
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
