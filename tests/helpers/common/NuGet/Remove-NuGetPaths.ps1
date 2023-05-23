function Remove-NuGetPaths {
    $NuGetPathsToRemove = Get-NuGetPaths
    $ChocolateyNuGetPath = "$(Get-TempDirectory)\chocolatey-invalid"

    if (Test-Path $ChocolateyNuGetPath) {
        Remove-Item -Path $ChocolateyNuGetPath -Recurse -Force -ErrorAction Stop
    }

    $script:NuGetCleared = $true

    # If we're in Test Kitchen, we're going to remove the NuGet config files because we don't need to worry about a user's config
    if ($env:TEST_KITCHEN) {
        foreach ($path in $NuGetPathsToRemove) {
            if (Test-Path $path) {
                Remove-Item -Path $path -Recurse -Force -ErrorAction Stop
            }
        }
    }
}
