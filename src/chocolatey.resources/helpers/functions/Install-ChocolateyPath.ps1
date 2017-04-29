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
Looks at both PATH environment variables to ensure a path variable
correctly shows up on the right PATH.

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
The full path to a location to add / ensure is in the PATH.

.PARAMETER PathType
Which PATH to add it to. If specifying `Machine`, this requires admin
privileges to run correctly.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Install-ChocolateyPath -PathToInstall "$($env:SystemDrive)\tools\gittfs"

.EXAMPLE
Install-ChocolateyPath "$($env:SystemDrive)\Program Files\MySQL\MySQL Server 5.5\bin" -PathType 'Machine'

.EXAMPLE
Install-ChocolateyPath "%ANDROID_HOME%\Tools" -PathType 'Machine'

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
  [parameter(Mandatory=$false, Position=2)][switch] $Force = $false,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)
  Set-StrictMode -Version 2
  #start over as admin as needed
  if (($pathType -eq [System.EnvironmentVariableTarget]::Machine) -and (-not (Test-ProcessAdminRights))) {
    $psArgs = "Install-ChocolateyPath -pathToInstall `'$pathToInstall`' -pathType `'$pathType`'"
    Start-ChocolateyProcessAsAdmin "$psArgs"
  }

  #$pathToInstall is not an existing directory
  $pathToInstall_ExpandedNormalized = [System.Environment]::ExpandEnvironmentVariables($pathToInstall).TrimEnd([System.IO.Path]::DirectorySeparatorChar)
  if (![System.IO.Directory]::Exists($pathToInstall_ExpandedNormalized) -and (!$Force)) {
      Write-Debug "$pathToInstall is not an existing directory.  Exiting. (use -Force to add anyway)"
      return
  }

  Write-Host "Running 'Install-ChocolateyPath' with pathToInstall:`'$pathToInstall`'";

  #
  $envPATH = Get-EnvironmentVariable -Name "PATH" -Scope ([System.EnvironmentVariableTarget]::Process) -PreserveVariables $false
  #bug in get-Environmentvariable (for process) forces me to expand myself.  Also, Get-Env can return null.
  $envPATH = [System.Environment]::ExpandEnvironmentVariables("" + $envPATH) 
  #envPATH with fully expanded variables and no traling PathSeparators
  $envPATH_ExpandedNormalized = @($envPATH.Split([System.IO.Path]::PathSeparator).ForEach({$_.TrimEnd([System.IO.Path]::DirectorySeparatorChar)}))
  #is pathToInstall already in the environment PATH variable?
  if ($pathToInstall_ExpandedNormalized -notin $envPATH_ExpandedNormalized)
  {
    Write-Host "PATH environment variable does not have $pathToInstall in it. Adding..."
    $newPath = $pathToInstall + [System.IO.Path]::PathSeparator + (Get-EnvironmentVariable -Name 'Path' -Scope $pathType)
    
    Set-EnvironmentVariable -Name 'PATH' -Value $newPath -Scope $pathType
    Update-SessionEnvironment
  }
}
