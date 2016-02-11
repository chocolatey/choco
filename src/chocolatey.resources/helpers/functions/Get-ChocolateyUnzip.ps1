# Copyright 2011 - Present RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

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
    $packagelibPath = $env:ChocolateyPackageFolder
    if (!(Test-Path -path $packagelibPath)) {
      New-Item $packagelibPath -type directory
    }

    $zipFilename=split-path $zipfileFullPath -Leaf
    $zipExtractLogFullPath= Join-Path $packagelibPath $zipFilename`.txt
  }

  Write-Host "Extracting $fileFullPath to $destination..."
  if (![System.IO.Directory]::Exists($destination)) {[System.IO.Directory]::CreateDirectory($destination)}

  $7zip = Join-Path "$helpersPath" '..\tools\7za.exe'
  if (!([System.IO.File]::Exists($7zip))) {
	Update-SessionEnvironment
	$7zip = Join-Path "$env:ChocolateyInstall" 'tools\7za.exe'
  }
  $7zip = [System.IO.Path]::GetFullPath($7zip)
  Write-Debug "7zip found at `'$7zip`'"

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

  # Make structures used by .NET for starting the 7z process
  
  # 7z command line
  $params = "x -aoa -o`"$destination`" -y `"$fileFullPath`""
  $process = New-Object System.Diagnostics.Process
  $process.StartInfo = new-object System.Diagnostics.ProcessStartInfo($7zip, $params)
  # Required for stdout redirect
  $process.StartInfo.UseShellExecute = $false
  $process.StartInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden

  Write-Debug "Executing command ['$7zip' $params]"
  if ($zipExtractLogFullPath) {
    # Redirect stdout for processing by choco
    $process.StartInfo.RedirectStandardOutput = $true
    [void] $process.Start()
    
    # Capture 7z's output into a StringBuilder and write it out in blocks, to improve I/O performance.
    $sb = New-Object -TypeName "System.Text.StringBuilder";
    
    # Remember whether anything was written to the file
    $written = $false
    # Open the file once
    $wStream = new-object IO.FileStream($zipExtractLogFullPath, [IO.FileAccess]::Write)
    $sWriter = new-object IO.StreamWriter($wStream)
    
    # Read each line from 7z's stdout synchroneously (ReadLine blocks).
    # Since stderr is not redirected, it gets automatically printed to the console, avoiding deadlocks.
    while(($process.StandardOutput -ne $null) -and (($line = $process.StandardOutput.ReadLine()) -ne $null)) {
      if($line.StartsWith("Extracting")) {
        # This is a line indicating an extracted file
        $file = $destination + "\" + $line.Substring(12)
        # Save the filename into the StringBuffer
        [void] $sb.Append($file + "`r`n")
        
        $written = $true;
        # Write out every 1MiB blocks, to save memory
        if ($sb.Length -ge 1048576) {
          $sWriter.Write($sb)
          [void] $sb.Clear();
        }
      }
      # Print the line, such that it looks as if stdout was not redirected
      Write-Verbose $line
    }
    # Write remaining buffered lines
    if ($sb.Length -gt 0) {
      $sWriter.Write($sb)
      [void] $sb.Clear();
    }
    # Close file
    $sWriter.Close();
    $wStream.Close();
    
    # If nothing was written at all, we don't need the file - remove it
    if (!$written) {
      Remove-Item -Force $zipExtractLogFullPath
    }
  } else {
    # If we don't want to capture the file list, just execute 7z without stdout redirection
    [void] $process.Start()
  }
  
  # Wait for 7z to finish. Even if 7z has closed its stdout, and all lines have been read, the process might not have quit yet.
  $process.WaitForExit()
  $exitCode = $process.ExitCode
  $process.Dispose()
  Write-Debug "Command ['$7zip' $params] exited with `'$exitCode`'."

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
