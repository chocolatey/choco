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

function Get-OSArchitectureWidth {
<#
.SYNOPSIS
Get the operating system architecture address width.

.DESCRIPTION
This will return the system architecture address width (probably 32 or
64 bit). If you pass a comparison, it will return true or false instead
of {`32`|`64`}.

.NOTES
When your installation script has to know what architecture it is run
on, this simple function comes in handy.

Available as `Get-OSArchitectureWidth` in 0.9.10+. If you need
compatibility with pre 0.9.10, please use the alias `Get-ProcessorBits`.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Compare
This optional parameter causes the function to return $true or $false,
depending on wether or not the bit width matches.
#>
param(
  $compare
)
  Write-Debug "Running 'Get-OSArchitectureWidth'"

  $bits = 64
  if (([System.IntPtr]::Size -eq 4) -and (Test-Path env:\PROCESSOR_ARCHITEW6432)) {
    $bits = 64
  } elseif ([System.IntPtr]::Size -eq 4) {
    $bits = 32
  }

  # Return bool|int
  if ("$compare" -ne '' -and $compare -eq $bits) {
    return $true
  } elseif ("$compare" -ne '') {
    return $false
  } else {
    return $bits
  }
}

Set-Alias Get-ProcessorBits Get-OSArchitectureWidth
Set-Alias Get-OSBitness Get-OSArchitectureWidth
