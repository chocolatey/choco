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
  [string] $fileType = 'exe',
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
      Start-ChocolateyProcessAsAdmin "$additionalInstallArgs" $file -validExitCodes $validExitCodes
      write-host "Overriding package arguments with `'$additionalInstallArgs`'";
    } else {
      Start-ChocolateyProcessAsAdmin "$silentArgs $additionalInstallArgs" $file -validExitCodes $validExitCodes
    }
  }

  write-host "$packageName has been uninstalled."
  #cutStart-Sleep 3
}