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

# Ideas from the Awesome Posh-Git - https://github.com/dahlbyk/posh-git
# Posh-Git License - https://github.com/dahlbyk/posh-git/blob/1941da2472eb668cde2d6a5fc921d5043a024386/LICENSE.txt
# http://www.jeremyskinner.co.uk/2010/03/07/using-git-with-windows-powershell/

$Global:ChocolateyTabSettings = New-Object PSObject -Property @{
    AllCommands = $false
}

$script:choco = "$env:ChocolateyInstall\choco.exe"

function script:chocoCmdOperations($commands, $command, $filter, $currentArguments) {
    $currentOptions = @('zzzz')
    if ($currentArguments -ne $null -and $currentArguments.Trim() -ne '') { $currentOptions = $currentArguments.Trim() -split ' ' }

    $commands.$command.Replace("  "," ") -split ' ' |
      where { $_ -notmatch "^(?:$($currentOptions -join '|' -replace "=", "\="))(?:\S*)\s?$" } |
      where { $_ -like "$filter*" }
}

$script:someCommands = @('-?','search','list','info','install','outdated','upgrade','uninstall','new','download','pack','push','sync','-h','--help','pin','source','config','feature','apikey')

$allcommands = " --debug --verbose --force --noop --help --accept-license --confirm --limit-output --no-progress --execution-timeout= --cache-location='' --proxy='' --proxy-user= --proxy-password= --proxy-bypass-list='' --proxy-bypass-on-local --fail-on-error-output --use-system-powershell"
$proListOptions = " --audit"
$proInstallUpgradeOptions = " --install-directory='' --max-download-rate= --install-arguments-sensitive= --package-parameters-sensitive= --skip-download-cache --use-download-cache --skip-virus-check --virus-check --virus-positives-minimum="
$proNewOptions = " --file='' --build-package --file64='' --from-programs-and-features --use-original-location --keep-remote --url='' --url64='' --checksum= --checksum64= --checksumtype= --pause-on-error"
$proUninstallOptions = " --from-programs-and-features"
$proPinOptions = " --note=''"

$commandOptions = @{
  list = "--lo --id-only --pre --exact --by-id-only --id-starts-with --detailed --approved-only --not-broken --source='' --user= --password= --local-only --prerelease --include-programs --page= --page-size= --order-by --order-by-popularity --download-cache-only" + $proListOptions + $allcommands
  search = "--pre --exact --by-id-only --id-starts-with --detailed --approved-only --not-broken --source='' --user= --password= --local-only --prerelease --include-programs --page= --page-size= --order-by --order-by-popularity --download-cache-only" + $allcommands
  info = "--pre --lo --source='' --user= --password= --local-only --prerelease" + $allcommands
  install = "-y -whatif -? --pre --version= --params='' --install-arguments='' --override-arguments --ignore-dependencies --source='' --source='windowsfeatures' --source='webpi' --user= --password= --prerelease --forcex86 --not-silent --package-parameters='' --allow-downgrade --force-dependencies --require-checksums --use-package-exit-codes --ignore-package-exit-codes --skip-automation-scripts --allow-multiple-versions --ignore-checksums --allow-empty-checksums --allow-empty-checksums-secure --download-checksum='' --download-checksum-type='' --download-checksum-x64='' --download-checksum-type-x64='' --stop-on-first-package-failure" + $proInstallUpgradeOptions + $allcommands
  pin = "--name= --version= -?" + $proPinOptions + $allcommands
  outdated = "-? --source='' --user= --password= --ignore-pinned --ignore-unfound" + $allcommands
  upgrade = "-y -whatif -? --pre --version= --except='' --params='' --install-arguments='' --override-arguments --ignore-dependencies --source='' --source='windowsfeatures' --source='webpi' --user= --password= --prerelease --forcex86 --not-silent --package-parameters='' --allow-downgrade --allow-multiple-versions --require-checksums --use-package-exit-codes --ignore-package-exit-codes --skip-automation-scripts --fail-on-unfound --fail-on-not-installed --ignore-checksums --allow-empty-checksums --allow-empty-checksums-secure --download-checksum='' --download-checksum-type='' --download-checksum-x64='' --download-checksum-type-x64='' --exclude-prerelease --stop-on-first-package-failure --use-remembered-options --ignore-remembered-options" + $proInstallUpgradeOptions + $allcommands
  uninstall = "-y -whatif -? --force-dependencies --remove-dependencies --all-versions --source='windowsfeatures' --source='webpi' --version= --uninstall-arguments='' --override-arguments --not-silent --params='' --package-parameters='' --use-package-exit-codes --ignore-package-exit-codes --skip-automation-scripts --use-autouninstaller --skip-autouninstaller --fail-on-autouninstaller --ignore-autouninstaller-failure --stop-on-first-package-failure" + $proUninstallOptions + $allcommands
  new = "--template-name= --output-directory='' --automaticpackage --version= --maintainer='' packageversion= maintainername='' maintainerrepo='' installertype= url='' url64='' silentargs='' --use-built-in-template -?" + $proNewOptions + $allcommands
  pack = "--version= -?" + $allcommands
  push = "--source='' --api-key= --timeout= -?" + $allcommands
  source = "--name= --source='' --user= --password= --priority= --bypass-proxy --allow-self-service -?" + $allcommands
  config = "--name= --value= -?" + $allcommands
  feature = "--name= -?" + $allcommands
  apikey = "--source='' --api-key= -?" + $allcommands
  download = "--internalize --ignore-dependencies --resources-location= --download-location= --outputdirectory= --source='' --version='' --prerelease --user= --password= --cert='' --certpassword= --append-use-original-location --recompile -?" + $allcommands
  sync = "--output-directory= --id= --package-id= -?" + $allcommands
}

try {
  # if license exists
  # add in pro/biz switches
}
catch {
}

function script:chocoCommands($filter) {
    $cmdList = @()
    if (-not $global:ChocolateyTabSettings.AllCommands) {
        $cmdList += $someCommands -like "$filter*"
    } else {
        $cmdList += (& $script:choco -h) |
            where { $_ -match '^  \S.*' } |
            foreach { $_.Split(' ', [StringSplitOptions]::RemoveEmptyEntries) } |
            where { $_ -like "$filter*" }
    }

    $cmdList #| sort
}

function script:chocoLocalPackages($filter) {
    if ($filter -ne $null -and $filter.StartsWith(".")) { return; } #file search
    @(& $script:choco list $filter -lo -r --id-starts-with) | %{ $_.Split('|')[0] }
}

function script:chocoLocalPackagesUpgrade($filter) {
    if ($filter -ne $null -and $filter.StartsWith(".")) { return; } #file search
    @('all|') + @(& $script:choco list $filter -lo -r --id-starts-with) | where { $_ -like "$filter*" } | %{ $_.Split('|')[0] }
}

function script:chocoRemotePackages($filter) {
    if ($filter -ne $null -and $filter.StartsWith(".")) { return; } #file search
    @('packages.config|') + @(& $script:choco search $filter --page=0 --page-size=30 -r --id-starts-with --order-by-popularity) | where { $_ -like "$filter*" } | %{ $_.Split('|')[0] }
}

function Get-AliasPattern($exe) {
  $aliases = @($exe) + @(Get-Alias | where { $_.Definition -eq $exe } | select -Exp Name)

  "($($aliases -join '|'))"
}

function ChocolateyTabExpansion($lastBlock) {
  switch -regex ($lastBlock -replace "^$(Get-AliasPattern choco) ","") {

    # Handles uninstall package names
    "^uninstall\s+(?<package>[^\.][^-\s]*)$" {
      chocoLocalPackages $matches['package']
    }

    # Handles install package names
    "^(install)\s+(?<package>[^\.][^-\s]+)$" {
      chocoRemotePackages $matches['package']
    }

    # Handles upgrade / uninstall package names
    "^upgrade\s+(?<package>[^\.][^-\s]*)$" {
      chocoLocalPackagesUpgrade $matches['package']
    }

    # Handles list/search first tab
    "^(list|search)\s+(?<subcommand>[^-\s]*)$" {
      @('<filter>','-?') | where { $_ -like "$($matches['subcommand'])*" }
    }

    # Handles new first tab
    "^(new)\s+(?<subcommand>[^-\s]*)$" {
      @('<name>','-?') | where { $_ -like "$($matches['subcommand'])*" }
    }

    # Handles pack first tab
    "^(pack)\s+(?<subcommand>[^-\s]*)$" {
      @('<PathtoNuspec>','-?') | where { $_ -like "$($matches['subcommand'])*" }
    }

    # Handles push first tab
    "^(push)\s+(?<subcommand>[^-\s]*)$" {
      @('<PathtoNupkg>','-?') | where { $_ -like "$($matches['subcommand'])*" }
    }

    # Handles source first tab
    "^(source)\s+(?<subcommand>[^-\s]*)$" {
      @('list','add','remove','disable','enable','-?') | where { $_ -like "$($matches['subcommand'])*" }
    }

    # Handles pin first tab
    "^(pin)\s+(?<subcommand>[^-\s]*)$" {
      @('list','add','remove','-?') | where { $_ -like "$($matches['subcommand'])*" }
    }

    # Handles feature first tab
    "^(feature)\s+(?<subcommand>[^-\s]*)$" {
      @('list','disable','enable','-?') | where { $_ -like "$($matches['subcommand'])*" }
    }
    # Handles config first tab
    "^(config)\s+(?<subcommand>[^-\s]*)$" {
      @('list','get','set','unset','-?') | where { $_ -like "$($matches['subcommand'])*" }
    }

    # Handles more options after others
    "^(?<cmd>$($commandOptions.Keys -join '|'))(?<currentArguments>.*)\s+(?<op>\S*)$" {
      chocoCmdOperations $commandOptions $matches['cmd'] $matches['op'] $matches['currentArguments']
    }

    # Handles choco <cmd> <op>
    "^(?<cmd>$($commandOptions.Keys -join '|'))\s+(?<op>\S*)$" {
      chocoCmdOperations $commandOptions $matches['cmd'] $matches['op']
    }

    # Handles choco <cmd>
    "^(?<cmd>\S*)$" {
      chocoCommands $matches['cmd']
    }
  }
}

$PowerTab_RegisterTabExpansion = if (Get-Module -Name powertab) { Get-Command Register-TabExpansion -Module powertab -ErrorAction SilentlyContinue }
if ($PowerTab_RegisterTabExpansion)
{
  & $PowerTab_RegisterTabExpansion "choco" -Type Command {
    param($Context, [ref]$TabExpansionHasOutput, [ref]$QuoteSpaces)  # 1:

    $line = $Context.Line
    $lastBlock = [System.Text.RegularExpressions.Regex]::Split($line, '[|;]')[-1].TrimStart()
    $TabExpansionHasOutput.Value = $true
    ChocolateyTabExpansion $lastBlock
  }

  return
}

if (Test-Path Function:\TabExpansion) {
    Rename-Item Function:\TabExpansion TabExpansionBackup
}

function TabExpansion($line, $lastWord) {
    $lastBlock = [System.Text.RegularExpressions.Regex]::Split($line, '[|;]')[-1].TrimStart()

    switch -regex ($lastBlock) {
        # Execute Chocolatey tab completion for all choco-related commands
        "^$(Get-AliasPattern choco) (.*)" { ChocolateyTabExpansion $lastBlock }

        # Fall back on existing tab expansion
        default { if (Test-Path Function:\TabExpansionBackup) { TabExpansionBackup $line $lastWord } }
    }
}
