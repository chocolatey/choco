# Get-EnvironmentVariable

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-EnvironmentVariable.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

Gets an Environment Variable.

## Syntax

~~~powershell
Get-EnvironmentVariable `
  -Name <String> `
  -Scope {Process | User | Machine} `
  [-PreserveVariables] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

This will will get an environment variable based on the variable name
and scope while accounting whether to expand the variable or not
(e.g.: `%TEMP%`-> `C:\User\Username\AppData\Local\Temp`).

## Notes

This helper reduces the number of lines one would have to write to get
environment variables, mainly when not expanding the variables is a
must.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell
Get-EnvironmentVariable -Name 'TEMP' -Scope User -PreserveVariables

~~~

**EXAMPLE 2**

~~~powershell
Get-EnvironmentVariable -Name 'PATH' -Scope Machine

~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -Name &lt;String&gt;
The environemnt variable you want to get the value from.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -Scope
The environemnt variable target scope. This is `Process`, `User`, or
`Machine`.


Valid options: Process, User, Machine

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | true
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -PreserveVariables
A switch parameter stating whether you want to expand the variables or
not. Defaults to false. Available in 0.9.10+.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | named
Default Value          | False
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


## Links

 * [[Get-EnvironmentVariableNames|HelpersGetEnvironmentVariableNames]]
 * [[Set-EnvironmentVariable|HelpersSetEnvironmentVariable]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Get-EnvironmentVariable -Full`.

View the source for [Get-EnvironmentVariable](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Get-EnvironmentVariable.ps1)
