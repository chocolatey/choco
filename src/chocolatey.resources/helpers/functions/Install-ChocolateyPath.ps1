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

function Install-ChocolateyPath {
<#
.SYNOPSIS
**NOTE:** Administrative Access Required when `-PathType 'Machine'.`

This puts a directory to the PATH environment variable.

.DESCRIPTION
Ensures that the given path is present in the given type of PATH
environment variable as well as in the current session.

.NOTES
This command will assert UAC/Admin privileges on the machine if
`-PathType 'Machine'`.

This is used when the application/tool is not being linked by Chocolatey
(not in the lib folder).

.INPUTS
None

.OUTPUTS
None

.PARAMETER PathToInstall
The exact path to ensure in the environment PATH.

.PARAMETER PathType
Which PATH to add it to. If specifying `Machine`, this requires admin
privileges to run correctly.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Install-ChocolateyPath -PathToInstall "$($env:SystemDrive)\tools\gittfs"

.EXAMPLE
Install-ChocolateyPath "$($env:SystemDrive)\Program Files\MySQL\MySQL Server 5.5\bin" -PathType 'Machine'

.LINK
Uninstall-ChocolateyPath

.LINK
Install-ChocolateyEnvironmentVariable

.LINK
Get-EnvironmentVariable

.LINK
Set-EnvironmentVariable

.LINK
Get-ToolsLocation
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $pathToInstall,
  [parameter(Mandatory=$false, Position=1)][System.EnvironmentVariableTarget] $pathType = [System.EnvironmentVariableTarget]::User,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters
  ## Called from chocolateysetup.psm1 - wrap any Write-Host in try/catch

  $paths = Parse-EnvPathList (Get-EnvironmentVariable -Name 'PATH' -Scope $pathType -PreserveVariables)
  if ((IndexOf-EnvPath $paths $pathToInstall) -eq -1) {
    try {
      Write-Host "PATH environment variable does not have $pathToInstall in it. Adding..."
    } catch {
      Write-Verbose "PATH environment variable does not have $pathToInstall in it. Adding..."
    }

    if ($pathType -eq [EnvironmentVariableTarget]::Machine -and -not (Test-ProcessAdminRights)) {
      $psArgs = "Install-ChocolateyPath -pathToInstall `'$pathToInstall`' -pathType `'$pathType`'"
      Start-ChocolateyProcessAsAdmin "$psArgs"
    } else {
      $paths += $pathToInstall
      Set-EnvironmentVariable -Name 'PATH' -Value $(Format-EnvPathList $paths) -Scope $pathType
    }
  }

  # Make change immediately available
  $paths = Parse-EnvPathList $env:PATH
  if ((IndexOf-EnvPath $paths $pathToInstall) -eq -1) {
    $paths += $pathToInstall
    $env:Path = Format-EnvPathList $paths
  }
}
