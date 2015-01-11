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

function Test-ProcessAdminRights {
<#
.SYNOPSIS
Tests whether the current process is running with administrative rights.

.DESCRIPTION
This function checks whether the current process has administrative rights
by checking if the current user identity is a member of the Administrators group.
It returns $true if the current process is running with administrative rights,
$false otherwise.

On Windows Vista and later, with UAC enabled, the returned value represents the
actual rights available to the process, i.e. if it returns $true, the process is
running elevated.

.OUTPUTS
System.Boolean

#>

  $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent([Security.Principal.TokenAccessLevels]'Query,Duplicate'))
  $isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
  Write-Debug "Test-ProcessAdminRights: returning $isAdmin"
  return $isAdmin
}
