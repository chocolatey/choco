function Write-FileUpdateLog {
  param (
    [string] $logFilePath,
    [string] $locationToMonitor,
    [scriptblock] $scriptToRun
  )
  Write-Debug "Running 'Write-FileUpdateLog' with logFilePath:`'$logFilePath`'', locationToMonitor:$locationToMonitor, Operation: `'$scriptToRun`'";

  Write-Debug "Tracking current state of `'$locationToMonitor`'"
  $originalContents = Get-ChildItem -Recurse $locationToMonitor | Select-Object LastWriteTimeUTC,FullName,Length

  & $scriptToRun

  $newContents = Get-ChildItem -Recurse $locationToMonitor | Select-Object LastWriteTimeUTC,FullName,Length

  if($originalContents -eq $null) {$originalContents = @()}
  if($newContents -eq $null) {$newContents = @()}

  $changedFiles = Compare-Object $originalContents $newContents -Property LastWriteTimeUtc,FullName,Length -PassThru | Group-Object FullName

  #log modified files
  $changedFiles | ? {$_.Count -gt 1} | % {$_.Name} | Add-Content $logFilePath
 
  #log added files
  $addOrDelete = $changedFiles | ? { $_.Count -eq 1 } | % {$_.Group}
  $addOrDelete | ? {$_.SideIndicator -eq "=>"} | % {$_.FullName} | Add-Content $logFilePath

  #log deleted files
  #$addOrDelete | ? {$_.SideIndicator -eq "<="} | % {$_.FullName} | Add-Content $logFilePath
}