// Copyright © 2011 - Present RealDimensions Software, LLC
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// 
// You may obtain a copy of the License at
// 
// 	http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.infrastructure.app.templates
{
    public class ChocolateyUninstallTemplate
    {
        public static string Template =
            @"#NOTE: Please remove any commented lines to tidy up prior to releasing the package, including this one
# REMOVE ANYTHING BELOW THAT IS NOT NEEDED
# If this is an MSI, cleaning up comments is all you need.
# If this is an exe, change installerType and silentArgs
# Auto Uninstaller should be able to detect and handle registry uninstalls (if it is turned on, it is in preview for 0.9.9).

$ErrorActionPreference = 'Stop'; # stop on all errors

$packageName = '[[PackageName]]'
$softwareName = '[[PackageName]]*' #part or all of the Display Name as you see it in Programs and Features. It should be enough to be unique
$installerType = 'MSI' 
#$installerType = 'EXE' 

$silentArgs = '/qn /norestart'
# https://msdn.microsoft.com/en-us/library/aa376931(v=vs.85).aspx
$validExitCodes = @(0, 3010, 1605, 1614, 1641)
if ($installerType -ne 'MSI') {
  # The below is somewhat naive and built for EXE installers
  $silentArgs = '/S' # /s /S /q /Q /quiet /silent /SILENT /VERYSILENT -s - try any of these to get the silent installer
  $validExitCodes = @(0)
}

$uninstalled = $false

Get-ItemProperty  -Path @($machine_key6432,$machine_key, $local_key) `
                  -ErrorAction SilentlyContinue `
  | ? { $_.DisplayName -like ""$softwareName"" } `
  | Select -First 1 `
  | % { 
        $file = ""$($_.UninstallString)""
     
        if ($installerType -eq 'MSI') {
        # The Product Code GUID is all that should be passed for MSI, and very 
        # FIRST, because it comes directly after /x, which is already set in the 
        # Uninstall-ChocolateyPackage msiargs (facepalm).
        $silentArgs = ""$($_.PSChildName) $silentArgs""
        
        # Don't pass anything for file, it is ignored for msi (facepalm number 2) 
        # Alternatively if you need to pass a path to an msi, determine that and 
        # use it instead of the above in silentArgs, still very first
        $file = ''
        }      

        Uninstall-ChocolateyPackage -PackageName $packageName `
                                    -FileType $installerType `
                                    -SilentArgs ""$silentArgs"" `
                                    -ValidExitCodes $validExitCodes `
                                    -File ""$file""
        $uninstalled = $true
      }

if (!($uninstalled)) {
    Write-Warning ""$packageName has already been uninstalled by other means.""
}


## OTHER HELPERS
## https://github.com/chocolatey/choco/wiki/HelpersReference
#Uninstall-ChocolateyZipPackage
#Uninstall-BinFile # Only needed if you added one in the installer script, choco will remove the ones it added automatically.
#remove any shortcuts you added

";
    }
}