﻿# Copyright 2011 - Present RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

$helpersPath = (Split-Path -parent $MyInvocation.MyCommand.Definition);

$global:DebugPreference = "SilentlyContinue"
if ($env:ChocolateyEnvironmentDebug -eq 'true') { $global:DebugPreference = "Continue"; }
$global:VerbosePreference = "SilentlyContinue"
if ($env:ChocolateyEnvironmentVerbose -eq 'true') { $global:VerbosePreference = "Continue"; $verbosity = $true }

$installArguments = $env:chocolateyInstallArguments

$overrideArgs = $false
if ($env:chocolateyInstallOverride -eq 'true') { $overrideArgs = $true }

$forceX86 = $false
if ($env:chocolateyForceX86 -eq 'true') { $forceX86 = $true }

$packageParameters = $env:chocolateyPackageParameters

# ensure module loading preference is on
$PSModuleAutoLoadingPreference = "All";

Write-Debug "Host version is $($host.Version), PowerShell Version is '$($PSVersionTable.PSVersion)' and CLR Version is '$($PSVersionTable.CLRVersion)'."

# grab functions from files
Get-Item $helpersPath\functions\*.ps1 |
  ? { -not ($_.Name.Contains(".Tests.")) } |
    % {
	  . $_.FullName;
	  #Export-ModuleMember -Function $_.BaseName
    }

# load extensions if they exist
$extensionsPath = Join-Path "$helpersPath" '..\extensions'
if (Test-Path($extensionsPath)) {
  Write-Debug 'Loading community extensions'
  #Resolve-Path $extensionsPath\**\*\*.psm1 | % { Write-Debug "Importing `'$_`'"; Import-Module $_.ProviderPath }
  Get-ChildItem $extensionsPath -recurse -filter "*.psm1" | Select -ExpandProperty FullName | % { Write-Debug "Importing `'$_`'"; Import-Module $_; }
  Get-ChildItem $extensionsPath -recurse -filter "*.dll" | Select -ExpandProperty FullName | % {
    $path = $_;
    if ($path.Contains("extensions\chocolatey\lib-synced")) { continue }

    try {
      Write-Debug "Importing '$path'";
      Write-Debug "Loading '$([System.IO.Path]::GetFileNameWithoutExtension($path))' extension.";
      Import-Module $path;
    } catch {
      if ($env:ChocolateyPowerShellHost -eq 'true') {
        Write-Warning "Import failed for '$path'.  Error: '$_'"
      } else {
        Write-Warning "Import failed for '$path'. If it depends on a newer version of the .NET framework, please make sure you are using the built-in PowerShell Host. Error: '$_'"
      }
    }
  }
}

Export-ModuleMember -Function * -Alias * -Cmdlet *
