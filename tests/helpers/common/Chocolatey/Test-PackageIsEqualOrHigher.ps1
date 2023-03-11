function Test-PackageIsEqualOrHigher {
    [CmdletBinding()]
    [OutputType([boolean])]
    param(
        [Parameter(Mandatory)]
        [string]$PackageName,
        [Parameter(Mandatory)]
        [NuGet.Versioning.SemanticVersion]$Version,
        [switch]$AllowMissingPackage
    )
    if (-not ${script:Chocolatey Installed Packages}) {
        ${script:Chocolatey Installed Packages} = (Invoke-Choco list --local-only --limitoutput).Lines |
        Where-Object { $_ -notmatch 'please upgrade' } |
        ConvertFrom-ChocolateyOutput -Command List
    }

    $package = ${script:Chocolatey Installed Packages} | Where-Object Name -EQ $PackageName

    if (-not $package) {
        return $AllowMissingPackage.IsPresent
    }

    [NuGet.Versioning.SemanticVersion]$installedVersion = $package.Version

    return Test-VersionEqualOrHigher -InstalledVersion $installedVersion -CompareVersion $Version
}
