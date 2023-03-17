# TODO: Should we cache the result
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

    $package = (Invoke-Choco list --limitoutput).Lines |
        Where-Object { $_ -notmatch 'please upgrade' } |
        ConvertFrom-ChocolateyOutput -Command List |
        Where-Object Name -EQ $PackageName
    if (!$package) {
        return $AllowMissingPackage.IsPresent
    }

    [NuGet.Versioning.NuGetVersion]$installedVersion = $package.Version

    return Test-VersionEqualOrHigher -InstalledVersion $installedVersion -CompareVersion $Version
}
