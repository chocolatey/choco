function Get-PackageScriptParameters {
    <#
        .SYNOPSIS
        Returns a splattable hashtable of arguments for a script,
        from current package parameters.

        .DESCRIPTION
        This parses a script file for the existing params available and then
        compares them to the package parameters provided.
        If it finds matching names, it outputs them in a splattable hashtable
        for use by the script.

        .NOTES
        Currently, this function ignores parameter aliases.

        .OUTPUTS
        [HashTable]

        .PARAMETER ScriptPath
        The path to the script to parse.

        .PARAMETER Parameters
        OPTIONAL - A parameter string to pass to Get-PackageParameters.

        .PARAMETER IgnoredArguments
        Allows splatting with arguments that do not apply and future expansion.
        Do not use directly.

        .EXAMPLE
        >
        # The default way of calling, uses the parameter environment variables
        # if available.
        $scriptParameters = Get-ScriptParameters -ScriptPath $packageScript

        .LINK
        Get-PackageParameters
    #>
    [OutputType([HashTable])]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ScriptPath,

        [Parameter()]
        [string]$Parameters = '',

        [parameter(ValueFromRemainingArguments = $true)][Object[]] $IgnoredArguments
    )
    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

    $packageParameters = Get-PackageParameters -Parameters $Parameters
    $splatHash = @{}

    # Check what parameters the script has
    $script = [System.Management.Automation.Language.Parser]::ParseFile($ScriptPath, [ref]$null, [ref]$null)
    $scriptParameters = $Script.ParamBlock.Parameters.Name.VariablePath.UserPath

    # For each of those in PackageParameters, add it to the splat
    foreach ($parameter in $scriptParameters) {
        if ($packageParameters.ContainsKey($parameter)) {
            $splatHash[$parameter] = $packageParameters[$parameter]
        }
    }

    # Return the splat
    $splatHash
}