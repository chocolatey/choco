function Install-ChocolateyDesktopLink {
<#
.SYNOPSIS
This adds a shortcut on the desktop to the specified file path.

.PARAMETER TargetFilePath
This is the location to the application/executable file that you want to add a shortcut to on the desktop.  This is mandatory.

.EXAMPLE
Install-ChocolateyDesktopLink -TargetFilePath "C:\tools\NHibernatProfiler\nhprof.exe"

This will create a new Desktop Shortcut pointing at the NHibernate Profiler exe.

#>
param(
  [string] $targetFilePath
)
  Write-Debug "Running 'Install-ChocolateyDesktopLink' with targetFilePath:`'$targetFilePath`'";
  
  if(!$targetFilePath) {
    Write-ChocolateyFailure "Install-ChocolateyDesktopLink" "Missing TargetFilePath input parameter."
    return
  }
  
  if(!(Test-Path($targetFilePath))) {
    Write-ChocolateyFailure "Install-ChocolateyDesktopLink" "TargetPath does not exist, so can't create shortcut."
    return
  }

  Write-Debug "Creating Shortcut..."
  
  try {
    $desktop = $([System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::DesktopDirectory))
    $link = Join-Path $desktop "$([System.IO.Path]::GetFileName($targetFilePath)).lnk"
    $workingDirectory = $([System.IO.Path]::GetDirectoryName($targetFilePath))

    $wshshell = New-Object -ComObject WScript.Shell
    $lnk = $wshshell.CreateShortcut($link)
    $lnk.TargetPath = $targetFilePath
    $lnk.WorkingDirectory = $workingDirectory
    $lnk.Save()

    Write-Debug "Desktop Shortcut created pointing at `'$targetFilePath`'."

    Write-ChocolateySuccess "Install-ChocolateyShortcut completed"
  }	
  catch {
    Write-ChocolateyFailure "Install-ChocolateyDesktopLink" "There were errors attempting to create shortcut. The error message was '$_'."
  }	
}