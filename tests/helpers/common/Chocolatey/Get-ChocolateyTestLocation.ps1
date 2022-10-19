function Get-ChocolateyTestLocation {
    <#
        .SYNOPSIS
            Gets the last set chocolatey test location.
            This is usually set when initializing the original
            location before creating snapshots,
            otherwise the value is typically null.
    #>
    [CmdletBinding()]
    param()

    $script:chocolateyTestLocation
}
