function Install-ChocolateyDesktopLink {
param(
  [string] $targetFilePath
)
  Write-Debug "Running 'Install-ChocolateyDesktopLink' with targetFilePath:`'$targetFilePath`'";
  
  if (test-path($targetFilePath)) {
    $desktop = $([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::DesktopDirectory))
    $link = Join-Path $desktop "$([System.IO.Path]::GetFileName($targetFilePath)).lnk"
    $workingDirectory = $([System.IO.Path]::GetDirectoryName($targetFilePath))
    
    $wshshell = New-Object -ComObject WScript.Shell
    $lnk = $wshshell.CreateShortcut($link )
    $lnk.TargetPath = $targetFilePath
    $lnk.WorkingDirectory = $workingDirectory
    $lnk.Save()
    Write-Host "`'$targetFilePath`' has been linked as a shortcut on your desktop"
  } else {
    $errorMessage = "`'$targetFilePath`' does not exist, not able to create a link"
    Write-Error $errorMessage
    throw $errorMessage
  }
}