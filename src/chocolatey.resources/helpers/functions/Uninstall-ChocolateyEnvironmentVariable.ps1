# Copyright © 2017 Chocolatey Software, Inc.
# Copyright © 2011 - 2017 RealDimensions Software, LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
#
# You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Uninstall-ChocolateyEnvironmentVariable {
<#
.SYNOPSIS
**NOTE:** Administrative Access Required when `-VariableType 'Machine'.`

Removes a persistent environment variable.

.DESCRIPTION
Uninstall-ChocolateyEnvironmentVariable removes an environment variable
with the specified name and value. The variable can be scoped either to
the User or to the Machine. If Machine level scoping is specified, the
command is elevated to an administrative session.

.NOTES
Available in 0.9.10+. If you need compatibility with older versions,
use Install-ChocolateyEnvironmentVariable and set `-VariableValue $null`

This command will assert UAC/Admin privileges on the machine when
`-VariableType Machine`.

This will remove the environment variable from the current session.

.INPUTS
None

.OUTPUTS
None

.PARAMETER VariableName
The name or key of the environment variable to remove.

.PARAMETER VariableType
Specifies whether this variable is at either the individual User level
or at the Machine level.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
# Remove an environment variable
Uninstall-ChocolateyEnvironmentVariable -VariableName 'bob'

.EXAMPLE
>
# Remove an environment variable from Machine
Uninstall-ChocolateyEnvironmentVariable -VariableName 'bob' -VariableType 'Machine'

.LINK
Install-ChocolateyEnvironmentVariable

.LINK
Set-EnvironmentVariable

.LINK
Install-ChocolateyPath
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $variableName,
  [parameter(Mandatory=$false, Position=1)]
  [System.EnvironmentVariableTarget] $variableType = [System.EnvironmentVariableTarget]::User,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  if ($variableType -eq [System.EnvironmentVariableTarget]::Machine) {
    if (Test-ProcessAdminRights) {
      Set-EnvironmentVariable -Name $variableName -Value $null -Scope $variableType
    } else {
      $psArgs = "Install-ChocolateyEnvironmentVariable -variableName `'$variableName`' -variableValue $null -variableType `'$variableType`'"
      Start-ChocolateyProcessAsAdmin "$psArgs"
    }
  } else {
    Set-EnvironmentVariable -Name $variableName -Value $null -Scope $variableType
  }

  Set-Content env:\$variableName $null
}
