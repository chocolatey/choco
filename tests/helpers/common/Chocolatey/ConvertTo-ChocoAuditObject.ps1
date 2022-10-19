function ConvertTo-ChocoAuditObject {
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline)]
        [string]
        $InputObject
    )

    process {
        # format of the 'choco list' output is:
        # <PACKAGE NAME> <VERSION> (ie. adobereader 2015.6.7)
        if (-not [string]::IsNullOrEmpty($InputObject)) {
            $props = $_.split('|')

            [pscustomobject]@{

                name           = $props[0]
                version        = $props[1]
                InstalledBy    = $props[2] -replace ('User:', '')
                Domain         = $props[3] -replace ('Domain:', '')
                RequestedBy    = $props[4] -replace ('Original User:', '')
                InstallDateUtc = $props[5] -replace ('InstallDateUtc:', '')
            }
        }
    }
}
