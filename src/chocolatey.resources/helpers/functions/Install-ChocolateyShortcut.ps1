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

function Install-ChocolateyShortcut {
<#
.SYNOPSIS
This adds a shortcut, at the specified location, with the option to specify
a number of additional properties for the shortcut, such as Working Directory,
Arguments, Icon Location, and Description.

.PARAMETER ShortcutFilePath
The full absolute path to where the shortcut should be created.  This is mandatory.

.PARAMETER TargetPath
The full absolute path to the target for new shortcut.  This is mandatory.

.PARAMETER WorkingDirectory
The full absolute path of the Working Directory that will be used by
the new shortcut.  This is optional

.PARAMETER Arguments
Additonal arguments that should be passed along to the new shortcut.  This
is optional.

.PARAMETER IconLocation
The full absolute path to an icon file to be used for the new shortcut.  This
is optional.

.PARAMETER Description
A text description to be associated with the new description.  This is optional.

.PARAMETER WindowStyle
Type of windows target application should open with.1
0 = Hidden, 1 = Normal Size, 3 = Maximized, 7 - Minimized.
Full list table 3.9 here: https://technet.microsoft.com/en-us/library/ee156605.aspx

.PARAMETER RunAsAdmin
Set "Run As Administrator" checkbox for the created the shortcut.

.PARAMETER PinToTaskbar
Pin the new shortcut to the taskbar.

.EXAMPLE
Install-ChocolateyShortcut -shortcutFilePath "C:\test.lnk" -targetPath "C:\test.exe"

This will create a new shortcut at the location of "C:\test.lnk" and link to the file
located at "C:\text.exe"

.EXAMPLE
Install-ChocolateyShortcut -shortcutFilePath "C:\notepad.lnk" -targetPath "C:\Windows\System32\notepad.exe" -workDirectory "C:\" -arguments "C:\test.txt" -iconLocation "C:\test.ico" -description "This is the description"

This will create a new shortcut at the location of "C:\notepad.lnk" and link to the
Notepad application.  In addition, other properties are being set to specify the working
directoy, an icon to be used for the shortcut, along with a description and arguments.

.EXAMPLE
Install-ChocolateyShortcut -shortcutFilePath "C:\notepad.lnk" -targetPath "C:\Windows\System32\notepad.exe" -WindowStyle 3 -RunAsAdmin -PinToTaskbar

Creates a new notepad shortcut on the root of c: that starts notepad as administrator.  Shortcut is also pinned to taskbar.

#>
	param(
	  [string] $shortcutFilePath,
	  [string] $targetPath,
	  [string] $workingDirectory,
	  [string] $arguments,
	  [string] $iconLocation,
	  [string] $description,
    [int] $windowstyle,
    [switch] $RunAsAdmin,
    [switch] $PinToTaskbar
	)

	Write-Debug "Running 'Install-ChocolateyShortcut' with parameters ShortcutFilePath: `'$shortcutFilePath`', TargetPath: `'$targetPath`', WorkingDirectory: `'$workingDirectory`', Arguments: `'$arguments`', IconLocation: `'$iconLocation`', Description: `'$description`', WindowStyle: `'$WindowStyle`', RunAsAdmin: `'$RunAsAdmin`', PinToTaskbar: `'$PinToTaskbar`'";

	if(!$shortcutFilePath) {
	  throw "Install-ChocolateyShortcut - `$shortcutFilePath can not be null."
	}

	$shortcutDirectory = $([System.IO.Path]::GetDirectoryName($shortcutFilePath))
	if (!(Test-Path($shortcutDirectory))) {
	  [System.IO.Directory]::CreateDirectory($shortcutDirectory) | Out-Null
    }

	if(!$targetPath) {
	  throw "Install-ChocolateyShortcut - `$targetFilePath can not be null."
	}

	if(!(Test-Path($targetPath))) {
	  Write-Warning "'$targetFilePath' does not exist. If it is not created the shortcut will not be valid."
	}

	if($iconLocation) {
		if(!(Test-Path($iconLocation))) {
		  Write-Warning "'$iconLocation' does not exist. A default icon will be used."
		}
	}

	if ($workingDirectory) {
	  if (!(Test-Path($workingDirectory))) {
		[System.IO.Directory]::CreateDirectory($workingDirectory) | Out-Null
      }
	}

	Write-Debug "Creating Shortcut..."

	try {
		$global:WshShell = New-Object -com "WScript.Shell"
	    $lnk = $global:WshShell.CreateShortcut($shortcutFilePath)
	    $lnk.TargetPath = $targetPath
		$lnk.WorkingDirectory = $workingDirectory
	    $lnk.Arguments = $arguments
	    if($iconLocation) {
	      $lnk.IconLocation = $iconLocation
	    }
		if ($description) {
		  $lnk.Description = $description
		}
    if ($WindowStyle) {
      $lnk.WindowStyle = $WindowStyle
    }

	    $lnk.Save()

		Write-Debug "Shortcut created."

    [System.IO.FileInfo]$Path = $shortcutFilePath
    If ($RunAsAdmin) {
      #In order to enable the "Run as Admin" checkbox, this code reads the .LNK as a stream
      #  and flips a specific bit while writing a new copy.  It then replaces the original
      #  .LNK with the copy, similar to this example: http://poshcode.org/2513
      $TempFileName = [IO.Path]::GetRandomFileName()
      $TempFile = [IO.FileInfo][IO.Path]::Combine($Path.Directory, $TempFileName)
      $Writer = New-Object System.IO.FileStream $TempFile, ([System.IO.FileMode]::Create)
      $Reader = $Path.OpenRead()
      While ($Reader.Position -lt $Reader.Length) {
        $Byte = $Reader.ReadByte()
        If ($Reader.Position -eq 22) {$Byte = 34}
        $Writer.WriteByte($Byte)
        }
      $Reader.Close()
      $Writer.Close()
      $Path.Delete()
      Rename-Item -Path $TempFile -NewName $Path.Name | Out-Null
    }

    If ($pintotaskbar) {
      $scfilename = $Path.FullName
      $pinverb = (new-object -com "shell.application").namespace($(split-path -parent $Path.FullName)).Parsename($(split-path -leaf $Path.FullName)).verbs() | ?{$_.Name -eq 'Pin to Tas&kbar'}
      If ($pinverb) {$pinverb.doit()}
    }
	}
	catch {
		Write-Warning "Unable to create shortcut. Error captured was $($_.Exception.Message)."
	}
}
