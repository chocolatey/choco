# Copyright © 2017 Chocolatey Software, Inc.
# Copyright © 2015 - 2017 RealDimensions Software, LLC
# Copyright © 2011 - 2015 RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
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
This unzips files using the 7-zip command line tool 7z.exe.
Supported archive formats are listed at:
https://sevenzip.osdn.jp/chm/general/formats.htm
Prior to 0.9.10.1, 7za.exe was used. Supported archive formats for
7za.exe are: 7z, lzma, cab, zip, gzip, bzip2, and tar.

.INPUTS
None

.OUTPUTS
Returns the passed in $destination.

.NOTES
If extraction fails, an exception is thrown.

If you are embedding files into a package, ensure that you have the
rights to redistribute those files if you are sharing this package
publicly (like on the community feed). Otherwise, please use
Install-ChocolateyZipPackage to download those resources from their
official distribution points.

Starting in 0.9.10, will automatically call Set-PowerShellExitCode to
set the package exit code based on 7-zip's exit code.

.PARAMETER FileFullPath
This is the full path to the zip file. If embedding it in the package
next to the install script, the path will be like
`"$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\\file.zip"`

In 0.10.1+, `File` is an alias for FileFullPath.

This can be a 32-bit or 64-bit file. This is mandatory in earlier versions
of Chocolatey, but optional if FileFullPath64 has been provided.

.PARAMETER FileFullPath64
Full file path to a 64-bit native installer to run. Available in 0.10.4+.
If embedding in the package, you can get it to the path with
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\INSTALLER_FILE"`

Provide this when you want to provide both 32-bit and 64-bit
installers or explicitly only a 64-bit installer (which will cause a package 
install failure on 32-bit systems).

.PARAMETER Destination
This is a directory where you would like the unzipped files to end up.
If it does not exist, it will be created.

.PARAMETER SpecificFolder
OPTIONAL - This is a specific directory within zip file to extract.

.PARAMETER PackageName
OPTIONAL - This will faciliate logging unzip activity for subsequent
uninstalls

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
# Path to the folder where the script is executing
$toolsDir = (Split-Path -parent $MyInvocation.MyCommand.Definition)
Get-ChocolateyUnzip -FileFullPath "c:\someFile.zip" -Destination $toolsDir

.LINK
Install-ChocolateyZipPackage
#>
param(
  [alias("file")][parameter(Mandatory=$false, Position=0)][string] $fileFullPath,
  [parameter(Mandatory=$true, Position=1)][string] $destination,
  [parameter(Mandatory=$false, Position=2)][string] $specificFolder,
  [parameter(Mandatory=$false, Position=3)][string] $packageName,
  [alias("file64")][parameter(Mandatory=$false)][string] $fileFullPath64,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

   $bitnessMessage = ''
    $zipfileFullPath=$fileFullPath
  if ((Get-ProcessorBits 32) -or $env:ChocolateyForceX86 -eq 'true') {
    if (!$fileFullPath) { throw "32-bit archive is not supported for $packageName"; }
    if ($fileFullPath64) { $bitnessMessage = '32-bit '; }
  } elseif ($fileFullPath64) {
    $zipfileFullPath = $fileFullPath64
    $bitnessMessage = '64-bit '
  }
  
  if ($zipfileFullPath -eq '' -or $zipfileFullPath -eq $null) {
    throw 'Package parameters incorrect, either FileFullPath or FileFullPath64 must be specified.'
  }
  
  if ($packageName) {
    $packagelibPath = $env:ChocolateyPackageFolder
    if (!(Test-Path -path $packagelibPath)) {
      New-Item $packagelibPath -type directory
    }

    $zipFilename=split-path $zipfileFullPath -Leaf
    $zipExtractLogFullPath= Join-Path $packagelibPath $zipFilename`.txt
  }

  if ($env:chocolateyPackageName -ne $null -and $env:chocolateyPackageName -eq $env:ChocolateyInstallDirectoryPackage) {
    Write-Warning "Install Directory override not available for zip packages at this time.`n If this package also runs a native installer using Chocolatey`n functions, the directory will be honored."
  }

  Write-Host "Extracting $bitnessMessage$zipfileFullPath to $destination..."
  if (![System.IO.Directory]::Exists($destination)) { [System.IO.Directory]::CreateDirectory($destination) | Out-Null }

  $7zip = Join-Path "$helpersPath" '..\tools\7z.exe'
  if (!([System.IO.File]::Exists($7zip))) {
    Update-SessionEnvironment
    $7zip = Join-Path "$env:ChocolateyInstall" 'tools\7z.exe'
  }
  $7zip = [System.IO.Path]::GetFullPath($7zip)
  Write-Debug "7zip found at `'$7zip`'"

  # 32-bit 7z would not find C:\Windows\System32\config\systemprofile\AppData\Local\Temp,
  # because it gets translated to C:\Windows\SysWOW64\... by the WOW redirection layer.
  # Replace System32 with sysnative, which does not get redirected.
  # 32-bit 7z is required so it can see both architectures
  if ([IntPtr]::Size -ne 4) {
    $fileFullPathNoRedirection = $zipfileFullPath -ireplace ([System.Text.RegularExpressions.Regex]::Escape([Environment]::GetFolderPath('System'))),(Join-Path $Env:SystemRoot 'SysNative')
    $destinationNoRedirection = $destination -ireplace ([System.Text.RegularExpressions.Regex]::Escape([Environment]::GetFolderPath('System'))),(Join-Path $Env:SystemRoot 'SysNative')
  } else {
    $fileFullPathNoRedirection = $zipfileFullPath
    $destinationNoRedirection = $destination
  }

  $params = "x -aoa -bd -bb1 -o`"$destinationNoRedirection`" -y `"$fileFullPathNoRedirection`""
  if ($specificfolder) {
    $params += " `"$specificfolder`""
  }
  Write-Debug "Executing command ['$7zip' $params]"

  # Capture 7z's output into a StringBuilder and write it out in blocks, to improve I/O performance.
  $global:zipFileList = New-Object System.Text.StringBuilder
  $global:zipDestinationFolder = $destination

  # Redirecting output slows things down a bit.
  $writeOutput = {
    if ($EventArgs.Data -ne $null) {
      $line = $EventArgs.Data
      Write-Verbose "$line"
      if ($line.StartsWith("- ")) {
        $global:zipFileList.AppendLine($global:zipDestinationFolder + "\" + $line.Substring(2))
      }
    }
  }

  $writeError = {
    if ($EventArgs.Data -ne $null) {
      Write-Error "$($EventArgs.Data)"
    }
  }

  $process = New-Object System.Diagnostics.Process
  $process.EnableRaisingEvents = $true
  Register-ObjectEvent -InputObject $process -SourceIdentifier "LogOutput_ChocolateyZipProc" -EventName OutputDataReceived -Action $writeOutput | Out-Null
  Register-ObjectEvent -InputObject $process -SourceIdentifier "LogErrors_ChocolateyZipProc" -EventName ErrorDataReceived -Action  $writeError | Out-Null

  $process.StartInfo = new-object System.Diagnostics.ProcessStartInfo($7zip, $params)
  $process.StartInfo.RedirectStandardOutput = $true
  $process.StartInfo.RedirectStandardError = $true
  $process.StartInfo.UseShellExecute = $false
  $process.StartInfo.WorkingDirectory = Get-Location
  $process.StartInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
  $process.StartInfo.CreateNoWindow = $true

  $process.Start() | Out-Null
  if ($process.StartInfo.RedirectStandardOutput) { $process.BeginOutputReadLine() }
  if ($process.StartInfo.RedirectStandardError) { $process.BeginErrorReadLine() }
  $process.WaitForExit()

  # For some reason this forces the jobs to finish and waits for
  # them to do so. Without this it never finishes.
  Unregister-Event -SourceIdentifier "LogOutput_ChocolateyZipProc"
  Unregister-Event -SourceIdentifier "LogErrors_ChocolateyZipProc"

  # sometimes the process hasn't fully exited yet.
  for ($loopCount=1; $loopCount -le 15; $loopCount++) {
    if ($process.HasExited) { break; }
    Write-Debug "Waiting for 7z.exe process to exit - $loopCount/15 seconds";
    Start-Sleep 1;
  }

  $exitCode = $process.ExitCode
  $process.Dispose()

  Set-PowerShellExitCode $exitCode
  Write-Debug "Command ['$7zip' $params] exited with `'$exitCode`'."

  if ($zipExtractLogFullPath) {
    Set-Content $zipExtractLogFullPath $global:zipFileList.ToString() -Encoding UTF8 -Force
  }

  Write-Debug "7z exit code: $exitCode"

  $errorMessageAddendum = " This is most likely an issue with the '$env:chocolateyPackageName' package and not with Chocolatey itself. Please follow up with the package maintainer(s) directly."
  switch ($exitCode) {
    0 { break }
    1 { throw 'Some files could not be extracted.' + $errorMessageAddendum } # this one is returned e.g. for access denied errors
    2 { throw '7-Zip encountered a fatal error while extracting the files.' + $errorMessageAddendum }
    7 { throw ('7-Zip command line error.' + $errorMessageAddendum) }
    8 { throw '7-Zip out of memory.' + $errorMessageAddendum }
    255 { throw 'Extraction cancelled by the user.' + $errorMessageAddendum }
    default { throw "7-Zip signalled an unknown error (code $exitCode)"  + $errorMessageAddendum}
  }

  $env:ChocolateyPackageInstallLocation = $destination
  return $destination
}
