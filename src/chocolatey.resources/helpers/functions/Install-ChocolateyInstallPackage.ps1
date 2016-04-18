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

function Install-ChocolateyInstallPackage {
<#
.SYNOPSIS
Installs software into "Programs and Features". Use
Install-ChocolateyPackage when software must be downloaded first.

.DESCRIPTION
This will run an installer (local file) on your machine.

.NOTES
If you are embedding files into a package, ensure that you have the
rights to redistribute those files if you are sharing this package
publicly (like on the community feed). Otherwise, please use
Install-ChocolateyPackage to download those resources from their
official distribution points.

This is a native installer wrapper function. A "true" package will
contain all the run time files and not an installer. That could come
pre-zipped and require unzipping in a PowerShell script. Chocolatey
works best when the packages contain the software it is managing. Most
software in the Windows world comes as installers and Chocolatey
understands how to work with that, hence this wrapper function.

.INPUTS
None

.OUTPUTS
None

.PARAMETER PackageName
The name of the package - while this is an arbitrary value, it's
recommended that it matches the package id.

.PARAMETER FileType
This is the extension of the file. This can be 'exe', 'msi', or 'msu'.
Licensed versions of Chocolatey use this to automatically determine
silent arguments. If this is not provided, Chocolatey will
automatically determine this using the downloaded file's extension.

.PARAMETER SilentArgs
OPTIONAL - These are the parameters to pass to the native installer.
Licensed versions of Chocolatey will automatically determine the
installer type and merge the arguments with what is provided here.

Try any of the to get the silent installer -
`/s /S /q /Q /quiet /silent /SILENT /VERYSILENT`. With msi it is always
`/quiet`. Please pass it in still but it will be overridden by
Chocolatey to `/quiet`. If you don't pass anything it could invoke the
installer with out any arguments. That means a nonsilent installer.

Please include the `notSilent` tag in your Chocolatey package if you
are not setting up a silent package. Please note that if you are
submitting to the community repository, it is nearly a requirement for
the package to be completely unattended.

.PARAMETER File
Full file path to native installer to run. If embedding in the package,
you can get it to the path with
`"$(Split-Path -parent $MyInvocation.MyCommand.Definition)\\INSTALLER_FILE"`

.PARAMETER UseOnlyPackageSilentArguments
Do not allow choco to provide/merge additional silent arguments and
only use the ones available with the package. Available in 0.9.10+.

.EXAMPLE
Install-ChocolateyInstallPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

.LINK
Install-ChocolateyPackage

#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $packageName,
  [parameter(Mandatory=$false, Position=1)]
  [alias("installerType","installType")][string] $fileType = 'exe',
  [parameter(Mandatory=$false, Position=2)][string] $silentArgs = '',
  [parameter(Mandatory=$true, Position=3)][string] $file,
  [parameter(Mandatory=$false)] $validExitCodes = @(0),
  [parameter(Mandatory=$false)]
  [alias("useOnlyPackageSilentArgs")][switch] $useOnlyPackageSilentArguments = $false
)
  Write-Debug "Running 'Install-ChocolateyInstallPackage' for $packageName with file:`'$file`', args: `'$silentArgs`', fileType: `'$fileType`', validExitCodes: `'$validExitCodes`', useOnlyPackageSilentArguments: '$($useOnlyPackageSilentArguments.IsPresent)'";
  $installMessage = "Installing $packageName..."
  Write-Host $installMessage

  if ($file -eq '' -or $file -eq $null) {
    throw 'Package parameters incorrect, File cannot be empty.'
  }

  if ($fileType -eq '' -or $fileType -eq $null) {
    Write-Debug 'No FileType supplied. Using the file extension to determine FileType'
    $fileType = [System.IO.Path]::GetExtension("$file").Replace(".", "")
  }
  $installerTypeLower = $fileType.ToLower()

  if ($installerTypeLower -ne 'msi' -and $installerTypeLower -ne 'exe' -and $installerTypeLower -ne 'msu') {
    Write-Warning "FileType '$fileType' is unrecognized, using 'exe' instead."
    $fileType = 'exe'
  }

  $ignoreFile = $file + '.ignore'
  try {
    '' | out-file $ignoreFile
  } catch {
    Write-Warning "Unable to generate `'$ignoreFile`'"
  }

  $additionalInstallArgs = $env:chocolateyInstallArguments;
  if ($additionalInstallArgs -eq $null) { 
    $additionalInstallArgs = ''; 
  } else {
    if ($additionalInstallArgs -match 'installdir' -or `
      $additionalInstallArgs -match 'targetdir' -or `
      $additionalInstallArgs -match 'dir\=' -or `
      $additionalInstallArgs -match '\/d\='
    ) {
@"
Pro / Business suports a single, ubiquitous install directory option.
 Stop the hassle of determining how to pass install directory overrides
 to install arguments for each package / installer type.
 Check out Pro / Business - https://bit.ly/choco_pro_business"
"@ | Write-Warning
    }
  }
  $overrideArguments = $env:chocolateyInstallOverride;

  if ($fileType -like 'msi') {
    $msiArgs = "/i `"$file`""
    if ($overrideArguments) {
      $msiArgs = "$msiArgs $additionalInstallArgs";
      Write-Host "Overriding package arguments with `'$additionalInstallArgs`'";
    } else {
      $msiArgs = "$msiArgs $silentArgs $additionalInstallArgs";
    }

    $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$msiArgs" 'msiexec' -validExitCodes $validExitCodes
    #Start-Process -FilePath msiexec -ArgumentList $msiArgs -Wait
  }

  if ($fileType -like 'exe') {
    if ($overrideArguments) {
      $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$additionalInstallArgs" $file -validExitCodes $validExitCodes
      write-host "Overriding package arguments with `'$additionalInstallArgs`'";
    } else {
      $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$silentArgs $additionalInstallArgs" $file -validExitCodes $validExitCodes
    }
  }

  if($fileType -like 'msu') {

    if ($overrideArguments) {
      $msuArgs = "$file $additionalInstallArgs"
    } else {
      $msuArgs = "$file $silentArgs $additionalInstallArgs"
    }
    $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$msuArgs" 'wusa.exe' -validExitCodes $validExitCodes
  }

  write-host "$packageName has been installed."
}
