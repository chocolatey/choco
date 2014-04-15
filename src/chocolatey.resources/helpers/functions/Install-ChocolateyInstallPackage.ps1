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
  [string] $fileType = 'exe',
  [string] $silentArgs = '',
  [string] $file,
  $validExitCodes = @(0)
)
  Write-Debug "Running 'Install-ChocolateyInstallPackage' for $packageName with file:`'$file`', args: `'$silentArgs`', fileType: `'$fileType`', validExitCodes: `'$validExitCodes`' ";
  $installMessage = "Installing $packageName..."
  write-host $installMessage

  $ignoreFile = $file + '.ignore'
  try {
    '' | out-file $ignoreFile
  } catch {
    Write-Warning "Unable to generate `'$ignoreFile`'"
  }

  $additionalInstallArgs = $env:chocolateyInstallArguments;
  if ($additionalInstallArgs -eq $null) { $additionalInstallArgs = ''; }
  $overrideArguments = $env:chocolateyInstallOverride;

  if ($fileType -like 'msi') {
    $msiArgs = "/i `"$file`""
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
      Start-ChocolateyProcessAsAdmin "$additionalInstallArgs" $file -validExitCodes $validExitCodes
      write-host "Overriding package arguments with `'$additionalInstallArgs`'";
    } else {
      Start-ChocolateyProcessAsAdmin "$silentArgs $additionalInstallArgs" $file -validExitCodes $validExitCodes
    }
  }

  if($fileType -like 'msu') {

    if ($overrideArguments) {
      $msuArgs = "$file $additionalInstallArgs"
    } else {
      $msuArgs = "$file $silentArgs $additionalInstallArgs"
    }
    Start-ChocolateyProcessAsAdmin "$msuArgs" 'wusa.exe' -validExitCodes $validExitCodes
  }

  write-host "$packageName has been installed."
}
