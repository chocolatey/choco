# TODO: Refactor the *-ChocolateySource functions to have a Get-ChocolateySource that can be piped to the Enable and Disable
function Enable-ChocolateySource {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Name = "*",

        [Parameter()]
        [switch]$All
    )
    # Significantly weird behaviour with piping this source list by property name.
    $CurrentSources = Get-ChocolateySource -Name $Name
    foreach ($Source in $CurrentSources) {
        $null = Invoke-Choco source enable --name $Source.Name
    }
}
