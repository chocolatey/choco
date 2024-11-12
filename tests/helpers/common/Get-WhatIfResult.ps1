function Get-WhatIfResult {
    <#
        .SYNOPSIS
        Runs a $Command in a new powershell.exe process, and then returns *only*
        the output lines that are prefixed with 'What if:' which are written as
        console output.
    #>
    [CmdletBinding()]
    param( 
        # The script to execute in the new process.
        [Parameter(Mandatory)]
        [scriptblock]
        $Command,

        # Any setup scripts that are required for running. All output from this
        # script block will be suppressed, if possible.
        [Parameter()]
        [scriptblock]
        $Preamble
    )

    $commandString = @'
. {{ {0} }} *>&1 > $null
& {{ {1} }}
'@ -f $Preamble, $Command

    $results = @(powershell -NoProfile -NonInteractive -Command $commandString)

    [pscustomobject]@{
        Output = $results
        WhatIf = @($results | Where-Object { $_ -like "What if:*" })
    }
}