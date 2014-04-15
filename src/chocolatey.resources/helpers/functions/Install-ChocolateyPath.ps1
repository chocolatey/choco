function Install-ChocolateyPath {
param(
  [string] $pathToInstall,
  [System.EnvironmentVariableTarget] $pathType = [System.EnvironmentVariableTarget]::User
)
  Write-Debug "Running 'Install-ChocolateyPath' with pathToInstall:`'$pathToInstall`'";

  #get the PATH variable
  $envPath = $env:PATH
  #$envPath = [Environment]::GetEnvironmentVariable('Path', $pathType)
  if (!$envPath.ToLower().Contains($pathToInstall.ToLower()))
  {
    Write-Host "PATH environment variable does not have $pathToInstall in it. Adding..."
    $actualPath = [Environment]::GetEnvironmentVariable('Path', $pathType)

    $statementTerminator = ";"
    #does the path end in ';'?
    $hasStatementTerminator = $actualPath -ne $null -and $actualPath.EndsWith($statementTerminator)
    # if the last digit is not ;, then we are adding it
    If (!$hasStatementTerminator -and $actualPath -ne $null) {$pathToInstall = $statementTerminator + $pathToInstall}
    if (!$pathToInstall.EndsWith($statementTerminator)) {$pathToInstall = $pathToInstall + $statementTerminator}
    $actualPath = $actualPath + $pathToInstall

    if ($pathType -eq [System.EnvironmentVariableTarget]::Machine) {
      $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
      $UACEnabled = Get-UACEnabled
      if ($currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator) -and !$UACEnabled) {
        [Environment]::SetEnvironmentVariable('Path', $actualPath, $pathType)
      } else {
        $psArgs = "[Environment]::SetEnvironmentVariable('Path',`'$actualPath`', `'$pathType`')"
        Start-ChocolateyProcessAsAdmin "$psArgs"
      }
    } else {
      [Environment]::SetEnvironmentVariable('Path', $actualPath, $pathType)
    }

    #add it to the local path as well so users will be off and running
    $envPSPath = $env:PATH
    $env:Path = $envPSPath + $statementTerminator + $pathToInstall
  }
}
