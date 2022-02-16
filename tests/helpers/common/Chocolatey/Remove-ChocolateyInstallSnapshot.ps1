function Remove-ChocolateyInstallSnapshot {
    <#
        .Synopsis
            Removes an existing snapshot
    #>
    [CmdletBinding()]
    param(
        # Whether to remove all stored snapshots
        [switch] $RemoveAll
    )

    if ($script:snapshots.Count -eq 0) {
        return
    }

    $snapshotPath = $script:snapshots.Dequeue()

    if ($snapshotPath) {

        if ($snapshotPath.SetWorkingDirectory) {
            # We ignore any errors that may occur when popping the location,
            # this is done so we do not have failures during teardown as
            # there are cases where the previous directory won't exist.
            $null = Pop-Location -StackName 'snapshots' -ErrorAction SilentlyContinue
        }

        if (Test-Path $snapshotPath.Path) {
            $null = robocopy "$($snapshotPath.Path)/install/logs" "$env:ChocolateyInstall\snapshot\$(Get-Date -Format 'yyyMMdd-HHmmss')" /MIR
            # We ignore any errors that may occur on this.
            # There are some rare cases where removal fails, and we do not
            # want anything to hang or be outputted to the console when the
            # removal fails (removal of very long paths seem to fail).
            $null = Remove-Item $snapshotPath.Path -Force -Recurse -ErrorAction SilentlyContinue
        }

        $env:CHOCOLATEY_TEST_PACKAGES_PATH = "$($snapshotPath.PreviousPath)\packages"
        $env:PATH = $snapshotPath.PathVariable

        # We can rely on this simple test, as we do not create the install
        # directory during quick snapshot creation
        if (Test-Path "$($snapshotPath.PreviousPath)\install") {
            $env:ChocolateyInstall = "$($snapshotPath.PreviousPath)\install"
        }
        else {
            $env:ChocolateyInstall = Get-ChocolateyTestLocation
        }

        $escapedPath = [regex]::Escape("$env:ChocolateyInstall\bin")
        if ($env:PATH -notmatch $escapedPath) {
            $env:PATH = "$env:ChocolateyInstall\bin;$env:PATH"
        }
    }

    if ($RemoveAll.IsPresent -and ($script:snapshots.Count -gt 0)) {
        Remove-ChocolateyInstallSnapshot -RemoveAll
    }
}
