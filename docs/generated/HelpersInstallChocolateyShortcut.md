# Install-ChocolateyShortcut

This adds a shortcut, at the specified location, with the option to specify
a number of additional properties for the shortcut, such as Working Directory,
Arguments, Icon Location, and Description.

## Usage

```powershell
Install-ChocolateyShortcut -shortcutFilePath "<path>" -targetPath "<path>"
```

## Examples

```powershell
Install-ChocolateyShortcut -shortcutFilePath "C:\test.lnk" -targetPath "C:\test.exe"
Install-ChocolateyShortcut -shortcutFilePath "C:\notepad.lnk" `
 -targetPath "C:\Windows\System32\notepad.exe" -workingDirectory `
 "C:\" -arguments "C:\test.txt" -iconLocation "C:\test.ico" `
 -description "This is the description"
```

## Parameters

* `-ShortcutFilePath`

    The full absolute path to where the shortcut should be created.

* `-TargetPath`

    The full absolute path to the target for new shortcut.

* `-WorkingDirectory` _(optional)_

    The full absolute path of the Working Directory that will be used by the new shortcut.

* `-Arguments` _(optional)_

    Additional arguments that should be passed along to the new shortcut.

* `-IconLocation` _(optional)_

    The full absolute path to an icon file to be used for the new shortcut.

* `-Description` _(optional)_

    A text description to be associated with the new description.

[[Function Reference|HelpersReference]]