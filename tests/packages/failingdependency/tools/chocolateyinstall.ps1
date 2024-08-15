Write-Error "This should fail!"
$env:ChocolateyExitCode = '15608'
#throw "This is crap"