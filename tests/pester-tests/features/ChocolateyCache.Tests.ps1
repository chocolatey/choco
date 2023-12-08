Describe 'Ensuring nupkgs cleared from temporary cache location (<Command>)' -ForEach @(
    @{ Command = 'install' }
    @{ Command = 'upgrade' }
    @{ Command = 'download' }
) -Tag ChocolateyCache {
    BeforeDiscovery {
        $HasLicensedExtension = Test-PackageIsEqualOrHigher -PackageName 'chocolatey.extension' -Version '6.0.0'
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
    }

    # Skip the download command if chocolatey.extension is not installed.
    Context 'Command (<Command>)' -Skip:($Command -eq 'download' -and -not $HasLicensedExtension) {
        BeforeAll {
            $PackageUnderTest = 'hasdependency'
            Restore-ChocolateyInstallSnapshot

            if ($Command -eq 'upgrade') {
                $prep = @(
                    Invoke-Choco install isdependency --version 1.0.0
                    Invoke-Choco install $PackageUnderTest --version 1.0.0
                )
            }

            # Clear the default Chocolatey cache directory
            $TempDir = Get-TempDirectory
            Remove-Item -Path $TempDir/chocolatey -Recurse -Force -ErrorAction SilentlyContinue
            $Output = Invoke-Choco $Command $PackageUnderTest
        }

        AfterAll {
            Remove-ChocolateyInstallSnapshot
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Did not retain a nupkg in the Chocolatey cache' {
            Get-ChildItem -Filter *.nupkg -Path $TempDir/chocolatey -Recurse | Should -BeNullOrEmpty -Because $Output.String
        }

        # Skipping Download command because it downloads direct without storing files in the cache.
        It 'Did retain a nuspec in the Chocolatey cache' -Skip:($Command -eq 'download') {
            Get-ChildItem -Filter "$PackageUnderTest.nuspec" -Path $TempDir/chocolatey -Recurse | Should -Not -BeNullOrEmpty -Because $Output.String
        }
    }
}
