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

function Write-FileUpdateLog {
  param (
    [string] $logFilePath,
    [string] $locationToMonitor,
    [scriptblock] $scriptToRun,
    [object[]] $argumentList
  )
  Write-Debug "Running 'Write-FileUpdateLog' with logFilePath:`'$logFilePath`'', locationToMonitor:$locationToMonitor, Operation: `'$scriptToRun`'";

  Write-Debug "Tracking current state of `'$locationToMonitor`'"
  $originalContents = Get-ChildItem -Recurse $locationToMonitor | Select-Object LastWriteTimeUTC,FullName,Length

  Invoke-Command -ScriptBlock $scriptToRun -ArgumentList $argumentList

  $newContents = Get-ChildItem -Recurse $locationToMonitor | Select-Object LastWriteTimeUTC,FullName,Length

  if($originalContents -eq $null) {$originalContents = @()}
  if($newContents -eq $null) {$newContents = @()}

  $changedFiles = Compare-Object $originalContents $newContents -Property LastWriteTimeUtc,FullName,Length -PassThru | Group-Object FullName

  #log modified files
  $changedFiles | ? {$_.Count -gt 1} | % {$_.Name} | Add-Content $logFilePath
 
  #log added files
  $addOrDelete = $changedFiles | ? { $_.Count -eq 1 } | % {$_.Group}
  $addOrDelete | ? {$_.SideIndicator -eq "=>"} | % {$_.FullName} | Add-Content $logFilePath

  #log deleted files
  #$addOrDelete | ? {$_.SideIndicator -eq "<="} | % {$_.FullName} | Add-Content $logFilePath
}