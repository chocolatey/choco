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

$creditsFile = "$PSScriptRoot\docs\legal\CREDITS.md"

if (!(Test-Path $creditsFile)) {
    Write-Warning "Unable to find the '$creditsFile' file. Please update the 7-Zip entries manually."
} else {
    $creditsContent = Get-Content -Raw -Encoding utf8 -LiteralPath $creditsFile
    $creditsContent = $creditsContent -replace '(7-Zip @ )[\d.]+', "`${1}$Version"
    $creditsContent = $creditsContent -replace '(#7-zip--)\d+', "`${1}$VersionWithoutDots"
    $creditsContent | Out-File -LiteralPath $creditsFile -Encoding utf8NoBOM -NoNewline
}

Write-Host -ForegroundColor Green "The 7-zip components have been updated. Be sure to update the BundledApplications.Tests.ps1 file on this branch."
