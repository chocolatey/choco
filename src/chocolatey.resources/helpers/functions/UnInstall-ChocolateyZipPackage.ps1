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
  
  $packagelibPath=$env:chocolateyPackageFolder
  $zipContentFile=(join-path $packagelibPath $zipFileName) + ".txt"
  if ((Test-Path -path $zipContentFile)) {
    $zipContentFile
    $zipContents=get-content $zipContentFile
    foreach ($fileInZip in $zipContents) {
      remove-item -Path "$fileInZip" -ErrorAction SilentlyContinue -Recurse -Force
    }
  }
}
