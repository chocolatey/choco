$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"
$zipFileLocation = Join-Path $toolsDir "test.zip"
$pp = Get-PackageParameters
if ($pp['Destination']) {
  $destinationPath = $pp['Destination']
} else {
  $destinationPath = $env:TEMP
}

Get-ChocolateyUnzip -FileFullPath $zipFileLocation -Destination $destinationPath
