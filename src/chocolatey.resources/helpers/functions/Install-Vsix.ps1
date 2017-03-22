function Install-Vsix {
<#
.SYNOPSIS
DO NOT USE. Not part of the public API.

.DESCRIPTION
Installs a VSIX package into a particular version of Visual Studio.

.NOTES
This is not part of the public API. Please use
Install-ChocolateyVsixPackage instead.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Installer
The path to the VSIX installer

.PARAMETER InstallFile
The VSIX file that is being installed.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.LINK
Install-ChocolateyVsixPackage
#>
param (
  [parameter(Mandatory=$true, Position=0)][string] $installer,
  [parameter(Mandatory=$true, Position=1)][string] $installFile,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  if ($env:chocolateyPackageName -ne $null -and $env:chocolateyPackageName -eq $env:ChocolateyInstallDirectoryPackage) {
    Write-Warning "Install Directory override not available for VSIX packages."
  }

  Write-Host "Installing $installFile using $installer"
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName=$installer
  $psi.Arguments="/q $installFile"
  $s = [System.Diagnostics.Process]::Start($psi)
  $s.WaitForExit()

  return $s.ExitCode
}
