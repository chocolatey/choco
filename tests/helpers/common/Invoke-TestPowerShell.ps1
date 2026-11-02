function Invoke-TestPowerShell {
    <#
        .SYNOPSIS
            Runs commands in a separate Windows PowerShell process and returns what they wrote and the exit code.

        .DESCRIPTION
            Some behaviour cannot be observed from inside the process running the tests: text written directly to the host
            is not captured by stream redirection, and commands that ask the host to exit with an exit code would end the
            test run. Running the commands in a separate process allows both to be checked.

            The commands are run with the Chocolatey installer module imported, if a module path is given, and everything
            written to the output, error, debug and host is returned as lines of text. Debug messages are written as "DEBUG: message".

        .OUTPUTS
            An object with Output (the lines written) and ExitCode (the exit code of the process).
    #>
    [CmdletBinding()]
    param(
        # The commands to run.
        [Parameter(Mandatory)]
        [string]$Command,

        # The path to a module to import before running the commands.
        [string]$ModulePath,

        # Run the commands with the debug stream enabled.
        [switch]$DebugOutput
    )
    end {
        # The streams are merged inside the process, as debug and warning messages are not otherwise written to its output.
        $lines = @(
            '$ProgressPreference = ''SilentlyContinue'''
            if ($ModulePath) { "Import-Module '$ModulePath'" }
            # Importing the Chocolatey module alters the debug preference, so we need to change it after importing.
            if ($DebugOutput) { '$global:DebugPreference = ''Continue''' }
            '& {'
            $Command
            '} *>&1 | ForEach-Object {'
            '    if ($_ -is [System.Management.Automation.DebugRecord]) { "DEBUG: $($_.Message)" }'
            '    elseif ($_ -is [System.Management.Automation.WarningRecord]) { "WARNING: $($_.Message)" }'
            '    else { "$_" }'
            '}'
        )
        $encoded = [Convert]::ToBase64String([System.Text.Encoding]::Unicode.GetBytes($lines -join "`n"))

        $output = & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand $encoded 2>&1 | ForEach-Object { "$_" }

        [pscustomobject]@{
            Output   = @($output)
            ExitCode = $LASTEXITCODE
        }
    }
}
