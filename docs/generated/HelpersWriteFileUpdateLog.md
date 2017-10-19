# Write-FileUpdateLog

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Write-FileUpdateLog.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

DEPRECATED - DO NOT USE. Will be removed in v1.

## Syntax

~~~powershell
Write-FileUpdateLog `
  [-LogFilePath <String>] `
  [-LocationToMonitor <String>] `
  [-ScriptToRun <ScriptBlock>] `
  [-ArgumentList <Object[]>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Monitors a location and writes changes to a log file.

## Notes

DEPRECATED.

Has issues with paths longer than 260 characters. See
https://github.com/chocolatey/choco/issues/156

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -LogFilePath [&lt;String&gt;]
The full path to where to write the log file.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -LocationToMonitor [&lt;String&gt;]
The location to watch for changes at.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -ScriptToRun [&lt;ScriptBlock&gt;]
The script block of what to run and monitor changes.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -ArgumentList [&lt;Object[]&gt;]
Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 4
Default Value          | 
Accept Pipeline Input? | false
 
###  -IgnoredArguments [&lt;Object[]&gt;]
Allows splatting with arguments that do not apply. Do not use directly.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 5
Default Value          | 
Accept Pipeline Input? | false
 
### &lt;CommonParameters&gt;

This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see `about_CommonParameters` http://go.microsoft.com/fwlink/p/?LinkID=113216 .



[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Write-FileUpdateLog -Full`.

View the source for [Write-FileUpdateLog](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Write-FileUpdateLog.ps1)
