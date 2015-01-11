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

function Update-SessionEnvironment {
<#
.SYNOPSIS
Updates the environment variables of the current powershell session with
any environment variable changes that may have occured during a chocolatey
package install.

.DESCRIPTION
When chocolatey installs a package, the package author may add or change
certain environment variables that will affect how the application runs
or how it is accessed. Often, these changes are not visible to the current
powershell session. This means the user needs to open a new powershell
session before these settings take effect which can render the installed
application unfunctional until that time.

Use the Update-SessionEnvironment command to refresh the current
powershell session with all environment settings possibly performed by
chocolatey package installs.

#>
  Write-Debug "Running 'Update-SessionEnvironment' - Updating the environment variables for the session."

  #ordering is important here, $user comes after so we can override $machine
  'Machine', 'User' |
    % {
      $scope = $_
      Get-EnvironmentVariableNames -Scope $scope |
        % {
          Set-Item "Env:$($_)" -Value (Get-EnvironmentVariable -Scope $scope -Name $_)
        }
    }

  #Path gets special treatment b/c it munges the two together
  $paths = 'Machine', 'User' |
    % {
      (Get-EnvironmentVariable -Name 'PATH' -Scope $_) -split ';'
    } |
    Select -Unique
  $Env:PATH = $paths -join ';'
}
