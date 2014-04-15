function Write-ChocolateyFailure {
param(
  [string] $packageName,
  [string] $failureMessage
)

  $chocTempDir = Join-Path $env:TEMP "chocolatey"
  $tempDir = Join-Path $chocTempDir "$packageName"
  if (![System.IO.Directory]::Exists($tempDir)) {[System.IO.Directory]::CreateDirectory($tempDir)}
  $successLog = Join-Path $tempDir 'success.log'
  try {
    if ([System.IO.File]::Exists($successLog)) {
      $oldSuccessLog = "$successLog".replace('.log','.log.old')
      write-debug "Renaming `'$successLog`' to `'$oldSuccessLog`'"
      Move-Item $successLog $oldSuccessLog -Force
      #[System.IO.File]::Move($successLog,(Join-Path ($successLog) '.old'))
    }
  } catch {
    Write-Error "Could not rename `'$successLog`' to `'$($successLog).old`': $($_.Exception.Message)"
  }

  $logFile = Join-Path $tempDir 'failure.log'
  #Write-Host "Writing to $logFile"

  $errorMessage = "$packageName did not finish successfully. Boo to the chocolatey gods!
-----------------------
[ERROR] $failureMessage
-----------------------"
  $errorMessage | Out-File -FilePath $logFile -Force -Append
  Write-Error $errorMessage
}
