# Copyright © 2017 Chocolatey Software, Inc.
# Copyright © 2011 - 2017 RealDimensions Software, LLC
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
0.9.10, please add the following to your nuspec (check for minimum
version):

~~~xml
<dependencies>
  <dependency id="chocolatey-core.extension" version="1.1.0" />
</dependencies>
~~~

.INPUTS
String

.OUTPUTS
This function searches registry objects and returns an array
of PSCustomObject with the matched key's properties.

Retrieve properties with dot notation, for example:
`$key.UninstallString`


.PARAMETER SoftwareName
Part or all of the Display Name as you see it in Programs and Features.
It should be enough to be unique.
The syntax follows the rules of the PowerShell `-like` operator, so the
`*` character is interpreted as a wildcard, which matches any (zero or
more) characters.

If the display name contains a version number, such as "Launchy (2.5)",
it is recommended you use a fuzzy search `"Launchy (*)"` (the wildcard
`*`) so if Launchy auto-updates or is updated outside of Chocolatey, the
uninstall script will not fail.

Take care not to abuse fuzzy/glob pattern searches. Be conscious of
programs that may have shared or common root words to prevent
overmatching. For example, "SketchUp*" would match two keys with
software names "SketchUp 2016" and "SketchUp Viewer" that are different
programs released by the same company.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply. Do not use directly.

.EXAMPLE
>
# Version match: Software name is "Gpg4Win (2.3.0)"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Gpg4win (*)"
$key.UninstallString

.EXAMPLE
>
# Fuzzy match: Software name is "Launchy 2.5"
[array]$key = Get-UninstallRegistryKey -SoftwareName "Launchy*"
$key.UninstallString

.EXAMPLE
>
# Exact match: Software name in Programs and Features is "VLC media player"
[array]$key = Get-UninstallRegistryKey -SoftwareName "VLC media player"
$key.UninstallString

.EXAMPLE
>
#  Version match: Software name is "SketchUp 2016"
# Note that the similar software name "SketchUp Viewer" would not be matched.
[array]$key = Get-UninstallRegistryKey -SoftwareName "SketchUp [0-9]*"
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
  [array]$keys = Get-ChildItem -Path @($machine_key6432, $machine_key, $local_key) -ErrorAction SilentlyContinue
  Write-Debug "Registry uninstall keys on system: $($keys.Count)"

  Write-Debug "Error handling check: `'Get-ItemProperty`' fails if a registry key is encoded incorrectly."
  [int]$maxAttempts = $keys.Count
  for ([int]$attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    [bool]$success = $false

    $keyPaths = $keys | Select-Object -ExpandProperty PSPath
    try {
      [array]$foundKey = Get-ItemProperty -Path $keyPaths -ErrorAction Stop | ? { $_.DisplayName -like $softwareName }
      $success = $true
    } catch {
      Write-Debug "Found bad key."
      foreach ($key in $keys){
        try {
          Get-ItemProperty $key.PsPath > $null
        } catch {
          $badKey = $key.PsPath
        }
      }
      Write-Verbose "Skipping bad key: $badKey"
      [array]$keys = $keys | ? { $badKey -NotContains $_.PsPath }
    }

    if ($success) { break; }

    if ($attempt -ge 10) {
      Write-Warning "Found 10 or more bad registry keys. Run command again with `'--verbose --debug`' for more info."
      Write-Debug "Each key searched should correspond to an installed program. It is very unlikely to have more than a few programs with incorrectly encoded keys, if any at all. This may be indicative of one or more corrupted registry branches."
    }
  }

  if ($foundKey -eq $null -or $foundkey.Count -eq 0) {
    Write-Warning "No registry key found based on  '$softwareName'"
  }

  Write-Debug "Found $($foundKey.Count) uninstall registry key(s) with SoftwareName:`'$SoftwareName`'";

  return $foundKey
}

Set-Alias Get-InstallRegistryKey Get-UninstallRegistryKey
