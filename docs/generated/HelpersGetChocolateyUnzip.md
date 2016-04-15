# Get-ChocolateyUnzip

Unzips a .zip file and returns the location for further processing.

## Usage

```powershell
Get-ChocolateyUnzip $fileFullPath $destination
```

## Examples

```powershell
Get-ChocolateyUnzip 'c:\tools\poshgit.zip' 'C:\tools\poshgit'
```

## Parameters

* `-fileFullPath`

    This is the full path to the .zip file

    Example: `'c:\tools\poshgit.zip'`

* `-destination`

    This is the destination folder to unpack the contents of the zip file. If the destination doesn't exist, it will be created.

    Example: `'C:\tools\poshgit'`

[[Function Reference|HelpersReference]]