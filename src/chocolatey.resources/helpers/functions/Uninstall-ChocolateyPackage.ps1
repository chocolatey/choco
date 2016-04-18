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
Uninstalls software from "Programs and Features".

.DESCRIPTION
This will uninstall software from your machine (in Programs and
Features). This may not be necessary if Auto Uninstaller is turned on.

Choco 0.9.9+ automatically tracks registry changes for "Programs and
Features" of the underlying software's native installers when
installing packages. The "Automatic Uninstaller" (auto uninstaller)
service is a feature that can use that information to automatically
determine how to uninstall these natively installed applications. This
means that a package may not need an explicit chocolateyUninstall.ps1
to reverse the installation done in the install script.

With auto uninstaller turned off, a chocolateyUninstall.ps1 is required
to perform uninstall from "Programs and Features". In the absence of
chocolateyUninstall.ps1, choco uninstall only removes the package from
Chocolatey but does not remove the sofware from your system without
auto uninstaller.

.NOTES
May not be required. Starting in 0.9.10+, the Automatic Uninstaller
(AutoUninstaller) is turned on by default.

.INPUTS
None

.OUTPUTS
None

.PARAMETER PackageName
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

.PARAMETER FileType
This is the extension of the file. This should be either exe or msi.

.PARAMETER SilentArgs
Please include the notSilent tag in your chocolatey nuget package if you
are not setting up a silent package.

.PARAMETER File
The full path to the native uninstaller to run.

.EXAMPLE
Uninstall-ChocolateyPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

.LINK
Install-ChocolateyPackage

.LINK
Uninstall-ChocolateyZipPackage
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

    Start-ChocolateyProcessAsAdmin "$msiArgs" 'msiexec' -validExitCodes $validExitCodes
    #Start-Process -FilePath msiexec -ArgumentList $msiArgs -Wait
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
