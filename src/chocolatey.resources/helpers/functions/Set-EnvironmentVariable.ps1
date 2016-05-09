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

  if ($Scope -eq [System.EnvironmentVariableTarget]::Process -or $Value -eq $null -or $Value -eq '') {
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

  try {
    # make everything refresh
    # because sometimes explorer.exe just doesn't get the message that things were updated.
    if (-not ("win32.nativemethods" -as [type])) {
        # import sendmessagetimeout from win32
        add-type -Namespace Win32 -Name NativeMethods -MemberDefinition @"
[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
public static extern IntPtr SendMessageTimeout(
    IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam,
    uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);
"@
    }

    $HWND_BROADCAST = [intptr]0xffff;
    $WM_SETTINGCHANGE = 0x1a;
    $result = [uintptr]::zero

    # notify all windows of environment block change
    [win32.nativemethods]::SendMessageTimeout($HWND_BROADCAST, $WM_SETTINGCHANGE,  [uintptr]::Zero, "Environment", 2, 5000, [ref]$result) | Out-Null

    # Set a user environment variable making the system refresh
    $setx = "$($env:SystemRoot)\System32\setx.exe"
    & "$setx" ChocolateyLastPathUpdate `"$(Get-Date -UFormat %c)`" | Out-Null

  } catch {
    Write-Warning "Failure attempting to let Explorer know about updated environment settings.`n  $($_.Exception.Message)"
  }

  Update-SessionEnvironment
}
