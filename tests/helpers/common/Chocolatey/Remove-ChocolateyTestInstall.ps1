function Remove-ChocolateyTestInstall {
    [CmdletBinding()]
    param()

    Remove-ChocolateyInstallSnapshot -RemoveAll
    $chocolateyTestDirectory = Get-ChocolateyTestLocation

    if (Test-Path $chocolateyTestDirectory) {
        $null = Remove-Item $chocolateyTestDirectory -Force -Recurse
    }
    elseif (!$chocolateyTestDirectory) {
        Write-Warning "No test directory was found!"
    }

    $env:ChocolateyInstall = Get-OriginalChocolateyPath
    Set-ChocolateyTestLocation -Directory $null
}
