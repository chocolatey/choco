## Install-ChocolateyFileAssociation

Install-ChocolateyFileAssociation can associate a file extension
with a downloaded application. Once this command has created an
association, all invocations of files with the specified extension
will be opened via the executable specified.

**NOTE:** This command will assert UAC/Admin privileges on the machine.

## Usage

```powershell
Install-ChocolateyFileAssociation ".txt" $sublimeExe
```

## Examples

```powershell
$sublimeDir = (Get-ChildItem $env:systemdrive\chocolatey\lib\sublimetext* | select $_.last)
$sublimeExe = "$sublimeDir\tools\sublime_text.exe"
Install-ChocolateyFileAssociation ".txt" $sublimeExe
```

This will create an association between Sublime Text 2 and all .txt files. Any .txt file opened will by default open with Sublime Text 2.

## Parameters

* `-Extension`

    The file extension to be associated.

* `-Executable`

    The path to the application's executable to be associated.

[[Function Reference|HelpersReference]]