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
