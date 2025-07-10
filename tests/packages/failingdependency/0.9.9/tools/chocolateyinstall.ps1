"Creating Runtime File"
$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
"Test File" | Out-File "$toolsDir\test-file.txt"

Write-Error "This should fail!"
$env:ChocolateyExitCode = '15608'
