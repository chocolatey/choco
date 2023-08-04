#Requires -Module @{ ModuleName = 'pester'; ModuleVersion = '5.3.1' }
#Requires -RunAsAdministrator
<#
    .SYNOPSIS
    Prepares a system to test as though it was Test Kitchen.
#>
param(
    # Path to place Chocolatey test related artifacts.
    [string]
    $TestPath = "$env:TEMP/chocolateyTests",

    # Indicate to skip packaging all of the tests packages. Useful for running tests after you've performed the tests previously.
    [switch]
    $SkipPackaging,

    # The remote repository to push packages to and to use during tests.
    [string]
    $RemoteRepository,

    # API Key used by the remote repository for pushing packages.
    [string]
    $ApiKey
)

if (-not (Test-Path "$TestPath/packages") -or -not $SkipPackaging) {
    if (($null -ne $RemoteRepository) -and ($null -ne $ApiKey))
    $null = New-Item -Path "$TestPath/packages" -ItemType Directory -Force
    # Get and pack packages
    $nuspecs = Get-ChildItem -Path $PSScriptRoot/src/chocolatey.tests.integration, $PSScriptRoot/tests/packages -Recurse -Include *.nuspec
    Get-ChildItem -Path $PSScriptRoot/tests/packages -Recurse -Include *.nupkg | Copy-Item -Destination "$TestPath/packages"

    foreach ($file in $nuspecs) {
        Write-Host "Packaging $file"
        $null = choco pack $file.FullName --out "$TestPath/packages"
    }

    Get-ChildItem -Path $TestPath/packages | ForEach-Object {
        choco push $_.FullName -s $RemoteRepository -k $ApiKey --force --allow-unofficial
    }
}

try {
    Push-Location $PSScriptRoot/tests
    $env:PSModulePath = "$PSScriptRoot/tests;$env:PSModulePath"

    Import-Module $PSScriptRoot\tests\helpers\common-helpers.psm1 -Force
    $null = Invoke-Choco source add --name hermes --source $RemoteRepository
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
        Should     = @{
            ErrorAction = 'Continue'
        }
    }

    Invoke-Pester -Configuration $PesterConfiguration
}
finally {
    Pop-Location
}
