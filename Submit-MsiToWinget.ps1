param(
[Parameter(Mandatory)]
$GitHubToken
)
gh repo sync choco-bot/winget-pkgs

if ($LASTEXITCODE -ne 0) {
	throw "Sync of choco-bot repository failed. Check permissions and try again"
}

$release = gh release view --json assets,name| convertfrom-json
wingetcreate update --submit --token $GitHubToken --urls ($release.assets | ? url -match msi | % url) --version "$($release.name).0" chocolatey.chocolatey
