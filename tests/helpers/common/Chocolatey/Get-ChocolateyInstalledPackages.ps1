function Get-ChocolateyInstalledPackages {
    (Invoke-Choco list -r).Lines | ConvertFrom-ChocolateyOutput -Command List
}
