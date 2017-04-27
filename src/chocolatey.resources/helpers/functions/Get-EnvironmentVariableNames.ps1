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

function Get-EnvironmentVariableNames([System.EnvironmentVariableTarget] $Scope) {
<#
.SYNOPSIS
Gets all environment variable names.

.DESCRIPTION
Provides a list of environment variable names based on the scope. This
can be used to loop through the list and generate names.

.NOTES
Process dumps the current environment variable names in memory /
session. The other scopes refer to the registry values.

.INPUTS
None

.OUTPUTS
A list of environment variables names.

.PARAMETER Scope
The environemnt variable target scope. This is `Process`, `User`, or
`Machine`.

.EXAMPLE
Get-EnvironmentVariableNames -Scope Machine

.LINK
Get-EnvironmentVariable

.LINK
Set-EnvironmentVariable
#>

  # Do not log function call

  # HKCU:\Environment may not exist in all Windows OSes (such as Server Core).
  switch ($Scope) {
    'User' { Get-Item 'HKCU:\Environment' -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Property }
    'Machine' { Get-Item 'HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager\Environment' | Select-Object -ExpandProperty Property }
    'Process' { Get-ChildItem Env:\ | Select-Object -ExpandProperty Key }
    default { throw "Unsupported environment scope: $Scope" }
  }
}
