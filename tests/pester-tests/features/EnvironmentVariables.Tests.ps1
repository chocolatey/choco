# This test is covering a number of different things
# 1. Verify that the TEMP/TMP variables are set with the cachelocation value when
# it is set, either in the chocolatey.config file or via command line
# 2. Verify that some of the expected environment variables are present during a
# package operation, i.e. ChocolateyPackageName
# 3. Verify that the ChocolateyPreviousPackageVersion environment variable is set
# during an upgrade operation. We are testing here for the normalized package
# version, even though the installed package version is not normalized.
# 4. Verifies the usage of the ChocolateyNotSilent environment variable by using
# the --not-silent command line argument
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
            @{ Name = 'ChocolateyPackageId' ; Value = 'test-environment' }
            @{ Name = 'ChocolateyPackageTitle' ; Value = 'test-environment (Install)' }
            @{ Name = 'ChocolateyPackageVersion' ; Value = '1.0.0' }
            @{ Name = 'ChocolateyPreviousPackageVersion' ; Value = '0.9.0' }
            @{ Name = 'ChocolateyNotSilent' ; Value = 'true' }
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
        $null = Invoke-Choco install test-environment --version 0.9 $cacheArg --not-silent
        $Output = Invoke-Choco upgrade test-environment --version 1.0.0 $cacheArg --not-silent
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

# Some environment variables should _only_ be present when the corresponding
# configuration has been set. An example of that would be the
# $env:ChocolateyNotSilent variable, which should only be present when the
# --not-silent command line argument is passed in.  When this doesn't happen,
# the environment variable should not be included in the output.
Describe "Ensuring Chocolatey Environment variables are not present (<_>)" -Tag EnvironmentVariables, Chocolatey {
    BeforeDiscovery {
        $TestedVariables = @(
            @{ Name = 'ChocolateyNotSilent' ; Value = 'true' }
        )
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
        $Output = Invoke-Choco install test-environment
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    It "Should exit with success (0)" {
        $Output.ExitCode | Should -Be 0 -Because $Output.String
    }

    It 'Should Output the expected value for <Name> environment variable' -ForEach $TestedVariables {
        $ExpectedLine = "$Name=$Value"
        $Output.Lines | Should -Not -Contain $ExpectedLine -Because $Output.String
    }
}

Describe "Ensuring variables are not incorrectly set for the SYSTEM account" -Tag EnvironmentVariables, Chocolatey, SystemAccount {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
        Invoke-Choco install psexec -s https://community.chocolatey.org/api/v2/

        # We execute this as an encoded command as it doesn't seem to like the strings unless it's encoded.
        $encodedCommand = @'
            "Temp: $($env:TEMP)" > {1}/before.txt
            "Tmp: $($env:TMP)" >> {1}/before.txt
            Import-Module {0}/helpers/chocolateyProfile.psm1 -Verbose *>&1
            refreshenv
            "Temp: $($env:TEMP)" > {1}/after.txt
            "Tmp: $($env:TMP)" >> {1}/after.txt
'@ -f (Get-ChocolateyTestLocation), $PSScriptRoot | ConvertTo-Base64String
        $Output = psexec /accepteula /s powershell.exe -NoProfile -EncodedCommand $encodedCommand 2>$null
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    It 'Should not change the temp environment variables' {
        $Before = Get-Content $PSScriptRoot/before.txt
        $After = Get-Content $PSScriptRoot/after.txt
        $Before | Should -Be $After -Because $Output
    }
}
