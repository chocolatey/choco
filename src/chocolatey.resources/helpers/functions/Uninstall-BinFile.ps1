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

function Uninstall-BinFile {
<#
.SYNOPSIS
Removes a shim (or batch redirect) for a file.

.DESCRIPTION
Chocolatey installs have the folder `$($env:ChocolateyInstall)\bin`
included in the PATH environment variable. Chocolatey automatically
shims executables in package folders that are not explicitly ignored,
putting them into the bin folder (and subsequently onto the PATH).

When you have other files you have shimmed, you need to use this
function to remove them from the bin folder.

.NOTES
Not normally needed for exe files in the package folder, those are
automatically discovered and the shims removed.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Name
The name of the redirect file without ".exe" appended to it.

.PARAMETER Path
The path to the original file. Can be relative from
`$($env:ChocolateyInstall)\bin` back to your file or a full path to the
file.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.LINK
Install-BinFile
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $name,
  [parameter(Mandatory=$false, Position=1)][string] $path,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

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
