function Set-EnvironmentVariable([string] $Name, [string] $Value, [System.EnvironmentVariableTarget] $Scope) {
    [Environment]::SetEnvironmentVariable($Name, $Value, $Scope)
}
