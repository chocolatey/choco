function Get-ChocoPath {
    <#
        .Synopsis
            Helper function to resolve the path to the chocolatey executable, or use an environment variable with the path.
    #>
    if (Test-Path Env:\CHOCO_EXECUTABLE) {
        return $env:CHOCO_EXECUTABLE
    }
    else {
        "$env:ChocolateyInstall\bin\choco.exe"
    }
}
