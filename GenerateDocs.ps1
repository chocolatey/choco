#Requires -PSEdition Desktop
# Copyright © 2017 Chocolatey Software, Inc
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

# Special thanks to Glenn Sarti (https://github.com/glennsarti) for his help on this.

$ErrorActionPreference = 'Stop'

$thisDirectory = (Split-Path -parent $MyInvocation.MyCommand.Definition);
$psModuleName = 'chocolateyInstaller'
$psModuleLocation = [System.IO.Path]::GetFullPath("$thisDirectory\src\chocolatey.resources\helpers\chocolateyInstaller.psm1")
$docsFolder = [System.IO.Path]::GetFullPath("$thisDirectory\docs\generated")
$chocoExe = [System.IO.Path]::GetFullPath("$thisDirectory\code_drop\temp\_PublishedApps\choco_merged\choco.exe")
$lineFeed = "`r`n"
$sourceLocation = 'https://github.com/chocolatey/choco/blob/master/'
$sourceCommands = $sourceLocation + 'src/chocolatey/infrastructure.app/commands'
$sourceFunctions = $sourceLocation + 'src/chocolatey.resources/helpers/functions'
$global:powerShellReferenceTOC = @'
---
Order: 40
xref: powershell-reference
Title: PowerShell Reference
Description: PowerShell Functions aka Helpers Reference
RedirectFrom:
  - docs/helpers-reference
  - docs/HelpersReference
---

# PowerShell Functions aka Helpers Reference

<!-- This documentation file is automatically generated from the files at $sourceFunctions using $($sourceLocation)GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

## Summary

In your Chocolatey packaging, you have the ability to use these functions (and others with Chocolatey's [PowerShell Extensions](xref:extensions)) to work with all aspects of software management. Keep in mind Chocolatey's automation scripts are just PowerShell, so you can do manage anything you want.

> :choco-info: **NOTE**
>
> These scripts are for package scripts, not for use directly in PowerShell. This is in the create packages section, not the using Chocolatey section.

## Main Functions

These functions call other functions and many times may be the only thing you need in your [chocolateyInstall.ps1 file](xref:chocolatey-install-ps1).

* [Install-ChocolateyPackage](xref:install-chocolateypackage)
* [Install-ChocolateyZipPackage](xref:install-chocolateyzippackage)
* [Install-ChocolateyPowershellCommand](xref:install-chocolateypowershellcommand)
* [Install-ChocolateyVsixPackage](xref:install-chocolateyvsixpackage)

## More Functions

### Administrative Access Functions

When creating packages that need to run one of the following commands below, one should add the tag `admin` to the nuspec.

* [Install-ChocolateyPackage](xref:install-chocolateypackage)
* [Start-ChocolateyProcessAsAdmin](xref:start-chocolateyprocessasadmin)
* [Install-ChocolateyInstallPackage](xref:install-chocolateyinstallpackage)
* [Install-ChocolateyPath](xref:install-chocolateypath) - when specifying machine path
* [Install-ChocolateyEnvironmentVariable](xref:install-chocolateyenvironmentvariable) - when specifying machine path
* [Install-ChocolateyExplorerMenuItem](xref:install-chocolateyexplorermenuitem)
* [Install-ChocolateyFileAssociation](xref:install-chocolateyfileassociation)

### Non-Administrator Safe Functions

When you have a need to run Chocolatey without Administrative access required (non-default install location), you can run the following functions without administrative access.

These are the functions from above as one list.

* [Install-ChocolateyZipPackage](xref:install-chocolateyzippackage)
* [Install-ChocolateyPowershellCommand](xref:install-chocolateypowershellcommand)
* [Get-ChocolateyPath](xref:get-chocolateypath)
* [Get-ChocolateyWebFile](xref:get-chocolateywebfile)
* [Get-ChocolateyUnzip](xref:get-chocolateyunzip)
* [Install-ChocolateyPath](xref:install-chocolateypath) - when specifying user path
* [Install-ChocolateyEnvironmentVariable](xref:install-chocolateyenvironmentvariable) - when specifying user path
* [Install-ChocolateyPinnedTaskBarItem](xref:install-chocolateypinnedtaskbaritem)
* [Install-ChocolateyShortcut](xref:install-chocolateyshortcut) - v0.9.9+
* [Update-SessionEnvironment](xref:update-sessionenvironment)
* [Get-PackageParameters](xref:get-packageparameters) - v0.10.8+

## Complete List (alphabetical order)

'@

function Get-Aliases($commandName){

  $aliasOutput = ''
  Get-Alias -Definition $commandName -ErrorAction SilentlyContinue | ForEach-Object { $aliasOutput += "``$($_.Name)``$lineFeed"}

  if ($aliasOutput -eq $null -or $aliasOutput -eq '') {
    $aliasOutput = 'None'
  }

  Write-Output $aliasOutput
}

function Convert-Example($objItem) {
  @"
**$($objItem.title.Replace('-','').Trim())**

~~~powershell
$($objItem.Code.Replace("`n",$lineFeed))
$($objItem.remarks | Where-Object { $_.Text } | ForEach-Object { $_.Text.Replace("`n", $lineFeed) })
~~~
"@
}

function Replace-CommonItems($text) {
  if ($text -eq $null) {return $text}

  $text = $text.Replace("`n",$lineFeed)
  $text = $text -replace "\*\*NOTE:\*\*", '> :choco-info: **NOTE**
>
>'
  $text = $text -replace '(community feed[s]?[^\]]|community repository)', '[$1](https://community.chocolatey.org/packages)'
  $text = $text -replace '(Chocolatey for Business|Chocolatey Professional|Chocolatey Pro)(?=[^\w])', '[$1](https://chocolatey.org/compare)'
  $text = $text -replace '(Pro[fessional]\s?/\s?Business)', '[$1](https://chocolatey.org/compare)'
  $text = $text -replace '([Ll]icensed editions)', '[$1](https://chocolatey.org/compare)'
  $text = $text -replace '([Ll]icensed versions)', '[$1](https://chocolatey.org/compare)'
  $text = $text -replace '\(https://docs.chocolatey.org/en-us/create/automatic-packages\)', '(xref:automatic-packaging)'
  $text = $text -replace 'Learn more about using this at https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument', '[Learn more](xref:parse-package-parameters)'
  $text = $text -replace 'at https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument#step-3---use-core-community-extension', 'in [the docs](xref:parse-package-parameters#step-3-use-core-community-extension)'
  $text = $text -replace 'https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument', 'https://docs.chocolatey.org/en-us/guides/create/parse-packageparameters-argument'
  $text = $text -replace '\[community feed\)\]\(https://community.chocolatey.org/packages\)', '[community feed](https://community.chocolatey.org/packages))'

  Write-Output $text
}

function Convert-Syntax($objItem, $hasCmdletBinding) {
  $cmd = $objItem.Name

  if ($objItem.parameter -ne $null) {
    $objItem.parameter | ForEach-Object {
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
  $paramText = $lineFeed + "###  -$($objItem.name.substring(0,1).ToUpper() + $objItem.name.substring(1))"
  if ( ($objItem.parameterValue -ne $null) -and ($objItem.parameterValue -ne 'SwitchParameter') ) {
    $paramText += ' '
    if ([string]($objItem.required) -eq 'false') { $paramText += "["}
    $paramText += "&lt;$($objItem.parameterValue)&gt;"
    if ([string]($objItem.required) -eq 'false') { $paramText += "]"}
  }
  $paramText += $lineFeed
  if ($objItem.description -ne $null) {
    $parmText += (($objItem.description | ForEach-Object { Replace-CommonItems $_.Text }) -join "$lineFeed") + $lineFeed + $lineFeed
  }
  if ($objItem.parameterValueGroup -ne $null) {
    $paramText += "$($lineFeed)Valid options: " + ($objItem.parameterValueGroup.parameterValue -join ", ") + $lineFeed + $lineFeed
  }

  $aliases = [string]((Get-Command -Name $commandName).parameters."$($objItem.Name)".Aliases -join ', ')
  $required = [string]($objItem.required)
  $position = [string]($objItem.position)
  $defValue = [string]($objItem.defaultValue)
  $acceptPipeline = [string]($objItem.pipelineInput)

  $padding = ($aliases.Length, $required.Length, $position.Length, $defValue.Length, $acceptPipeline.Length | Measure-Object -Maximum).Maximum

    $paramText += @"
Property               | Value
---------------------- | $([string]('-' * $padding))
Aliases                | $($aliases)
Required?              | $($required)
Position?              | $($position)
Default Value          | $($defValue)
Accept Pipeline Input? | $($acceptPipeline)

"@

  Write-Output $paramText
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
  $commandText = $commandText -creplace '^(DEPRECATION NOTICE|Usage|Troubleshooting|Examples|Exit Codes|Connecting to Chocolatey.org|See It In Action|Alternative Sources|Resources|Packages.config|Scripting \/ Integration - Best Practices \/ Style Guide)', '## $1'
  $commandText = $commandText -replace '^(Commands|How To Pass Options)', '## $1'
  $commandText = $commandText -replace '^(WebPI|Windows Features|Ruby|Cygwin|Python)\s*$', '### $1'
  $commandText = $commandText -replace '(?<!\s)NOTE:', '> :choco-info: **NOTE**'
  $commandText = $commandText -replace '\*> :choco-info: \*\*NOTE\*\*\*', '> :choco-info: **NOTE**'
  $commandText = $commandText -replace 'the command reference', '[how to pass arguments](xref:choco-commands#how-to-pass-options-switches)'
  $commandText = $commandText -replace '(community feed[s]?|community repository)', '[$1](https://community.chocolatey.org/packages)'
  #$commandText = $commandText -replace '\`(apikey|install|upgrade|uninstall|list|search|info|outdated|pin)\`', '[[`$1`|Commands$1]]'
  $commandText = $commandText -replace '\`([choco\s]*)(apikey|install|upgrade|uninstall|list|search|info|outdated|pin)\`', '[`$1$2`](xref:choco-command-$2)'
  $commandText = $commandText -replace '^(.+):\s(.+.gif)$', '![$1]($2)'
  $commandText = $commandText -replace '^(\s+)\<\?xml', "~~~xml$lineFeed`$1<?xml"
  $commandText = $commandText -replace '^(\s+)</packages>', "`$1</packages>$lineFeed~~~"
  $commandText = $commandText -replace '(Chocolatey for Business|Chocolatey Professional|Chocolatey Pro)(?=[^\w])', '[$1](https://chocolatey.org/compare)'
  $commandText = $commandText -replace '(Pro[fessional]\s?/\s?Business)', '[$1](https://chocolatey.org/compare)'
  $commandText = $commandText -replace '([Ll]icensed editions)', '[$1](https://chocolatey.org/compare)'
  $commandText = $commandText -replace '([Ll]icensed versions)', '[$1](https://chocolatey.org/compare)'
  $commandText = $commandText -replace 'https://raw.githubusercontent.com/wiki/chocolatey/choco/images', '/assets/images'
  $commandText = $commandText -replace 'https://chocolatey.org/docs/features-automatically-recompile-packages', 'https://docs.chocolatey.org/en-us/guides/create/recompile-packages'
  $commandText = $commandText -replace 'https://chocolatey.org/docs/features-private-cdn', 'https://docs.chocolatey.org/en-us/features/private-cdn'
  $commandText = $commandText -replace 'https://chocolatey.org/docs/features-virus-check', 'https://docs.chocolatey.org/en-us/features/virus-check'
  $commandText = $commandText -replace 'https://chocolatey.org/docs/features-synchronize', 'https://docs.chocolatey.org/en-us/features/package-synchronization'
  $commandText = $commandText -replace 'explicity', 'explicit'
  $commandText = $commandText -replace 'https://chocolatey.org/docs/features-create-packages-from-installers', 'https://docs.chocolatey.org/en-us/features/package-builder'
  $commandText = $commandText -replace 'See https://chocolatey.org/docs/features-create-packages-from-installers', 'See more information about [Package Builder features](xref:package-builder)'
  $commandText = $commandText -replace 'See https://docs.chocolatey.org/en-us/features/package-builder', 'See more information about [Package Builder features](xref:package-builder)'
  $commandText = $commandText -replace 'https://chocolatey.org/docs/features-install-directory-override', 'https://docs.chocolatey.org/en-us/features/install-directory-override'
  $commandText = $commandText -replace 'y.org/docs/features-package-reducer', 'y.org/docs/en-us/features/package-reducer'
  $commandText = $commandText -replace 'https://chocolatey.org/docs/features-package-reducer', 'https://docs.chocolatey.org/en-us/features/package-reducer'
  $commandText = $commandText -replace 'https://chocolatey.org/docs/en-us/features/package-reducer', 'https://docs.chocolatey.org/en-us/features/package-reducer'
  $commandText = $commandText -replace '\[community feed\)\]\(https://community.chocolatey.org/packages\)', '[community feed](https://community.chocolatey.org/packages))'
  $commandText = $commandText -replace '> :choco-info: \*\*NOTE\*\*\s', '> :choco-info: **NOTE**
>
> '

  $optionsSwitches = @'
## $1

> :choco-info: **NOTE**
>
> Options and switches apply to all items passed, so if you are
 running a command like install that allows installing multiple
 packages, and you use `--version=1.0.0`, it is going to look for and
 try to install version 1.0.0 of every package passed. So please split
 out multiple package calls when wanting to pass specific options.

Includes [default options/switches](xref:choco-commands#default-options-and-switches) (included below for completeness).

~~~
'@

  $commandText = $commandText -replace '^(Options and Switches)', $optionsSwitches

   $optionsSwitches = @'
## $1

> :choco-info: **NOTE**
>
> Options and switches apply to all items passed, so if you are
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
  $commandText = [Regex]::Replace($commandText, '\s?\s?\*\s(\w+)\s\-',
    {
        param($m)
        $commandName = $m.Groups[1].Value
        $commandNameUpper = $($commandName.Substring(0,1).ToUpper() + $commandName.Substring(1))
        " * [$commandName](xref:choco-command-$($commandName)) -"
    }
  )
  #$commandText = $commandText -replace '\s?\s?\*\s(\w+)\s\-', ' * [[$1|Commands$1]] -'
  $commandText = $commandText.Replace("## Default Options and Switches", "## See Help Menu In Action$lineFeed$lineFeed![choco help in action](/assets/images/gifs/choco_help.gif)$lineFeed$lineFeed## Default Options and Switches")

  Write-Output $commandText
}

function Generate-TopLevelCommandReference {
  Write-Host "Generating Top Level Command Reference"
  $fileName = "$docsFolder\choco\commands\index.md"
  $commandOutput = @("---")
  $commandOutput += @("Order: 40")
  $commandOutput += @("xref: choco-commands")
  $commandOutput += @("Title: Commands")
  $commandOutput += @("Description: Full list of all available Chocolatey commands")
  $commandOutput += @("RedirectFrom:")
  $commandOutput += @("  - docs/commandsreference")
  $commandOutput += @("  - docs/commands-reference")
  $commandOutput += @("---$lineFeed")
  $commandOutput += @("# Command Reference$lineFeed")
  $commandOutput += @("<!-- This file is automatically generated based on output from the files at $sourceCommands using $($sourceLocation)GenerateDocs.ps1. Contributions are welcome at the original location(s). --> $lineFeed")
  $commandOutput += $(& $chocoExe -? -r)
  $commandOutput += @("$lineFeed~~~$lineFeed")
  $commandOutput += @("$lineFeed$lineFeed*NOTE:* This documentation has been automatically generated from ``choco -h``. $lineFeed")

  $commandOutput | 
      ForEach-Object { Convert-CommandText($_) } |
      ForEach-Object { Convert-CommandReferenceSpecific($_) } |
      Out-File $fileName -Encoding UTF8 -Force
}

function Move-GeneratedFiles {
  if(-not(Test-Path "$docsFolder\create\commands")){ mkdir "$docsFolder\create\commands" -EA Continue | Out-Null }

  Move-Item -Path "$docsFolder\choco\commands\apikey.md" -Destination "$docsFolder\create\commands\api-key.md"
  Move-Item -Path "$docsFolder\choco\commands\new.md" -Destination "$docsFolder\create\commands\new.md"
  Move-Item -Path "$docsFolder\choco\commands\pack.md" -Destination "$docsFolder\create\commands\pack.md"
  Move-Item -Path "$docsFolder\choco\commands\push.md" -Destination "$docsFolder\create\commands\push.md"
  Move-Item -Path "$docsFolder\choco\commands\template.md" -Destination "$docsFolder\create\commands\template.md"
  Move-Item -Path "$docsFolder\choco\commands\templates.md" -Destination "$docsFolder\create\commands\templates.md"
  Move-Item -Path "$docsFolder\choco\commands\convert.md" -Destination "$docsFolder\create\commands\convert.md"
}

function Generate-CommandReference($commandName, $order) {
  if(-not(Test-Path "$docsFolder\choco\commands")){ mkdir "$docsFolder\choco\commands" -EA Continue | Out-Null }
  $fileName = Join-Path "$docsFolder\choco\commands" "$($commandName.ToLower()).md"
  $commandNameLower = $commandName.ToLower()

  Write-Host "Generating $fileName ..."
  $commandOutput += @("---")
  $commandOutput += @("Order: $order")
  $commandOutput += @("xref: choco-command-$commandNameLower")

  if($commandName -eq 'List') {
    $commandOutput += @("Title: $commandName/Search")
    $commandOutput += @("Description: $commandName/Search Command (choco $commandNameLower)")
  } else {
    $commandOutput += @("Title: $commandName")
    $commandOutput += @("Description: $commandName Command (choco $commandNameLower)")
  }

  $commandOutput += @("RedirectFrom:")
  $commandOutput += @("  - docs/commands$commandNameLower")
  $commandOutput += @("  - docs/commands-$commandNameLower")

  if($commandName -eq 'Features') {
    $commandOutput += @("ShowInNavbar: false")
    $commandOutput += @("ShowInSidebar: false")
  }

  if($commandName -eq 'Templates') {
    $commandOutput += @("ShowInNavbar: false")
    $commandOutput += @("ShowInSidebar: false")
  }

  $commandOutput += @("---$lineFeed")
  $commandOutput += @("<!-- This file is automatically generated based on output from $($sourceCommands)/Chocolatey$($commandName)Command.cs using $($sourceLocation)GenerateDocs.ps1. Contributions are welcome at the original location(s). If the file is not found, it is not part of the open source edition of Chocolatey or the name of the file is different. --> $lineFeed")

  $commandOutput += @(@"
> :choco-warning: **WARNING** SHIM DEPRECATION
>
> With the release of Chocolatey CLI v1.0.0 we have deprecated the following shims/shortcuts:
>
> - `chocolatey` (Alias for `choco`)
> - `cinst` (Shortcut for `choco install`)
> - `cpush` (Shortcut for `choco push`)
> - `cuninst` (Shortcut for `cuninst`)
> - `cup` (Shortcut for `choco upgrade`)
>
> We recommend that any scripts calling these shims be updated to use the full command, as
> these shims will be removed in Chocolatey CLI v2.0.0.

"@)

  $commandOutput += $(& $chocoExe $commandName.ToLower() -h -r)
  $commandOutput += @("$lineFeed~~~$lineFeed$lineFeed[Command Reference](xref:choco-commands)")
  $commandOutput += @("$lineFeed$lineFeed*NOTE:* This documentation has been automatically generated from ``choco $($commandName.ToLower()) -h``. $lineFeed")
  $commandOutput | 
      ForEach-Object { Convert-CommandText $_ $commandName.ToLower() } | 
      Out-File $fileName -Encoding UTF8 -Force
}

try
{
  Write-Host "Importing the Module $psModuleName ..."
  Import-Module "$psModuleLocation" -Force -Verbose

  # Switch Get-PackageParameters back for documentation
  Remove-Item alias:Get-PackageParameters
  Remove-Item function:Get-PackageParametersBuiltIn
  Set-Alias -Name Get-PackageParametersBuiltIn -Value Get-PackageParameters -Scope Global

  if (Test-Path($docsFolder)) { Remove-Item $docsFolder -Force -Recurse -EA SilentlyContinue }
  if(-not(Test-Path $docsFolder)){ mkdir $docsFolder -EA Continue | Out-Null }
  if(-not(Test-Path "$docsFolder\create\functions")){ mkdir "$docsFolder\create\functions" -EA Continue | Out-Null }

  Write-Host 'Creating per PowerShell function markdown files...'
  $helperOrder = 10;
  Get-Command -Module $psModuleName -CommandType Function | ForEach-Object -Process { Get-Help $_ -Full } | ForEach-Object -Process { `
    $commandName = $_.Name
    $fileName = Join-Path "$docsFolder\create\functions" "$($_.Name.ToLower()).md"
    $global:powerShellReferenceTOC += "$lineFeed * [$commandName](xref:$([System.IO.Path]::GetFileNameWithoutExtension($fileName)))"
    $hasCmdletBinding = (Get-Command -Name $commandName).CmdLetBinding

    Write-Host "Generating $fileName ..."
    $SplitName = $_.Name -split "-"
    $NameNoHyphen = $_.Name -replace '-', ''

    if($_.Name -eq 'Get-OSArchitectureWidth') {
      $FormattedName = "get-os-architecture-width"
    } elseif($_.Name -eq 'Get-UACEnabled') {
      $FormattedName = "get-uac-enabled"
    }else {
      $FormattedName = $SplitName[0].ToLower() + ($SplitName[1] -creplace '[A-Z]', '-$&').ToLower()
    }

    @"
---
Order: $($helperOrder)
xref: $($_.Name.ToLower())
Title: $($_.Name)
Description: Information on $($_.Name) function
RedirectFrom:
  - docs/helpers-$($FormattedName)
  - docs/helpers$($NameNoHyphen.ToLower())
---

# $($_.Name)

<!-- This documentation is automatically generated from $sourceFunctions/$($_.Name)`.ps1 using $($sourceLocation)GenerateDocs.ps1. Contributions are welcome at the original location(s). -->

$(Replace-CommonItems $_.Synopsis)

## Syntax
$( ($_.syntax.syntaxItem | ForEach-Object { Convert-Syntax $_ $hasCmdletBinding }) -join "$lineFeed$lineFeed")
$( if ($_.description -ne $null) { $lineFeed + "## Description" + $lineFeed + $lineFeed + $(Replace-CommonItems $_.description.Text) })
$( if ($_.alertSet -ne $null) { $lineFeed + "## Notes" + $lineFeed + $lineFeed +  $(Replace-CommonItems $_.alertSet.alert.Text) })

## Aliases

$(Get-Aliases $_.Name)
$( if ($_.Examples -ne $null) { Write-Output "$lineFeed## Examples$lineFeed$lineFeed"; ($_.Examples.Example | ForEach-Object { Convert-Example $_ }) -join "$lineFeed$lineFeed"; Write-Output "$lineFeed" })
## Inputs

$( if ($_.InputTypes -ne $null -and $_.InputTypes.Length -gt 0 -and -not $_.InputTypes.Contains('inputType')) { $lineFeed + " * $($_.InputTypes)" + $lineFeed} else { 'None'})

## Outputs

$( if ($_.ReturnValues -ne $null -and $_.ReturnValues.Length -gt 0 -and -not $_.ReturnValues.StartsWith('returnValue')) { "$lineFeed * $($_.ReturnValues)$lineFeed"} else { 'None'})

## Parameters
$( if ($_.parameters.parameter.count -gt 0) { $_.parameters.parameter | ForEach-Object { Convert-Parameter $_ $commandName }}) $( if ($hasCmdletBinding) { "$lineFeed### &lt;CommonParameters&gt;$lineFeed$($lineFeed)This cmdlet supports the common parameters: -Verbose, -Debug, -ErrorAction, -ErrorVariable, -OutBuffer, and -OutVariable. For more information, see ``about_CommonParameters`` http://go.microsoft.com/fwlink/p/?LinkID=113216 ." } )

$( if ($_.relatedLinks -ne $null) {Write-Output "$lineFeed## Links$lineFeed$lineFeed"; $_.relatedLinks.navigationLink | Where-Object { $_.linkText -ne $null} | ForEach-Object { Write-Output "* [$($_.LinkText)](xref:$($_.LinkText.ToLower()))$lineFeed" }})

[Function Reference](xref:powershell-reference)

> :choco-info: **NOTE**
>
> This documentation has been automatically generated from ``Import-Module `"`$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1`" -Force; Get-Help $($_.Name) -Full``.

View the source for [$($_.Name)]($sourceFunctions/$($_.Name)`.ps1)
"@  | Out-File $fileName -Encoding UTF8 -Force
  $helperOrder = $helperOrder + 10
  }

  Write-Host "Generating Top Level PowerShell Reference"
  $fileName = Join-Path "$docsFolder\create\functions" 'index.md'

  $global:powerShellReferenceTOC += @'


## Chocolatey for Business Functions

 * [Install-ChocolateyWindowsService](xref:install-chocolateywindowsservice)
 * [Start-ChocolateyWindowsService](xref:start-chocolateywindowsservice)
 * [Stop-ChocolateyWindowsService](xref:stop-chocolateywindowsservice)
 * [Uninstall-ChocolateyWindowsService](xref:uninstall-chocolateywindowsservice)

## Variables

There are also a number of environment variables providing access to some values from the nuspec and other information that may be useful. They are accessed via `$env:variableName`.

### Environment Variables

Chocolatey makes a number of environment variables available (You can access any of these with $env:TheVariableNameBelow):

 * TEMP/TMP - Overridden to the CacheLocation, but may be the same as the original TEMP folder
 * ChocolateyInstall - Top level folder where Chocolatey is installed
 * ChocolateyPackageName - The name of the package, equivalent to the `<id />` field in the nuspec (0.9.9+)
 * ChocolateyPackageTitle - The title of the package, equivalent to the `<title />` field in the nuspec (0.10.1+)
 * ChocolateyPackageVersion - The version of the package, equivalent to the `<version />` field in the nuspec (0.9.9+)

#### Advanced Environment Variables

The following are more advanced settings:

 * ChocolateyPackageParameters - Parameters to use with packaging, not the same as install arguments (which are passed directly to the native installer). Based on `--package-parameters`. (0.9.8.22+)
 * CHOCOLATEY_VERSION - The version of Choco you normally see. Use if you are 'lighting' things up based on choco version. (0.9.9+) - Otherwise take a dependency on the specific version you need.
 * ChocolateyForceX86 = If available and set to 'true', then user has requested 32bit version. (0.9.9+) - Automatically handled in built in Choco functions.
 * OS_PLATFORM - Like Windows, macOS, Linux. (0.9.9+)
 * OS_VERSION - The version of OS, like 6.1 something something for Windows. (0.9.9+)
 * OS_NAME - The reported name of the OS. (0.9.9+)
 * IS_PROCESSELEVATED = Is the process elevated? (0.9.9+)
 * ChocolateyPackageInstallLocation - Install location of the software that the package installs. Displayed at the end of the package install. (0.9.10+)

#### Set By Options and Configuration

Some environment variables are set based on options that are passed, configuration and/or features that are turned on:

 * ChocolateyEnvironmentDebug - Was `--debug` passed? If using the built-in PowerShell host, this is always true (but only logs debug messages to console if `--debug` was passed) (0.9.10+)
 * ChocolateyEnvironmentVerbose - Was `--verbose` passed? If using the built-in PowerShell host, this is always true (but only logs verbose messages to console if `--verbose` was passed). (0.9.10+)
 * ChocolateyForce - Was `--force` passed? (0.9.10+)
 * ChocolateyForceX86 - Was `-x86` passed? (CHECK)
 * ChocolateyRequestTimeout - How long before a web request will time out. Set by config `webRequestTimeoutSeconds` (CHECK)
 * ChocolateyResponseTimeout - How long to wait for a download to complete? Set by config `commandExecutionTimeoutSeconds` (CHECK)
 * ChocolateyPowerShellHost - Are we using the built-in PowerShell host? Set by `--use-system-powershell` or the feature `powershellHost` (0.9.10+)

#### Business Edition Variables

 * ChocolateyInstallArgumentsSensitive - Encrypted arguments passed from command line `--install-arguments-sensitive` that are not logged anywhere. (0.10.1+ and licensed editions 1.6.0+)
 * ChocolateyPackageParametersSensitive - Package parameters passed from command line `--package-parameters-sensitive` that are not logged anywhere.  (0.10.1+ and licensed editions 1.6.0+)
 * ChocolateyLicensedVersion - What version is the licensed edition on?
 * ChocolateyLicenseType - What edition / type of the licensed edition is installed?

#### Experimental Environment Variables

The following are experimental or use not recommended:

 * OS_IS64BIT = This may not return correctly - it may depend on the process the app is running under (0.9.9+)
 * CHOCOLATEY_VERSION_PRODUCT = the version of Choco that may match CHOCOLATEY_VERSION but may be different (0.9.9+) - based on git describe
 * IS_ADMIN = Is the user an administrator? But doesn't tell you if the process is elevated. (0.9.9+)

#### Not Useful Or Anti-Pattern If Used

 * ChocolateyInstallOverride - Not for use in package automation scripts. Based on `--override-arguments` being passed. (0.9.9+)
 * ChocolateyInstallArguments - The installer arguments meant for the native installer. You should use chocolateyPackageParameters instead. Based on `--install-arguments` being passed. (0.9.9+)
 * ChocolateyIgnoreChecksums - Was `--ignore-checksums` passed or the feature `checksumFiles` turned off? (0.9.9.9+)
 * ChocolateyAllowEmptyChecksums - Was `--allow-empty-checksums` passed or the feature `allowEmptyChecksums` turned on? (0.10.0+)
 * ChocolateyAllowEmptyChecksumsSecure - Was `--allow-empty-checksums-secure` passed or the feature `allowEmptyChecksumsSecure` turned on? (0.10.0+)
 * ChocolateyChecksum32 - Was `--download-checksum` passed? (0.10.0+)
 * ChocolateyChecksumType32 - Was `--download-checksum-type` passed? (0.10.0+)
 * ChocolateyChecksum64 - Was `--download-checksum-x64` passed? (0.10.0)+
 * ChocolateyChecksumType64 - Was `--download-checksum-type-x64` passed? (0.10.0)+
 * ChocolateyPackageExitCode - The exit code of the script that just ran - usually set by `Set-PowerShellExitCode` (CHECK)
 * ChocolateyLastPathUpdate - Set by Chocolatey as part of install, but not used for anything in particular in packaging.
 * ChocolateyProxyLocation - The explicit proxy location as set in the configuration `proxy` (0.9.9.9+)
 * ChocolateyDownloadCache - Use available download cache? Set by `--skip-download-cache`, `--use-download-cache`, or feature `downloadCache` (0.9.10+ and licensed editions 1.1.0+)
 * ChocolateyProxyBypassList - Explicitly set locations to ignore in configuration `proxyBypassList` (0.10.4+)
 * ChocolateyProxyBypassOnLocal - Should the proxy bypass on local connections? Set based on configuration `proxyBypassOnLocal` (0.10.4+)
 * http_proxy - Set by original `http_proxy` passthrough, or same as `ChocolateyProxyLocation` if explicitly set. (0.10.4+)
 * https_proxy - Set by original `https_proxy` passthrough, or same as `ChocolateyProxyLocation` if explicitly set. (0.10.4+)
 * no_proxy- Set by original `no_proxy` passthrough, or same as `ChocolateyProxyBypassList` if explicitly set. (0.10.4+)
 * ChocolateyPackageFolder - Not for use in package automation scripts. Recommend using `$toolsDir = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"` as per template generated by `choco new`
 * ChocolateyToolsLocation - Not for use in package automation scripts. Recommend using Get-ToolsLocation instead
'@

  $global:powerShellReferenceTOC | Out-File $fileName -Encoding UTF8 -Force

  Write-Host "Generating command reference markdown files"
  Generate-CommandReference 'Config' '10'
  Generate-CommandReference 'Download' '20'
  Generate-CommandReference 'Export' '30'
  Generate-CommandReference 'Find' '35'
  Generate-CommandReference 'Feature' '40'
  Generate-CommandReference 'Features' '45'
  Generate-CommandReference 'Help' '50'
  Generate-CommandReference 'Info' '60'
  Generate-CommandReference 'Install' '70'
  Generate-CommandReference 'List' '80'
  Generate-CommandReference 'Optimize' '90'
  Generate-CommandReference 'Outdated' '100'
  Generate-CommandReference 'Pin' '110'
  Generate-CommandReference 'Search' '120'
  Generate-CommandReference 'SetApiKey' '130'
  Generate-CommandReference 'Source' '140'
  Generate-CommandReference 'Sources' '150'
  Generate-CommandReference 'Support' '160'
  Generate-CommandReference 'Sync' '170'
  Generate-CommandReference 'Synchronize' '180'
  Generate-CommandReference 'Uninstall' '190'
  Generate-CommandReference 'UnpackSelf' '200'
  Generate-CommandReference 'Upgrade' '220'

  Generate-CommandReference 'New' '10'
  Generate-CommandReference 'Pack' '20'
  Generate-CommandReference 'ApiKey' '30'
  Generate-CommandReference 'Push' '40'
  Generate-CommandReference 'Template' '50'
  Generate-CommandReference 'Templates' '55'
  Generate-CommandReference 'Convert' '60'

  Generate-TopLevelCommandReference
  Move-GeneratedFiles

  Exit 0
}
catch
{
  Throw "Failed to generate documentation.  $_"
  Exit 255
}
