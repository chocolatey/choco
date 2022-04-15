function Disable-ChocolateyFeature {
    [Alias('Disable-ChocoFeature')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]
        $Name
    )
    Set-ChocolateyFeature -Name $Name -Disable
}
