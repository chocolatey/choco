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

function Install-BinFile {
param(
  [string] $name,
  [string] $path,
  [switch] $useStart,
  [string] $command = ''
)
  Write-Debug "Running 'Install-BinFile' for $name with path:`'$path`'|`$useStart:$useStart|`$command:$command";

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