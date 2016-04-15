## Install-ChocolateyPinnedTaskBarItem

Creates an item in the task bar linking to the provided path.

## Usage

```powershell
Install-ChocolateyPinnedTaskBarItem `
 "${env:ProgramFiles(x86)}\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe"
```

## Examples

```powershell
Install-ChocolateyPinnedTaskBarItem `
 "${env:ProgramFiles(x86)}\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe"
```

This will create a Visual Studio task bar icon.

## Parameters

* `-targetFilePath`

    The path to the application that should be launched when clicking on the task bar icon.

[[Function Reference|HelpersReference]]