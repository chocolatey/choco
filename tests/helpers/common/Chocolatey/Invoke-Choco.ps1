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
        [string[]]$Arguments,

        # Pipeline input to direct into choco.exe. Used mainly to "respond" to prompts
        # in a noninteractive context. Provide input as a single string with line-feed
        # characters ("`n") between input characters in that context.
        [Parameter(ValueFromPipeline)]
        [string]$PipelineInput
    )
    begin {
        $chocoPath = Get-ChocoPath
        $firstArgument, [string[]]$remainingArguments = $Arguments
        $arguments = @(
            $firstArgument
            '--allow-unofficial'

            if ($env:CHOCO_TEST_PROXY) {
                "--proxy='$($env:CHOCO_TEST_PROXY)'"
                if ($env:CHOCO_TEST_PROXY_USER) {
                    "--proxy-user='$($env:CHOCO_TEST_PROXY_USER)'"
                    "--proxy-password='$($env:CHOCO_TEST_PROXY_PASSWORD)'"
                }
            }

            $remainingArguments
        )
    }
    end {
        $stopwatch = [System.Diagnostics.Stopwatch]::new()
        $stopwatch.Start()

        $output = if ($PipelineInput) {
            $PipelineInput | & $chocoPath @arguments
        }
        else {
            & $chocoPath @arguments
        }

        $CommandExitCode = $LastExitCode
        $stopwatch.Stop()
        # We do not use env:ChocolateyInstall here as it is not guaranteed due to snapshots.
        # Saving into the default Chocolatey install location as .log so Test Kitchen can pick up the file.
        [PSCustomObject]@{
            Date       = Get-Date -Format 'o'
            Duration   = $stopwatch.Elapsed
            Invocation = ($MyInvocation.PositionMessage -split "`r`n")[0]
            ExitCode   = $CommandExitCode
        } | Export-Csv -Path $env:ALLUSERSPROFILE/chocolatey/logs/testInvocations.log -NoTypeInformation -Append

        [PSCustomObject]@{
            # We trim all the lines, so we do not take into account
            # trimming the lines when asserting, and that extra whitespace
            # is not considered in our assertions.
            Lines     = if ($output) {
                $output.Trim()
            }
            else {
                @()
            }
            String    = $output -join "`r`n"
            ExitCode  = $CommandExitCode
            Duration  = $stopwatch.Elapsed
        }
    }
}
