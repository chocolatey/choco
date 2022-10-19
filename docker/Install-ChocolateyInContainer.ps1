$nupkgDir = 'C:\choco-nupkg'
$7zPath   = Join-Path $nupkgDir 'tools\7z.exe'

function Install-LocalChocolateyPackage {
param (
  [string]$chocolateyPackageFilePath = '',
  [string]$sevenZipPath              = ''
)

  if ($chocolateyPackageFilePath -eq $null -or $chocolateyPackageFilePath -eq '') {
    throw "You must specify a local package to run the local install."
  }

  if (!(Test-Path($chocolateyPackageFilePath))) {
    throw "No file exists at $chocolateyPackageFilePath"
  }
  
  if ($sevenZipPath -eq $null -or $sevenZipPath -eq '') {
    throw "You must specify a path to 7zip"
  }

  if (!(Test-Path($sevenZipPath))) {
    throw "No file exists at 7zipPath"
  }

  if ($env:TEMP -eq $null) {
    $env:TEMP = Join-Path $env:SystemDrive 'temp'
  }
  $chocoTempDir = Join-Path $env:TEMP "chocolatey"
  $tempDir = Join-Path $chocoTempDir "chocoInstall"
  if (![System.IO.Directory]::Exists($tempDir)) {[System.IO.Directory]::CreateDirectory($tempDir)}
  $file = Join-Path $tempDir "chocolatey.zip"
  Copy-Item $chocolateyPackageFilePath $file -Force

  # unzip the package
  Write-Output "Extracting $file to $tempDir..."
  $params = 'x -o"{0}" -bd -y "{1}"' -f $tempDir, $file
  # use more robust Process as compared to Start-Process -Wait (which doesn't
  # wait for the process to finish in PowerShell v3)
  $process = New-Object System.Diagnostics.Process

  try {
    $process.StartInfo = New-Object System.Diagnostics.ProcessStartInfo -ArgumentList $sevenZipPath, $params
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden

    $null = $process.Start()
    $process.BeginOutputReadLine()
    $process.WaitForExit()

    $exitCode = $process.ExitCode
  }
  finally {
    $process.Dispose()
  }

    $errorMessage = "Unable to unzip package using 7zip. Error:"
    if ($exitCode -ne 0) {
    $errorDetails = switch ($exitCode) {
        1 { "Some files could not be extracted" }
        2 { "7-Zip encountered a fatal error while extracting the files" }
        7 { "7-Zip command line error" }
        8 { "7-Zip out of memory" }
        255 { "Extraction cancelled by the user" }
        default { "7-Zip signalled an unknown error (code $exitCode)" }
    }

    throw ($errorMessage, $errorDetails -join [Environment]::NewLine)
  }

  # Call chocolatey install
  Write-Output "Installing chocolatey on this machine"
  $toolsFolder = Join-Path $tempDir "tools"
  $chocoInstallPS1 = Join-Path $toolsFolder "chocolateyInstall.ps1"

  & $chocoInstallPS1

  Write-Output 'Ensuring chocolatey commands are on the path'
  $chocoInstallVariableName = "ChocolateyInstall"
  $chocoPath = [Environment]::GetEnvironmentVariable($chocoInstallVariableName)
  if ($chocoPath -eq $null -or $chocoPath -eq '') {
    $chocoPath = 'C:\ProgramData\Chocolatey'
  }

  $chocoExePath = Join-Path $chocoPath 'bin'

  if ($($env:Path).ToLower().Contains($($chocoExePath).ToLower()) -eq $false) {
    $env:Path = [Environment]::GetEnvironmentVariable('Path',[System.EnvironmentVariableTarget]::Machine);
  }
}

If (-not (Test-Path $nupkgDir)) {
    Throw "Cannot find $nupkgDir"
}

If (-not (Test-Path $7zPath)) {
    Throw "Cannot find $7zPath"
}


$nupkgPath = Get-Childitem -Path $nupkgDir | 
    ` Where-Object { $_.name -match "chocolatey.\d.*nupkg" } | 
    ` Select-Object -First 1 -ExpandProperty fullname

    
Write-Host "Installing Chocolatey from $nupkgPath"
Install-LocalChocolateyPackage -chocolateyPackageFilePath $nupkgPath -sevenZipPath $7zPath


Write-Host "Removing temporary files"
Remove-Item -Force -Recurse -EA 0 -Path $nupkgDir