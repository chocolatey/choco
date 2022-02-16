function Set-ChocolateyTestLocation {
    <#
        .SYNOPSIS
            Sets the specified directory as the test
            location directory, to be used later in processing.
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = "Low")]
    param(
        [string]$Directory
    )

    # Recommendation from PSScriptAnalyzer to use SupportsShouldProcess
    if ($PSCmdlet.ShouldProcess($Directory)) {
        $script:chocolateyTestLocation = $Directory
    }
}
