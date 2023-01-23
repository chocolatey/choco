function Test-NuGetPaths {
    Context 'NuGet directory tests' {
        BeforeDiscovery {
            $NuGetPathsToCheck = Get-NuGetPaths
            $ChocolateyNuGetPath = "$(Get-TempDirectory)chocolatey-invalid"
        }

        AfterAll {
            $script:NuGetCleared = $null
        }

        It 'Did not create <_> directory' -ForEach $NuGetPathsToCheck -Skip:((-not $env:TEST_KITCHEN)) {
            $script:NuGetCleared | Should -BeTrue -Because 'NuGet configurations were not removed before running tests'
            $_ | Should -Not -Exist
        }

        It "Did not create <_>" -ForEach $ChocolateyNuGetPath {
            $_ | Should -Not -Exist
        }
    }
}
