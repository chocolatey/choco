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

function Install-ChocolateyPinnedTaskBarItem {
<#
.SYNOPSIS
Creates an item in the task bar linking to the provided path.

.NOTES
Does not work with SYSTEM, but does not error. It warns with the error
message.

.INPUTS
None

.OUTPUTS
None

.PARAMETER TargetFilePath
The path to the application that should be launched when clicking on the
task bar icon.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
# This will create a Visual Studio task bar icon.
Install-ChocolateyPinnedTaskBarItem -TargetFilePath "${env:ProgramFiles(x86)}\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe"

.LINK
Install-ChocolateyShortcut

.LINK
Install-ChocolateyExplorerMenuItem
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $targetFilePath,
  [parameter(ValueFromRemainingArguments = $true)][Object[]]$ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters
 
  try{
	if (test-path($targetFilePath)) {
		$verb = "Pin To Taskbar"
		$path= split-path $targetFilePath
		$shell=new-object -com "Shell.Application"
		$folder=$shell.Namespace($path)
		$item = $folder.Parsename((split-path $targetFilePath -leaf))
		$itemVerb = $item.Verbs() | ? {$_.Name.Replace("&","") -eq $verb}
		if($itemVerb -eq $null){
			Write-Host "TaskBar verb not found for $item. It may have already been pinned"
		} else {
			$itemVerb.DoIt()
		}
		Write-Host "`'$targetFilePath`' has been pinned to the task bar on your desktop"
	} else {
		$errorMessage = "`'$targetFilePath`' does not exist, not able to pin to task bar"
	}

	if ($errorMessage) {
		Write-Warning $errorMessage
	}
  } catch {
	 Write-Warning "Unable to create pin. Error captured was $($_.Exception.Message)."
  }
}
