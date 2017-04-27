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

function Get-VirusCheckValid {
<#
.SYNOPSIS
Used in Pro/Business editions. Runtime virus check against downloaded
resources.

.DESCRIPTION
Run a runtime malware check against downloaded resources prior to
allowing Chocolatey to execute a file. This is available in 0.9.10+ only
in Pro / Business editions.

.NOTES
Only licensed editions of Chocolatey provide runtime malware protection.

.INPUTS
None

.OUTPUTS
None

.PARAMETER Url
Not used

.PARAMETER File
The full file path to the file to verify against anti-virus scanners.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.
#>
param(
  [parameter(Mandatory=$false, Position=0)][string] $url,
  [parameter(Mandatory=$false, Position=1)][string] $file = '',
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)
  Write-Debug "No runtime virus checking built into FOSS Chocolatey. Check out Pro/Business - https://chocolatey.org/compare"
}
