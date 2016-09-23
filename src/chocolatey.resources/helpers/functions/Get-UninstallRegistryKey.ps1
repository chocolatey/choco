# Copyright © 2011 - Present RealDimensions Software, LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
#
# You may obtain a copy of the License at
#
#   http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

function Get-UninstallRegistryKey {
<#
.SYNOPSIS
Retrieve registry key(s) for system-installed applications from an
exact or wildcard search.

.DESCRIPTION
This function will attempt to retrieve a matching registry key for an
already installed application, usually to be used with a
chocolateyUninstall.ps1 automation script.

The function also prevents `Get-ItemProperty` from failing when
handling wrongly encoded registry keys.

.NOTES
Available in 0.9.10+. If you need to maintain compatibility with pre
0.9.10, please add the following to your nuspec:

~~~xml
<dependencies>
  <dependency id="chocolatey-uninstall.extension" />
</dependencies>
~~~

.INPUTS
String

.OUTPUTS
This function searches registry objects and returns PSCustomObject of
the matched key's properties.

Retrieve properties with dot notation, for example:
`$key.UninstallString`


.PARAMETER SoftwareName
Part or all of the Display Name as you see it in Programs and Features.
It should be enough to be unique.

If the display name contains a version number, such as "Launchy 2.5",
it is recommended you use a fuzzy search `"Launchy*"` (the wildcard `*`)
as if the version is upgraded or autoupgraded, suddenly the uninstall
script will stop working and it may not be clear as to what went wrong
at first.

.EXAMPLE
>
# Software name in Programs and Features is "Gpg4Win (2.3.0)"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Gpg4win*"
$key.DisplayName

.EXAMPLE
>
# Software name is "Launchy 2.5"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Launchy*"
$key.UninstallString

.EXAMPLE
>
# Software name is "Mozilla Firefox"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Mozilla Firefox"
$key.UninstallString

.LINK
Install-ChocolateyPackage

.LINK
Install-ChocolateyInstallPackage

.LINK
Uninstall-ChocolateyPackage
#>
[CmdletBinding()]
param(
  [parameter(Mandatory=$true, Position=0, ValueFromPipeline=$true)]
  [string] $softwareName,
  [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  if ($softwareName -eq $null -or $softwareName -eq '') {
    throw "$SoftwareName cannot be empty for Get-UninstallRegistryKey"
  }

  $ErrorActionPreference = 'Stop'
  $local_key       = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*'
  $machine_key     = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*'
  $machine_key6432 = 'HKLM:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'

  Write-Verbose "Retrieving all uninstall registry keys"
  [array]$keys = Get-ChildItem -Path @($machine_key6432, $machine_key, $local_key) `
                               -ErrorAction SilentlyContinue

  Write-Debug "Error handling check: Get-ItemProperty will fail if a registry key is written incorrectly."
  Write-Debug "If such a key is found, loop up to 10 times to try to bypass all badKeys"
  [int]$maxAttempts = 10
  for ([int]$attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    [bool]$success = $FALSE

    try {
      [array]$foundKey = Get-ItemProperty -Path $keys.PsPath `
                                          -ErrorAction SilentlyContinue `
                         | Where-Object { $_.DisplayName -like $softwareName }
      $success = $TRUE
    } catch {
      Write-Debug "Found bad key."
      foreach ($key in $keys){ try { Get-ItemProperty $key.PsPath > $null } catch { $badKey = $key.PsPath }}
      Write-Verbose "Skipping bad key: $($key.PsPath)"
      [array]$keys = Get-ChildItem -Path @($machine_key6432, $machine_key, $local_key) `
                                   -ErrorAction SilentlyContinue `
                     | Where-Object { $badKey -NotContains $_.PsPath }
    }

    if ($success) { break; }
  }

  return $foundKey
}

Set-Alias Get-InstallRegistryKey Get-UninstallRegistryKey
