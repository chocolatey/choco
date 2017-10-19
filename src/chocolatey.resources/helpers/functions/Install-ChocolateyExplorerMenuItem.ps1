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

function Install-ChocolateyExplorerMenuItem {
<#
.SYNOPSIS
**NOTE:** Administrative Access Required.

Creates a windows explorer context menu item that can be associated with
a command

.DESCRIPTION
Install-ChocolateyExplorerMenuItem can add an entry in the context menu
of Windows Explorer. The menu item is given a text label and a command.
The command can be any command accepted on the windows command line. The
menu item can be applied to either folder items or file items.

Because this command accesses and edits the root class registry node, it
will be elevated to admin.

.NOTES
This command will assert UAC/Admin privileges on the machine.

Chocolatey will automatically add the path of the file or folder clicked
to the command. This is done simply by appending a %1 to the end of the
command.

.INPUTS
None

.OUTPUTS
None

.PARAMETER MenuKey
A unique string to identify this menu item in the registry

.PARAMETER MenuLabel
The string that will be displayed in the context menu

.PARAMETER Command
A command line command that will be invoked when the menu item is
selected

.PARAMETER Type
Specifies if the menu item should be applied to a folder or a file

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
# This will create a context menu item in Windows Explorer when any file
# is right clicked. The menu item will appear with the text "Open with
# Sublime Text 2" and will invoke sublime text 2 when selected.
$sublimeDir = (Get-ChildItem $env:ALLUSERSPROFILE\chocolatey\lib\sublimetext* | select $_.last)
$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
Install-ChocolateyExplorerMenuItem "sublime" "Open with Sublime Text 2" $sublimeExe

.EXAMPLE
>
# This will create a context menu item in Windows Explorer when any
# folder is right clicked. The menu item will appear with the text
# "Open with Sublime Text 2" and will invoke sublime text 2 when selected.
$sublimeDir = (Get-ChildItem $env:ALLUSERSPROFILE\chocolatey\lib\sublimetext* | select $_.last)
$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
Install-ChocolateyExplorerMenuItem "sublime" "Open with Sublime Text 2" $sublimeExe "directory"

.LINK
Install-ChocolateyShortcut
#>
param(
  [parameter(Mandatory=$true, Position=0)][string] $menuKey,
  [parameter(Mandatory=$false, Position=1)][string] $menuLabel,
  [parameter(Mandatory=$false, Position=2)][string] $command,
  [parameter(Mandatory=$false, Position=3)]
  [ValidateSet('file','directory')][string] $type = "file",
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)
try {

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  if($type -eq "file") {$key = "*"} elseif($type -eq "directory") {$key="directory"} else{ return 1}
  $elevated = "`
    if( -not (Test-Path -path HKCR:) ) {New-PSDrive -Name HKCR -PSProvider registry -Root Hkey_Classes_Root};`
    if(!(test-path -LiteralPath 'HKCR:\$key\shell\$menuKey')) { new-item -Path 'HKCR:\$key\shell\$menuKey' };`
    Set-ItemProperty -LiteralPath 'HKCR:\$key\shell\$menuKey' -Name '(Default)'  -Value '$menuLabel';`
    if(!(test-path -LiteralPath 'HKCR:\$key\shell\$menuKey\command')) { new-item -Path 'HKCR:\$key\shell\$menuKey\command' };`
    Set-ItemProperty -LiteralPath 'HKCR:\$key\shell\$menuKey\command' -Name '(Default)' -Value '$command \`"%1\`"';`
    return 0;"

  Start-ChocolateyProcessAsAdmin $elevated
  Write-Host "'$menuKey' explorer menu item has been created"
}
catch {
    $errorMessage = "'$menuKey' explorer menu item was not created - $($_.Exception.Message)"
	Write-Warning $errorMessage
  }
}
