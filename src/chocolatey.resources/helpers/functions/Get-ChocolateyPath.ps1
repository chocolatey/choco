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

function Get-ChocolateyPath {
    <#
.SYNOPSIS
Retrieve the paths available to be used by maintainers of packages.

.DESCRIPTION
This function will attempt to retrieve the path according to the specified Path Type
to a valid location that can be used by maintainers in certain scenarios.

.NOTES
Available in 1.2.0+.

.INPUTS
None

.OUTPUTS
This function outputs the full path stored accordingly with specified path type.
If no path could be found, there is no output.

.PARAMETER pathType
The type of path that should be looked up.
Available values are:
- `PackagePath` - The path to the the package that is being installed. Typically `C:\ProgramData\chocolatey\lib\<PackageName>`
- `InstallPath` - The path to where Chocolatey is installed. Typically `C:\ProgramData\chocolatey`

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
$path = Get-ChocolateyPath -PathType 'PackagePath'
#>
    param(
        [parameter(Mandatory = $true)]
        [alias('type')] [string] $pathType
    )

    $result = $null

    switch ($pathType) {
        'PackagePath' {
            if (Test-Path Env:\ChocolateyPackagePath) {
                $result = "$env:ChocolateyPackagePath"
            }
            elseif (Test-Path Env:\PackagePath) {
                $result = "$env:PackagePath"
            }
            else {
                $installPath = Get-ChocolateyPath -pathType 'InstallPath'
                $result = "$installPath\lib\$env:ChocolateyPackageName"
            }
        }
        'InstallPath' {
            if (Test-Path Env:\ChocolateyInstall) {
                $result = "$env:ChocolateyInstall"
            }
            elseif (Test-Path Env:\ProgramData) {
                $result = "$env:ProgramData\chocolatey"
            }
            else {
                $result = "$env:SystemDrive\ProgramData\chocolatey"
            }
        }
        Default {
            throw "The path type $pathType is not a supported."
        }
    }

    if ((Test-Path $result)) {
        $result
    }
}