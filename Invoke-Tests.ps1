#Requires -Module @{ ModuleName = 'pester'; ModuleVersion = '5.3.1' }
#Requires -RunAsAdministrator
<#
    .SYNOPSIS
    Extracts and "installs" Chocolatey nupkg package for testing with Pester.
#>
param(
    # Path to place Chocolatey test related artifacts.
    [string]
    $TestPath = "$env:TEMP/chocolateyTests",

    # Path of the nupkg to be tested. Defaults to `code_drop/Packages/Chocolatey/chocolatey.<version>.nupkg`
    [ValidateScript({
            $count = (Get-Item $_).Count
            if ($count -ne 1) {
                throw "Expected 1 item, found $count"
            }
            $true
        })]
    [string]
    $TestPackage,

    # Indicate to skip packaging all of the tests packages. Useful for running tests after you've performed the tests previously.
    [switch]
    $SkipPackaging
)
$packageRegex = 'chocolatey\.\d.*\.nupkg'

# Check if there are any tests that exceed Test Kitchen maximum lengths
$TestsLocation = Join-Path $PSScriptRoot tests
$MaxFileNameLength = 110
$LongFiles = Get-ChildItem $TestsLocation -Recurse |
    Where-Object { ($_.FullName.Length - $TestsLocation.Length) -gt $MaxFileNameLength } |
    Select-Object -Property @{Name = 'RelativePath' ; Expression = { $_.FullName.Replace($TestsLocation, [string]::Empty)}}, @{ Name = 'ReductionNeeded' ; Expression = { $_.FullName.Length - $TestsLocation.Length - $MaxFileNameLength } }

if ($LongFiles) {
    Write-Host "Tests' file paths may be too long for Test Kitchen use. Please shorten file names or paths:"
    $LongFiles | Format-List | Out-String | Out-Host
    throw "Unable to complete tests due to long file paths"
}

# Use TstPkg as TestPackage has ValidateScript that can't be circumvented
if (-not $TestPackage) {
    $TstPkg = Get-ChildItem $PSScriptRoot/code_drop/Packages/Chocolatey -Filter *.nupkg | Where-Object Name -Match $packageRegex
}
else {
    $TstPkg = Get-ChildItem $TestPackage
}

if (-not (Test-Path "$TestPath/packages") -or -not $SkipPackaging) {
    $null = New-Item -Path "$TestPath/packages" -ItemType Directory -Force
    # Get and pack packages
    $nuspecs = Get-ChildItem -Path $PSScriptRoot/src/chocolatey.tests.integration, $PSScriptRoot/tests/packages -Recurse | Where-Object Name -Like '*.nuspec'

    foreach ($file in $nuspecs) {
        Write-Host "Packaging $file"
        $null = choco pack $file.FullName --out "$TestPath/packages"
    }
}

Copy-Item -Path $TstPkg.FullName -Destination $TestPath -Force
$nupkg = Get-ChildItem -Path "$TestPath/$($TstPkg.Name)"

if (Test-Path "$TestPath\chocolatey") {
    Write-Host "$TestPath\chocolatey already exists. Removing so we can continue installation successfully."
    Remove-Item "$TestPath/chocolatey" -Recurse -Force
}

try {
    Push-Location $TestPath
    Import-Module $PSScriptRoot\tests\helpers\common-helpers.psm1 -Force
    Expand-ZipArchive -Source $nupkg.FullName -Destination ./chocolatey
    Import-Module $TestPath/Chocolatey/tools/ChocolateyInstall/helpers/chocolateyInstaller.psm1
    Import-Module $TestPath/Chocolatey/tools/ChocolateySetup.psm1
    $environmentVariables = @{
        UserPath                 = Get-EnvironmentVariable -Name 'PATH' -Scope 'User'
        UserChocolateyInstall    = Get-EnvironmentVariable -Name 'ChocolateyInstall' -Scope 'User'
        UserPSModulePath         = Get-EnvironmentVariable -Name 'PSModulePath' -Scope 'User'
        MachinePath              = Get-EnvironmentVariable -Name 'PATH' -Scope 'Machine'
        MachineChocolateyInstall = Get-EnvironmentVariable -Name 'ChocolateyInstall' -Scope 'Machine'
        MachinePSModulePath      = Get-EnvironmentVariable -Name 'PSModulePath' -Scope 'Machine'
    }

    $env:ChocolateyInstall = "$TestPath/base"
    $null = Initialize-Chocolatey
    # It seems this is getting clobbered by Intialize-Chocolatey... No idea why...
    $env:ChocolateyInstall = "$TestPath/base"

    Pop-Location
    Push-Location $PSScriptRoot/tests
    $env:PSModulePath = "$PSScriptRoot/tests;$env:PSModulePath"

    Import-Module $PSScriptRoot\tests\helpers\common-helpers.psm1 -Force
    $null = Invoke-Choco source add --name hermes --source "$TestPath/packages"
    Enable-ChocolateyFeature -Name allowGlobalConfirmation
    $PesterConfiguration = [PesterConfiguration]@{
        Run        = @{
            PassThru = $true
            Path     = "$PSScriptRoot/tests/chocolatey-tests"
        }
        TestResult = @{
            Enabled       = $true
            TestSuiteName = "Pester - Chocolatey"
        }
        Output     = @{
            Verbosity = 'Minimal'
        }
        Filter     = @{
            ExcludeTag = @(
                'Background'
                'Licensed'
                'CCM'
                'WIP'
                'NonAdmin'
                'Internal'
                if (-not $env:VM_RUNNING -and -not $env:TEST_KITCHEN) {
                    'VMOnly'
                }
            )
        }
    }

    Invoke-Pester -Configuration $PesterConfiguration

}
finally {
    # For some reason we need to import this again... I'm not 100% sure on why...
    Import-Module $TestPath/Chocolatey/tools/ChocolateyInstall/helpers/chocolateyInstaller.psm1
    # Put back Path and Chocolatey
    Set-EnvironmentVariable -Name 'PATH' -Scope 'User' -Value $environmentVariables.UserPath
    Set-EnvironmentVariable -Name 'ChocolateyInstall' -Scope 'User' -Value $environmentVariables.UserChocolateyInstall
    Set-EnvironmentVariable -Name 'PSModulePath' -Scope 'User' -Value $environmentVariables.UserPSModulePath
    Set-EnvironmentVariable -Name 'PATH' -Scope 'Machine' -Value $environmentVariables.MachinePath
    Set-EnvironmentVariable -Name 'ChocolateyInstall' -Scope 'Machine' -Value $environmentVariables.MachineChocolateyInstall
    Set-EnvironmentVariable -Name 'PSModulePath' -Scope 'Machine' -Value $environmentVariables.MachinePSModulePath
    Pop-Location
}
