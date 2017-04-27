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

If what is provided is empty or null, Chocolatey will use 'exe'
starting in 0.10.1.

.PARAMETER SilentArgs
OPTIONAL - These are the parameters to pass to the native uninstaller,
including any arguments to make the uninstaller silent/unattended.
Licensed editions of Chocolatey will automatically determine the
installer type and merge the arguments with what is provided here.

Try any of the to get the silent (unattended) uninstaller -
`/s /S /q /Q /quiet /silent /SILENT /VERYSILENT`. With msi it is always
`/quiet`. Please pass it in still but it will be overridden by
Chocolatey to `/quiet`. If you don't pass anything it could invoke the
installer with out any arguments. That means a nonsilent installer.

Please include the `notSilent` tag in your Chocolatey package if you
are not setting up a silent/unattended package. Please note that if you
are submitting to the community repository, it is nearly a requirement
for the package to be completely unattended.

.PARAMETER File
The full path to the native uninstaller to run.

.PARAMETER ValidExitCodes
Array of exit codes indicating success. Defaults to `@(0)`.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Uninstall-ChocolateyPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

.EXAMPLE
>
    Uninstall-ChocolateyPackage -PackageName $packageName `
                                -FileType $installerType `
                                -SilentArgs "$silentArgs" `
                                -ValidExitCodes $validExitCodes `
                                -File "$file"

.LINK
Install-ChocolateyPackage

.LINK
Install-ChocolateyInstallPackage

.LINK
Uninstall-ChocolateyZipPackage

.LINK
Get-UninstallRegistryKey
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $packageName,
  [parameter(Mandatory=$false, Position=1)]
  [alias("installerType")][string] $fileType = 'exe',
  [parameter(Mandatory=$false, Position=2)][string[]] $silentArgs = '',
  [parameter(Mandatory=$false, Position=3)][string] $file,
  [parameter(Mandatory=$false)] $validExitCodes = @(0),
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)
  [string]$silentArgs = $silentArgs -join ' '

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  $installMessage = "Uninstalling $packageName..."
  write-host $installMessage

  $additionalInstallArgs = $env:chocolateyInstallArguments;
  if ($additionalInstallArgs -eq $null) { $additionalInstallArgs = ''; }
  $overrideArguments = $env:chocolateyInstallOverride;

  if ($fileType -eq $null) { $fileType = '' }
  $installerTypeLower = $fileType.ToLower()
  if ($installerTypeLower -ne 'msi' -and $installerTypeLower -ne 'exe') {
    Write-Warning "FileType '$fileType' is unrecognized, using 'exe' instead."
    $fileType = 'exe'
  }

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

  Write-Host "$packageName has been uninstalled."
}
