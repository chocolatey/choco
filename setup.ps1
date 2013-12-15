### install chocolatey ###
if(-not $env:ChocolateyInstall -or -not (Test-Path "$env:ChocolateyInstall")){
    iex ((new-object net.webclient).DownloadString("https://chocolatey.org/install.ps1"))
}

cinst pester -version 2.0.2
