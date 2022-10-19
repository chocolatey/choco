#requires -version 3

if (-not ("NuGet.Versioning.VersionRange" -as [Type])) {
    Add-Type -Path $PSScriptRoot\common\NuGet.Versioning.dll
}

# Common variables setup
$script:snapshots = [System.Collections.Generic.Queue[hashtable]]::new()
$script:snapshotStack = @()
$script:ChocoCommandHeaders = @{
    "List"       = @("Name", "Version")
    "PinList"    = @("Name", "Version")
    "SourceList" = @("Name", "Url", "1", "2", "3", "Priority", "BypassProxy", "SelfService", "AdminOnly")
    "Feature"    = @("Name", "State", "Description")
}
$script:features = $null
$script:LicenseType = $null
$script:chocolateyTestLocation = $null
$script:originalChocolateyInstall = $env:ChocolateyInstall

Get-ChildItem -Path $PSScriptRoot\common -Filter *.ps1 -Recurse | ForEach-Object { . $_.FullName }

# Prepare information that will be useful for troubleshooting.
$Output = Invoke-Choco list -lo
# Saving to log file as those are currently the only files picked up by Team City
$Output.Lines | Out-File $env:ChocolateyInstall\Logs\LocalPackages.log
# Removing any existing snapshot logs to enable local testing without needing to clean the snapshot folder
Remove-Item -Path $env:ChocolateyInstall\snapshot -Force -Recurse -ErrorAction Ignore
