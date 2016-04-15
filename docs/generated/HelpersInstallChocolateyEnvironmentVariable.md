## Install-ChocolateyEnvironmentVariable

Install-ChocolateyEnvironmentVariable creates an environment variable
with the specified name and value. The variable is persistent and
will remain after reboots and across multiple PowerShell and command
line sessions. The variable can be scoped either to the user or to
the machine. If machine level scoping is specified, the command is
elevated to an administrative session.

**NOTE:** This command will assert UAC/Admin privileges on the machine if $variableType== 'Machine'.

## Usage

```powershell
Install-ChocolateyEnvironmentVariable "JAVA_HOME" "d:\oracle\jdk\bin"
```

## Parameters

* `-variableName`

    The name or key of the environment variable

## Examples

```powershell
Install-ChocolateyEnvironmentVariable "JAVA_HOME" "d:\oracle\jdk\bin"
```

Creates a User environment variable "JAVA_HOME" pointing to "d:\oracle\jdk\bin".

```powershell
Install-ChocolateyEnvironmentVariable "_NT_SYMBOL_PATH" "symsrv*symsrv.dll*f:\localsymbols*http://msdl.microsoft.com/download/symbols" "Machine"
```

Creates a User environment variable "_NT_SYMBOL_PATH" pointing to "symsrv*symsrv.dll*f:\localsymbols*http://msdl.microsoft.com/download/symbols".

## Parameters

* `-variableValue`

    A string value assigned to the above name

* `-variableType` _(optional)_

    Pick only one : 'User' or 'Machine'

    Example: `'User'` or `'Machine'`

    Defaults to `'User'`

[[Function Reference|HelpersReference]]