$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
"simple file" | Out-File "$toolsDir\simplefile.txt" -force

Write-Output "$env:PackageName $env:PackageVersion Installed"

