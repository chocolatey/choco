function Test-WindowsVersionEqualOrHigherThan {
    <#
        .Synopsis
            Helper function that can be used to assert whether the current
            Windows version is equal to or higher than a certain threshold.

            Windows releases map to OS build numbers (for example, Windows 10
            1903 is build 10.0.18362 and Windows Server 2022 is build
            10.0.20348) on Microsoft's Windows release information pages.
        .LINK
            https://learn.microsoft.com/en-us/windows/release-health/release-information
        .LINK
            https://learn.microsoft.com/en-us/windows-server/get-started/windows-server-release-info
    #>
    [CmdletBinding()]
    [OutputType([boolean])]
    param(
        [Parameter(Mandatory)]
        [System.Version]$Version
    )

    (Get-WindowsVersion) -ge $Version
}
