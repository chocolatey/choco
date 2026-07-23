[CmdletBinding()]

param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$GitHubToken
)
$syncOutput = gh repo sync choco-bot/winget-pkgs 2>&1

if ($LASTEXITCODE -ne 0) {
    throw "Sync of choco-bot/winget-pkgs failed (exit code $LASTEXITCODE): $syncOutput"
}

$releaseJson = gh release view --repo chocolatey/choco --json assets, name 2>&1

if ($LASTEXITCODE -ne 0) {
    throw "Failed to query latest release for chocolatey/choco (exit code $LASTEXITCODE): $releaseJson"
}

$release = $releaseJson | ConvertFrom-Json
$msiUrls = ($release.assets | Where-Object { $_.name -like '*.msi' }).url

if ($msiUrls.Count -eq 0) {
    throw "No .msi assets found in latest release '$($release.name)'"
}

$wingetOutput = wingetcreate update --submit --token $GitHubToken --urls $msiUrls --version "$($release.name).0" chocolatey.chocolatey 2>&1

if ($LASTEXITCODE -ne 0) {
    throw "wingetcreate update failed (exit code $LASTEXITCODE): $wingetOutput"
}
