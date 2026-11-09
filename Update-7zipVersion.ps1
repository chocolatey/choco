#Requires -PSEdition Core

param(
    [string] $Version = 'latest'
)

$ErrorActionPreference = 'Stop'

if ($Version -eq 'latest') {
    $latestRelease = Invoke-RestMethod 'https://api.github.com/repos/ip7z/7zip/releases/latest'
    $Version = $latestRelease.tag_name
    Write-Host "The latest 7-Zip release on GitHub is Version $Version."
}

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

    $licenseRe = '(?s)(### 7-Zip @ [^\r\n]*\r?\n(?:(?!###).)*?```txt\r?\n).*?(```)'

    if ($creditsContent -notmatch $licenseRe) {
        Write-Warning "Unable to find the 7-Zip license terms in '$creditsFile'. Please update them manually."
    } else {
        $licenseContent = Get-Content -Raw -Encoding utf8 -LiteralPath $PSScriptRoot/src/chocolatey.resources/tools/7zip.license.txt
        $creditsContent = $creditsContent -replace $licenseRe, { $_.Groups[1].Value + $licenseContent + $_.Groups[2].Value }
    }

    $creditsContent | Out-File -LiteralPath $creditsFile -Encoding utf8NoBOM -NoNewline
}

$bundleTestFile = "$PSScriptRoot\tests\pester-tests\BundledApplications.Tests.ps1"
$bundleTestRe = "(Name\s*=\s*'7z'\s*;\s*Version\s*=\s*)'[^']*'"

if (!(Test-Path $bundleTestFile)) {
    Write-Warning "Unable to find the '$bundleTestFile' file. Please update the test with the new 7-Zip version manually."
} else {
    $bundleTestContent = Get-Content -Raw -Encoding utf8 -LiteralPath $bundleTestFile

    if ($bundleTestContent -notmatch $bundleTestRe) {
        Write-Warning "Unable to find the 7-Zip entry in '$bundleTestFile'. Please update the test with the new 7-Zip version manually."
    } else {
        $bundleTestContent = $bundleTestContent -replace $bundleTestRe, "`${1}'$Version'"
        $bundleTestContent | Out-File -LiteralPath $bundleTestFile -Encoding utf8BOM -NoNewline
    }
}

Write-Host -ForegroundColor Green "The 7-zip components have been updated to Version $Version."
