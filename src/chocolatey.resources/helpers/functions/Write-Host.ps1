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

function Write-Host {
param(
  [Parameter(Position=0,Mandatory=$false,ValueFromPipeline=$true, ValueFromRemainingArguments=$true)][object] $Object,
  [Parameter()][switch] $NoNewLine,
  [Parameter(Mandatory=$false)][ConsoleColor] $ForegroundColor,
  [Parameter(Mandatory=$false)][ConsoleColor] $BackgroundColor,
  [Parameter(Mandatory=$false)][Object] $Separator
)

  $chocoPath = (Split-Path -parent $helpersPath)
  $chocoInstallLog = Join-Path $chocoPath 'chocolateyInstall.log'
  "$(get-date -format 'yyyyMMdd-HH:mm:ss') [CHOCO] $Object"| Out-File -FilePath $chocoInstallLog -Force -Append

  $oc = Get-Command 'Write-Host' -Module 'Microsoft.PowerShell.Utility'
  if($env:ChocolateyEnvironmentQuiet -eq 'true') {
    $oc = {}
  }

  #I owe this guy a drink - http://powershell.com/cs/blogs/tobias/archive/2011/08/03/clever-splatting-to-pass-optional-parameters.aspx
  & $oc @PSBoundParameters
}
