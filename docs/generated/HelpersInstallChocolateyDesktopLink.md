# Install-ChocolateyDesktopLink

This adds a shortcut on the desktop to the specified file path.

**NOTE:** This is deprecated in favour of [[Install-ChocolateyShortcut|HelpersInstallChocolateyShortcut]].

## Usage

```powershell
Install-ChocolateyDesktopLink $targetFilePath
```

## Examples

```powershell
Install-ChocolateyDesktopLink 'C:\tools\NHibernateProfiler\nhprof.exe'
```

## Parameters

* `-targetFilePath`

    This is the location to the application/executable file that you want to add a shortcut to on the desktop.

    Example: `'C:\tools\NHibernateProfiler\nhprof.exe'`

[[Function Reference|HelpersReference]]