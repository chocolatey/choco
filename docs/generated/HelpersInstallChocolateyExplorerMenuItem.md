## Install-ChocolateyExplorerMenuItem

Install-ChocolateyExplorerMenuItem can add an entry in the context menu of
Windows Explorer. The menu item is given a text label and a command. The command
can be any command accepted on the Windows command line. The menu item can be
applied to either folder items or file items.

**NOTE:** This command will assert UAC/Admin privileges on the machine.

## Usage

```powershell
Install-ChocolateyExplorerMenuItem "sublime" "Open with Sublime Text 2" $sublimeExe
```

## Examples

```powershell
$sublimeDir = (Get-ChildItem $env:systemdrive\chocolatey\lib\sublimetext* `
 | select $_.last)
$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
Install-ChocolateyExplorerMenuItem "sublime" "Open with Sublime Text" $sublimeExe
```

This will create a context menu item in Windows Explorer when any file is right clicked. The menu item will appear with the text "Open with Sublime Text 2" and will invoke sublime text 2 when selected.

```powershell
$sublimeDir = (Get-ChildItem $env:systemdrive\chocolatey\lib\sublimetext* `
 | select $_.last)
$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
Install-ChocolateyExplorerMenuItem "sublime" "Open with Sublime Text 2" `
 $sublimeExe "directory"
```

This will create a context menu item in Windows Explorer when any folder is right clicked. The menu item will appear with the text "Open with Sublime Text 2" and will invoke sublime text 2 when seleted.

## Parameters

* `-MenuKey`

    A unique string to identify this menu item in the registry.

* `-MenuLabel`

    The string that will be displayed in the context menu.

* `-Command`

    A command line command that will be invoked when the menu item is selected.

[[Function Reference|HelpersReference]]