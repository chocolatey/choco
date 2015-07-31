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

function Get-EnvironmentVariable([string] $Name, [System.EnvironmentVariableTarget] $Scope, [bool] $PreserveVariables = $False) {
    if ($pathType -eq [System.EnvironmentVariableTarget]::Machine) {
        $reg = [Microsoft.Win32.Registry]::Machine.OpenSubKey("Environment", $true)
    } else {
        $reg = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey("Environment", $true)
    }

    if ($PreserveVariables -eq $True) {
        $option = [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames
    } else {
        $option = [Microsoft.Win32.RegistryValueOptions]::None
    }

    $value = $reg.GetValue('Path', $null, $option)

    $reg.Close()

    return $value
}

# Some enhancements to think about here.
# $machinePath = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey("SYSTEM\CurrentControlSet\Control\Session Manager\Environment\").GetValue("PATH", "", [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames).ToString();
