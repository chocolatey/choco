# Write-FileUpdateLog

DEPRECATED - DO NOT USE. Will be removed in v1.

## Syntax

~~~powershell
Write-FileUpdateLog `
  [-LogFilePath <String>] `
  [-LocationToMonitor <String>] `
  [-ScriptToRun <ScriptBlock>] `
  [-ArgumentList <Object[]>]
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

###  -LogFilePath [\<String\>]
The full path to where to write the log file.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -LocationToMonitor [\<String\>]
The location to watch for changes at.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -ScriptToRun [\<ScriptBlock\>]
The script block of what to run and monitor changes.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
###  -ArgumentList [\<Object[]\>]
Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 4
Default Value          | 
Accept Pipeline Input? | false
 




[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Write-FileUpdateLog -Full`.
