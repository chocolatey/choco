# Copyright © 2017 Chocolatey Software, Inc.
# Copyright © 2016 - 2017 Original authors from https://github.com/chocolatey/chocolatey-coreteampackages
# Copyright © 2016 Miodrag Milić - https://github.com/majkinetor/au-packages/commit/bf95d56fe5851ee2e4f6f15f79c1a2877a7950a1
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

# special thanks to the Core Community Maintainers team and their work
# on the Get-PackageParameters function that is in the
# `chocolatey-core.extension` package. It was used as a reference to
# create this function and some of the documentation and naming was
# used. However nearly all of the code is a different implementation.

function Get-PackageParameters {
<#
.SYNOPSIS
Parses a string and returns a hash table array of those values for use
in package scripts.

.DESCRIPTION
This looks at a string value and parses it into a hash table array for
use in package scripts. By default this will look at
`$env:ChocolateyPackageParameters` (`--params="'/ITEM:value'"`) and
`$env:ChocolateyPackageParametersSensitive`
(`--package-parameters-sensitive="'/PASSWORD:value'"` in commercial
editions).

Learn more about using this at https://chocolatey.org/docs/how-to-parse-package-parameters-argument

.NOTES
Available in 0.10.8+. If you need compatibility with older versions,
take a dependency on the `chocolatey-core.extension` package which
also provides this functionality. If you are pushing to the community
package repository (https://chocolatey.org/packages), you are required
to take a dependency on the core extension until January 2018. How to
do this is explained at https://chocolatey.org/docs/how-to-parse-package-parameters-argument#step-3---use-core-community-extension.

The differences between this and the `chocolatey-core.extension` package
functionality is that the extension function can only do one string at a
time and it only looks at `$env:ChocolateyPackageParameters` by default.
It also only supports splitting by `:`, with this function you can
either split by `:` or `=`. For compatibility with the core extension,
build all docs with `/Item:Value`.

.INPUTS
None

.OUTPUTS
[HashTable]

.PARAMETER Parameters
OPTIONAL - Specify a string to parse. If not set, will use
`$env:ChocolateyPackageParameters` and
`$env:ChocolateyPackageParametersSensitive` to parse values from.

Parameters should be passed as "/NAME:value" or "/NAME=value". For
compatibility with `chocolatey-core.extension`, use `:`.

For example `-Parameters "/ITEM1:value /ITEM2:value with spaces"

NOTE: In 0.10.9+, to maintain compatibility with the prior art of the
chocolatey-core.extension method, quotes and apostrophes surrounding
parameter values will be removed. When the param is used, those items
can be added back if desired, but it's most important to ensure that
existing packages are compatible on upgrade.

.PARAMETER IgnoredArguments
Allows splatting with arguments that do not apply and future expansion.
Do not use directly.

.EXAMPLE
>
# The default way of calling, uses `$env:ChocolateyPackageParameters`
# and `$env:ChocolateyPackageParametersSensitive` - this is typically
# how things are passed in from choco.exe
$pp = Get-PackageParameters

.EXAMPLE
>
# see https://chocolatey.org/docs/how-to-parse-package-parameters-argument
# command line call: `choco install <pkg_id> --params "'/LICENSE:value'"`
$pp = Get-PackageParameters
# Read-Host, PromptForChoice, etc are not blocking calls with Chocolatey.
# Chocolatey has a custom PowerShell host that will time these calls
# after 30 seconds, allowing headless operation to continue but offer
# prompts to users to ask questions during installation.
if (!$pp['LICENSE']) { $pp['LICENSE'] = Read-Host 'License key?' }
# set a default if not passed
if (!$pp['LICENSE']) { $pp['LICENSE'] = '1234' }

.EXAMPLE
>
$pp = Get-PackageParameters
if (!$pp['UserName']) { $pp['UserName'] = "$env:UserName" }
# Requires Choocolatey v0.10.8+ for Read-Host -AsSecureString
if (!$pp['Password']) { $pp['Password'] = Read-Host "Enter password for $($pp['UserName']):" -AsSecureString}
# fail the install/upgrade if not value is not determined
if (!$pp['Password']) { throw "Package needs Password to install, that must be provided in params or in prompt." }

.EXAMPLE
>
# Pass in your own values
Get-PackageParameters -Parameters "/Shortcut /InstallDir:'c:\program files\xyz' /NoStartup" | set r
if ($r.Shortcut) {... }
Write-Host $r.InstallDir

.LINK
Install-ChocolateyPackage

.LINK
Install-ChocolateyInstallPackage

.LINK
Install-ChocolateyZipPackage
#>
param(
 [parameter(Mandatory=$false, Position=0)]
 [alias("params")][string] $parameters = '',
 [parameter(ValueFromRemainingArguments = $true)][Object[]] $ignoredArguments
)

  Write-FunctionCallLogMessage -Invocation $MyInvocation -Parameters $PSBoundParameters

  $useDefaultParameters = $false
  $loggingAllowed = $true
  $paramStrings = @($parameters)

  if (!$parameters -or $parameters -eq '') {
    $useDefaultParameters = $true
    # if we are using default parameters, we are going to loop over two items
    Write-Debug 'Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters'
    $paramStrings = @("$env:ChocolateyPackageParameters","$env:ChocolateyPackageParametersSensitive")
    if ($env:ChocolateyPackageParametersSensitive) {
      Write-Debug "Sensitive parameters detected, no logging of parameters."
      $loggingAllowed = $false
    }
  }

  $paramHash = @{}

  foreach ($paramString in $paramStrings) {
    if (!$paramString -or $paramString -eq '') { continue }

    Select-String '(?:^|\s+)\/(?<ItemKey>[^\:\=\s)]+)(?:(?:\:|=){1}(?:\''|\"){0,1}(?<ItemValue>.*?)(?:\''|\"){0,1}(?:(?=\s+\/)|$))?' -Input $paramString -AllMatches | % { $_.Matches } | % {
      if (!$_) { continue } #Posh v2 issue?
      $paramItemName = ($_.Groups["ItemKey"].Value).Trim()
      $paramItemValue = ($_.Groups["ItemValue"].Value).Trim()
      if (!$paramItemValue -or $paramItemValue -eq '') { $paramItemValue = $true }

      if ($loggingAllowed) { Write-Debug "Adding package param '$paramItemName'='$paramItemValue'" }
      $paramHash[$paramItemName] = $paramItemValue
    }
  }

  $paramHash
}

# override Get-PackageParameters in chocolatey-core.extension package
Copy-Item Function:Get-PackageParameters Function:Get-PackageParametersBuiltIn -Force
#Rename-Item Function:Get-PackageParameters Get-PackageParametersBuiltIn
Set-Alias -Name Get-PackageParameters -Value Get-PackageParametersBuiltIn -Scope Global
