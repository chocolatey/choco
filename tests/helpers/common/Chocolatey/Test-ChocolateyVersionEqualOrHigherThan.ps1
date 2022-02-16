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
    $installedVersion = Get-ChocolateyVersion

    return Test-VersionEqualOrHigher -InstalledVersion $installedVersion -CompareVersion $Version
}
