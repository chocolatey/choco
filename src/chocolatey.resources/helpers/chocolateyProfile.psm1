if (Get-Module chocolateyProfile) { return }

$thisDirectory = (Split-Path -parent $MyInvocation.MyCommand.Definition)

. $thisDirectory\functions\Get-EnvironmentVariable.ps1
. $thisDirectory\functions\Get-EnvironmentVariableNames.ps1
. $thisDirectory\functions\Update-SessionEnvironment.ps1
. $thisDirectory\ChocolateyTabExpansion.ps1

Export-ModuleMember -Alias refreshenv -Function 'Update-SessionEnvironment', 'TabExpansion'