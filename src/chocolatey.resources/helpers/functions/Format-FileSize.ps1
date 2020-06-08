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

Function Format-FileSize {
<#
.SYNOPSIS
DO NOT USE. Not part of the public API.

.DESCRIPTION
Formats file size into a human readable format.

.NOTES
Available in 0.9.10+.

This function is not part of the API.

.INPUTS
None

.OUTPUTS
Returns a string representation of the file size in a more friendly
format based on the passed in bytes.

.PARAMETER Size
The size of a file in bytes.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
Format-FileSize -Size $fileSizeBytes

.LINK
Get-WebFile
#>
param (
  [Parameter(Mandatory=$true, Position=0)][double] $size,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  # Do not log function call, it interrupts the single line download progress output.

  Foreach ($unit in @('B', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB')) {
    If ($size -lt 1024) {
      return [string]::Format("{0:0.##} {1}", $size, $unit)
    }
    $size /= 1024
  }

  return [string]::Format("{0:0.##} YB", $size)
}
