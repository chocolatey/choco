param(
    [Parameter(Mandatory)]
    $Version
)

$Version = $Version -replace '\.'

Invoke-WebRequest "https://7-zip.org/a/7z$Version.exe" -OutFile 7zipInstaller.exe
7z x 7zipInstaller.exe License.txt 7z.exe 7z.dll -o'src/chocolatey.resources/tools' -aoa
Remove-Item $PSScriptRoot/src/chocolatey.resources/tools/7zip.license.txt
Rename-Item $PSScriptRoot/src/chocolatey.resources/tools/License.txt 7zip.license.txt
Remove-Item 7zipInstaller.exe

Write-Host -ForegroundColor Green "The 7-zip components have been updated. Be sure to update the BundledApplications.Tests.ps1 file, and the CREDITS files that exist on this branch."
