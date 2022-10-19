function Enable-ChocolateyFeature {
    [Alias('Enable-ChocoFeature')]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]
        $Name
    )

    Set-ChocolateyFeature -Name $Name -Enable
}
