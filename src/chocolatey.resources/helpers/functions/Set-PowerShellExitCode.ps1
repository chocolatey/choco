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

Function Set-PowerShellExitCode {
<#
.SYNOPSIS
Sets the exit code for the PowerShell scripts.

.DESCRIPTION
Sets the exit code as an environment variable that is checked and used
as the exit code for the package at the end of the package script.

.NOTES
This tells PowerShell that it should prepare to shut down.

.INPUTS
None

.OUTPUTS
None

.PARAMETER ExitCode
The exit code to set.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Set-PowerShellExitCode 3010
#>
param (
  [parameter(Mandatory=$false, Position=0)][int] $exitCode,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  # Do not log function call - can mess things up

  if ($exitCode -eq $null -or $exitCode -eq '') {
    Write-Debug '$exitCode was passed null'
    return
  }

  try {
    $host.SetShouldExit($exitCode);
  } catch {
    Write-Warning "Unable to set host exit code"
  }

  $env:ChocolateyExitCode = $exitCode;
}
