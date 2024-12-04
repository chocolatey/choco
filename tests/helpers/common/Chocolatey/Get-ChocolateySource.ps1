function Get-ChocolateySource {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Name = "*"
    )
    # Significantly weird behaviour with piping this source list by property name.
    (Invoke-Choco source list -r).Lines | ConvertFrom-ChocolateyOutput -Command SourceList | Where-Object {
        $_.Name -like $Name
    }
}
