$scriptRoot = Split-Path $MyInvocation.MyCommand.Definition

. "$scriptRoot\Test-ExtensionAvailable.ps1"
Export-ModuleMember -Function "Test-ExtensionAvailable"