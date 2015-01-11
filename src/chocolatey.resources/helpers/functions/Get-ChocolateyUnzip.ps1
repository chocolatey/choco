function Get-ChocolateyUnzip {
<#
.SYNOPSIS
Unzips an archive file and returns the location for further processing.

.DESCRIPTION
This unzips files using the 7-zip standalone command line tool 7za.exe.
Supported archive formats are: 7z, lzma, cab, zip, gzip, bzip2, Z and tar.

.PARAMETER FileFullPath
This is the full path to your zip file.

.PARAMETER Destination
This is a directory where you would like the unzipped files to end up.
If it does not exist, it will be created.

.PARAMETER SpecificFolder
OPTIONAL - This is a specific directory within zip file to extract.

.PARAMETER PackageName
OPTIONAL - This will faciliate logging unzip activity for subsequent uninstall

.EXAMPLE
$scriptPath = (Split-Path -parent $MyInvocation.MyCommand.Definition)
Get-ChocolateyUnzip "c:\someFile.zip" $scriptPath somedirinzip\somedirinzip

.OUTPUTS
Returns the passed in $destination.

.NOTES
This helper reduces the number of lines one would have to write to unzip a file to 1 line.
If extraction fails, an exception is thrown.

#>
param(
  [string] $fileFullPath,
  [string] $destination,
  [string] $specificFolder,
  [string] $packageName
)
  $zipfileFullPath=$fileFullPath
  if ($specificfolder) {
    $fileFullPath=join-path $fileFullPath $specificFolder
  }

  Write-Debug "Running 'Get-ChocolateyUnzip' with fileFullPath:`'$fileFullPath`'', destination: `'$destination`', specificFolder: `'$specificFolder``, packageName: `'$packageName`'";

  if ($packageName) {
    $packagelibPath=$env:chocolateyPackageFolder
    if (!(Test-Path -path $packagelibPath)) {
      New-Item $packagelibPath -type directory
    }

    $zipFilename=split-path $zipfileFullPath -Leaf
    $zipExtractLogFullPath=join-path $packagelibPath $zipFilename`.txt
  }

  Write-Host "Extracting $fileFullPath to $destination..."
  if (![System.IO.Directory]::Exists($destination)) {[System.IO.Directory]::CreateDirectory($destination)}

  Update-SessionEnvironment
  # On first install, env:ChocolateyInstall might be null still - join-path has issues
  $7zip = Join-Path "$env:ALLUSERSPROFILE" 'chocolatey\chocolateyinstall\tools\7za.exe'
  if ($env:ChocolateyInstall){
    $7zip = Join-Path "$env:ChocolateyInstall" 'chocolateyinstall\tools\7za.exe'
  }

  # 32-bit 7za.exe would not find C:\Windows\System32\config\systemprofile\AppData\Local\Temp,
  # because it gets translated to C:\Windows\SysWOW64\... by the WOW redirection layer.
  # Replace System32 with sysnative, which does not get redirected.
  if ([IntPtr]::Size -ne 4) {
    $fileFullPath32 = $fileFullPath -ireplace ([regex]::Escape([Environment]::GetFolderPath('System'))),(Join-Path $Env:SystemRoot sysnative)
    $destination32 = $destination -ireplace ([regex]::Escape([Environment]::GetFolderPath('System'))),(Join-Path $Env:SystemRoot sysnative)
  } else {
    $fileFullPath32 = $fileFullPath
    $destination32 = $destination
  }

  $exitCode = -1
  $unzipOps = {
    param($7zip, $destination, $fileFullPath, [ref]$exitCodeRef)
    $process = Start-Process $7zip -ArgumentList "x -o`"$destination`" -y `"$fileFullPath`"" -Wait -WindowStyle Hidden -PassThru
    # this is here for specific cases in Posh v3 where -Wait is not honored
    try { if (!($process.HasExited)) { Wait-Process -Id $process.Id } } catch { }

    $exitCodeRef.Value = $process.ExitCode
  }

  if ($zipExtractLogFullPath) {
    Write-Debug "wrapping 7za invocation with Write-FileUpdateLog"
    Write-FileUpdateLog -logFilePath $zipExtractLogFullPath -locationToMonitor $destination -scriptToRun $unzipOps -argumentList $7zip,$destination32,$fileFullPath32,([ref]$exitCode)
  } else {
    Write-Debug "calling 7za directly"
    Invoke-Command $unzipOps -ArgumentList $7zip,$destination32,$fileFullPath32,([ref]$exitCode)
  }

  Write-Debug "7za exit code: $exitCode"
  switch ($exitCode) {
    0 { break }
    1 { throw 'Some files could not be extracted' } # this one is returned e.g. for access denied errors
    2 { throw '7-Zip encountered a fatal error while extracting the files' }
    7 { throw '7-Zip command line error' }
    8 { throw '7-Zip out of memory' }
    255 { throw 'Extraction cancelled by the user' }
    default { throw "7-Zip signalled an unknown error (code $exitCode)" }
  }

  return $destination
}
