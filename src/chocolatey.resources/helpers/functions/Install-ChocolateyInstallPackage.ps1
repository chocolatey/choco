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
Installs a package

.DESCRIPTION
This will run an installer (local file) on your machine.

.PARAMETER PackageName
The name of the package - this is arbitrary, call it whatever you want.
It's recommended you call it the same as your nuget package id.

.PARAMETER FileType
This is the extension of the file. This should be either exe or msi.

.PARAMETER SilentArgs
OPTIONAL - These are the parameters to pass to the native installer.
Try any of these to get the silent installer - /s /S /q /Q /quiet /silent /SILENT /VERYSILENT
With msi it is always /quiet.
If you don't pass anything it will invoke the installer with out any arguments. That means a nonsilent installer.

Please include the notSilent tag in your chocolatey nuget package if you are not setting up a silent package.

.PARAMETER File
The full path to the native installer to run

.EXAMPLE
Install-ChocolateyInstallPackage '__NAME__' 'EXE_OR_MSI' 'SILENT_ARGS' 'FilePath'

.OUTPUTS
None

.NOTES
This helper reduces the number of lines one would have to write to run an installer to 1 line.
There is no error handling built into this method.

.LINK
Install-ChocolateyPackage
#>
param(
  [string] $packageName,
  [alias("installerType")][string] $fileType = 'exe',
  [string] $silentArgs = '',
  [string] $file,
  $validExitCodes = @(0),
  [switch] $onlyUsePackageSilentArgs
)
  Write-Debug "Running 'Install-ChocolateyInstallPackage' for $packageName with file:`'$file`', args: `'$silentArgs`', fileType: `'$fileType`', validExitCodes: `'$validExitCodes`' ";
  $installMessage = "Installing $packageName..."
  Write-Host $installMessage

  if ($file -eq '' -or $file -eq $null) {
    throw 'Package parameters incorrect, File cannot be empty.'
  }
  if ($fileType -eq '' -or $fileType -eq $null) {
    throw 'Package parameters incorrect, FileType cannot be empty.'
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
Pro / Business supports a single, ubiquitous install directory option.
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

    $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$msiArgs" "$($env:SystemRoot)\System32\msiexec.exe" -validExitCodes $validExitCodes
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
    $env:ChocolateyExitCode = Start-ChocolateyProcessAsAdmin "$msuArgs" "$($env:SystemRoot)\System32\wusa.exe" -validExitCodes $validExitCodes
  }

  write-host "$packageName has been installed."
}
