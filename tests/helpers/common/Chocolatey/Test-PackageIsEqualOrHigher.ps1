function Test-PackageIsEqualOrHigher {
    [CmdletBinding()]
    [OutputType([boolean])]
    param(
        [Parameter(Mandatory)]
        [string]$PackageName,
        [Parameter(Mandatory)]
        [NuGet.Versioning.NuGetVersion]$Version,
        [switch]$AllowMissingPackage
    )
    if (-not $script:ChocolateyInstalledPackages) {
        $script:ChocolateyInstalledPackages = (Invoke-Choco list --limitoutput).Lines |
        Where-Object { $_ -notmatch 'please upgrade' } |
        ConvertFrom-ChocolateyOutput -Command List
    }

    $package = $script:ChocolateyInstalledPackages | Where-Object Name -EQ $PackageName

    if (-not $package) {
        return $AllowMissingPackage.IsPresent
    }

    [NuGet.Versioning.NuGetVersion]$installedVersion = $package.Version

    return Test-VersionEqualOrHigher -InstalledVersion $installedVersion -CompareVersion $Version
}
