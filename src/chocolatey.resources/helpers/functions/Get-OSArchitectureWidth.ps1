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

As of 0.10.14+, ARM64 architecture will automatically select 32bit width as
there is an emulator for 32 bit and there are no current plans by Microsoft to
ship 64 bit x86 emulation for ARM64. For more details, see
https://github.com/chocolatey/choco/issues/1800#issuecomment-484293844.

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

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  $bits = 64
  if (([System.IntPtr]::Size -eq 4) -and (Test-Path env:\PROCESSOR_ARCHITEW6432)) {
    $bits = 64
  } elseif ([System.IntPtr]::Size -eq 4) {
    $bits = 32
  } 

  # ARM64 has a x86 32bit emulator, so we need to select 32 bit if we detect 
  # ARM64 - According to Microsoft on 2019 APR 18 (jkunkee), there are no 
  # current plans to ship 64-bit emulation for ARM64.
  $processorArchitecture = $env:PROCESSOR_ARCHITECTURE
  if ($processorArchitecture -and $processorArchitecture -eq 'ARM64') {
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
