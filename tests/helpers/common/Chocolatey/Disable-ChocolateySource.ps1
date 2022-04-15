# TODO: Refactor the *-ChocolateySource functions to have a Get-ChocolateySource that can be piped to the Enable and Disable
function Disable-ChocolateySource {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Name = "*",

        [Parameter()]
        [switch]$All
    )
    # Significantly weird behaviour with piping this source list by property name.
    $CurrentSources = (Invoke-Choco source list -r).Lines | ConvertFrom-ChocolateyOutput -Command SourceList | Where-Object {
        $_.Name -like $Name
    }
    foreach ($Source in $CurrentSources) {
        $null = Invoke-Choco source disable --name $Source.Name
    }
}
