# Copyright 2011 - Present RealDimensions Software, LLC
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

function Get-ToolsLocation {
<#
.SYNOPSIS
Gets the top level location for tools/software installed outside of
package folders.

.DESCRIPTION
Creates or uses an environment variable that a user can control to
communicate with packages about where they would like software that is
not installed through native installers, but doesn't make much sense
to be kept in package folders. Most software coming in packages stays
with the package itself, but there are some things that seem to fall
out of this category, like things that have plugins that are installed
into the same directory as the tool. Having that all combined in the
same package directory could get tricky.

.NOTES
This is the successor to the poorly named `Get-BinRoot`. Available as
`Get-ToolsLocation` in 0.9.10+. If you need compatibility with pre
0.9.10, please use `Get-BinRoot`.

Sets an environment variable called `ChocolateyToolsLocation`. If the
older `ChocolateyBinRoot` is set, it uses the value from that and
removes the older variable.

.INPUTS
None

.OUTPUTS
None
#>
  Write-Debug "Running 'Get-ToolsLocation'";
  $invocation = $MyInvocation
  if ($invocation -ne $null -and $invocation.InvocationName -ne $null -and $invocation.InvocationName.ToLower() -eq 'get-binroot') {
    Write-Host "Get-BinRoot is going to be deprecated in v1 and removed in v2. It is being replaced with Get-ToolsLocation, however many packages no longer require a special separate directory since package folders no longer have versions on them. Some do though and should continue to use Get-ToolsLocation."
  }

  $toolsLocation = $env:ChocolateyToolsLocation

  if ($toolsLocation -eq $null) {
    $binRoot = $env:ChocolateyBinRoot
    $olderRoot = $env:chocolatey_bin_root

    if ($binRoot -eq $null -and $olderRoot -eq $null) {
      $toolsLocation = Join-Path $env:systemdrive 'tools'
    } else {
      if ($olderRoot -ne $null) {
        if ($binRoot -eq $null) {
          $binRoot = $olderRoot
        }
        Set-EnvironmentVariable -Name "chocolatey_bin_root" -Value '' -Scope User
      }

      $toolsLocation = $binRoot
      Set-EnvironmentVariable -Name "ChocolateyBinRoot" -Value '' -Scope User
    }
  }

  # Add a drive letter if one doesn't exist
  if (-not($toolsLocation -imatch "^\w:")) {
    $toolsLocation = join-path $env:systemdrive $toolsLocation
  }

  if (-not($env:ChocolateyToolsLocation -eq $toolsLocation)) {
    Set-EnvironmentVariable -Name "ChocolateyToolsLocation" -Value $toolsLocation -Scope User
  }

  return $toolsLocation
}

Set-Alias Get-BinRoot Get-ToolsLocation -Force -Scope Global -Option AllScope
