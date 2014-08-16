$helpersPath = (Split-Path -parent $MyInvocation.MyCommand.Definition);

$DebugPreference = "SilentlyContinue"
if ($env:ChocolateyEnvironmentDebug -eq 'true') {$DebugPreference = "Continue";}


# grab functions from files
Resolve-Path $helpersPath\functions\*.ps1 |
    ? { -not ($_.ProviderPath.Contains(".Tests.")) } |
    % { . $_.ProviderPath }

Export-ModuleMember -Function `
	Get-BinRoot,`
	Get-CheckSumValid,`
	Get-ChocolateyWebFile,`
	Get-ChocolateyUnzip,`
	Get-ProcessorBits,`
	Get-UACEnabled,`
	Install-ChocolateyInstallPackage,`
	Install-ChocolateyPackage,`
	Install-ChocolateyZipPackage,`
	Install-ChocolateyPowershellCommand,`
	Install-ChocolateyPath,`
	Install-ChocolateyDesktopLink,`
	Install-ChocolateyPinnedTaskBarItem,`
	Install-ChocolateyExplorerMenuItem,`
	Install-ChocolateyFileAssociation,`
	Install-ChocolateyEnvironmentVariable,`
	Install-ChocolateyVsixPackage,`
	Write-ChocolateySuccess,`
	Write-ChocolateyFailure,`
	Write-Host,`Write-Debug,`Write-Error,`
	Start-ChocolateyProcessAsAdmin,`
	Test-ProcessAdminRights,`
	Uninstall-ChocolateyPackage,`
	Update-SessionEnvironment,`
	Get-EnvironmentVariableNames,`
	Get-EnvironmentVariable,`
	Set-EnvironmentVariable
