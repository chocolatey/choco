Import-Module helpers/common-helpers

Describe "choco upgrade" -Tag Chocolatey, UpgradeCommand {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Can upgrade packages with dependencies containing side by side installations and outdated dependency" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install chocolatey-core.extension --version 1.3.0 --confirm
            $null = Invoke-Choco install 7zip --version 16.04 --confirm
            $null = Invoke-Choco install chocolatey-core.extension --version 1.3.5.1 --sxs --confirm

            $Output = Invoke-Choco upgrade 7zip --version 21.7 --confirm
        }

        AfterAll {
            $null = Invoke-Choco uninstall 7zip 7zip.install --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Upgrades version <OldVersion> of the package <Name>" -ForEach @(
            @{ Name = "7zip"; OldVersion = "16.04" }
            @{ Name = "7zip.install"; OldVersion = "16.04" }
            @{ Name = "chocolatey-core.extension"; OldVersion = "1.3.0" }
        ) {
            "$env:ChocolateyInstall\lib\$Name\$Name.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$Name\$Name.nuspec"
            $XML.package.metadata.version | Should -Not -Be $OldVersion
        }

        It "Have not upgraded side by side installation of <Name> v<Version>" -ForEach @(
            @{ Name = "chocolatey-core.extension"; Version = "1.3.5.1" }
        ) {
            "$env:ChocolateyInstall\lib\$Name.$Version\$Name.$Version.nupkg" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$Name.$Version\$Name.$Version.nuspec"
            $XML.package.metadata.id | Should -Be $Name
            $XML.package.metadata.version | Should -Be $Version
        }

        It "Outputs a message showing that upgrading was successful" {
            $Output.String | SHould -Match "Chocolatey upgraded 3/3 packages\."
        }
    }

    Context "Can upgrade packages with dependencies containing side by side installations and up to date dependency" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install chocolatey-core.extension --version 1.3.3 --confirm
            $null = Invoke-Choco install 7zip --version 16.04 --confirm
            $null = Invoke-Choco install chocolatey-core.extension --version 1.3.5.1 --sxs --confirm

            $Output = Invoke-Choco upgrade 7zip --version 21.7 --confirm
        }

        AfterAll {
            $null = Invoke-Choco uninstall 7zip 7zip.install --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Upgrades version <OldVersion> of the package <Name>" -ForEach @(
            @{ Name = "7zip"; OldVersion = "16.04" }
            @{ Name = "7zip.install"; OldVersion = "16.04" }
        ) {
            "$env:ChocolateyInstall\lib\$Name\$Name.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$Name\$Name.nuspec"
            $XML.package.metadata.version | Should -Not -Be $OldVersion
        }

        It "Have not upgraded dependency <Name>" {
            "$env:ChocolateyInstall\lib\chocolatey-core.extension\chocolatey-core.extension.nupkg" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\chocolatey-core.extension\chocolatey-core.extension.nuspec"
            $XML.package.metadata.id | Should -Be 'chocolatey-core.extension'
            $XML.package.metadata.version | Should -Be '1.3.3'
        }

        It "Have not upgraded side by side installation of <Name> v<Version>" -ForEach @(
            @{ Name = "chocolatey-core.extension"; Version = "1.3.5.1" }
        ) {
            "$env:ChocolateyInstall\lib\$Name.$Version\$Name.$Version.nupkg" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$Name.$Version\$Name.$Version.nuspec"
            $XML.package.metadata.id | Should -Be $Name
            $XML.package.metadata.version | Should -Be $Version
        }

        It "Outputs a message showing that upgrading was successful" {
            $Output.String | SHould -Match "Chocolatey upgraded 2/2 packages\."
        }
    }


    Context "Can upgrade packages with dependencies containing outdated side by side installations and up to date dependency" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install chocolatey-core.extension --version 1.3.3 --confirm
            $null = Invoke-Choco install 7zip --version 16.04 --confirm
            $null = Invoke-Choco install chocolatey-core.extension --version 1.3.0 --sxs --confirm

            $Output = Invoke-Choco upgrade 7zip --version 21.7 --confirm
        }

        AfterAll {
            $null = Invoke-Choco uninstall 7zip 7zip.install --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Upgrades version <OldVersion> of the package <Name>" -ForEach @(
            @{ Name = "7zip"; OldVersion = "16.04" }
            @{ Name = "7zip.install"; OldVersion = "16.04" }
        ) {
            "$env:ChocolateyInstall\lib\$Name\$Name.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$Name\$Name.nuspec"
            $XML.package.metadata.version | Should -Not -Be $OldVersion
        }

        It "Have not upgraded dependency chocolatey-core.extension" {
            "$env:ChocolateyInstall\lib\chocolatey-core.extension\chocolatey-core.extension.nupkg" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\chocolatey-core.extension\chocolatey-core.extension.nuspec"
            $XML.package.metadata.id | Should -Be 'chocolatey-core.extension'
            $XML.package.metadata.version | Should -Be '1.3.3'
        }

        It "Have not upgraded side by side installation of <Name> v<Version>" -ForEach @(
            @{ Name = "chocolatey-core.extension"; Version = "1.3.0" }
        ) {
            "$env:ChocolateyInstall\lib\$Name.$Version\$Name.$Version.nupkg" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$Name.$Version\$Name.$Version.nuspec"
            $XML.package.metadata.id | Should -Be $Name
            $XML.package.metadata.version | Should -Be $Version
        }

        It "Outputs a message showing that upgrading was successful" {
            $Output.String | SHould -Match "Chocolatey upgraded 2/2 packages\."
        }
    }
}
