# TODO: Refactor the *-ChocolateySource functions to have a Get-ChocolateySource that can be piped to the Enable and Disable
function Disable-ChocolateySource {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Name = "*",

        [Parameter()]
        [switch]$All
    )

    $CurrentSources = Get-ChocolateySource -Name $Name
    foreach ($Source in $CurrentSources) {
        $null = Invoke-Choco source disable --name $Source.Name
    }
}
