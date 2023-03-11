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
    if (-not ${script:Chocolatey Installed Version}) {
        ${script:Chocolatey Installed Version} = ((Invoke-Choco list -lo -r).Lines | ConvertFrom-ChocolateyOutput -Command List | Where-Object Name -EQ 'chocolatey').Version
    }

    return Test-VersionEqualOrHigher -InstalledVersion ${script:Chocolatey Installed Version} -CompareVersion $Version
}
