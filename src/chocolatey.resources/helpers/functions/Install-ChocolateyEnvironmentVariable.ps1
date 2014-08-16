function Install-ChocolateyEnvironmentVariable {
<#
.SYNOPSIS
Creates a persistent environment variable

.DESCRIPTION
Install-ChocolateyEnvironmentVariable creates an environment variable
with the specified name and value. The variable is persistent and
will remain after reboots and accross multiple powershell and command
line sessions. The variable can be scoped either to the user or to
the machine. If machine level scoping is specified, the comand is
elevated to an administrative session.

.PARAMETER variableName
The name or key of the environment variable

.PARAMETER variableValue
A string value assigned to the above name.

.PARAMETER variableType
Specifies whether this variable is to be accesible at either the
individual user level or at the Machine level.

.EXAMPLE
Install-ChocolateyEnvironmentVariable "JAVA_HOME" "d:\oracle\jdk\bin"
Creates a User environmet variable "JAVA_HOME" pointing to
"d:\oracle\jdk\bin".

.EXAMPLE
Install-ChocolateyEnvironmentVariable "_NT_SYMBOL_PATH" "symsrv*symsrv.dll*f:\localsymbols*http://msdl.microsoft.com/download/symbols" Machine
Creates a User environmet variable "_NT_SYMBOL_PATH" pointing to
"symsrv*symsrv.dll*f:\localsymbols*http://msdl.microsoft.com/download/symbols".
The command will be elevated to admin priviledges.

#>
param(
  [string] $variableName,
  [string] $variableValue,
  [System.EnvironmentVariableTarget] $variableType = [System.EnvironmentVariableTarget]::User
)
  Write-Debug "Running 'Install-ChocolateyEnvironmentVariable' with variableName:`'$variableName`' and variableValue:`'$variableValue`'";

  if ($variableType -eq [System.EnvironmentVariableTarget]::Machine) {
    if (Test-ProcessAdminRights) {
      Set-EnvironmentVariable -Name $variableName -Value $variableValue -Scope $variableType
    } else {
      $psArgs = "Install-ChocolateyEnvironmentVariable -variableName `'$variableName`' -variableValue `'$variableValue`' -variableType `'$variableType`'"
      Start-ChocolateyProcessAsAdmin "$psArgs"
    }
  } else {
    Set-EnvironmentVariable -Name $variableName -Value $variableValue -Scope $variableType
  }

  Set-Content env:\$variableName $variableValue
}
