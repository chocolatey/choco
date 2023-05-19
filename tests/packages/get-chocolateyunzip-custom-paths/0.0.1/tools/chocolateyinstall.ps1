$toolsDir = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$zipFileLocation = Join-Path $toolsDir "test.zip"
$pp = Get-PackageParameters
if ($pp['Destination']) {
    $destinationPath = $pp['Destination']
}
else {
    $destinationPath = $env:TEMP
}

Get-ChocolateyUnzip -fileFullPath $zipFileLocation -destination $destinationPath
