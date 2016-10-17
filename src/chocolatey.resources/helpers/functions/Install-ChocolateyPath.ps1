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

function Install-ChocolateyPath {
<#
.SYNOPSIS
**NOTE:** Administrative Access Required when `-PathType 'Machine'.`

This puts a directory to the PATH environment variable of the requested scope (Machine or User).

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
  [parameter(Mandatory=$true, Position=0)][alias("Path")][string] $pathToInstall,
  [parameter(Mandatory=$false, Position=1)][alias("Scope")][ValidateSet('User','Machine')][System.EnvironmentVariableTarget] $pathType = [System.EnvironmentVariableTarget]::User,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)
  Write-Debug "Running 'Install-ChocolateyPath' with pathToInstall:`'$pathToInstall`'";
  Write-Output "Only evaluating and updating path scope `"$pathType`", path will not be assessed nor added for other scope, so path may exist in other scope as well."
  $pathToInstall = $pathToInstall.trimend('\')
  $originalPathToInstall = $pathToInstall
  #array drops blanks (one of which is always created by final semi-colon)
  $actualPathArray = (Get-EnvironmentVariable -Name 'Path' -Scope $pathType -PreserveVariables).split(';',[System.StringSplitOptions]::RemoveEmptyEntries)
  #checks for match with and without trailing slash
  if (($actualpathArray -inotcontains $pathToInstall.ToLower()) -AND ($actualpathArray -inotcontains "$(($pathToInstall + '\').ToLower())"))
  {
    Write-Host "PATH environment variable for scope `"$pathType`" does not contain `"$pathToInstall`". Adding..."

    $actualPathArray += $pathToInstall
    $actualPath = ($actualPathArray -join(';')) + ';'

    if ($pathType -eq [System.EnvironmentVariableTarget]::Machine) {
      if (Test-ProcessAdminRights) {
        Set-EnvironmentVariable -Name 'Path' -Value $actualPath -Scope $pathType
      } else {
        $psArgs = "Install-ChocolateyPath -pathToInstall `'$originalPathToInstall`' -pathType `'$pathType`'"
        Start-ChocolateyProcessAsAdmin "$psArgs"
      }
    } else {
      Set-EnvironmentVariable -Name 'Path' -Value $actualPath -Scope $pathType
    }

    #add it to the local path as well so users will be off and running
    Update-SessionEnvironment
  }
  else
  {
    Write-Host "PATH environment variable for scope `"$pathType`" already contains `"$pathToInstall`". NOT Adding..."
  }
}
