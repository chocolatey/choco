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

function Uninstall-BinFile {
param(
  [string] $name, 
  [string] $path
)
  Write-Debug "Running 'Uninstall-BinFile' for $name with path:`'$path`'";

  $nugetPath = [System.IO.Path]::GetFullPath((Join-Path "$helpersPath" '..\'))
  $nugetExePath = Join-Path "$nugetPath" 'bin'
  $packageBatchFileName = Join-Path $nugetExePath "$name.bat"
  $packageBashFileName = Join-Path $nugetExePath "$name"
  $packageShimFileName = Join-Path $nugetExePath "$name.exe"
  $path = $path.ToLower().Replace($nugetPath.ToLower(), "%DIR%..\").Replace("\\","\")
  $pathBash = $path.Replace("%DIR%..\","`$DIR/../").Replace("\","/")

  Write-Debug "Attempting to remove the batch and bash shortcuts: $packageBatchFileName and $packageBashFileName"

  if (Test-Path $packageBatchFileName) {
    Write-Host "Removing batch file $packageBatchFileName which pointed to `'$path`'."
    Remove-Item $packageBatchFileName
  }
  else {
    Write-Debug "Tried to remove batch file $packageBatchFileName but it was already removed."
  }

  if (Test-Path $packageBashFileName) {
    Write-Host "Removing bash file $packageBashFileName which pointed to `'$path`'."
    Remove-Item $packageBashFileName
  }
  else {
    Write-Debug "Tried to remove bash file $packageBashFileName but it was already removed."
  }

  Write-Debug "Attempting to remove the shim: $packageShimFileName"
  if (Test-Path $packageShimFileName) {
    Write-Host "Removing shim $packageShimFileName which pointed to `'$path`'."
    Remove-Item $packageShimFileName
  }
  else {
    Write-Debug "Tried to remove shim $packageShimFileName but it was already removed."
  }
}

Set-Alias Remove-BinFile Uninstall-BinFile