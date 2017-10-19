# Install-ChocolateyEnvironmentVariable

<!-- This documentation is automatically generated from https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyEnvironmentVariable.ps1 using https://github.com/chocolatey/choco/tree/stable/GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

**NOTE:** Administrative Access Required when `-VariableType 'Machine'.`

Creates a persistent environment variable.

## Syntax

~~~powershell
Install-ChocolateyEnvironmentVariable `
  [-VariableName <String>] `
  [-VariableValue <String>] `
  [-VariableType {Process | User | Machine}] `
  [-IgnoredArguments <Object[]>] [<CommonParameters>]
~~~

## Description

Install-ChocolateyEnvironmentVariable creates an environment variable
with the specified name and value. The variable is persistent and
will remain after reboots and across multiple PowerShell and command
line sessions. The variable can be scoped either to the User or to
the Machine. If Machine level scoping is specified, the command is
elevated to an administrative session.

## Notes

This command will assert UAC/Admin privileges on the machine when
`-VariableType Machine`.

This will add the environment variable to the current session.

## Aliases

None

## Examples

 **EXAMPLE 1**

~~~powershell

# Creates a User environment variable "JAVA_HOME" pointing to
# "d:\oracle\jdk\bin".
Install-ChocolateyEnvironmentVariable "JAVA_HOME" "d:\oracle\jdk\bin"
~~~

**EXAMPLE 2**

~~~powershell

# Creates a User environment variable "_NT_SYMBOL_PATH" pointing to
# "symsrv*symsrv.dll*f:\localsymbols*http://msdl.microsoft.com/download/symbols".
# The command will be elevated to admin priviledges.
Install-ChocolateyEnvironmentVariable `
  -VariableName "_NT_SYMBOL_PATH" `
  -VariableValue "symsrv*symsrv.dll*f:\localsymbols*http://msdl.microsoft.com/download/symbols" `
  -VariableType Machine
~~~

**EXAMPLE 3**

~~~powershell

# Remove an environment variable
Install-ChocolateyEnvironmentVariable -VariableName 'bob' -VariableValue $null
~~~ 

## Inputs

None

## Outputs

None

## Parameters

###  -VariableName [&lt;String&gt;]
The name or key of the environment variable

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 1
Default Value          | 
Accept Pipeline Input? | false
 
###  -VariableValue [&lt;String&gt;]
A string value assigned to the above name.

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 2
Default Value          | 
Accept Pipeline Input? | false
 
###  -VariableType
Specifies whether this variable is to be accesible at either the
individual user level or at the Machine level.


Valid options: Process, User, Machine

Property               | Value
---------------------- | -----
Aliases                | 
Required?              | false
Position?              | 3
Default Value          | User
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

 * [[Uninstall-ChocolateyEnvironmentVariable|HelpersUninstallChocolateyEnvironmentVariable]]
 * [[Get-EnvironmentVariable|HelpersGetEnvironmentVariable]]
 * [[Set-EnvironmentVariable|HelpersSetEnvironmentVariable]]
 * [[Install-ChocolateyPath|HelpersInstallChocolateyPath]]


[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from `Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1" -Force; Get-Help Install-ChocolateyEnvironmentVariable -Full`.

View the source for [Install-ChocolateyEnvironmentVariable](https://github.com/chocolatey/choco/tree/stable/src/chocolatey.resources/helpers/functions/Install-ChocolateyEnvironmentVariable.ps1)
