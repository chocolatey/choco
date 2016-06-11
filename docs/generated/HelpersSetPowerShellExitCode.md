﻿# Set-PowerShellExitCode

Sets the exit code for the PowerShell scripts.

## Syntax

~~~powershell
Set-PowerShellExitCode `
  -ExitCode <Int32> `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Sets the exit code as an environment variable that is checked and used
as the exit code for the package at the end of the package script.

## Notes

This tells PowerShell that it should prepare to shut down.

## Aliases

None

## Inputs

None

## Outputs

None

## Parameters

###  -ExitCode &lt;Int32&gt;
The exit code to set.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 0
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


## Examples

 **EXAMPLE 1**

~~~powershell
Set-PowerShellExitCode 3010

~~~


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Set-PowerShellExitCode -Full`.
