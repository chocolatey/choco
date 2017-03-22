# Copyright © 2011 - Present RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
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

function Uninstall-ChocolateyZipPackage {
<#
.SYNOPSIS
Uninstalls a previous installed zip package, may not be necessary.

.DESCRIPTION
This will uninstall a zip file if installed via Install-ChocolateyZipPackage.
This is not necessary if the files are unzipped to the package directory.

.NOTES
Not necessary if files are unzippped to package directory.

This helper reduces the number of lines one would have to remove the
files extracted from a previously installed zip file.
This method has error handling built into it.

.INPUTS
None

.OUTPUTS
None

.PARAMETER PackageName
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

.PARAMETER ZipFileName
This is the zip filename originally installed.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Uninstall-ChocolateyZipPackage '__NAME__' 'filename.zip'

.LINK
Install-ChocolateyZipPackage

.LINK
Uninstall-ChocolateyPackage
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $packageName,
  [parameter(Mandatory=$true, Position=1)][string] $zipFileName,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  $packagelibPath=$env:chocolateyPackageFolder
  $zipContentFile=(join-path $packagelibPath $zipFileName) + ".txt"
  if ((Test-Path -path $zipContentFile)) {
    $zipContentFile
    $zipContents=get-content $zipContentFile
    foreach ($fileInZip in $zipContents) {
      if ($fileInZip -ne $null -and $fileInZip.Trim() -ne '') {
        Remove-Item -Path "$fileInZip" -ErrorAction SilentlyContinue -Recurse -Force
      }
    }
  }
}
