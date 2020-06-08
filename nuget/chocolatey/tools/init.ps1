param($installPath, $toolsPath, $package, $project)

$modules = Get-ChildItem $ToolsPath -Filter *.psm1
$modules | ForEach-Object { import-module -name  $_.FullName }

@"
========================
Chocolatey
========================
Welcome to Chocolatey, your local machine repository built on the NuGet infrastructure. Chocolatey allows you to install application packages to your machine with the goodness of a #chocolatey #nuget combo. 
Application executables get added to the path automatically so you can call them from anywhere (command line/powershell prompt), not just in Visual Studio.

Lets get Chocolatey!
----------
Visual Studio -
----------
Please run Initialize-Chocolatey one time per machine to set up the repository. 
If you are upgrading, please remember to run Initialize-Chocolatey again.
After you have run Initialize-Chocolatey, you can safely uninstall the chocolatey package from your current Visual Studio solution.
----------
Alternative NuGet -
----------
If you are not using NuGet in Visual Studio, please navigate to the directory with the chocolateysetup.psm1 and run that in Powershell, followed by Initialize-Chocolatey.
Upgrade is the same, just run Initialize-Chocolatey again.
----------
Once you've run initialize or upgrade, you can uninstall this package from the local project without affecting your chocolatey repository.
========================
"@ | Write-Host
