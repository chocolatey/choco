function Get-ChocolateyLicense {
    if ($script:LicenseType -and $script:LicenseType -ne '') {
        return $script:LicenseType
    }

    $package = (Invoke-Choco list --local-only --limitoutput).Lines |
        ConvertFrom-ChocolateyOutput -Command List |
        Where-Object Name -Match "^chocolatey-license-" |
        Select-Object -First 1 # We only expect one package, so we only take the first result

    if ($package) {
        $script:LicenseType = $package.Name -replace "^chocolatey-license-"
    }
    else {
        $script:LicenseType = ''
    }

    $script:LicenseType
}
