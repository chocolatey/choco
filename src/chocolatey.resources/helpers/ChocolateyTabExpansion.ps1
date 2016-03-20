$Global:ChocolateyTabSettings = New-Object PSObject -Property @{
    AllCommands = $false
}

function script:chocoCmdOperations($commands, $command, $filter) {
    $commands.$command -split ' ' |
        where { $_ -like "$filter*" }
}

$script:someCommands = @('-?', '-h', '--help','search','list','install','pin','outdated','upgrade','uninstall','pack','push','new','source','config','feature','apikey')

$allcommands = " -? --help --debug --verbose --accept-license -y --confirm --force --noop -whatif --limit-output --execution-timeout= --cache-location='' --fail-on-error-output --use-system-powershell"

$proInstallUpgradeOptions = " --skip-download-cache --use-download-cache --skip-virus-check --virus-check --virus-positives-minimum="

$subcommands = @{
  search = '--source= --lo --local-only--pre --prerelease --include-programs --all-versions --user= --password= --page= --page-size= --exact --id-only' + $allcommands
  list = '--source= --lo --local-only--pre --prerelease --include-programs --all-versions --user= --password= --page= --page-size= --exact --id-only' + $allcommands
  install = "--source='' --version= --pre --prerelease --forcex86 --install-arguments='' --override-arguments --not-silent --params='' --package-parameters='' --allow-downgrade --allow-multiple-versions --ignore-dependencies --force-dependencies --skip-automation-scripts --user= --password= --ignore-checksums" + $allcommands + $proInstallUpgradeOptions
  upgrade = "--source='' --version= --pre --prerelease --forcex86 --install-arguments='' --override-arguments --not-silent --params='' --package-parameters='' --allow-downgrade --allow-multiple-versions --ignore-dependencies --skip-automation-scripts --fail-on-unfound --fail-on-not-installed --user= --password= --ignore-checksums --except=''" + $allcommands + $proInstallUpgradeOptions
  uninstall = "--source='' --version= --all-versions --uninstall-arguments='' --override-arguments --not-silent --params=''  --package-parameters='' --force-dependencies --remove-dependencies --skip-automation-scripts" + $allcommands
  pin = "list add remove --name= --version=" + $allcommands
  outdated = "--source='' --user= --password=" + $allcommands
  pack = "<PathtoNuspec> --version=" + $allcommands
  push = "<PathToNupkg> --source='' --api-key= --timeout=" + $allcommands
  new = "<name> --automaticpackage --template-name= --name= --version= --maintainer='' packageversion= maintainername='' maintainerrepo='' installertype= url='' url64='' silentargs=''" + $allcommands
  source = "list add remove disable enable --name= --source='' --user= --password= --priority=" + $allcommands
  config = 'list get set unset --name= --value=' + $allcommands
  feature = 'list disable enable --name=' + $allcommands
  apikey = "--source='' --api-key=" + $allcommands
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
        $cmdList += choco -h |
            where { $_ -match '^  \S.*' } |
            foreach { $_.Split(' ', [StringSplitOptions]::RemoveEmptyEntries) } |
            where { $_ -like "$filter*" }
    }

    $cmdList | sort
}

function script:chocoLocalPackages($filter) {
    @('all|') + (choco list -lo -r) | where { $_.Split('|')[0] -like "$filter*" } | %{ $_.Split('|')[0] }
}

function script:chocoRemotePackages($filter) {
    @('packages.config|') + (choco search $filter --page=0 --page-size=5 -r --id-only) | where { $_.Split('|')[0] -like "$filter*" } | %{ $_.Split('|')[0] }
}

function Get-AliasPattern($exe) {
  $aliases = @($exe) + @(Get-Alias | where { $_.Definition -eq $exe } | select -Exp Name)
  
  "($($aliases -join '|'))"
}

function ChocolateyTabExpansion($lastBlock) {
  switch -regex ($lastBlock -replace "^$(Get-AliasPattern choco) ","") {

    # Handles install package names
    "^(install)\s+(?<package>[^-\s]*)$" {
      chocoRemotePackages $matches['package']
    }
  
    # Handles upgrade / uninstall package names
    "^(upgrade|uninstall)\s+(?<package>[^-\s]*)$" {
      chocoLocalPackages $matches['package']
    }
  
    # Handles more options after others
    "^(?<cmd>$($subcommands.Keys -join '|'))(.*)\s+(?<op>\S*)$" {
      chocoCmdOperations $subcommands $matches['cmd'] $matches['op']
    }
    
    # Handles choco <cmd> <op>
    "^(?<cmd>$($subcommands.Keys -join '|'))\s+(?<op>\S*)$" {
      chocoCmdOperations $subcommands $matches['cmd'] $matches['op']
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
    $lastBlock = [regex]::Split($line, '[|;]')[-1].TrimStart()
    $TabExpansionHasOutput.Value = $true
    ChocolateyTabExpansion $lastBlock
  }

  return
}

if (Test-Path Function:\TabExpansion) {
    Rename-Item Function:\TabExpansion TabExpansionBackup
}

function TabExpansion($line, $lastWord) {
    $lastBlock = [regex]::Split($line, '[|;]')[-1].TrimStart()

    switch -regex ($lastBlock) {
        # Execute Chocolatey tab completion for all choco-related commands
        "^$(Get-AliasPattern choco) (.*)" { ChocolateyTabExpansion $lastBlock }
        "^$(Get-AliasPattern choco.exe) (.*)" { ChocolateyTabExpansion $lastBlock }
        "^$(Get-AliasPattern chocolatey) (.*)" { ChocolateyTabExpansion $lastBlock }

        # Fall back on existing tab expansion
        default { if (Test-Path Function:\TabExpansionBackup) { TabExpansionBackup $line $lastWord } }
    }
}
