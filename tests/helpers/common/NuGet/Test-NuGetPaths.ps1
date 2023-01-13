function Test-NuGetPaths {
    $script:NuGetCleared = $false
    $NuGetPathsToCheck = Get-NuGetPaths
    $ChocolateyNuGetPath = "$(Get-TempDirectory)chocolatey-invalid"

    It 'Did not create <_> directory' -ForEach $NuGetPathsToCheck -Skip:((-not $env:TEST_KITCHEN)) {
        if (-not $script:NuGetCleared) {
            Set-ItResult -Skipped -Because 'NuGet configurations were not removed before running tests'
        }
        $_ | Should -Not -Exist
    }

    It "Did not create <_>" -ForEach $ChocolateyNuGetPath {
        $_ | Should -Not -Exist
    }
}
