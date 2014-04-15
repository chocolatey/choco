function UnInstall-ChocolateyZipPackage {
<#
.SYNOPSIS
UnInstalls a previous installed zip package

.DESCRIPTION
This will uninstall a zip file if installed via Install-ChocolateyZipPackage

.PARAMETER PackageName
The name of the package the zip file is associated with

.PARAMETER ZipFileName
This is the zip filename originally installed.

.EXAMPLE
UnInstall-ChocolateyZipPackage '__NAME__' 'filename.zip' 

.OUTPUTS
None

.NOTES
This helper reduces the number of lines one would have to remove the files extracted from a previously installed zip file.
This method has error handling built into it.

  
#>
param(
  [string] $packageName, 
  [string] $zipFileName
)
  Write-Debug "Running 'UnInstall-ChocolateyZipPackage' for $packageName $zipFileName "
  
  try {
    $packagelibPath=$env:chocolateyPackageFolder
    $zipContentFile=(join-path $packagelibPath $zipFileName) + ".txt"
    $zipContentFile
    $zipContents=get-content $zipContentFile
    foreach ($fileInZip in $zipContents) {
      remove-item "$fileInZip" -ErrorAction SilentlyContinue
    }

    Write-ChocolateySuccess $packageName
  } catch {
      Write-ChocolateyFailure $packageName $($_.Exception.Message)
    throw 
  }
}