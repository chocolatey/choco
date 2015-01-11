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

function Write-Error {
param(
  [Parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true)][string] $Message='',
  [Parameter(Mandatory=$false)][System.Management.Automation.ErrorCategory] $Category,
  [Parameter(Mandatory=$false)][string] $ErrorId,
  [Parameter(Mandatory=$false)][object] $TargetObject,
  [Parameter(Mandatory=$false)][string] $CategoryActivity,
  [Parameter(Mandatory=$false)][string] $CategoryReason,
  [Parameter(Mandatory=$false)][string] $CategoryTargetName,
  [Parameter(Mandatory=$false)][string] $CategoryTargetType,
  [Parameter(Mandatory=$false)][string] $RecommendedAction
)

  $chocoPath = (Split-Path -parent $helpersPath)
  $chocoInstallLog = Join-Path $chocoPath 'chocolateyInstall.log'
  "$(get-date -format 'yyyyMMdd-HH:mm:ss') [ERROR] $Message" | Out-File -FilePath $chocoInstallLog -Force -Append

  $oc = Get-Command 'Write-Error' -Module 'Microsoft.PowerShell.Utility' 
  & $oc @PSBoundParameters
}