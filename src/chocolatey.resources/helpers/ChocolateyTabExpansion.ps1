# Copyright © 2017 - 2021 Chocolatey Software, Inc.
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
    if (-not [string]::IsNullOrWhiteSpace($currentArguments)) {
        $currentOptions = $currentArguments.Trim() -split ' '
    }

    $commands.$command.Replace("  ", " ") -split ' ' |
        Where-Object { $_ -notmatch "^(?:$($currentOptions -join '|' -replace "=", "\="))(?:\S*)\s?$" } |
        Where-Object { $_ -like "$filter*" }
}

$script:chocoCommands = @('apikey','cache','config','export','feature','help','info','install','license','list','new','outdated','pack','pin','push','rule','search','source','support','template','uninstall','upgrade','--help','--version')

# ensure these all have a space to start, or they will cause issues
$allCommands = " --accept-license --cache-location='' --debug --fail-on-standard-error --force --help --ignore-http-cache --include-headers --limit-output --log-file='' --no-color --no-progress --noop --online --proxy='' --proxy-bypass-list='' --proxy-bypass-on-local --proxy-password='' --proxy-user='' --skip-compatibility-checks --timeout='' --trace --use-system-powershell --verbose --yes"

$commandOptions = @{
    apikey    = "--api-key='' --source=''"
    cache     = "--expired"
    config    = "--name='' --value=''"
    export    = "--include-version --output-file-path=''"
    feature   = "--name=''"
    info      = "--cert='' --certpassword='' --disable-repository-optimizations --include-configured-sources --local-only --password='' --prerelease --source='' --user='' --version=''"
    install   = "--allow-downgrade --allow-empty-checksums --allow-empty-checksums-secure --apply-args-to-dependencies --apply-package-parameters-to-dependencies --cert='' --certpassword='' --disable-repository-optimizations --download-checksum='' --download-checksum-x64='' --download-checksum-type='' --download-checksum-type-x64='' --exit-when-reboot-detected --force-dependencies --forcex86 --ignore-checksum --ignore-dependencies --ignore-detected-reboot --ignore-package-exit-codes --include-configured-sources --install-arguments='' --not-silent --override-arguments --package-parameters='' --password='' --pin --prerelease --require-checksums --skip-hooks --skip-scripts --source='' --stop-on-first-failure --use-package-exit-codes --user='' --version=''"
    license   = ""
    list      = "--by-id-only --by-tag-only --detail --exact --id-only --id-starts-with --include-programs --page='' --page-size='' --prerelease --source='' --version=''"
    new       = "--automaticpackage --download-checksum='' --download-checksum-x64='' --download-checksum-type='' --maintainer='' --name='' --output-directory='' --template='' --use-built-in-template --version=''"
    outdated  = "--cert='' --certpassword='' --disable-repository-optimizations --ignore-pinned --ignore-unfound --include-configured-sources --password='' --prerelease --source='' --user=''"
    pack      = "--output-directory='' --version=''"
    pin       = "--name='' --version=''"
    push      = "--api-key='' --source=''"
    rule      = "--name=''"
    search    = "--all-versions --approved-only --by-id-only --by-tag-only --cert='' --certpassword='' --detail --disable-repository-optimizations --download-cache-only --exact --id-only --id-starts-with --include-configured-sources --include-programs --not-broken --order-by='' --order-by-popularity --page='' --page-size='' --password='' --prerelease --source='' --user=''"
    source    = "--admin-only --allow-self-service --bypass-proxy --cert='' --certpassword='' --name='' --password='' --priority='' --source='' --user=''"
    support   = ""
    template  = "--name=''"
    uninstall = "--all-versions --apply-args-to-dependencies --apply-package-parameters-to-dependencies --exit-when-reboot-detected --fail-on-autouninstaller --force-dependencies --ignore-autouninstaller-failure --ignore-detected-reboot --ignore-package-exit-codes --not-silent --override-arguments --package-parameters='' --skip-autouninstaller --skip-hooks --skip-scripts --source='' --stop-on-first-failure --uninstall-arguments='' --use-autouninstaller --use-package-exit-codes --version=''"
    upgrade   = "--allow-downgrade --allow-empty-checksums --allow-empty-checksums-secure --apply-args-to-dependencies --apply-package-parameters-to-dependencies --cert='' --certpassword='' --disable-repository-optimizations --download-checksum='' --download-checksum-x64='' --download-checksum-type='' --download-checksum-type-x64='' --except='' --exclude-prerelease --exit-when-reboot-detected --fail-on-not-installed --fail-on-unfound --forcex86 --ignore-checksums --ignore-dependencies --ignore-detected-reboot --ignore-package-exit-codes --ignore-pinned --ignore-remembered-arguments --ignore-unfound --include-configured-sources --install-arguments='' --install-if-not-installed --not-silent --override-arguments --package-parameters='' --password='' --pin --prerelease --require-checksums --skip-hooks --skip-if-not-installed --skip-scripts --source='' --stop-on-first-failure --use-package-exit-codes --use-remembered-arguments --user='' --version=''"
}

$licenseFile = "$env:ChocolateyInstall\license\chocolatey.license.xml"

if (Test-Path $licenseFile) {
    # Add pro-only commands
    $script:chocoCommands = @(
        $script:chocoCommands
        'download'
        'optimize'
    )

    $commandOptions.download = "--append-use-original-location --cert='' --certpassword='' --disable-repository-optimizations --download-location='' --ignore-dependencies --ignore-unfound --installed-packages --internalize --internalize-all-urls --output-directory='' --password='' --prerelease --resources-location='' --skip-download-cache --skip-virus-check --source='' --use-download-cache --user='' --version='' --virus-check --virus-positives-minimum=''"
    $commandOptions.optimize = "--id='' --reduce-nupkg-only"

    # Add pro switches to commands that have additional switches on Pro
    $proInstallUpgradeOptions = " --install-arguments-sensitive='' --install-directory='' --max-download-bits-per-second='' --no-reduce-package-size --package-parameters-sensitive='' --reason='' --reduce-package-size --reduce-nupkg-only --skip-download-cache --skip-virus-check --use-download-cache --virus-check --virus-positives-minimum=''"

    $commandOptions.install += $proInstallUpgradeOptions
    $commandOptions.new += " --build-package --pause-on-error --use-original-location"
    $commandOptions.pin += " --reason=''"
    $commandOptions.upgrade += $proInstallUpgradeOptions + " --exclude-chocolatey-packages-during-upgrade-all --include-chocolatey-packages-during-upgrade-all"

    # Add Business-only commands and options if the license is a Business or Trial license
    [xml]$xml = Get-Content -Path $licenseFile -ErrorAction Stop
    $licenseType = $xml.license.type

    if ('Business', 'BusinessTrial' -contains $licenseType) {

        # Add business-only commands
        $script:chocoCommands = @(
            $script:chocoCommands
            'convert'
            'sync'
        )

        $commandOptions.convert = "--ignore-dependencies --include-all --to-format=''"
        $commandOptions.list += " --show-audit"
        $commandOptions.new += " --file='' --file64='' --from-programs-and-features --include-architecture-in-name --remove-architecture-from-name --url='' --url64=''"
        $commandOptions.push += " --client-code='' --endpoint='' redirect-url='' --skip-cleanup"
        $commandOptions.sync = "--id='' --output-directory='' --package-id=''"
        $commandOptions.uninstall += " --from-programs-and-features"

        # Add --use-self-service to commands that support it
        $selfServiceCommands = 'download', 'info', 'install', 'list', 'optimize', 'outdated', 'pin', 'push', 'search', 'sync', 'uninstall', 'upgrade'
        foreach ($command in $selfServiceCommands) {
            $commandOptions.$command += ' --use-self-service'
        }
    }
}

foreach ($key in @($commandOptions.Keys)) {
    $commandOptions.$key = ($commandOptions.$key + $allCommands).Trim()
}

# Consistent ordering for commands so the added pro commands aren't weirdly out of order
$script:chocoCommands = $script:chocoCommands | Sort-Object -Property { $_ -replace '[^a-z](.*$)', '$1--' }

function script:chocoCommands($filter) {
    $cmdList = @()
    if (-not $global:ChocolateyTabSettings.AllCommands) {
        $cmdList += $script:chocoCommands -like "$filter*"
    }
    else {
        $cmdList += (& $script:choco -h) |
            Where-Object { $_ -match '^  \S.*' } |
            ForEach-Object { $_.Split(' ', [StringSplitOptions]::RemoveEmptyEntries) } |
            Where-Object { $_ -like "$filter*" }
    }

    $cmdList #| sort
}

function script:chocoLocalPackages($filter) {
    if ($filter -and $filter.StartsWith(".")) {
        return;
    } #file search
    @(& $script:choco list $filter -r --id-starts-with) | ForEach-Object { $_.Split('|')[0] }
}

function script:chocoLocalPackagesUpgrade($filter) {
    if ($filter -and $filter.StartsWith(".")) {
        return;
    } #file search
    @('all|') + @(& $script:choco list $filter -r --id-starts-with) |
        Where-Object { $_ -like "$filter*" } |
        ForEach-Object { $_.Split('|')[0] }
}

function script:chocoRemotePackages($filter) {
    if ($filter -and $filter.StartsWith(".")) {
        return;
    } #file search
    @('packages.config|') + @(& $script:choco search $filter --page='0' --page-size='30' -r --id-starts-with --order-by='popularity') |
        Where-Object { $_ -like "$filter*" } |
        ForEach-Object { $_.Split('|')[0] }
}

function Get-AliasPattern($exe) {
    $aliases = @($exe) + @(Get-Alias | Where-Object { $_.Definition -eq $exe } | Select-Object -Exp Name)

    "($($aliases -join '|'))"
}

function Get-ChocoOrderByOptions {
    <#
        .SYNOPSIS
        Returns the list of canonical --order-by values for Chocolatey.

        .DESCRIPTION
        These values correspond to the distinct, non-aliased entries in the
        PackageOrder enum. They are sorted alphabetically and must be updated
        manually when the enum changes.

        .OUTPUTS
        A string in the format "Id|LastPublished|Popularity|Title|Unsorted"
    #>
    return @("Id", "LastPublished", "Popularity", "Title", "Unsorted")
}

function ChocolateyTabExpansion($lastBlock) {
    switch -regex ($lastBlock -replace "^$(Get-AliasPattern choco) ", "") {

        # Handles apikey first tab
        "^(apikey)\s+(?<subcommand>[^-\s]*)$" {
            @('add', 'list', 'remove', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles cache first tab
        "^(cache)\s+(?<subcommand>[^-\s]*)$" {
            @('list', 'remove', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles config first tab
        "^(config)\s+(?<subcommand>[^-\s]*)$" {
            @('get', 'list', 'set', 'unset', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles feature first tab
        "^(feature)\s+(?<subcommand>[^-\s]*)$" {
            @('disable', 'enable', 'get', 'list', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles install package names
        "^(install)\s+(?<package>[^\.][^-\s]+)$" {
            chocoRemotePackages $matches['package']
        }

        # Handles license first tab
        "^(license)\s+(?<subcommand>[^-\s]*)$" {
            @('info', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles list first tab
        "^(list)\s+(?<subcommand>[^-\s]*)$" {
            @('<filter>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles new first tab
        "^(new)\s+(?<subcommand>[^-\s]*)$" {
            @('<name>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles pack first tab
        "^(pack)\s+(?<subcommand>[^-\s]*)$" {
            @('<PathtoNuspec>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles pin first tab
        "^(pin)\s+(?<subcommand>[^-\s]*)$" {
            @('add', 'list', 'remove', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles push first tab
        "^(push)\s+(?<subcommand>[^-\s]*)$" {
            @('<PathtoNupkg>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles rule first tab
        "^(rule)\s+(?<subcommand>[^-\s]*)$" {
            @('get', 'list', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles search first tab
        "^(search)\s+(?<subcommand>[^-\s]*)$" {
            @('<filter>', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles source first tab
        "^(source)\s+(?<subcommand>[^-\s]*)$" {
            @('add', 'disable', 'enable', 'list', 'remove', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles template first tab
        "^(template)\s+(?<subcommand>[^-\s]*)$" {
            @('info', 'list', '--help') | Where-Object { $_ -like "$($matches['subcommand'])*" }
        }

        # Handles uninstall package names
        "^uninstall\s+(?<package>[^\.][^-\s]*)$" {
            chocoLocalPackages $matches['package']
        }

        # Handles upgrade / uninstall package names
        "^upgrade\s+(?<package>[^\.][^-\s]*)$" {
            chocoLocalPackagesUpgrade $matches['package']
        }

        # Custom completion for --order-by values
        "^search.*--order-by='?(?<prefix>.*)'?$" {
            $prefix = $matches['prefix']
            return Get-ChocoOrderByOptions | Where-Object { $_ -like "$prefix*" } | ForEach-Object { "--order-by='$_'"}
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

$PowerTab_RegisterTabExpansion = if (Get-Module -Name powertab) {
    Get-Command Register-TabExpansion -Module powertab -ErrorAction SilentlyContinue
}
if ($PowerTab_RegisterTabExpansion) {
    & $PowerTab_RegisterTabExpansion "choco" -Type Command {
        param($Context, [ref]$TabExpansionHasOutput, [ref]$QuoteSpaces)  # 1:

        $line = $Context.Line
        $lastBlock = [System.Text.RegularExpressions.Regex]::Split($line, '[|;]')[-1].TrimStart()
        $TabExpansionHasOutput.Value = $true
        ChocolateyTabExpansion $lastBlock
    }

    return
}

# PowerShell up to v5.x: use a custom TabExpansion function.
if ($PSVersionTable.PSVersion.Major -lt 5) {
    if (Test-Path Function:\TabExpansion) {
        Rename-Item Function:\TabExpansion TabExpansionBackup
    }

    function TabExpansion($line, $lastWord) {
        $lastBlock = [System.Text.RegularExpressions.Regex]::Split($line, '[|;]')[-1].TrimStart()


        switch -regex ($lastBlock) {
            # Execute Chocolatey tab completion for all choco-related commands
            "^$(Get-AliasPattern choco) (.*)" {
                ChocolateyTabExpansion $lastBlock
            }

            # Fall back on existing tab expansion
            default {
                if (Test-Path Function:\TabExpansionBackup) {
                    TabExpansionBackup $line $lastWord
                }
            }
        }
    }
}
else { # PowerShell v5+: use the Register-ArgumentCompleter cmdlet (PowerShell no longer calls TabExpansion after 7.4, but this available from 5.x)
    function script:Get-AliasNames($exe) {
        @($exe) + @(Get-Alias | Where-Object { $_.Definition -eq $exe } | Select-Object -Exp Name)
    }

    Register-ArgumentCompleter -Native -CommandName (Get-AliasNames choco) -ScriptBlock {
        param($wordToComplete, $commandAst, $cursorColumn)

        # NOTE:
        # * The stringified form of $commandAst is the command's own command line (irrespective of
        #   whether other statements are on the same line or whether it is part of a pipeline).
        # * However, trailing whitespace is trimmed in the string representation of $commandAst. 
        #   Therefore, when the actual command line ends in space(s), they must be added back
        #   so that ChocolateyTabExpansion recognizes the start of a new argument.
        $ownCommandLine = [string] $commandAst
        $ownCommandLine = $ownCommandLine.Substring(0, [Math]::Min($ownCommandLine.Length, $cursorColumn))
        $ownCommandLine += ' ' * ($cursorColumn - $ownCommandLine.Length)

        ChocolateyTabExpansion $ownCommandLine
    }
}

# SIG # Begin signature block
# MIInYgYJKoZIhvcNAQcCoIInUzCCJ08CAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCB7g5+j1N/cQwVj
# lndPGk84X8jdSPiBRzbh9NqKUfVyR6CCIN8wggWNMIIEdaADAgECAhAOmxiO+dAt
# 5+/bUOIIQBhaMA0GCSqGSIb3DQEBDAUAMGUxCzAJBgNVBAYTAlVTMRUwEwYDVQQK
# EwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xJDAiBgNV
# BAMTG0RpZ2lDZXJ0IEFzc3VyZWQgSUQgUm9vdCBDQTAeFw0yMjA4MDEwMDAwMDBa
# Fw0zMTExMDkyMzU5NTlaMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2Vy
# dCBJbmMxGTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lD
# ZXJ0IFRydXN0ZWQgUm9vdCBHNDCCAiIwDQYJKoZIhvcNAQEBBQADggIPADCCAgoC
# ggIBAL/mkHNo3rvkXUo8MCIwaTPswqclLskhPfKK2FnC4SmnPVirdprNrnsbhA3E
# MB/zG6Q4FutWxpdtHauyefLKEdLkX9YFPFIPUh/GnhWlfr6fqVcWWVVyr2iTcMKy
# unWZanMylNEQRBAu34LzB4TmdDttceItDBvuINXJIB1jKS3O7F5OyJP4IWGbNOsF
# xl7sWxq868nPzaw0QF+xembud8hIqGZXV59UWI4MK7dPpzDZVu7Ke13jrclPXuU1
# 5zHL2pNe3I6PgNq2kZhAkHnDeMe2scS1ahg4AxCN2NQ3pC4FfYj1gj4QkXCrVYJB
# MtfbBHMqbpEBfCFM1LyuGwN1XXhm2ToxRJozQL8I11pJpMLmqaBn3aQnvKFPObUR
# WBf3JFxGj2T3wWmIdph2PVldQnaHiZdpekjw4KISG2aadMreSx7nDmOu5tTvkpI6
# nj3cAORFJYm2mkQZK37AlLTSYW3rM9nF30sEAMx9HJXDj/chsrIRt7t/8tWMcCxB
# YKqxYxhElRp2Yn72gLD76GSmM9GJB+G9t+ZDpBi4pncB4Q+UDCEdslQpJYls5Q5S
# UUd0viastkF13nqsX40/ybzTQRESW+UQUOsxxcpyFiIJ33xMdT9j7CFfxCBRa2+x
# q4aLT8LWRV+dIPyhHsXAj6KxfgommfXkaS+YHS312amyHeUbAgMBAAGjggE6MIIB
# NjAPBgNVHRMBAf8EBTADAQH/MB0GA1UdDgQWBBTs1+OC0nFdZEzfLmc/57qYrhwP
# TzAfBgNVHSMEGDAWgBRF66Kv9JLLgjEtUYunpyGd823IDzAOBgNVHQ8BAf8EBAMC
# AYYweQYIKwYBBQUHAQEEbTBrMCQGCCsGAQUFBzABhhhodHRwOi8vb2NzcC5kaWdp
# Y2VydC5jb20wQwYIKwYBBQUHMAKGN2h0dHA6Ly9jYWNlcnRzLmRpZ2ljZXJ0LmNv
# bS9EaWdpQ2VydEFzc3VyZWRJRFJvb3RDQS5jcnQwRQYDVR0fBD4wPDA6oDigNoY0
# aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0QXNzdXJlZElEUm9vdENB
# LmNybDARBgNVHSAECjAIMAYGBFUdIAAwDQYJKoZIhvcNAQEMBQADggEBAHCgv0Nc
# Vec4X6CjdBs9thbX979XB72arKGHLOyFXqkauyL4hxppVCLtpIh3bb0aFPQTSnov
# Lbc47/T/gLn4offyct4kvFIDyE7QKt76LVbP+fT3rDB6mouyXtTP0UNEm0Mh65Zy
# oUi0mcudT6cGAxN3J0TU53/oWajwvy8LpunyNDzs9wPHh6jSTEAZNUZqaVSwuKFW
# juyk1T3osdz9HNj0d1pcVIxv76FQPfx2CWiEn2/K2yCNNWAcAgPLILCsWKAOQGPF
# mCLBsln1VWvPJ6tsds5vIy30fnFqI2si/xK4VC0nftg62fC2h5b9W9FcrBjDTZ9z
# twGpn1eqXijiuZQwggawMIIEmKADAgECAhAIrUCyYNKcTJ9ezam9k67ZMA0GCSqG
# SIb3DQEBDAUAMGIxCzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMx
# GTAXBgNVBAsTEHd3dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IFRy
# dXN0ZWQgUm9vdCBHNDAeFw0yMTA0MjkwMDAwMDBaFw0zNjA0MjgyMzU5NTlaMGkx
# CzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2VydCwgSW5jLjFBMD8GA1UEAxM4
# RGlnaUNlcnQgVHJ1c3RlZCBHNCBDb2RlIFNpZ25pbmcgUlNBNDA5NiBTSEEzODQg
# MjAyMSBDQTEwggIiMA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQDVtC9C0Cit
# eLdd1TlZG7GIQvUzjOs9gZdwxbvEhSYwn6SOaNhc9es0JAfhS0/TeEP0F9ce2vnS
# 1WcaUk8OoVf8iJnBkcyBAz5NcCRks43iCH00fUyAVxJrQ5qZ8sU7H/Lvy0daE6ZM
# swEgJfMQ04uy+wjwiuCdCcBlp/qYgEk1hz1RGeiQIXhFLqGfLOEYwhrMxe6TSXBC
# Mo/7xuoc82VokaJNTIIRSFJo3hC9FFdd6BgTZcV/sk+FLEikVoQ11vkunKoAFdE3
# /hoGlMJ8yOobMubKwvSnowMOdKWvObarYBLj6Na59zHh3K3kGKDYwSNHR7OhD26j
# q22YBoMbt2pnLdK9RBqSEIGPsDsJ18ebMlrC/2pgVItJwZPt4bRc4G/rJvmM1bL5
# OBDm6s6R9b7T+2+TYTRcvJNFKIM2KmYoX7BzzosmJQayg9Rc9hUZTO1i4F4z8ujo
# 7AqnsAMrkbI2eb73rQgedaZlzLvjSFDzd5Ea/ttQokbIYViY9XwCFjyDKK05huzU
# tw1T0PhH5nUwjewwk3YUpltLXXRhTT8SkXbev1jLchApQfDVxW0mdmgRQRNYmtwm
# KwH0iU1Z23jPgUo+QEdfyYFQc4UQIyFZYIpkVMHMIRroOBl8ZhzNeDhFMJlP/2NP
# TLuqDQhTQXxYPUez+rbsjDIJAsxsPAxWEQIDAQABo4IBWTCCAVUwEgYDVR0TAQH/
# BAgwBgEB/wIBADAdBgNVHQ4EFgQUaDfg67Y7+F8Rhvv+YXsIiGX0TkIwHwYDVR0j
# BBgwFoAU7NfjgtJxXWRM3y5nP+e6mK4cD08wDgYDVR0PAQH/BAQDAgGGMBMGA1Ud
# JQQMMAoGCCsGAQUFBwMDMHcGCCsGAQUFBwEBBGswaTAkBggrBgEFBQcwAYYYaHR0
# cDovL29jc3AuZGlnaWNlcnQuY29tMEEGCCsGAQUFBzAChjVodHRwOi8vY2FjZXJ0
# cy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9vdEc0LmNydDBDBgNVHR8E
# PDA6MDigNqA0hjJodHRwOi8vY3JsMy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVz
# dGVkUm9vdEc0LmNybDAcBgNVHSAEFTATMAcGBWeBDAEDMAgGBmeBDAEEATANBgkq
# hkiG9w0BAQwFAAOCAgEAOiNEPY0Idu6PvDqZ01bgAhql+Eg08yy25nRm95RysQDK
# r2wwJxMSnpBEn0v9nqN8JtU3vDpdSG2V1T9J9Ce7FoFFUP2cvbaF4HZ+N3HLIvda
# qpDP9ZNq4+sg0dVQeYiaiorBtr2hSBh+3NiAGhEZGM1hmYFW9snjdufE5BtfQ/g+
# lP92OT2e1JnPSt0o618moZVYSNUa/tcnP/2Q0XaG3RywYFzzDaju4ImhvTnhOE7a
# brs2nfvlIVNaw8rpavGiPttDuDPITzgUkpn13c5UbdldAhQfQDN8A+KVssIhdXNS
# y0bYxDQcoqVLjc1vdjcshT8azibpGL6QB7BDf5WIIIJw8MzK7/0pNVwfiThV9zeK
# iwmhywvpMRr/LhlcOXHhvpynCgbWJme3kuZOX956rEnPLqR0kq3bPKSchh/jwVYb
# KyP/j7XqiHtwa+aguv06P0WmxOgWkVKLQcBIhEuWTatEQOON8BUozu3xGFYHKi8Q
# xAwIZDwzj64ojDzLj4gLDb879M4ee47vtevLt/B3E+bnKD+sEq6lLyJsQfmCXBVm
# zGwOysWGw/YmMwwHS6DTBwJqakAwSEs0qFEgu60bhQjiWQ1tygVQK+pKHJ6l/aCn
# HwZ05/LWUpD9r4VIIflXO7ScA+2GRfS0YW6/aOImYIbqyK+p/pQd52MbOoZWeE4w
# gga0MIIEnKADAgECAhANx6xXBf8hmS5AQyIMOkmGMA0GCSqGSIb3DQEBCwUAMGIx
# CzAJBgNVBAYTAlVTMRUwEwYDVQQKEwxEaWdpQ2VydCBJbmMxGTAXBgNVBAsTEHd3
# dy5kaWdpY2VydC5jb20xITAfBgNVBAMTGERpZ2lDZXJ0IFRydXN0ZWQgUm9vdCBH
# NDAeFw0yNTA1MDcwMDAwMDBaFw0zODAxMTQyMzU5NTlaMGkxCzAJBgNVBAYTAlVT
# MRcwFQYDVQQKEw5EaWdpQ2VydCwgSW5jLjFBMD8GA1UEAxM4RGlnaUNlcnQgVHJ1
# c3RlZCBHNCBUaW1lU3RhbXBpbmcgUlNBNDA5NiBTSEEyNTYgMjAyNSBDQTEwggIi
# MA0GCSqGSIb3DQEBAQUAA4ICDwAwggIKAoICAQC0eDHTCphBcr48RsAcrHXbo0Zo
# dLRRF51NrY0NlLWZloMsVO1DahGPNRcybEKq+RuwOnPhof6pvF4uGjwjqNjfEvUi
# 6wuim5bap+0lgloM2zX4kftn5B1IpYzTqpyFQ/4Bt0mAxAHeHYNnQxqXmRinvuNg
# xVBdJkf77S2uPoCj7GH8BLuxBG5AvftBdsOECS1UkxBvMgEdgkFiDNYiOTx4OtiF
# cMSkqTtF2hfQz3zQSku2Ws3IfDReb6e3mmdglTcaarps0wjUjsZvkgFkriK9tUKJ
# m/s80FiocSk1VYLZlDwFt+cVFBURJg6zMUjZa/zbCclF83bRVFLeGkuAhHiGPMvS
# GmhgaTzVyhYn4p0+8y9oHRaQT/aofEnS5xLrfxnGpTXiUOeSLsJygoLPp66bkDX1
# ZlAeSpQl92QOMeRxykvq6gbylsXQskBBBnGy3tW/AMOMCZIVNSaz7BX8VtYGqLt9
# MmeOreGPRdtBx3yGOP+rx3rKWDEJlIqLXvJWnY0v5ydPpOjL6s36czwzsucuoKs7
# Yk/ehb//Wx+5kMqIMRvUBDx6z1ev+7psNOdgJMoiwOrUG2ZdSoQbU2rMkpLiQ6bG
# RinZbI4OLu9BMIFm1UUl9VnePs6BaaeEWvjJSjNm2qA+sdFUeEY0qVjPKOWug/G6
# X5uAiynM7Bu2ayBjUwIDAQABo4IBXTCCAVkwEgYDVR0TAQH/BAgwBgEB/wIBADAd
# BgNVHQ4EFgQU729TSunkBnx6yuKQVvYv1Ensy04wHwYDVR0jBBgwFoAU7NfjgtJx
# XWRM3y5nP+e6mK4cD08wDgYDVR0PAQH/BAQDAgGGMBMGA1UdJQQMMAoGCCsGAQUF
# BwMIMHcGCCsGAQUFBwEBBGswaTAkBggrBgEFBQcwAYYYaHR0cDovL29jc3AuZGln
# aWNlcnQuY29tMEEGCCsGAQUFBzAChjVodHRwOi8vY2FjZXJ0cy5kaWdpY2VydC5j
# b20vRGlnaUNlcnRUcnVzdGVkUm9vdEc0LmNydDBDBgNVHR8EPDA6MDigNqA0hjJo
# dHRwOi8vY3JsMy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkUm9vdEc0LmNy
# bDAgBgNVHSAEGTAXMAgGBmeBDAEEAjALBglghkgBhv1sBwEwDQYJKoZIhvcNAQEL
# BQADggIBABfO+xaAHP4HPRF2cTC9vgvItTSmf83Qh8WIGjB/T8ObXAZz8OjuhUxj
# aaFdleMM0lBryPTQM2qEJPe36zwbSI/mS83afsl3YTj+IQhQE7jU/kXjjytJgnn0
# hvrV6hqWGd3rLAUt6vJy9lMDPjTLxLgXf9r5nWMQwr8Myb9rEVKChHyfpzee5kH0
# F8HABBgr0UdqirZ7bowe9Vj2AIMD8liyrukZ2iA/wdG2th9y1IsA0QF8dTXqvcnT
# mpfeQh35k5zOCPmSNq1UH410ANVko43+Cdmu4y81hjajV/gxdEkMx1NKU4uHQcKf
# ZxAvBAKqMVuqte69M9J6A47OvgRaPs+2ykgcGV00TYr2Lr3ty9qIijanrUR3anzE
# wlvzZiiyfTPjLbnFRsjsYg39OlV8cipDoq7+qNNjqFzeGxcytL5TTLL4ZaoBdqbh
# OhZ3ZRDUphPvSRmMThi0vw9vODRzW6AxnJll38F0cuJG7uEBYTptMSbhdhGQDpOX
# gpIUsWTjd6xpR6oaQf/DJbg3s6KCLPAlZ66RzIg9sC+NJpud/v4+7RWsWCiKi9EO
# LLHfMR2ZyJ/+xhCx9yHbxtl5TPau1j/1MIDpMPx0LckTetiSuEtQvLsNz3Qbp7wG
# WqbIiOWCnb5WqxL3/BAPvIXKUjPSxyZsq8WhbaM2tszWkPZPubdcMIIG7TCCBNWg
# AwIBAgIQBNI793flHTneCMtwLiiYFTANBgkqhkiG9w0BAQsFADBpMQswCQYDVQQG
# EwJVUzEXMBUGA1UEChMORGlnaUNlcnQsIEluYy4xQTA/BgNVBAMTOERpZ2lDZXJ0
# IFRydXN0ZWQgRzQgQ29kZSBTaWduaW5nIFJTQTQwOTYgU0hBMzg0IDIwMjEgQ0Ex
# MB4XDTI0MDUwOTAwMDAwMFoXDTI3MDUxMTIzNTk1OVowdTELMAkGA1UEBhMCVVMx
# DzANBgNVBAgTBkthbnNhczEPMA0GA1UEBxMGVG9wZWthMSEwHwYDVQQKExhDaG9j
# b2xhdGV5IFNvZnR3YXJlLCBJbmMxITAfBgNVBAMTGENob2NvbGF0ZXkgU29mdHdh
# cmUsIEluYzCCAaIwDQYJKoZIhvcNAQEBBQADggGPADCCAYoCggGBAPDJgdZWj0RV
# lBBBniCyGy19FB736U5AahB+dAw3nmafOEeG+syql0m9kzV0gu4bSd4Al587ioAG
# DUPAGhXf0R+y11cx7c1cgdyxvfBvfMEkgD7sOUeF9ggZJc0YZ4qc7Pa6qqMpHDru
# pjshvLmQMSLaGKF68m+w2mJiZkLMYBEotPiAC3+IzI1MQqidCfN6rfQUmtcKyrVz
# 2zCt8CvuR3pSyNCBcQgKZ/+NwBfDqPTt1wKq5JCIQiLnbDZwJ9F5433enzgUGQgh
# KRoIwfp/hap7t7lrNf859Xe1/zHT4qtNgzGqSdJ2Kbz1YAMFjZokYHv/sliyxJN9
# 7++0BApX2t45JsQaqyQ60TSKxqOH0JIIDeYgwxfJ8YFmuvt7T4zVM8u02Axp/1YV
# nKP2AOVca6FDe9EiccrexAWPGoP+WQi8WFQKrNVKr5XTLI0MNTjadOHfF0XUToyF
# H8FVnZZV1/F1kgd/bYbt/0M/QkS4FGmJoqT8dyRyMkTlTynKul4N3QIDAQABo4IC
# AzCCAf8wHwYDVR0jBBgwFoAUaDfg67Y7+F8Rhvv+YXsIiGX0TkIwHQYDVR0OBBYE
# FFpfZUilS5A+fjYV80ib5qKkBoczMD4GA1UdIAQ3MDUwMwYGZ4EMAQQBMCkwJwYI
# KwYBBQUHAgEWG2h0dHA6Ly93d3cuZGlnaWNlcnQuY29tL0NQUzAOBgNVHQ8BAf8E
# BAMCB4AwEwYDVR0lBAwwCgYIKwYBBQUHAwMwgbUGA1UdHwSBrTCBqjBToFGgT4ZN
# aHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0RpZ2lDZXJ0VHJ1c3RlZEc0Q29kZVNp
# Z25pbmdSU0E0MDk2U0hBMzg0MjAyMUNBMS5jcmwwU6BRoE+GTWh0dHA6Ly9jcmw0
# LmRpZ2ljZXJ0LmNvbS9EaWdpQ2VydFRydXN0ZWRHNENvZGVTaWduaW5nUlNBNDA5
# NlNIQTM4NDIwMjFDQTEuY3JsMIGUBggrBgEFBQcBAQSBhzCBhDAkBggrBgEFBQcw
# AYYYaHR0cDovL29jc3AuZGlnaWNlcnQuY29tMFwGCCsGAQUFBzAChlBodHRwOi8v
# Y2FjZXJ0cy5kaWdpY2VydC5jb20vRGlnaUNlcnRUcnVzdGVkRzRDb2RlU2lnbmlu
# Z1JTQTQwOTZTSEEzODQyMDIxQ0ExLmNydDAJBgNVHRMEAjAAMA0GCSqGSIb3DQEB
# CwUAA4ICAQAW9ANNkR2cF6ulbM+/XUWeWqC7UTqtsRwj7WAo8XTr52JebRchTGDH
# BZP9sDRZsFt+lPcPvBrv41kWoaFBmebTaPMh6YDHaON+uc19CTWXsMh8eog0lzGU
# iA3mKdbVit0udrgNlBUqTIuvMlMFIARWSz90FMeQrCFokLmqoqjp7u0sVPM7ng6T
# 9D8ct/m5LSpIa5TJCjAfyfw75GK0wzTDdTi1MgiAIyX0EedMrEwXjOjSApQ+uhIW
# v/AHDf8ukJzDFTTeiUkYZ1w++z70QZkzLfQTi6eH9vqgyXWcnGCwOxKquqe8RSIe
# M3FdtLstn9nI8S4qeiKdmomG6FAZTzYiGULJdJGsLh6Uii56zZdq3bSre/yrfed4
# hf/0MqEtWSU7LpkWM8AApRkIKRBZIQ73/7WxwsF9kHoZxqoRMDGTzWt+S7/XrSOa
# QbKf0CxdxMPHKC2A1u3xGNDChtQEwpHxYXf/teD7GeFYFQJg/wn4dC72mZze97+c
# YcpmI4R13Q7owmRthK1hnuq4EOQIcoTPbQXiaRzULbYrcOnJi7EbXcqdeAAnZAyV
# b6zGqAaE9Sw4RYvkosL5IlBgrdIwSFJMbeirBoM2GukIHQ8UaEu3l1PoNQvVbqM1
# 8zHiN4WA4rp9G9wfcAlZWq9iKF34sA+Xu03qSVaKPKn6YJMl5PfUsDCCBu0wggTV
# oAMCAQICEAqA7xhLjfEFgtHEdqeVdGgwDQYJKoZIhvcNAQELBQAwaTELMAkGA1UE
# BhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMUEwPwYDVQQDEzhEaWdpQ2Vy
# dCBUcnVzdGVkIEc0IFRpbWVTdGFtcGluZyBSU0E0MDk2IFNIQTI1NiAyMDI1IENB
# MTAeFw0yNTA2MDQwMDAwMDBaFw0zNjA5MDMyMzU5NTlaMGMxCzAJBgNVBAYTAlVT
# MRcwFQYDVQQKEw5EaWdpQ2VydCwgSW5jLjE7MDkGA1UEAxMyRGlnaUNlcnQgU0hB
# MjU2IFJTQTQwOTYgVGltZXN0YW1wIFJlc3BvbmRlciAyMDI1IDEwggIiMA0GCSqG
# SIb3DQEBAQUAA4ICDwAwggIKAoICAQDQRqwtEsae0OquYFazK1e6b1H/hnAKAd/K
# N8wZQjBjMqiZ3xTWcfsLwOvRxUwXcGx8AUjni6bz52fGTfr6PHRNv6T7zsf1Y/E3
# IU8kgNkeECqVQ+3bzWYesFtkepErvUSbf+EIYLkrLKd6qJnuzK8Vcn0DvbDMemQF
# oxQ2Dsw4vEjoT1FpS54dNApZfKY61HAldytxNM89PZXUP/5wWWURK+IfxiOg8W9l
# KMqzdIo7VA1R0V3Zp3DjjANwqAf4lEkTlCDQ0/fKJLKLkzGBTpx6EYevvOi7XOc4
# zyh1uSqgr6UnbksIcFJqLbkIXIPbcNmA98Oskkkrvt6lPAw/p4oDSRZreiwB7x9y
# krjS6GS3NR39iTTFS+ENTqW8m6THuOmHHjQNC3zbJ6nJ6SXiLSvw4Smz8U07hqF+
# 8CTXaETkVWz0dVVZw7knh1WZXOLHgDvundrAtuvz0D3T+dYaNcwafsVCGZKUhQPL
# 1naFKBy1p6llN3QgshRta6Eq4B40h5avMcpi54wm0i2ePZD5pPIssoszQyF4//3D
# oK2O65Uck5Wggn8O2klETsJ7u8xEehGifgJYi+6I03UuT1j7FnrqVrOzaQoVJOee
# StPeldYRNMmSF3voIgMFtNGh86w3ISHNm0IaadCKCkUe2LnwJKa8TIlwCUNVwppw
# n4D3/Pt5pwIDAQABo4IBlTCCAZEwDAYDVR0TAQH/BAIwADAdBgNVHQ4EFgQU5Dv8
# 8jHt/f3X85FxYxlQQ89hjOgwHwYDVR0jBBgwFoAU729TSunkBnx6yuKQVvYv1Ens
# y04wDgYDVR0PAQH/BAQDAgeAMBYGA1UdJQEB/wQMMAoGCCsGAQUFBwMIMIGVBggr
# BgEFBQcBAQSBiDCBhTAkBggrBgEFBQcwAYYYaHR0cDovL29jc3AuZGlnaWNlcnQu
# Y29tMF0GCCsGAQUFBzAChlFodHRwOi8vY2FjZXJ0cy5kaWdpY2VydC5jb20vRGln
# aUNlcnRUcnVzdGVkRzRUaW1lU3RhbXBpbmdSU0E0MDk2U0hBMjU2MjAyNUNBMS5j
# cnQwXwYDVR0fBFgwVjBUoFKgUIZOaHR0cDovL2NybDMuZGlnaWNlcnQuY29tL0Rp
# Z2lDZXJ0VHJ1c3RlZEc0VGltZVN0YW1waW5nUlNBNDA5NlNIQTI1NjIwMjVDQTEu
# Y3JsMCAGA1UdIAQZMBcwCAYGZ4EMAQQCMAsGCWCGSAGG/WwHATANBgkqhkiG9w0B
# AQsFAAOCAgEAZSqt8RwnBLmuYEHs0QhEnmNAciH45PYiT9s1i6UKtW+FERp8FgXR
# GQ/YAavXzWjZhY+hIfP2JkQ38U+wtJPBVBajYfrbIYG+Dui4I4PCvHpQuPqFgqp1
# PzC/ZRX4pvP/ciZmUnthfAEP1HShTrY+2DE5qjzvZs7JIIgt0GCFD9ktx0LxxtRQ
# 7vllKluHWiKk6FxRPyUPxAAYH2Vy1lNM4kzekd8oEARzFAWgeW3az2xejEWLNN4e
# KGxDJ8WDl/FQUSntbjZ80FU3i54tpx5F/0Kr15zW/mJAxZMVBrTE2oi0fcI8VMbt
# oRAmaaslNXdCG1+lqvP4FbrQ6IwSBXkZagHLhFU9HCrG/syTRLLhAezu/3Lr00Gr
# JzPQFnCEH1Y58678IgmfORBPC1JKkYaEt2OdDh4GmO0/5cHelAK2/gTlQJINqDr6
# JfwyYHXSd+V08X1JUPvB4ILfJdmL+66Gp3CSBXG6IwXMZUXBhtCyIaehr0XkBoDI
# GMUG1dUtwq1qmcwbdUfcSYCn+OwncVUXf53VJUNOaMWMts0VlRYxe5nK+At+DI96
# HAlXHAL5SlfYxJ7La54i71McVWRP66bW+yERNpbJCjyCYG2j+bdpxo/1Cy4uPcU3
# AWVPGrbn5PhDBf3Froguzzhk++ami+r3Qrx5bIbY3TVzgiFI7Gq3zWcxggXZMIIF
# 1QIBATB9MGkxCzAJBgNVBAYTAlVTMRcwFQYDVQQKEw5EaWdpQ2VydCwgSW5jLjFB
# MD8GA1UEAxM4RGlnaUNlcnQgVHJ1c3RlZCBHNCBDb2RlIFNpZ25pbmcgUlNBNDA5
# NiBTSEEzODQgMjAyMSBDQTECEATSO/d35R053gjLcC4omBUwDQYJYIZIAWUDBAIB
# BQCggYQwGAYKKwYBBAGCNwIBDDEKMAigAoAAoQKAADAZBgkqhkiG9w0BCQMxDAYK
# KwYBBAGCNwIBBDAcBgorBgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAvBgkqhkiG
# 9w0BCQQxIgQgG81No88spw3iUpuf26rTdLTI+AYY5scUeL5UCJbM9YowDQYJKoZI
# hvcNAQEBBQAEggGAM+K6xp9ulsnktUdD2HJ5Bbw3xLXulQOWiiC9QJXNoHMZ6CY5
# 3AFsQQ1kuh9caa0NdvZ8xFFJbTRkH6NxU66p8d6wXOJDGQhzxu7y7FX6ZjSK9AN4
# thVUEr2lgXBHIiH/X+pqGLO+uL8yjFqhPACUAHm+iv8hUysxVlUAo/L0AE390Shl
# QRDlWoKy6a97DspWl+N6h9IoKe2oRDaegvIp0SjSfQN1Nd/7GzhgeXoJ1xFlBTri
# QaCEONN5zc/p48leljsXwR4mIpt0JytFUMiPJt4kTwqCoqXCWmHsJQtbLvjYLPBC
# HqvFaWH8GQ8DRmOm9FIKrM262NdvPELNUfjshAg5/5fR0nB8xMbvbFB4xCDO6HcR
# uW8LMKhsZ96w5hMytpKdd71wjdP0Y9HJTzI15MIB9BaHeTs9ELo7UPE1NwbhBfXi
# HZunreKtEvgWVdmdT0b36gcg+w8/RIawAShHTYLziuk4PbjvIdml3aUPB1hh0iWX
# QCdjSI+Gux2dLblkoYIDJjCCAyIGCSqGSIb3DQEJBjGCAxMwggMPAgEBMH0waTEL
# MAkGA1UEBhMCVVMxFzAVBgNVBAoTDkRpZ2lDZXJ0LCBJbmMuMUEwPwYDVQQDEzhE
# aWdpQ2VydCBUcnVzdGVkIEc0IFRpbWVTdGFtcGluZyBSU0E0MDk2IFNIQTI1NiAy
# MDI1IENBMQIQCoDvGEuN8QWC0cR2p5V0aDANBglghkgBZQMEAgEFAKBpMBgGCSqG
# SIb3DQEJAzELBgkqhkiG9w0BBwEwHAYJKoZIhvcNAQkFMQ8XDTI1MDYyMDE3MDc0
# NlowLwYJKoZIhvcNAQkEMSIEIIi3YSBomHWudbWivSrSJjcnsAhKgeL6t3bHLGFQ
# osikMA0GCSqGSIb3DQEBAQUABIICAHzAuow09rHai9XiksoRn+crDIem2ECGgYPd
# ik10pbZIY0moHMNYoRW9oBeJlvFAocEKMW2iREwoIsMqXjQz/ELsbI2cjjgmc3rE
# SJau5uv7nA65o273j4NwaKcFJdLH/LqJ6Jep4IcR7X5b5ZXml1HdsJTtAE0VpE9/
# O2D0lSUiCPaawPsLqTM7ejatQNCGI/Irpy5jQTExOVtSIvz29elOtv22F/lU9doG
# VIRJfaqh6YLAO7K6QDKo+isH7JtvKA0Uw+l7uM0CCFwK1TTUm8xRwBvG6GfJBehd
# L0OHP3+21ojyVv/LdstD/kf2PZOTAgeTAEr1GZbzXG5Vak0P3GxUHd8UXYqm8UcP
# BfJpAQLJBVvxNa7qBvNjOwFZxoZiqk7xkf15zvkv6y/0KYKRl+4jlSi9mIDUIJO5
# xhu17FUJNZiRSjNPiUMmtcXvFwTIsx8k+jumsaXQngtGxeVusEne29gXmoyaNZCp
# Ics19xhjosUYtRiVDZGUvE9wJeKm4KJPVLRb2Btr5NWTHghvDbqr5FCxYQRvA1m+
# VMa/Vni6NN+zKKtjiuCArIoXxIYPOerIgPY40itUcj79VV9tgxyDKNl1U1Ml811J
# C7YzK4rL9mVpJasjllXvOwBxiGIcvplyoXmEY0f7oevLc6z67yCH6Rc38Rr0SdyB
# 0aUSN3fL
# SIG # End signature block
