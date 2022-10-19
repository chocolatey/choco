function Invoke-Choco {
    <#
        .Synopsis
            Helper function to call chocolatey with any number of specified arguments,
            and return a hashtable with the output as well as the exit code.
    #>
    [CmdletBinding()]
    param(
        # The arguments to use when calling the Choco executable
        [Parameter(Position = 1, ValueFromRemainingArguments)]
        [string[]]$Arguments
    )

    $chocoPath = Get-ChocoPath
        $firstArgument, [string[]]$remainingArguments = $Arguments
    $arguments = @($firstArgument; '--allow-unofficial'; $remainingArguments)
    $output = & $chocoPath @arguments
    [PSCustomObject]@{
        # We trim all the lines, so we do not take into account
        # trimming the lines when asserting, and that extra whitespace
        # is not considered in our assertions.
        Lines    = if ($output) { $output.Trim() } else { @() }
        String   = $output -join "`r`n"
        ExitCode = $LastExitCode
    }
}
