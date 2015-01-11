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

function Install-ChocolateyFileAssociation {
<#
.SYNOPSIS
Creates an association between a file extension and a executable

.DESCRIPTION
Install-ChocolateyFileAssociation can associate a file extension
with a downloaded application. Once this command has created an
association, all invocations of files with the specified extension
will be opened via the executable specified.

This command will run with elevated privileges.

.PARAMETER Extension
The file extension to be associated.

.PARAMETER Executable
The path to the application's executable to be associated.

.EXAMPLE
C:\PS>$sublimeDir = (Get-ChildItem $env:ALLUSERSPROFILE\chocolatey\lib\sublimetext* | select $_.last)
C:\PS>$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
C:\PS>Install-ChocolateyFileAssociation ".txt" $sublimeExe

This will create an association between Sublime Text 2 and all .txt files. Any .txt file opened will by default open with Sublime Text 2.

#>
param(
  [string] $extension,
  [string] $executable
)
  Write-Debug "Running 'Install-ChocolateyFileAssociation' associating $extension with `'$executable`'";
  if(-not(Test-Path $executable)){
    $errorMessage = "`'$executable`' does not exist, not able to create association"
    Write-Error $errorMessage
    throw $errorMessage
  }
  $extension=$extension.trim()
  if(-not($extension.StartsWith("."))) {
      $extension = ".$extension"
  }
  $fileType = Split-Path $executable -leaf
  $fileType = $fileType.Replace(" ","_")
  $elevated = @"
    cmd /c "assoc $extension=$fileType"
    cmd /c 'ftype $fileType="$executable" "%1" "%*"'
    New-PSDrive -Name HKCR -PSProvider Registry -Root HKEY_CLASSES_ROOT
    Set-ItemProperty -Path "HKCR:\$fileType" -Name "(Default)" -Value "$fileType file" -ErrorAction Stop
"@
  Start-ChocolateyProcessAsAdmin $elevated
  Write-Host "`'$extension`' has been associated with `'$executable`'"
}
