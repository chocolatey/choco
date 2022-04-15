function ConvertFrom-ChocolateyOutput {
    <#
        .Synopsis
            Converts from Chocolatey's --LimitOutput (-r) to PowerShell object

        .Example
            (Invoke-Choco list -lo -r).Lines | ConvertFrom-ChocolateyOutput -Command List

        .Example
            (Invoke-Choco pin list -r).Lines | ConvertFrom-ChocolateyOutput -Command PinList

        .Example
            (Invoke-Choco source list -r).Lines | ConvertFrom-ChocolateyOutput -Command SourceList

        .Example
            (Invoke-Choco feature list -r).Lines | ConvertFrom-ChocolateyOutput -Command Feature
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias("Lines")]
        $InputObject,

        [Parameter(Mandatory)]
        [ValidateSet("List", "PinList", "SourceList", "SelfService", "Feature")]
        [string]$Command
    )
    begin {
        # This is of limited use as we can't check pipelined commands broken up over several lines.
        if ($MyInvocation.Line -match "choco\s" -and $MyInvocation.Line -notmatch "(--LimitOutput|--Limit-Output|-r)") {
            Write-Warning "Chocolatey may not have been called with --LimitOutput."
        }

        if (-not $script:ChocoCommandHeaders.ContainsKey($Command)) {
            throw "No Headers found for '$($Command)'"
        }

        # TODO: If Issue 2591 is merged, the header portion of these calls will not be needed.
        $ConversionParams = @{
            Delimiter = "|"
            Header    = $script:ChocoCommandHeaders[$Command]
        }
    }
    process {
        $InputObject | Where-Object { $_ -like '*|*' } | ConvertFrom-Csv @ConversionParams
    }
}
