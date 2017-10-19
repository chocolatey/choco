# Write-FunctionCallLogMessage

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Write-FunctionCallLogMessage.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

DO NOT USE. Not part of the public API.

## Syntax

~~~powershell
Write-FunctionCallLogMessage `
  [-Invocation <Object>] `
  [-Parameters <Object>] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Writes function call as a debug message.

## Notes

Available in 0.10.2+.

This function is not part of the API.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell

# This is how this function should always be called
Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -Invocation [&lt;Object&gt;]
The invocation of the function (`$MyInvocation`)

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -Parameters [&lt;Object&gt;]
The parameters passed to the function (`$PSBoundParameters`)

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -IgnoredArguments [&lt;Object[]&gt;]
Allows splatting with arguments that do not apply. Do not use directly.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | 
Accept Pipeline Input? | false
 
### &lt;CommonParameters&gt;

This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see `about_CommonParameters` http://go.microsoft.com/fwlink/p/?LinkID=113216 .



[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Write-FunctionCallLogMessage -Full`.

View the source for [Write-FunctionCallLogMessage](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Write-FunctionCallLogMessage.ps1)
