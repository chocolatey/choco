function Test-VersionEqualOrHigher {
    <#
        .SYNOPSIS
            Helper function to compare whether the specified InstallVersion
            is higher or equal to the version specified in CompareVersion.
    #>
    [CmdletBinding()]
    [OutputType([boolean])]
    param(
        [Parameter(Mandatory)]
        [NuGet.Versioning.SemanticVersion]$InstalledVersion,
        [Parameter(Mandatory)]
        [NuGet.Versioning.SemanticVersion]$CompareVersion
    )

    if ($installedVersion -gt $CompareVersion) {
        return $true
    }

    # We are using `ge` here instead of `eq` as it will
    # take the correct intention into consideration, and the
    # previous `gt` call somehow do not match against all situations
    # it is supposed to.
    $result = $installedVersion -ge $CompareVersion

    # If the previous assertion is $true, and both the installed
    # and the acquired package version are pre-releases, we'll need
    # to compare the release part do see which string is higher than the
    # other.
    # That means alpha < beta, ceta > beta and beta-2016 > beta.
    if ($result -and $installedVersion.IsPrerelease -and $CompareVersion.IsPrerelease) {
        return $installedVersion.Release -ge $CompareVersion.Release
    }
    # If the previous assertion was false, we only need to see
    # if the acquired package version is a pre-release (assuming both versions were)
    # considered somewhat equal.
    elseif ($result -and $CompareVersion.IsPrerelease) {
        return $true # In this case, the installed version is not a pre-release
    }

    # Lastly, if we get here, then either the installed version
    # was lower than the acquired package, or the installed version
    # is a pre-release while the acquired package version was not.
    # To be certain of this, we do an extra check to ensure we
    # are returning the correct result to the user.
    return $result -and !$installedVersion.IsPrerelease
}
