function Get-ChocolateyVersion {
    <#
        .Synopsis
            Returns the current Chocolatey version as SemVer
    #>
    [Alias('Get-ChocoVersion')]
    [OutputType('NuGet.Versioning.NuGetVersion')]
    [CmdletBinding()]
    param()
    if (-not $script:runningVersion) {
        [NuGet.Versioning.NuGetVersion]$script:runningVersion = (Invoke-Choco --version).Lines | Where-Object { $_ -Match "^\d+\.[\d\.]+" } | Select-Object -First 1
    }

    $script:runningVersion
}
