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
        [NuGet.Versioning.NuGetVersion]$script:runningVersion = ((Invoke-Choco --version).Lines | Where-Object { $_ -NotMatch "please upgrade" }) -join '`r`n'
    }

    $script:runningVersion
}
