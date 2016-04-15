# Install-ChocolateyPath

This puts a directory on the PATH environment variable. This is used when the application/tool is not being linked by Chocolatey (not in the lib folder).

**NOTE:** This command will assert UAC/Admin privileges on the machine if $pathType == 'Machine'.

## Usage

```powershell
Install-ChocolateyPath $pathToInstall $pathType
```

## Examples

```powershell
Install-ChocolateyPath "$env:SystemDrive\tools\gittfs"
Install-ChocolateyPath "$env:SystemDrive\Program Files\MySQL\MySQL Server 5.5\bin" `
 'Machine'
```

## Parameters

* `-pathToInstall`

    This is a directory that you want to add to the PATH environment variable.

    Example: `"$env:SystemDrive\tools\gittfs"`

* `-pathType` _(optional)_

    Pick only one : 'User' or 'Machine'

    Example: `'User'` or `'Machine'`

    Defaults to `'User'`

[[Function Reference|HelpersReference]]