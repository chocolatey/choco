function Set-ChocolateyFeature {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]
        $Name,
        [Parameter(Mandatory = $true, ParameterSetName='Enable')]
        [switch]
        $Enable,
        [Parameter(Mandatory = $true, ParameterSetName='Disable')]
        [switch]
        $Disable
    )
    $action = $PSCmdlet.ParameterSetName
    $failures = $Name | ForEach-Object {
        $output = Invoke-Choco feature $action "--name=$_"
        if ($output.ExitCode -ne 0) {
            Write-Host "An error occurred setting ($action) feature: $_"
            Write-Host ("Chocolatey output: {0}{1}{2}" -f $output.ExitCode, ([Environment]::NewLine), $output.String)
            $_
        }
    }

    if ($failures) {
        $errorCountMessage = 'An error has'
        if ($failures.Count -gt 1) {
            $errorCountMessage = 'Errors have'
            $featurePlurality = 's'
        }

        throw "{0} occured setting ($action) feature{1}: {2}" -f $errorCountMessage, $featurePlurality, ($failures -join ', ')
    }
}
