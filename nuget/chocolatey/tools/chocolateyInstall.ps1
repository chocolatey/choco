$toolsPath = (Split-Path -parent $MyInvocation.MyCommand.Definition)

# ensure module loading preference is on
$PSModuleAutoLoadingPreference = "All";

$modules = Get-ChildItem $toolsPath -Filter *.psm1
$modules | ForEach-Object {
														$psm1File = $_.FullName;
														$moduleName = $([System.IO.Path]::GetFileNameWithoutExtension($psm1File))
														if (Get-Module $moduleName) {
                              remove-module $moduleName -ErrorAction SilentlyContinue;
                            }
														import-module -name  $psm1File;
													}

Initialize-Chocolatey
