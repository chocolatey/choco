function Test-HasNuGetV3Source {
    [CmdletBinding()]
    [OutputType([boolean])]
    param(
    )
    if (-not $script:ChocolateyEnabledSources) {
        $script:ChocolateyEnabledSources = (Invoke-Choco source list --limitoutput).Lines | ConvertFrom-ChocolateyOutput -Command SourceList | Where-Object Disabled -NE $true
    }
    $null -ne ($script:ChocolateyEnabledSources | Where-Object Url -match 'index\.json')
}
