# Get-WebFile

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-WebFile.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Downloads a file from an HTTP/HTTPS location. Prefer HTTPS when
available.

## Syntax

~~~powershell
Get-WebFile `
  [-Url <String>] `
  [-FileName <String>] `
  [-UserAgent <String>] `
  [-Passthru] `
  [-Quiet] `
  [-Options <Hashtable>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This will download a file from an HTTP/HTTPS location, saving the file
to the FileName location specified.

## Notes

This is a low-level function and not recommended for use in package
scripts. It is recommended you call `Get-ChocolateyWebFile` instead.

Starting in 0.9.10, will automatically call Set-PowerShellExitCode to
set the package exit code to 404 if the resource is not found.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -Url [&lt;String&gt;]
This is the url to download the file from. Prefer HTTPS when available.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -FileName [&lt;String&gt;]
This is the full path to the file to create. If downloading to the
package folder next to the install script, the path will be like
`"$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\\file.exe"`

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -UserAgent [&lt;String&gt;]
The user agent to use as part of the request. Defaults to 'chocolatey
command line'.

Property               | Value
---------------------- | -----------------------
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | chocolatey command line
Accept Pipeline Input? | false
 
###  -Passthru
DO NOT USE - holdover from original function.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | False
Accept Pipeline Input? | false
 
###  -Quiet
Silences the progress output.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | False
Accept Pipeline Input? | false
 
###  -Options [&lt;Hashtable&gt;]
OPTIONAL - Specify custom headers. Available in 0.9.10+.

Property               | Value
---------------------- | --------------
Aliases                | 
Required?              | false
Position?              | named
Default Value          | @{Headers=@{}}
Accept Pipeline Input? | false
 
###  -IgnoredArguments [&lt;Object[]&gt;]
Allows splatting with arguments that do not apply. Do not use directly.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | 
Accept Pipeline Input? | false
 
### &lt;CommonParameters&gt;

This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see `about_CommonParameters` http://go.microsoft.com/fwlink/p/?LinkID=113216 .


## Links

 * [[Get-ChocolateyWebFile|HelpersGetChocolateyWebFile]]
 * [[Get-FtpFile|HelpersGetFtpFile]]
 * [[Get-WebHeaders|HelpersGetWebHeaders]]
 * [[Get-WebFileName|HelpersGetWebFileName]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-WebFile -Full`.

View the source for [Get-WebFile](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-WebFile.ps1)
