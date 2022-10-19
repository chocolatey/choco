function Get-ChocoLogPath {
    <#
        .Synopsis
            Helper function to resolve the path to the chocolatey log, or use an environment variable with the path of the Chocolatey executable.
    #>
    if (Test-Path Env:\CHOCO_EXECUTABLE) {
        "$(Split-Path -Parent $env:CHOCO_EXECUTABLE)\logs\chocolatey.log"
    }
    else {
        "$env:ChocolateyInstall\logs\chocolatey.log"
    }
}