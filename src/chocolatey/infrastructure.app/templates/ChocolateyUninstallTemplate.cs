﻿// Copyright © 2017 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
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
            @"# IMPORTANT: Before releasing this package, copy/paste the next 2 lines into PowerShell to remove all comments from this file:
#   $f='c:\path\to\thisFile.ps1'
#   gc $f | ? {$_ -notmatch ""^\s*#""} | % {$_ -replace '(^.*?)\s*?[^``]#.*','$1'} | Out-File $f+"".~"" -en utf8; mv -fo $f+"".~"" $f

## NOTE: In 80-90% of the cases (95% with licensed versions due to Package Synchronizer and other enhancements), you may 
## not need this file due to AutoUninstaller. See https://chocolatey.org/docs/commands-uninstall

## If this is an MSI, cleaning up comments is all you need.
## If this is an exe, change installerType and silentArgs
## Auto Uninstaller should be able to detect and handle registry uninstalls (if it is turned on, it is in preview for 0.9.9).
## - https://chocolatey.org/docs/helpers-uninstall-chocolatey-package

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
  # Uncomment matching EXE type (sorted by most to least common)
  #$silentArgs = '/S'           # NSIS
  #$silentArgs = '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-' # Inno Setup
  #$silentArgs = '/s'           # InstallShield
  #$silentArgs = '/s /v""/qn""' # InstallShield with MSI
  #$silentArgs = '/s'           # Wise InstallMaster
  #$silentArgs = '-s'           # Squirrel
  #$silentArgs = '-q'           # Install4j
  #$silentArgs = '-s -u'        # Ghost
  # Note that some installers, in addition to the silentArgs above, may also need assistance of AHK to achieve silence.
  #$silentArgs = ''             # none; make silent with input macro script like AutoHotKey (AHK)
                                #       https://chocolatey.org/packages/autohotkey.portable
  $validExitCodes = @(0)
}

$uninstalled = $false
# Get-UninstallRegistryKey is new to 0.9.10, if supporting 0.9.9.x and below,
# take a dependency on ""chocolatey-uninstall.extension"" in your nuspec file.
# This is only a fuzzy search if $softwareName includes '*'. Otherwise it is 
# exact. In the case of versions in key names, we recommend removing the version
# and using '*'.
[array]$key = Get-UninstallRegistryKey -SoftwareName $softwareName

if ($key.Count -eq 1) {
  $key | % { 
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
  }
} elseif ($key.Count -eq 0) {
  Write-Warning ""$packageName has already been uninstalled by other means.""
} elseif ($key.Count -gt 1) {
  Write-Warning ""$($key.Count) matches found!""
  Write-Warning ""To prevent accidental data loss, no programs will be uninstalled.""
  Write-Warning ""Please alert package maintainer the following keys were matched:""
  $key | % {Write-Warning ""- $($_.DisplayName)""}
}


## OTHER HELPERS
## https://chocolatey.org/docs/helpers-reference
#Uninstall-ChocolateyZipPackage $packageName # Only necessary if you did not unpack to package directory - see https://chocolatey.org/docs/helpers-uninstall-chocolatey-zip-package
#Uninstall-ChocolateyEnvironmentVariable # 0.9.10+ - https://chocolatey.org/docs/helpers-uninstall-chocolatey-environment-variable 
#Uninstall-BinFile # Only needed if you used Install-BinFile - see https://chocolatey.org/docs/helpers-uninstall-bin-file
## Remove any shortcuts you added

";
    }
}
