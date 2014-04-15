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

.EXAMPLE
Install-ChocolateyShortcut -shortcutFilePath "C:\test.lnk" -targetPath "C:\test.exe"

This will create a new shortcut at the location of "C:\test.lnk" and link to the file
located at "C:\text.exe"

.EXAMPLE
Install-ChocolateyShortcut -shortcutFilePath "C:\notepad.lnk" -targetPath "C:\Windows\System32\notepad.exe" -workDirectory "C:\" -arguments "C:\test.txt" -iconLocation "C:\test.ico" -description "This is the description"

This will create a new shortcut at the location of "C:\notepad.lnk" and link to the
Notepad application.  In addition, other properties are being set to specify the working 
directoy, an icon to be used for the shortcut, along with a description and arguments.

#>
	param(
	  [string] $shortcutFilePath,
	  [string] $targetPath,
	  [string] $workingDirectory,
	  [string] $arguments,
	  [string] $iconLocation,
	  [string] $description
	)

	Write-Debug "Running 'Install-ChocolateyShortcut' with parameters ShortcutFilePath: `'$shortcutFilePath`', TargetPath: `'$targetPath`', WorkingDirectory: `'$workingDirectory`', Arguments: `'$arguments`', IconLocation: `'$iconLocation`', Description: `'$description`'";

	if(!$shortcutFilePath) {
		Write-ChocolateyFailure "Install-ChocolateyShortcut" "Missing ShortCutFilePath input parameter."
		return
	}
	
	if(!$targetPath) {
		Write-ChocolateyFailure "Install-ChocolateyShortcut" "Missing TargetPath input parameter."
		return
	}
	
	if(!(Test-Path($targetPath))) {
		Write-ChocolateyFailure "Install-ChocolateyShortcut" "TargetPath does not exist, so can't create shortcut."
		return
	}
	
	if($iconLocation) {
		if(!(Test-Path($iconLocation))) {
			Write-ChocolateyFailure "Install-ChocolateyShortcut" "IconLocation does not exist, so can't create shortcut."
			return
		}
	}
	
	if($workingDirectory) {
		if(!(Test-Path($workingDirectory))) {
			Write-ChocolateyFailure "Install-ChocolateyShortcut" "WorkingDirectory does not exist, so can't create shortcut."
			return
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
	    $lnk.Description = $description
	    $lnk.Save()
		
		Write-Debug "Shortcut created."

		Write-ChocolateySuccess "Install-ChocolateyShortcut completed"
		
	}
	catch {
		Write-ChocolateyFailure "Install-ChocolateyShortcut" "There were errors attempting to create shortcut. The error message was '$_'."
	}
}