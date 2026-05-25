#Requires -PSEdition Core

param(
    [Parameter(Mandatory)]
    [string] $Version
)

$ErrorActionPreference = 'Stop'

$VersionWithoutDots = $Version -replace '\.'

Invoke-WebRequest "https://7-zip.org/a/7z$VersionWithoutDots.exe" -OutFile 7zipInstaller.exe
7z x 7zipInstaller.exe License.txt 7z.exe 7z.dll -o'src/chocolatey.resources/tools' -aoa
Remove-Item $PSScriptRoot/src/chocolatey.resources/tools/7zip.license.txt
Rename-Item $PSScriptRoot/src/chocolatey.resources/tools/License.txt 7zip.license.txt
Remove-Item 7zipInstaller.exe

$creditsFile = "$PSScriptRoot\docs\legal\CREDITS.json"

$creditsContent = Get-Content -Encoding utf8 -LiteralPath $creditsFile | ConvertFrom-Json

$7zipDependency = $creditsContent.dependencies | Where-Object name -eq '7-Zip'

if (-not $7zipDependency) {
    Write-Warning "The 7-Zip entry in '$creditsFile' could not be found. Please update the entry manually."
} else {
    $7zipDependency.version = $Version
    
    $json = ($creditsContent | ConvertTo-Json -Depth 4) -replace "`r`n","`n"
    "$json`n" | Out-File -LiteralPath $creditsFile -Encoding utf8NoBOM -NoNewline
}

$bundleTestFile = "$PSScriptRoot\tests\pester-tests\BundledApplications.Tests.ps1"

if (!(Test-Path $bundleTestFile)) {
    Write-Warning "Unable to find the 'BundleApplications.Tests.ps1' file. Please update the test with new 7zip version manually."
} else {
    $updateRe = "(Name\s*=\s*'7z'.*Version\s*=\s*)'[^']*'"
    $bundleTestContent = Get-Content -Encoding utf8 -LiteralPath $bundleTestFile | % {
        if ($_ -match $updateRe) {
            $_ -replace $updateRe,"`$1'$Version'"
        } else {
            $_
        }
    }

    $bundleTestContent | Out-File -Encoding utf8BOM -LiteralPath $bundleTestFile
}

Write-Host -ForegroundColor Green "The 7-zip components have been updated to Version $Version."
