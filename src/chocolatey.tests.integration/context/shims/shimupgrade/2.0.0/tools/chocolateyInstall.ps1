try {
  Write-Output "This is $packageName v$packageVersion being installed to `n '$packageFolder'."
  Write-Error "Oh no! An error"
  throw "We had an error captain!"
} catch {
  throw $_.Exception
}
