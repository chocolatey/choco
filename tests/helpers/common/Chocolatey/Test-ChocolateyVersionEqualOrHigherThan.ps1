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
        [NuGet.Versioning.SemanticVersion]$Version
    )
    $installedVersion = ((Invoke-Choco list -lo -r).Lines | ConvertFrom-ChocolateyOutput -Command List | Where-Object Name -eq 'chocolatey').Version

    return Test-VersionEqualOrHigher -InstalledVersion $installedVersion -CompareVersion $Version
}
