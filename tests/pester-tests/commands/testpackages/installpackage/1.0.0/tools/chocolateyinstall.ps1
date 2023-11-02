$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$PackageParameters = Get-PackageParameters
"simple file" | Out-File "$toolsDir\simplefile.txt" -force

Write-Output "This is $packageName v$packageVersion being installed to `n $packageFolder"
Write-Host "Ya!"
Write-Debug "A debug message"
Write-Verbose "Yo!"
Write-Warning "A warning!"

Write-Output "$packageName v$packageVersion has been installed to `n $packageFolder"

Write-Host "Package Parameters:"
foreach ($key in $PackageParameters.Keys) {
    Write-Host "$key - $($PackageParameters[$key])"
}
