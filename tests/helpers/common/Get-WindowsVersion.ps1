function Get-WindowsVersion {
    <#
        .Synopsis
            Returns the current Windows version as a [System.Version].
    #>
    [OutputType([System.Version])]
    [CmdletBinding()]
    param()
    if (-not $script:windowsVersion) {
        # Use CIM rather than [System.Environment]::OSVersion so we always get
        # the true build number, and not the value an unmanifested process can
        # be told by the operating system.
        [System.Version]$script:windowsVersion = (Get-CimInstance -ClassName Win32_OperatingSystem).Version
    }

    $script:windowsVersion
}
