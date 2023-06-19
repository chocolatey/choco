function Test-ChocolateyVersionEqualOrHigherThan {
    <#
        .Synopsis
            Helper function that can be used to assert whether the current
            Chocolatey version is equal to or higher than a certain threshold.
    #>
    [Alias('Test-ChocoVersionEqualOrHigherThan')]
    [CmdletBinding()]
    [OutputType([boolean])]
    param(
        [NuGet.Versioning.NuGetVersion]$Version
    )
    if (-not $script:ChocolateyInstalledVersion) {
        $script:ChocolateyInstalledVersion = ((Invoke-Choco list -r).Lines | ConvertFrom-ChocolateyOutput -Command List | Where-Object Name -EQ 'chocolatey').Version
    }

    return Test-VersionEqualOrHigher -InstalledVersion $script:ChocolateyInstalledVersion -CompareVersion $Version
}
