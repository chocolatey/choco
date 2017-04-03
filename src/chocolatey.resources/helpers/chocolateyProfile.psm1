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

if (Get-Module chocolateyProfile) { return }

$thisDirectory = (Split-Path -parent $MyInvocation.MyCommand.Definition)

. $thisDirectory\functions\Write-FunctionCallLogMessage.ps1
. $thisDirectory\functions\Get-EnvironmentVariable.ps1
. $thisDirectory\functions\Get-EnvironmentVariableNames.ps1
. $thisDirectory\functions\Update-SessionEnvironment.ps1
. $thisDirectory\ChocolateyTabExpansion.ps1

Export-ModuleMember -Alias refreshenv -Function 'Update-SessionEnvironment', 'TabExpansion'
