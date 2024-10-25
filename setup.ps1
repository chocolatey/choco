### install chocolatey ###
if (-not $env:ChocolateyInstall -or -not (Test-Path "$env:ChocolateyInstall")) {
    Invoke-Expression ((New-Object net.webclient).DownloadString("https://community.chocolatey.org/install.ps1"))
}

choco install dotnetfx -y
choco install visualstudio2019buildtools -y
choco install netfx-4.8-devpack -y
choco install dotnet-sdk -y
