Describe 'Ensuring credentials do not bleed from configured sources' -Tag CredentialProvider -ForEach @(
    @{
        Command = 'info'
        ExitCode = 0
}
    @{
        Command = 'install'
        ExitCode = 1
}
    @{
        Command = 'outdated'
        ExitCode = 0
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
