function Get-ChocolateyVersion {
    <#
        .Synopsis
            Returns the current Chocolatey version as SemVer
    #>
    [Alias('Get-ChocoVersion')]
    [OutputType('NuGet.Versioning.SemanticVersion')]
    [CmdletBinding()]
    param()
    if (-not $script:runningVersion) {
        [NuGet.Versioning.SemanticVersion]$script:runningVersion = ((Invoke-Choco --version).Lines | Where-Object { $_ -NotMatch "please upgrade" }) -join '`r`n'
    }

    $script:runningVersion
}
