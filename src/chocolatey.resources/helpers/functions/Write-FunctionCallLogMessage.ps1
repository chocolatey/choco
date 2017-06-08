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

function Write-FunctionCallLogMessage {
<#
.SYNOPSIS
DO NOT USE. Not part of the public API.

.DESCRIPTION
Writes function call as a debug message.

.NOTES
Available in 0.10.2+.

This function is not part of the API.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Invocation
The invocation of the function (`$MyInvocation`)

.PARAMETER Parameters
The parameters passed to the function (`$PSBoundParameters`)

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
# This is how this function should always be called
Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

#>
param (
  $invocation,
  $parameters,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  # do not log function call - recursion?

  $argumentsPassed = ''
  foreach ($param in $parameters.GetEnumerator()) {
    if ($param.Key -eq 'ignoredArguments') { continue; }
    $paramValue = $param.Value -Join ' '
    if ($param.Key -eq 'sensitiveStatements' -or $param.Key -eq 'password') {
      $paramValue = '[REDACTED]'
    }
    $argumentsPassed += "-$($param.Key) '$paramValue' "
  }

  Write-Debug "Running $($invocation.InvocationName) $argumentsPassed"
}
