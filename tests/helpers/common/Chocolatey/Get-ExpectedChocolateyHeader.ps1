function Get-ExpectedChocolateyHeader {
    $packages = (Invoke-Choco list --local-only --limitoutput).Lines |
        Where-Object { $_ -NotMatch 'please upgrade' } |
        ConvertFrom-ChocolateyOutput -Command List

    $licenseType = Get-ChocolateyLicense

    if ($licenseType -eq "msp") {
        $licenseType = "ManagedServiceProvider"
    }
    elseif ($licenseType -eq 'education') {
        $licenseType = "Educational"
    }
    elseif ($licenseType -eq 'trial') {
        $licenseType = "BusinessTrial"
    }

    if ($packages.Name -contains "chocolatey.extension") {
        # hard coded license type for now
        return "Chocolatey v$(Get-ChocolateyVersion) $licenseType"
    }
    else {
        return "Chocolatey v$(Get-ChocolateyVersion)"
    }
}
