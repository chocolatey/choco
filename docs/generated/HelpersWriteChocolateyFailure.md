# [DEPRECATED] Write-ChocolateyFailure

Notes an unsuccessful Chocolatey install.

**NOTE:** This has been deprecated and is no longer useful as of 0.9.9. Instead please just use `throw $_.Exception` when catching errors. Although try/catch is no longer necessary unless you want to do some error handling.

## Usage

```powershell
Write-ChocolateyFailure $packageName $failureMessage
```

## Examples

```powershell
Write-ChocolateyFailure 'StExBar' "$($_.Exception.Message)"
```

## Parameters

* `-packageName`

    This is an arbitrary name.

    Example: `'7zip'`

* `-failureMessage`

    This is the message logged back to the main Chocolatey window.

    Example: `"$($_.Exception.Message)"`

[[Function Reference|HelpersReference]]