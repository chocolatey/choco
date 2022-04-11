function Initialize-ChocolateyTestInstall {
    <#
        .Synopsis
            Sets up a temporary Chocolatey install for testing
    #>
    [CmdletBinding()]
    param(
        # The location to create the temporary installation
        [string]$Directory = "$(Get-TempDirectory)ChocolateyTests\original",

        # A source to add, disabling all others
        [string]$Source,

        # Whether the source called 'hermes' should be disabled or not
        [switch]$DisableHermesSource,

        # Whether the source called 'chocolatey' should be enabled or not
        [switch]$EnableChocolateySource
    )
    end {
        if (-not (Test-Path $Directory -PathType Container)) {
            $null = New-Item $Directory -Force -ItemType Directory
        }

        # TODO: If this will be used cross-platform, it should not use robocopy
        $null = robocopy $env:ChocolateyInstall $Directory /MIR

        $env:ChocolateyInstall = $Directory
        Set-ChocolateyTestLocation -Directory $Directory

        Invoke-Choco feature disable -n shownonelevatedwarnings

        # This should only affect the newly copied choco, so no need to clean it up
        if ($PSBoundParameters.ContainsKey("Source")) {
            Disable-ChocolateySource -All
            $null = Invoke-Choco source add -n TestSource -s $Source
        }

        if ($DisableHermesSource.IsPresent) {
            Disable-ChocolateySource -Name 'hermes'
        }
        else {
            Enable-ChocolateySource -Name 'hermes'
        }

        if ($EnableChocolateySource.IsPresent) {
            Enable-ChocolateySource -Name 'chocolatey'
        }
        else {
            Disable-ChocolateySource -Name 'chocolatey'
        }
    }
}
