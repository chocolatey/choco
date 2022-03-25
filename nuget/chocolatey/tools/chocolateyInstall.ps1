$toolsPath = Split-Path -Parent $MyInvocation.MyCommand.Definition

# Ensure module loading preference is on
$PSModuleAutoLoadingPreference = "All"

$modules = Get-ChildItem $toolsPath -Filter *.psm1
$modules | ForEach-Object {
	  $psm1File = $_.FullName
	  $moduleName = [System.IO.Path]::GetFileNameWithoutExtension($psm1File)

	  if (Get-Module $moduleName) {
        Remove-Module $moduleName -ErrorAction SilentlyContinue
    }

	  Import-Module -Name $psm1File
}

Initialize-Chocolatey
