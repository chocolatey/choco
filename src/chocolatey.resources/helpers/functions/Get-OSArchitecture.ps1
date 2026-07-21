# Copyright © 2017 - 2025 Chocolatey Software, Inc
# Copyright © 2011 - 2017 RealDimensions Software, LLC
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

function Get-OSArchitecture {
    <#
.SYNOPSIS
Gets the native processor architecture of the operating system.

.DESCRIPTION
Returns the native architecture of the operating system as one of `x86`,
`x64`, or `arm64`. Unlike Get-OSArchitectureWidth - which reports the bit
width of the current process and is therefore affected by emulation - this
function reports the *native* architecture even when Chocolatey is running
under emulation, for example an x86 or x64 process on Windows on ARM.

.NOTES
This is useful when a package ships native arm64 binaries and needs to decide
whether to install them. Pair it with Get-OSArchitectureWidth when you need
both the native architecture and the process bit width.

.INPUTS
None

.OUTPUTS
Returns a string: `x86`, `x64`, or `arm64`.
#>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [parameter(ValueFromRemainingArguments = $true)]
        [Object[]] $ignoredArguments
    )

    Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

    # 1. Prefer the value Chocolatey computed reliably (via IsWow64Process2) and
    #    exposed to the package environment. This is correct even under emulation.
    if ($env:OS_PROCESSOR_ARCHITECTURE) {
        return $env:OS_PROCESSOR_ARCHITECTURE.ToLowerInvariant()
    }

    # 2. When running outside of a Chocolatey session (for example, a dot-sourced
    #    helper), query the operating system architecture via WMI. This reports the
    #    native architecture (such as 'ARM 64-bit') rather than the emulated
    #    architecture that the environment variables would report. Get-WmiObject is
    #    used rather than Get-CimInstance to retain PowerShell v2 compatibility.
    try {
        $osArchitecture = (Get-WmiObject -Class Win32_OperatingSystem -ErrorAction Stop).OSArchitecture
        if ($osArchitecture -match 'ARM') {
            return 'arm64'
        }
        elseif ($osArchitecture -match '64') {
            return 'x64'
        }
        elseif ($osArchitecture) {
            return 'x86'
        }
    }
    catch {
        Write-Debug "Unable to determine OS architecture from Win32_OperatingSystem: $($_.Exception.Message)"
    }

    # 3. Last resort: derive from the operating system bitness. This cannot detect
    #    arm64, but is only reached when the methods above are unavailable.
    if ([System.Environment]::Is64BitOperatingSystem) {
        return 'x64'
    }

    return 'x86'
}

Set-Alias Get-ProcessorArchitecture Get-OSArchitecture
