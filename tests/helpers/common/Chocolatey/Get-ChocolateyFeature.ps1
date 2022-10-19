function Get-ChocolateyFeature {
    # Helper function to determine what features we have
    # available or not. Once all features have been found,
    # this is better served as a specific package we can install
    # during provision instead.
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    [Alias('Get-ChocolateyFeatures')]
    param()

    if ($null -ne $script:features) {
        return $script:features
    }

    $license = Get-ChocolateyLicense
    # Just so we don't return the actual license for tests
    # we only specify as true if there is a license.
    $isLicensed = if ($license) { $true } else { $false }
    $is30Licensed = $isLicensed -and (Test-PackageIsEqualOrHigher 'chocolatey.extension' '3.0.0-alpha')
    $is22Licensed = $isLicensed -and ($is30Licensed -or (Test-PackageIsEqualOrHigher 'chocolatey.extension' '2.2.0-beta'))

    # These features are collected from <https://chocolatey.org/compare>
    $script:features = [pscustomobject]@{
        License                 = $license

        Audit                   = $license -in @('business', 'trial')
        AutoSync                = $isLicensed -and $license -ne 'architect'
        Builder                 = $isLicensed -and $license -ne 'professional'
        BuilderUI               = $isLicensed
        CCM                     = $isLicensed -and $license -notin @('professional', 'architect')
        CDNCache                = $isLicensed -and $license -ne 'architect'
        CommercialCmdlet        = $is30Licensed -and $license -in @('business', 'msp', 'trial')
        CommercialCmdletInstall = $is30Licensed
        DirectoryInstall        = $isLicensed -and $license -notin @('professional', 'architect')
        DirectoryOverride       = $isLicensed -and $license -ne 'architect'
        Downloader              = $isLicensed
        Enhanced                = $isLicensed -and $license -ne 'architect'
        FromPrograms            = $isLicensed -and $license -notin @('professional', 'architect')
        Internalizer            = $isLicensed -and $license -ne 'professional'
        Intune                  = $is30Licensed -and $license -in @('business', 'trial')
        Is30Licensed            = $is30Licensed
        QDE                     = $isLicensed -and $license -notin @('professional', 'architect')
        Reducer                 = $isLicensed -and $license -ne 'architect'
        SelfService             = $isLicensed -and $license -notin @('professional', 'architect')
        Sync                    = $isLicensed -and $license -notin @('architect', 'msp', 'professional')
        Throttle                = $isLicensed -and $license -ne 'architect'
        VirusScan               = $isLicensed -and $license -ne 'architect'
        VirusTotal              = $isLicensed -and $license -notin @('architect', 'msp')

        # The following are different tests that was discovered during writing tests.
        # These could be either fixes of bugs, or undocumented features.
        PackageProxy            = !$isLicensed -or $is22Licensed
    }

    $script:features
}
