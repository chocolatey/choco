function New-ChocolateyInstallSnapshot {
    <#
        .Synopsis
            Creates a new temporary snapshot
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

    if (-not (Test-Path $SnapshotPath)) {
        Write-Verbose "Creating sub directories in $SnapshotPath"
        if (!($NoSnapshotCopy)) {
            $null = New-Item -Path "$SnapshotPath\install" -Force -ItemType Directory
        }
        $null = New-Item -Path "$SnapshotPath\packages" -Force -ItemType Directory
    }

    Write-Verbose 'Adding information to queue'
    $null = $script:snapshots.Enqueue(@{
            Path                = $SnapshotPath
            PreviousPath        = $env:ChocolateyInstall
            PathVariable        = $env:PATH
            SetWorkingDirectory = $SetWorkingDirectory
            NoSnapshotCopy      = $NoSnapshotCopy
        })

    if (!($NoSnapshotCopy)) {
        $chocolateyTestLocation = Get-ChocolateyTestLocation
        Write-Verbose "Copying files from '$chocolateyTestLocation' to '$SnapshotPath\install"
        $null = robocopy $chocolateyTestLocation "$SnapshotPath\install" /MIR
        $null = Remove-Item -Path "$SnapshotPath\install\logs" -Recurse -Force -ErrorAction SilentlyContinue

        $env:ChocolateyInstall = "$SnapshotPath\install"
        $env:PATH = "$SnapshotPath\install\bin;$env:PATH"
    }
    else {
        $chocolateyTestLocation = Get-ChocolateyTestLocation
        Write-Verbose "Resetting chocolatey install path to '$chocolateyTestlocation"
        $env:ChocolateyInstall = "$chocolateyTestLocation"
        $env:PATH = "$chocolateyTestLocation\bin;$env:PATH"
    }

    $env:CHOCOLATEY_TEST_PACKAGES_PATH = "$SnapshotPath\packages"

    if ($SetWorkingDirectory) {
        $null = Push-Location $env:CHOCOLATEY_TEST_PACKAGES_PATH -StackName 'snapshots'
    }

    [PSCustomObject]@{
        InstallPath  = $env:ChocolateyInstall
        PackagesPath = $env:CHOCOLATEY_TEST_PACKAGES_PATH
    }
}
