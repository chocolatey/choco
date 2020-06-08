$packageName = 'badpackage'

try {

  Write-Host "Ya!"
  Write-Debug "A debug message"
  Write-Warning "A warning!"
  Write-Error "Oh no! An error"
  throw "We had an error captain!"

  Write-ChocolateySuccess "$packageName"
} catch {
  Write-ChocolateyFailure "$packageName" "$($_.Exception.Message)"
  throw
}
