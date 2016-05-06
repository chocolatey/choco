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

function Uninstall-ChocolateyPackage {
<#
.SYNOPSIS
Uninstalls a package

.DESCRIPTION
This will uninstall a package on your machine.

.PARAMETER PackageName
The name of the package 

.PARAMETER FileType
This is the extension of the file. This should be either exe or msi.

.PARAMETER SilentArgs
Please include the notSilent tag in your chocolatey nuget package if you are not setting up a silent package.

.PARAMETER File
The full path to the native uninstaller to run

.EXAMPLE
Uninstall-ChocolateyPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

.OUTPUTS
None

.NOTES
This helper reduces the number of lines one would have to write to run an uninstaller to 1 line.
There is no error handling built into this method.

.LINK
Uninstall-ChocolateyPackage
#>
param(
  [string] $packageName, 
  [alias("installerType")][string] $fileType = 'exe',
  [string] $silentArgs = '',
  [string] $file,
  $validExitCodes = @(0)
)
  Write-Debug "Running 'Uninstall-ChocolateyPackage' for $packageName with fileType:`'$fileType`', silentArgs: `'$silentArgs`', file: `'$file`'";
  
  $installMessage = "Uninstalling $packageName..."
  write-host $installMessage

  $additionalInstallArgs = $env:chocolateyInstallArguments;
  if ($additionalInstallArgs -eq $null) { $additionalInstallArgs = ''; }
  $overrideArguments = $env:chocolateyInstallOverride;
    
  if ($fileType -like 'msi') {
    $msiArgs = "/x" 
    if ($overrideArguments) { 
      $msiArgs = "$msiArgs $additionalInstallArgs";
      write-host "Overriding package arguments with `'$additionalInstallArgs`'";
    } else {
      $msiArgs = "$msiArgs $silentArgs $additionalInstallArgs";
    }

    Start-ChocolateyProcessAsAdmin "$msiArgs" "$($env:SystemRoot)\System32\msiexec.exe" -validExitCodes $validExitCodes
  }
  if ($fileType -like 'exe') {
    if ($overrideArguments) {
      Write-Host "Overriding package arguments with `'$additionalInstallArgs`'";
      Start-ChocolateyProcessAsAdmin "$additionalInstallArgs" $file -validExitCodes $validExitCodes
    } else {
      Start-ChocolateyProcessAsAdmin "$silentArgs $additionalInstallArgs" $file -validExitCodes $validExitCodes
    }
  }

  write-host "$packageName has been uninstalled."
  #cutStart-Sleep 3
}