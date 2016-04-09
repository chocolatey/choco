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

function Get-EnvironmentVariable {
<#
.SYNOPSIS
Gets an Environment Variable.

.DESCRIPTION
This will will get an environment variable based on the variable name and scope while accounting whether to expand the variable or not (e.g.: %TEMP% -> C:\User\Username\AppData\Local\Temp).

.PARAMETER Name
The environemnt variable you want to get the value from.

.PARAMETER Scope
The environemnt variable target scope.

.PARAMETER PreserveVariables
A switch parameter stating whether you want to expand the variables or not. Defaults to false.

.EXAMPLE
Get-EnvironmentVariable 'TEMP' User -PreserveVariables

.NOTES
This helper reduces the number of lines one would have to write to get environment variables, mainly when not expanding the variables is a must.
#>
[CmdletBinding()]
[OutputType([string])]
param(
  [Parameter(Mandatory=$true)]
  [string] $Name, 
  [Parameter(Mandatory=$true)]
  [System.EnvironmentVariableTarget] $Scope, 
  [Parameter(Mandatory=$false)]
  [switch] $PreserveVariables = $false
)
  [string] $MACHINE_ENVIRONMENT_REGISTRY_KEY_NAME = "SYSTEM\CurrentControlSet\Control\Session Manager\Environment\";
  [Microsoft.Win32.RegistryKey] $win32RegistryKey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($MACHINE_ENVIRONMENT_REGISTRY_KEY_NAME)
  if ($Scope -eq [System.EnvironmentVariableTarget]::User) {
    [string] $USER_ENVIRONMENT_REGISTRY_KEY_NAME = "Environment";
    [Microsoft.Win32.RegistryKey] $win32RegistryKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($USER_ENVIRONMENT_REGISTRY_KEY_NAME)
  } elseif ($Scope -eq [System.EnvironmentVariableTarget]::Process) {
    [Environment]::GetEnvironmentVariable($Name, $Scope)
  }

  [Microsoft.Win32.RegistryValueOptions] $registryValueOptions = [Microsoft.Win32.RegistryValueOptions]::None

  if ($PreserveVariables) {
    Write-Verbose "Choosing not to expand environment names"
    $registryValueOptions = [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames
  }

  [string] $environmentVariableValue = [string]::Empty

  try {
    Write-Verbose "Getting environment variable $Name"
    $environmentVariableValue = $win32RegistryKey.GetValue($Name, [string]::Empty, $registryValueOptions)
  } catch {
    Write-Debug "Unable to retrieve the $Name environment variable. Details: $_"
  } finally {
    $win32RegistryKey.Close()
  }

  return $environmentVariableValue
}