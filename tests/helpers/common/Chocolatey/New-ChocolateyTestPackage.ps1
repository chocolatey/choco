function New-ChocolateyTestPackage {
    <#
        .Synopsis
            Builds given packages and (optionally) ensures they are available in a given directory
    #>
    [CmdletBinding()]
    param(
        # The path to the base location of packages
        [Parameter(Mandatory)]
        [ValidateScript( { Test-Path $_ -PathType Container })]
        [string]$TestPath,

        # Name of the package to build
        [Parameter(Mandatory)]
        [string]$Name,

        # Version of the package to build
        [Parameter(Mandatory)]
        [string]$Version,

        # Location to store the built package(s)
        [ValidateNotNullOrEmpty()]
        [string]$Destination = $env:CHOCOLATEY_TEST_PACKAGES_PATH
    )
    process {
        $NuspecFile = Get-Item "$TestPath\$Name\$Version\$Name.nuspec"

        Write-Verbose "Building '$($NuspecFile.Count)' packages"

        foreach ($Package in $NuspecFile) {
            Write-Verbose "Building '$($Package.Name)' with '$($Package.FullName)'"
            $ExpectedPackage = Join-Path $Destination "$Name.$Version.nupkg"

            if (-not (Test-Path $ExpectedPackage)) {
                $BuildOutput = Invoke-Choco pack $Package.FullName --outputdirectory $Destination

                if ($BuildOutput.ExitCode -ne 0) {
                    throw $BuildOutput.String
                }
            }
        }
    }
}
