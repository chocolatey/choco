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

function Set-EnvironmentVariable([string] $Name, [string] $Value, [System.EnvironmentVariableTarget] $Scope) {
	Write-Debug "Calling Set-EnvironmentVariable with `$Name = '$Name', `$Value = '$Value', `$Scope = '$Scope'"

  if ($Scope -eq [System.EnvironmentVariableTarget]::Process) {
    return [Environment]::SetEnvironmentVariable($Name, $Value, $Scope)
  }

  [string]$keyHive = 'HKEY_LOCAL_MACHINE'
  [string]$registryKey = "SYSTEM\CurrentControlSet\Control\Session Manager\Environment\"
  [Microsoft.Win32.RegistryKey] $win32RegistryKey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($registryKey)
  if ($Scope -eq [System.EnvironmentVariableTarget]::User) {
    $keyHive = 'HKEY_CURRENT_USER'
    $registryKey = "Environment"
    [Microsoft.Win32.RegistryKey] $win32RegistryKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($registryKey)
  }
  
  [Microsoft.Win32.RegistryValueKind]$registryType = [Microsoft.Win32.RegistryValueKind]::String
  try { 
    $registryType = $reg.GetValueKind($Name)
  } catch { 
    # the value doesn't yet exist
    # move along, nothing to see here
  }
  Write-Debug "Registry type for $Name is/will be $registryType"

  if ($Name -eq 'PATH') {
    $registryType = [Microsoft.Win32.RegistryValueKind]::ExpandString
  }

  [Microsoft.Win32.Registry]::SetValue($keyHive + "\" + $registryKey, $Name, $Value, $registryType)
}
