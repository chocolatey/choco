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

# Special thanks to Glenn Sarti (https://github.com/glennsarti) for his help on this.

$ErrorActionPreference = 'Stop'

$thisDirectory = (Split-Path -parent $MyInvocation.MyCommand.Definition);
$psModuleName = 'chocolateyInstaller'
$psModuleLocation = [System.IO.Path]::GetFullPath("$thisDirectory\..\src\chocolatey.resources\helpers\chocolateyInstaller.psm1")
$docsFolder = [System.IO.Path]::GetFullPath("$thisDirectory\..\docs\generated")
$chocoExe = [System.IO.Path]::GetFullPath("$thisDirectory\..\code_drop\chocolatey\console\choco.exe")
$lineFeed = "`r`n"
$global:powerShellReferenceTOC = @'
# PowerShell Functions aka Helpers Reference

## Main Functions

These functions call other functions and many times may be the only thing you need in your [[chocolateyInstall.ps1 file|ChocolateyInstallPS1]].

* [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
* [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]
* [[Install-ChocolateyPowershellCommand|HelpersInstallChocolateyPowershellCommand]]
* [[Install-ChocolateyVsixPackage|HelpersInstallChocolateyVsixPackage]]

## Error / Success Functions

* [[Write-ChocolateySuccess|HelpersWriteChocolateySuccess]] - **DEPRECATED**
* [[Write-ChocolateyFailure|HelpersWriteChocolateyFailure]] - **DEPRECATED**

You really don't need a try catch with Chocolatey PowerShell files anymore.

## More Functions

### Administrative Access Functions

When creating packages that need to run one of the following commands below, one should add the tag `admin` to the nuspec.

* [[Install-ChocolateyPackage|HelpersInstallChocolateyPackage]]
* [[Start-ChocolateyProcessAsAdmin|HelpersStartChocolateyProcessAsAdmin]]
* [[Install-ChocolateyInstallPackage|HelpersInstallChocolateyInstallPackage]]
* [[Install-ChocolateyPath|HelpersInstallChocolateyPath]] - when specifying machine path
* [[Install-ChocolateyEnvironmentVariable|HelpersInstallChocolateyEnvironmentVariable]] - when specifying machine path
* [[Install-ChocolateyExplorerMenuItem|HelpersInstallChocolateyExplorerMenuItem]]
* [[Install-ChocolateyFileAssociation|HelpersInstallChocolateyFileAssociation]]

### Non-Administrator Safe Functions

When you have a need to run Chocolatey without Administrative access required (non-default install location), you can run the following functions without administrative access.

These are the functions from above as one list.

* [[Install-ChocolateyZipPackage|HelpersInstallChocolateyZipPackage]]
* [[Install-ChocolateyPowershellCommand|HelpersInstallChocolateyPowershellCommand]]
* [[Write-ChocolateySuccess|HelpersWriteChocolateySuccess]]
* [[Write-ChocolateyFailure|HelpersWriteChocolateyFailure]]
* [[Get-ChocolateyWebFile|HelpersGetChocolateyWebFile]]
* [[Get-ChocolateyUnzip|HelpersGetChocolateyUnzip]]
* [[Install-ChocolateyPath|HelpersInstallChocolateyPath]] - when specifying user path
* [[Install-ChocolateyEnvironmentVariable|HelpersInstallChocolateyEnvironmentVariable]] - when specifying user path
* [[Install-ChocolateyDesktopLink|HelpersInstallChocolateyDesktopLink]] - **DEPRECATED** - see [[Install-ChocolateyShortcut|HelpersInstallChocolateyShortcut]]
* [[Install-ChocolateyPinnedTaskBarItem|HelpersInstallChocolateyPinnedTaskBarItem]]
* [[Install-ChocolateyShortcut|HelpersInstallChocolateyShortcut]] - v0.9.9+
* [[Update-SessionEnvironment|HelpersUpdateSessionEnvironment]]

## Complete List (alphabetical order)

'@

function Convert-Example($objItem) {
  @"
**$($objItem.title.Replace('-','').Trim())**

~~~powershell
$($objItem.Code.Replace("`n",$lineFeed))
$($objItem.remarks | ? { $_.Text -ne ''} | % { Write-Output $_.Text.Replace("`n", $lineFeed) })
~~~
"@
}

function Replace-CommonItems($text) {
  if ($text -eq $null) {return $text}

  $text = $text.Replace("`n",$lineFeed)
  $text = $text -replace '(community feed[s]?|community repository)', '[$1](https://chocolatey.org/packages)'
  $text = $text -replace '(Chocolatey for Business|Chocolatey Pro[fessional]*)', '[$1](https://chocolatey.org/compare)'
  $text = $text -replace '(Pro[fessional]\s?/\s?Business)', '[$1](https://chocolatey.org/compare)'
  $text = $text -replace '([Ll]icensed editions)', '[$1](https://chocolatey.org/compare)'
  $text = $text -replace '([Ll]icensed versions)', '[$1](https://chocolatey.org/compare)'

  Write-Output $text
}

function Convert-Syntax($objItem, $hasCmdletBinding) {
  $cmd = $objItem.Name

  if ($objItem.parameter -ne $null) {
    $objItem.parameter | % {
      $cmd += ' `' + $lineFeed
      $cmd += "  "
      if ($_.required -eq $false) { $cmd += '['}
      $cmd += "-$($_.name.substring(0,1).toupper() + $_.name.substring(1))"


      if ($_.parameterValue -ne $null) { $cmd += " <$($_.parameterValue)>" }
      if ($_.parameterValueGroup -ne $null) { $cmd += " {" + ($_.parameterValueGroup.parameterValue -join ' | ') + "}"}
      if ($_.required -eq $false) { $cmd += ']'}
    }
  }
  if ($hasCmdletBinding) { $cmd += " [<CommonParameters>]"}
  Write-Output "$lineFeed~~~powershell$lineFeed$($cmd)$lineFeed~~~"
}

function Convert-Parameter($objItem, $commandName) {
  $parmText = $lineFeed + "###  -$($objItem.name.substring(0,1).ToUpper() + $objItem.name.substring(1))"
  if ( ($objItem.parameterValue -ne $null) -and ($objItem.parameterValue -ne 'SwitchParameter') ) {
    $parmText += ' '
    if ([string]($objItem.required) -eq 'false') { $parmText += "["}
    $parmText += "&lt;$($objItem.parameterValue)&gt;"
    if ([string]($objItem.required) -eq 'false') { $parmText += "]"}
  }
  $parmText += $lineFeed
  if ($objItem.description -ne $null) {
    $parmText += (($objItem.description | % { Replace-CommonItems $_.Text }) -join "$lineFeed") + $lineFeed + $lineFeed
  }
  if ($objItem.parameterValueGroup -ne $null) {
    $parmText += "$($lineFeed)Valid options: " + ($objItem.parameterValueGroup.parameterValue -join ", ") + $lineFeed + $lineFeed
  }

  $aliases = [string]((Get-Command -Name $commandName).parameters."$($objItem.Name)".Aliases -join ', ')
  $required = [string]($objItem.required)
  $position = [string]($objItem.position)
  $defValue = [string]($objItem.defaultValue)
  $acceptPipeline = [string]($objItem.pipelineInput)

  $padding = ($aliases.Length, $required.Length, $position.Length, $defValue.Length, $acceptPipeline.Length | Measure-Object -Maximum).Maximum

    $parmText += @"
Property               | Value
---------------------- | $([string]('-' * $padding))
Aliases                | $($aliases)
Required?              | $($required)
Position?              | $($position)
Default Value          | $($defValue)
Accept Pipeline Input? | $($acceptPipeline)

"@

  Write-Output $parmText
}

function Convert-CommandText {
param(
  [string]$commandText,
  [string]$commandName = ''
)
  if ( $commandText -match '^\s?NOTE: Options and switches apply to all items passed, so if you are\s?$' `
   -or $commandText -match '^\s?installing multiple packages, and you use \`\-\-version\=1\.0\.0\`, it is\s?$' `
   -or $commandText -match '^\s?going to look for and try to install version 1\.0\.0 of every package\s?$' `
   -or $commandText -match '^\s?passed\. So please split out multiple package calls when wanting to\s?$' `
   -or $commandText -match '^\s?pass specific options\.\s?$' `
     ) {
    return
  }
  $commandText = $commandText -creplace '^(.+)(\s+Command\s*)$', "# `$1`$2 (choco $commandName)"
  $commandText = $commandText -replace '^(Usage|Troubleshooting|Examples|Connecting to Chocolatey.org|See It In Action|Alternative Sources|Resources|Packages.config)', '## $1'
  $commandText = $commandText -replace '^(Commands|How To Pass Options)', '## $1'
  $commandText = $commandText -replace '^(WebPI|Windows Features|Ruby|Cygwin|Python)\s*$', '### $1'
  $commandText = $commandText -replace 'NOTE\:', '**NOTE:**'
  $commandText = $commandText -replace 'the command reference', '[[how to pass arguments|CommandsReference#how-to-pass-options--switches]]'
  $commandText = $commandText -replace '(community feed[s]?|community repository)', '[$1](https://chocolatey.org/packages)'
  #$commandText = $commandText -replace '\`(apikey|install|upgrade|uninstall|list|search|info|outdated|pin)\`', '[[`$1`|Commands$1]]'
  $commandText = $commandText -replace '\`([choco\s]*)(apikey|install|upgrade|uninstall|list|search|info|outdated|pin)\`', '[[`$1$2`|Commands$2]]'
  $commandText = $commandText -replace '^(.+):\s(.+.gif)$', '![$1]($2)'
  $commandText = $commandText -replace '^(\s+)\<\?xml', "~~~xml$lineFeed`$1<?xml"
  $commandText = $commandText -replace '^(\s+)</packages>', "`$1</packages>$lineFeed~~~"
  $commandText = $commandText -replace '(Chocolatey for Business|Chocolatey Pro[fessional]*)', '[$1](https://chocolatey.org/compare)'
  $commandText = $commandText -replace '(Pro[fessional]\s?/\s?Business)', '[$1](https://chocolatey.org/compare)'
  $commandText = $commandText -replace '([Ll]icensed editions)', '[$1](https://chocolatey.org/compare)'
  $commandText = $commandText -replace '([Ll]icensed versions)', '[$1](https://chocolatey.org/compare)'

  $optionsSwitches = @'
## $1

**NOTE:** Options and switches apply to all items passed, so if you are
 running a command like install that allows installing multiple
 packages, and you use `--version=1.0.0`, it is going to look for and
 try to install version 1.0.0 of every package passed. So please split
 out multiple package calls when wanting to pass specific options.

Includes [[default options/switches|CommandsReference#default-options-and-switches]] (included below for completeness).

~~~
'@

  $commandText = $commandText -replace '^(Options and Switches)', $optionsSwitches

   $optionsSwitches = @'
## $1

**NOTE:** Options and switches apply to all items passed, so if you are
 running a command like install that allows installing multiple
 packages, and you use `--version=1.0.0`, it is going to look for and
 try to install version 1.0.0 of every package passed. So please split
 out multiple package calls when wanting to pass specific options.

~~~
'@

  $commandText = $commandText -replace '^(Default Options and Switches)', $optionsSwitches

  Write-Output $commandText
}

function Convert-CommandReferenceSpecific($commandText) {
  $commandText = $commandText -replace '\s?\s?\*\s(\w+)\s\-', ' * [[$1|Commands$1]] -'
  $commandText = $commandText.Replace("## Default Options and Switches", "## See Help Menu In Action$lineFeed$lineFeed![choco help in action](https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_help.gif)$lineFeed$lineFeed## Default Options and Switches")

  Write-Output $commandText
}

function Generate-TopLevelCommandReference {
  Write-Host "Generating Top Level Command Reference"
  $fileName = "$docsFolder\CommandsReference.md"
  $commandOutput = @("# Command Reference$lineFeed")
  $commandOutput += $(& $chocoExe -? -r)
  $commandOutput += @("$lineFeed~~~$lineFeed")
  $commandOutput += @("$lineFeed$lineFeed*NOTE:* This documentation has been automatically generated from ``choco -h``. $lineFeed")

  $commandOutput | %{ Convert-CommandText($_) } | %{ Convert-CommandReferenceSpecific($_) } | Out-File $fileName -Encoding UTF8 -Force
}

function Generate-CommandReference($commandName) {
  $fileName = Join-Path $docsFolder "Commands$($commandName.substring(0,1).toupper() + $commandName.substring(1)).md"
  Write-Host "Generating $fileName ..."
  $commandOutput += $(& $chocoExe $commandName -h -r)
  $commandOutput += @("$lineFeed~~~$lineFeed$lineFeed[[Command Reference|CommandsReference]]")
  $commandOutput += @("$lineFeed$lineFeed*NOTE:* This documentation has been automatically generated from ``choco $commandName -h``. $lineFeed")
  $commandOutput | %{ Convert-CommandText $_ $commandName } | Out-File $fileName -Encoding UTF8 -Force
}

try
{
  Write-Host "Importing the Module $psModuleName ..."
  Import-Module "$psModuleLocation" -Force -Verbose

  if (Test-Path($docsFolder)) { Remove-Item $docsFolder -Force -Recurse -EA SilentlyContinue }
  if(-not(Test-Path $docsFolder)){ mkdir $docsFolder -EA Continue | Out-Null }

  Write-Host 'Creating per PowerShell function markdown files...'
  Get-Command -Module $psModuleName | ForEach-Object -Process { Get-Help $_ -Full } | ForEach-Object -Process { `
    $commandName = $_.Name
    $fileName = Join-Path $docsFolder "Helpers$($_.Name.Replace('-','')).md"
    $global:powerShellReferenceTOC += "$lineFeed * [[$commandName|$([System.IO.Path]::GetFileNameWithoutExtension($fileName))]]"
    $hasCmdletBinding = (Get-Command -Name $commandName).CmdLetBinding

    Write-Host "Generating $fileName ..."
    @"
# $($_.Name)

$(Replace-CommonItems $_.Synopsis)

## Syntax
$( ($_.syntax.syntaxItem | % { Convert-Syntax $_ $hasCmdletBinding }) -join "$lineFeed$lineFeed")
$( if ($_.description -ne $null) { $lineFeed + "## Description" + $lineFeed + $lineFeed + $(Replace-CommonItems $_.description.Text) })
$( if ($_.alertSet -ne $null) { $lineFeed + "## Notes" + $lineFeed + $lineFeed +  $(Replace-CommonItems $_.alertSet.alert.Text) })

## Aliases

$( if ($_.aliases -ne $null) { $_.aliases } else { 'None'} )

## Inputs

$( if ($_.InputTypes -ne $null -and $_.InputTypes.Length -gt 0 -and -not $_.InputTypes.Contains('inputType')) { $lineFeed + " * $($_.InputTypes)" + $lineFeed} else { 'None'})

## Outputs

$( if ($_.ReturnValues -ne $null -and $_.ReturnValues.Length -gt 0 -and -not $_.ReturnValues.StartsWith('returnValue')) { "$lineFeed * $($_.ReturnValues)$lineFeed"} else { 'None'})

## Parameters
$( if ($_.parameters.parameter.count -gt 0) { $_.parameters.parameter | % { Convert-Parameter $_ $commandName }}) $( if ($hasCmdletBinding) { "$lineFeed### &lt;CommonParameters&gt;$lineFeed$($lineFeed)This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see ``about_CommonParameters`` http://go.microsoft.com/fwlink/p/?LinkID=113216 ." } )

$( if ($_.Examples -ne $null) { Write-Output "$lineFeed## Examples$lineFeed$lineFeed"; ($_.Examples.Example | % { Convert-Example $_ }) -join "$lineFeed$lineFeed" })
$( if ($_.relatedLinks -ne $null) {Write-Output "$lineFeed## Links$lineFeed$lineFeed"; $_.relatedLinks.navigationLink | ? { $_.linkText -ne $null} | % { Write-Output "* [[$($_.LinkText)|Helpers$($_.LinkText.Replace('-',''))]]$lineFeed" }})

[[Function Reference|HelpersReference]]

***NOTE:*** This documentation has been automatically generated from ``Import-Module `"`$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1`" -Force; Get-Help $($_.Name) -Full``.
"@  | Out-File $fileName -Encoding UTF8 -Force
  }

  Write-Host "Generating Top Level PowerShell Reference"
  $fileName = Join-Path $docsFolder 'HelpersReference.md'

  $global:powerShellReferenceTOC += @'

## Variables

There are also a number of environment variables providing access to some values from the nuspec and other information that may be useful. They are accessed via `$env:variableName`.

* __chocolateyPackageFolder__ = the folder where Chocolatey has downloaded and extracted the NuGet package, typically `C:\ProgramData\chocolatey\lib\packageName`.
* __chocolateyPackageName__ (since 0.9.9.0) = The package name, which is equivalent to the `<id>` tag in the nuspec
* __chocolateyPackageVersion__ (since 0.9.9.0) = The package version, which is equivalent to the `<version>` tag in the nuspec

`chocolateyPackageVersion` may be particularly useful, since that would allow you in some cases to create packages for new releases of the updated software by only changing the `<version>` in the nuspec and not having to touch the `chocolateyInstall.ps1` at all. An example of this:

~~~powershell
$url = "http://www.thesoftware.com/downloads/thesoftware-$env:chocolateyPackageVersion.zip"

Install-ChocolateyZipPackage '$env:chocolateyPackageName' $url $binRoot
~~~
'@

  $global:powerShellReferenceTOC | Out-File $fileName -Encoding UTF8 -Force

  Write-Host "Generating command reference markdown files"
  Generate-CommandReference('list')
  Generate-CommandReference('search')
  Generate-CommandReference('info')
  Generate-CommandReference('install')
  Generate-CommandReference('pin')
  Generate-CommandReference('outdated')
  Generate-CommandReference('upgrade')
  Generate-CommandReference('uninstall')
  Generate-CommandReference('config')
  Generate-CommandReference('source')
  Generate-CommandReference('sources')
  Generate-CommandReference('feature')
  Generate-CommandReference('features')
  Generate-CommandReference('new')
  Generate-CommandReference('pack')
  Generate-CommandReference('apiKey')
  Generate-CommandReference('setapiKey')
  Generate-CommandReference('push')
  Generate-CommandReference('unpackself')
  Generate-CommandReference('update')
  Generate-CommandReference('version')
  Generate-TopLevelCommandReference

  Exit 0
}
catch
{
  Throw "Failed to generate documentation.  $_"
  Exit 255
}
