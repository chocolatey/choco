Describe 'Ensuring that corrupted .arguments file responds correctly for <CommandDescription> <Command>' -ForEach @(
    @{
        Command = 'install'
        CommandDescription = 'default'
        ExpectedExitCode = 0
        Parameters = @('installpackage')
    }
    # It seems that upgrade and uninstall exit with -1 when the beforemodify fails.
    # As such, these tests will expect those exit codes for success.
    @{
        Command = 'upgrade'
        CommandDescription = 'default'
        ExpectedExitCode = -1
        Parameters = @('upgradepackage')
    }
    @{
        Command = 'upgrade'
        CommandDescription = 'remember'
        ExpectedExitCode = 1
        Parameters = @('upgradepackage')
    }
    @{
        Command = 'download'
        CommandDescription = 'default'
        ExpectedExitCode = 0
        Parameters = @('upgradepackage')
    }
    @{
        Command = 'list'
        CommandDescription = 'verbose'
        ExpectedExitCode = 0
        Parameters = @('--verbose')
    }
    @{
        Command = 'list'
        CommandDescription = 'default'
        ExpectedExitCode = 0
        Parameters = @()
    }
    @{
        Command = 'info'
        CommandDescription = 'verbose'
        ExpectedExitCode = 0
        Parameters = @('upgradepackage', '--local-only')
    }
    # It seems that upgrade and uninstall exit with -1 when the beforemodify fails.
    # As such, these tests will expect those exit codes for success.
    @{
        Command = 'uninstall'
        CommandDescription = 'default'
        ExpectedExitCode = -1
        Parameters = @('upgradepackage')
    }
    @{
        Command = 'pin'
        CommandDescription = 'default'
        ExpectedExitCode = 0
        Parameters = @('list')
    }
    @{
        Command = 'pin'
        CommandDescription = 'default'
        ExpectedExitCode = 0
        Parameters = @('add','-n=upgradepackage')
    }
) -Tag ArgumentsFileDecryption {
    BeforeDiscovery {
        $HasLicensedExtension = Test-PackageIsEqualOrHigher -PackageName 'chocolatey.extension' -Version '6.0.0'
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
    }

    # Skip the download command if chocolatey.extension is not installed.
    Context 'Command (<Command>) failure scenario (<ErrorType>)' -Skip:($Command -eq 'download' -and -not $HasLicensedExtension) -ForEach @(
        @{
            ErrorType = 'Base64 invalid'
            DecryptionError = 'The input is not a valid Base-64 string as it contains a non-base 64 character, more than two padding characters, or an illegal character among the padding characters.'
            # `!` is an invalid Base64 character: https://en.wikipedia.org/wiki/Base64#Base64_table_from_RFC_4648
            FileContents = 'InvalidBase64!'
        }
        @{
            ErrorType = 'Invalid decryption'
            DecryptionError = 'Key not valid for use in specified state.'
            # The contents of this was taken from a throw away VM. As such, DPAPI will not be able to decrypt it, and will error.
            FileContents = 'AQAAANCMnd8BFdERjHoAwE/Cl+sBAAAAn1/taDnOFUqGb17fBymxHQQAAAACAAAAAAAQZgAAAAEAACAAAAAU8gmqznJYKdkuj8bgk8sgg6Le3sbGoGkZOV3YtRFfwwAAAAAOgAAAAAIAACAAAAD1I9LYxrEhx9m71eF3VqyAike+XJTePhDAcrOilAFjQlAAAAA8lfiMR5Ns/AntLdVR3eBQSduCnipRCbdu/er/+YABMTzJDMGqnXuIsKwWoNIhrB14Yit4jVPipt3a/Nx18xx+YsnUewI4P6GlDL5do1y8mkAAAABMxvyPgCtN36BwAOXvJghIh9Hs8jUZOJtQIlWci8BnJkBmaaoSZ6pTGULk4TbFXMf/FK1NPo2mPM0YVL8QgJyK'
        }
    ) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            if ($CommandDescription -eq 'remember') {
                Enable-ChocolateyFeature -Name useRememberedArgumentsForUpgrades
            }

            Invoke-Choco install upgradepackage --version 1.0.0
            $argumentsFile = Join-Path $env:ChocolateyInstall ".chocolatey/upgradepackage.1.0.0/.arguments"
            $FileContents | Set-Content -Path $argumentsFile -Encoding utf8 -Force
            # Remove the `download` directory so the download command doesn't fail the second test.
            Remove-Item -Path $PWD/download -Recurse -Force -ErrorAction SilentlyContinue

            $Output = Invoke-Choco $Command @Parameters --debug
        }

        AfterAll {
            Remove-ChocolateyInstallSnapshot
        }

        It 'Exits correctly (<ExpectedExitCode>)' {
            $Output.ExitCode | Should -Be $ExpectedExitCode -Because $Output.String
        }

        It 'Outputs expected messages' {
            $shouldContain = -not ($CommandDescription -eq 'verbose' -or $CommandDescription -eq 'remember')
            $Output.Lines | Should -Not:$shouldContain -Contain "We failed to decrypt '$($env:ChocolateyInstall)\.chocolatey\upgradepackage.1.0.0\.arguments'. Error from decryption:" -Because $Output.String
            $Output.Lines | Should -Not:$shouldContain -Contain "'$DecryptionError'" -Because $Output.String
        }
    }
}
