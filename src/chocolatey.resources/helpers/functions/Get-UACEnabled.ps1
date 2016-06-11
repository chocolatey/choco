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

function Get-UACEnabled {
<#
.SYNOPSIS
Determines if UAC (User Account Control) is turned on or off.

.DESCRIPTION
This is a low level function used by Chocolatey to decide whether
prompting for elevated privileges is necessary or not.

.NOTES
This checks the `EnableLUA` registry value to be determine the state of
a system.

.INPUTS
None

.OUTPUTS
System.Boolean
#>

  $uacRegPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"
  $uacRegValue = "EnableLUA"
  $uacEnabled = $false

  Write-Debug "Running 'Get-UACEnabled'"

  # http://msdn.microsoft.com/en-us/library/windows/desktop/ms724832(v=vs.85).aspx
  $osVersion = [Environment]::OSVersion.Version
  if ($osVersion -ge [Version]'6.0')
  {
    $uacRegSetting = Get-ItemProperty -Path $uacRegPath
    try {
      $uacValue = $uacRegSetting.EnableLUA
      if ($uacValue -eq 1) {
        $uacEnabled = $true
      }
    } catch {
      #regkey doesn't exist, so proceed with false

    }
  }

 return $uacEnabled
}
