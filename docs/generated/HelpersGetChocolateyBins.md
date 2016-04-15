# Get-ChocolateyBins

Creates batch files for all executables in `$packageFolder` in the Chocolatey bin folder (usually C:\ProgramData\Chocolatey\bin)

## Usage

```powershell
Get-ChocolateyBins $packageFolder
```

## Examples

```powershell
$installDir = Split-Path -Parent (Get-ItemProperty ` 
HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Vim UninstallString).UninstallString
# special batch files we want to create
$diffExeIgnore = Join-Path $installDir "diff.exe.ignore"
$uninstallExeIgnore = Join-Path $installDir "uninstall.exe.ignore"
$gvimExeGui = Join-Path $installDir "gvim.exe.gui"

New-Item $diffExeIgnore,$uninstallExeIgnore,$gvimExeGui `
 -Type File -Force | Out-Null Get-ChocolateyBins $installDir
Remove-Item $diffExeIgnore,$uninstallExeIgnore,$gvimExeGui
```

## Parameters
* `-packageFolder`

    This is the name of the folder to search for executables in.

    Example: `'c:\program files (x86)\vim\vim73\'`

[[Function Reference|HelpersReference]]