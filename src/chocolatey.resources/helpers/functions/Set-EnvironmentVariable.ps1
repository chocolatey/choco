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

function Set-EnvironmentVariable([string] $Name, [string] $Value, [System.EnvironmentVariableTarget] $Scope) {
	Write-Debug "Calling Set-EnvironmentVariable with `$Name = '$Name', `$Value = '$Value', `$Scope = '$Scope'"
    [Environment]::SetEnvironmentVariable($Name, $Value, $Scope)
}
