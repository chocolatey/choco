function Restore-ChocolateyInstallSnapshot {
    <#
        .Synopsis
            Removes an existing snapshot, and creates a completely new one.
    #>
    [CmdletBinding()]
    param(
        # The location to create the temporary installation
        [string]$SnapshotPath = "$(Get-TempDirectory)ChocolateyTests\snapshots\$(New-Guid)",
        # Whether the current work directory should be moved to the package
        # path
        [Alias('SetWorkDir')]
        [switch]$SetWorkingDirectory,

        # Whether we should do a quick creation, basically only create a new
        # package snapshot directory but without copying over any files from
        # the base test installation.
        # This can be used when we do not need to change any state, like purely
        # calling the help command without changing any configurations.
        [Alias('Quick')]
        [switch]$NoSnapshotCopy
    )

    Remove-ChocolateyInstallSnapshot
    return New-ChocolateyInstallSnapshot -SnapshotPath $SnapshotPath -SetWorkingDirectory:$SetWorkingDirectory -NoSnapshotCopy:$NoSnapshotCopy
}
