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

function Install-BinFile {
<#
.SYNOPSIS
Creates a shim (or batch redirect) for a file that is on the PATH.

.DESCRIPTION
Chocolatey installs have the folder `$($env:ChocolateyInstall)\bin`
included in the PATH environment variable. Chocolatey automatically
shims executables in package folders that are not explicitly ignored,
putting them into the bin folder (and subsequently onto the PATH).

When you have other files you want to shim to add them to the PATH or
if you want to handle the shimming explicitly, use this function.

If you do use this function, ensure you also add `Uninstall-BinFile` to
your `chocolateyUninstall.ps1` script as Chocolatey will not
automatically clean up shims created with this function.

.NOTES
Not normally needed for exe files in the package folder, those are
automatically discovered and added as shims after the install script
completes.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Name
The name of the redirect file, will have .exe appended to it.

.PARAMETER Path
The path to the original file. Can be relative from
`$($env:ChocolateyInstall)\bin` back to your file or a full path to the
file.

.PARAMETER UseStart
This should be passed if the shim should not wait on the action to
complete. This is usually the case with GUI apps, you don't want the
command shell blocked waiting for the GUI app to be shut back down.

.PARAMETER Command
OPTIONAL - This is any additional command arguments you want passed
every time to the command. This is not normally used, but may be
necessary if you are calling something and then your application. For
example if you are calling Java with your JAR, the command would be the
JAR file plus any other options to start Java appropriately.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.LINK
Uninstall-BinFile

.LINK
Install-ChocolateyShortcut

.LINK
Install-ChocolateyPath
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $name,
  [parameter(Mandatory=$true, Position=1)][string] $path,
  [parameter(Mandatory=$false)]
  [alias("isGui")][switch] $useStart,
  [parameter(Mandatory=$false)][string] $command = '',
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  $nugetPath = [System.IO.Path]::GetFullPath((Join-Path "$helpersPath" '..\'))
  $nugetExePath = Join-Path "$nugetPath" 'bin'
  $packageBatchFileName = Join-Path $nugetExePath "$name.bat"
  $packageBashFileName = Join-Path $nugetExePath "$name"
  $packageShimFileName = Join-Path $nugetExePath "$name.exe"

  if (Test-Path ($packageBatchFileName)) {Remove-Item $packageBatchFileName -force}
  if (Test-Path ($packageBashFileName)) {Remove-Item $packageBashFileName -force}
  $originalPath = $path
  $path = $path.ToLower().Replace($nugetPath.ToLower(), "..\").Replace("\\","\")

  $ShimGenArgs = "-o `"$packageShimFileName`" -p `"$path`" -i `"$originalPath`""
  if ($command -ne $null -and $command -ne '') {
    $ShimGenArgs +=" -c $command"
  }

  if ($useStart) {
    $ShimGenArgs +=" -gui"
  }

  if ($debug) {
    $ShimGenArgs +=" -debug"
  }

  $ShimGen = Join-Path "$helpersPath" '..\tools\shimgen.exe'
  if (!([System.IO.File]::Exists($ShimGen))) {
	  Update-SessionEnvironment
	  $ShimGen = Join-Path "$env:ChocolateyInstall" 'tools\shimgen.exe'
  }

  $ShimGen = [System.IO.Path]::GetFullPath($ShimGen)
  Write-Debug "ShimGen found at `'$ShimGen`'"

  Write-Debug "Calling $ShimGen $ShimGenArgs"

  if (Test-Path ("$ShimGen")) {
    #Start-Process "$ShimGen" -ArgumentList "$ShimGenArgs" -Wait -WindowStyle Hidden
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = new-object System.Diagnostics.ProcessStartInfo($ShimGen, $ShimGenArgs)
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden

    $process.Start() | Out-Null
    $process.WaitForExit()
  }

  if (Test-Path ($packageShimFileName)) {
    Write-Host "Added $packageShimFileName shim pointed to `'$path`'."
  } else {
    Write-Warning "An error occurred generating shim, using old method."

    $path = "%DIR%$($path)"
    $pathBash = $path.Replace("%DIR%..\","`$DIR/../").Replace("\","/")
    Write-Host "Adding $packageBatchFileName and pointing to `'$path`'."
    Write-Host "Adding $packageBashFileName and pointing to `'$path`'."
    if ($useStart) {
      Write-Host "Setting up $name as a non-command line application."
"@echo off
SET DIR=%~dp0%
start """" ""$path"" %*" | Out-File $packageBatchFileName -encoding ASCII

      $sw = New-Object IO.StreamWriter "$packageBashFileName"
      $sw.Write("#!/bin/sh`nDIR=`${0%/*}`n""$pathBash"" ""`$@"" &`n")
      $sw.Close()
      $sw.Dispose()
    } else {

"@echo off
SET DIR=%~dp0%
cmd /c """"$path"" %*""
exit /b %ERRORLEVEL%" | Out-File $packageBatchFileName -encoding ASCII

      $sw = New-Object IO.StreamWriter "$packageBashFileName"
      $sw.Write("#!/bin/sh`nDIR=`${0%/*}`n""$pathBash"" ""`$@""`nexit `$?`n")
      $sw.Close()
      $sw.Dispose()

    }
  }
}

Set-Alias Generate-BinFile Install-BinFile
Set-Alias Add-BinFile Install-BinFile
