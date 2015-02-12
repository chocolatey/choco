# Copyright 2011 - Present RealDimensions Software, LLC & original authors/contributors from https://github.com/chocolatey/chocolatey
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

$DebugPreference = "SilentlyContinue"
if ($env:ChocolateyEnvironmentDebug -eq 'true') { $DebugPreference = "Continue"; }
$VerbosePreference = "SilentlyContinue"
if ($env:ChocolateyEnvironmentVerbose -eq 'true') { $VerbosePreference = "Continue"; $verbosity = $true }

Write-Debug "Posh version is $($psversiontable.PsVersion.ToString())"

# grab functions from files
Get-Item $helpersPath\functions\*.ps1 | 
  ? { -not ($_.Name.Contains(".Tests.")) } |
    % { 
	  . $_.FullName;  
	  Export-ModuleMember -Function $_.BaseName
    }