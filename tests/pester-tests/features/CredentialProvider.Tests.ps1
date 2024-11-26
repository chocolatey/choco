# These tests are to ensure that credentials from one configured and enabled source are not
# picked up and used when a URL is matching based on the hostname. These tests use an authenticated
# source without explicitly providing a username/password. It is expected that Chocolatey will prompt for
# the username and password.
Describe 'Ensuring credentials do not bleed from configured sources' -Tag CredentialProvider -ForEach @(
    # Info and outdated are returning 0 in all test cases we've thrown at them.
    # Suspect the only way either of these commands actually return non-zero is in a scenario where
    # something goes catastrophically wrong outside of the actual command calls.
    @{
        Command = 'info'
        ExitCode = 0
    }
    @{
        Command = 'outdated'
        ExitCode = 0
    }
    @{
        Command = 'install'
        ExitCode = 1
    }
    @{
        Command = 'search'
        ExitCode = 1
    }
    @{
        Command = 'upgrade'
        ExitCode = 1
    }
    @{
        Command = 'download'
        ExitCode = 1
    }
) {
    BeforeDiscovery {
        $HasLicensedExtension = Test-PackageIsEqualOrHigher -PackageName 'chocolatey.extension' -Version '6.0.0'
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
        Disable-ChocolateySource -All
        Enable-ChocolateySource -Name 'hermes'
        $SetupSource = Get-ChocolateySource -Name 'hermes-setup'
        Remove-Item download -force -recurse
    }

    # Skip the download command if chocolatey.extension is not installed.
    Context 'Command (<Command>)' -Skip:($Command -eq 'download' -and -not $HasLicensedExtension) {
        BeforeAll {
            # The package used ultimately doesn't matter as we don't expect to find it.
            # Picking a package that should be found if the behaviour changes.
            $PackageUnderTest = 'chocolatey-compatibility.extension'
            Restore-ChocolateyInstallSnapshot
            # Chocolatey will prompt for credentials, we need to force something in there, and this will do that.
            $Output = 'n' | Invoke-Choco $Command $PackageUnderTest --confirm --source="'$($SetupSource.Url)'"
        }

        AfterAll {
            Remove-ChocolateyInstallSnapshot
        }

        It 'Exits Correctly (<ExitCode>)' {
            $Output.ExitCode | Should -Be $ExitCode -Because $Output.String
        }

        It 'Outputs error message' {
            $FilteredOutput = $Output.Lines -match "Failed to fetch results from V2 feed at '$($SetupSource.Url.Trim('/'))"
            $FilteredOutput.Count | Should -BeGreaterOrEqual 1 -Because $Output.String
        }
    }
}
