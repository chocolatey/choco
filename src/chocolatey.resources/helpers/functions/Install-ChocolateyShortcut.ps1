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

function Install-ChocolateyShortcut {
<#
.SYNOPSIS
Creates a shortcut

.DESCRIPTION
This adds a shortcut, at the specified location, with the option to specify
a number of additional properties for the shortcut, such as Working Directory,
Arguments, Icon Location, and Description.

.NOTES
If this errors, as it may if being run under the local SYSTEM account with
particular folder that SYSTEM doesn't have, it will display a warning instead
of failing a package installation.

.INPUTS
None

.OUTPUTS
None

.PARAMETER ShortcutFilePath
The full absolute path to where the shortcut should be created.

.PARAMETER TargetPath
The full absolute path to the target for new shortcut.

.PARAMETER WorkingDirectory
OPTIONAL - The full absolute path of the Working Directory that will be
used by the new shortcut.

As of v0.10.12, the directory will be created unless it contains environment
variable expansion like `%AppData%\FooBar`.

.PARAMETER Arguments
OPTIONAL - Additonal arguments that should be passed along to the new
shortcut.

.PARAMETER IconLocation
OPTIONAL- The full absolute path to an icon file to be used for the new
shortcut.

.PARAMETER Description
OPTIONAL - A text description to be associated with the new description.

.PARAMETER WindowStyle
OPTIONAL - Type of windows target application should open with.
Available in 0.9.10+.
0 = Hidden, 1 = Normal Size, 3 = Maximized, 7 - Minimized.
Full list table 3.9 here: https://technet.microsoft.com/en-us/library/ee156605.aspx

.PARAMETER RunAsAdmin
OPTIONAL - Set "Run As Administrator" checkbox for the created the
shortcut. Available in 0.9.10+.

.PARAMETER PinToTaskbar
OPTIONAL - Pin the new shortcut to the taskbar. Available in 0.9.10+.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
# This will create a new shortcut at the location of "C:\test.lnk" and
# link to the file located at "C:\text.exe"

Install-ChocolateyShortcut -ShortcutFilePath "C:\test.lnk" -TargetPath "C:\test.exe"

.EXAMPLE
>
# This will create a new shortcut at the location of "C:\notepad.lnk"
# and link to the Notepad application.  In addition, other properties
# are being set to specify the working directory, an icon to be used for
# the shortcut, along with a description and arguments.

Install-ChocolateyShortcut `
  -ShortcutFilePath "C:\notepad.lnk" `
  -TargetPath "C:\Windows\System32\notepad.exe" `
  -WorkingDirectory "C:\" `
  -Arguments "C:\test.txt" `
  -IconLocation "C:\test.ico" `
  -Description "This is the description"

.EXAMPLE
>
# Creates a new notepad shortcut on the root of c: that starts
# notepad.exe as Administrator. Shortcut is also pinned to taskbar.
# These parameters are available in 0.9.10+.

Install-ChocolateyShortcut `
  -ShortcutFilePath "C:\notepad.lnk" `
  -TargetPath "C:\Windows\System32\notepad.exe" `
  -WindowStyle 3 `
  -RunAsAdmin `
  -PinToTaskbar

.LINK
Install-ChocolateyDesktopLink

.LINK
Install-ChocolateyExplorerMenuItem

.LINK
Install-ChocolateyPinnedTaskBarItem
#>
	param(
    [parameter(Mandatory=$true, Position=0)][string] $shortcutFilePath,
    [parameter(Mandatory=$true, Position=1)][string] $targetPath,
    [parameter(Mandatory=$false, Position=2)][string] $workingDirectory,
    [parameter(Mandatory=$false, Position=3)][string] $arguments,
    [parameter(Mandatory=$false, Position=4)][string] $iconLocation,
    [parameter(Mandatory=$false, Position=5)][string] $description,
    [parameter(Mandatory=$false, Position=6)][int] $windowStyle,
    [parameter(Mandatory=$false)][switch] $runAsAdmin,
    [parameter(Mandatory=$false)][switch] $pinToTaskbar,
    [parameter(ValueFromRemainingArguments = $true)][Object[]]$ignoredArguments
	)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

	# http://powershell.com/cs/blogs/tips/archive/2009/02/05/validating-a-url.aspx
	function isURIWeb($address) {
	  $uri = $address -as [System.URI]
	  $uri.AbsoluteURI -ne $null -and $uri.Scheme -match '[http|https]'
	}

  if (!$shortcutFilePath) {
    # shortcut file path could be null if someone is trying to get special
    # paths for LocalSystem (SYSTEM).
    Write-Warning "Unable to create shortcut. `$shortcutFilePath can not be null."
    return
  }

	$shortcutDirectory = $([System.IO.Path]::GetDirectoryName($shortcutFilePath))
  if (!(Test-Path($shortcutDirectory))) {
    [System.IO.Directory]::CreateDirectory($shortcutDirectory) | Out-Null
  }

	if (!$targetPath) {
	  throw "Install-ChocolateyShortcut - `$targetPath can not be null."
	}

	if(!(Test-Path($targetPath)) -and !(IsURIWeb($targetPath))) {
	  Write-Warning "'$targetPath' does not exist. If it is not created the shortcut will not be valid."
	}

	if($iconLocation) {
		if(!(Test-Path($iconLocation))) {
		  Write-Warning "'$iconLocation' does not exist. A default icon will be used."
		}
	}

  if ($workingDirectory) {
    if ($workingDirectory -notmatch '%\w+%' -and !(Test-Path($workingDirectory))) {
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

    if ($windowStyle) {
      $lnk.WindowStyle = $windowStyle
    }

    $lnk.Save()

		Write-Debug "Shortcut created."

    [System.IO.FileInfo]$Path = $shortcutFilePath
    If ($runAsAdmin) {
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

    If ($pinToTaskbar) {
      $scfilename = $Path.FullName
      $pinverb = (new-object -com "shell.application").namespace($(split-path -parent $Path.FullName)).Parsename($(split-path -leaf $Path.FullName)).verbs() | ?{$_.Name -eq 'Pin to Tas&kbar'}
      If ($pinverb) {$pinverb.doit()}
    }
	}
	catch {
		Write-Warning "Unable to create shortcut. Error captured was $($_.Exception.Message)."
	}
}
