# Copyright © 2022 Chocolatey Software, Inc.
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

function Get-ChocolateyConfigValue {
    <#
.SYNOPSIS
Retrieve a value from the Chocolatey Configuration file

.DESCRIPTION
This function will attempt to retrieve the path according to the specified Path Type
to a valid location that can be used by maintainers in certain scenarios.

.NOTES
Available in 2.1.0+

.INPUTS
None

.OUTPUTS
This function outputs the value of the specified configuration key.
If the key is not found, there is no output.

.PARAMETER configKey
The name of the configuration value that should be looked up.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
$value = Get-ChocolateyConfigValue -configKey 'cacheLocation'
#>
    param(
        [parameter(Mandatory = $true)]
        [string] $configKey,
        [parameter(ValueFromRemainingArguments = $true)]
        [Object[]] $ignoredArguments
    )

    try {
        $installLocation = Get-ChocolateyPath -pathType 'InstallPath'
        $configPath = Join-Path $installLocation "config\chocolatey.config"
        [xml]$configContents = Get-Content -Path $configPath
        return $configContents.chocolatey.config.add |
                Where-Object { $_.key -eq $configKey } |
                Select-Object -ExpandProperty value
    }
    catch {
        Write-Error "Unable to read config value '$configKey' with error" -Exception $_
    }
}
