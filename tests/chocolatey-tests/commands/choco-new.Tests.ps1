Import-Module helpers/common-helpers

$defaultTemplateFiles = @(
    "_TODO.txt"
    "default-package.nuspec"
    "ReadMe.md"
    "tools\chocolateyinstall.ps1"
    "tools\chocolateybeforemodify.ps1"
    "tools\chocolateyuninstall.ps1"
    "tools\LICENSE.txt"
    "tools\VERIFICATION.txt"
)

$EmptyFolders = @(
    "EmptyFolder1"
    "EmptyFolder2\EmptySubFolder"
)
Describe "choco new" -Tag Chocolatey, NewCommand {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $expectedHeader = Get-ExpectedChocolateyHeader
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Create new package with default template" {
        BeforeAll {
            New-ChocolateyInstallSnapshot -SetWorkDir

            $Output = Invoke-Choco new default-package
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays the directory created" {
            $Output.Lines | Should -Contain "Creating a new package specification at $PWD\default-package"
        }

        It "Displays the file path to <_>" -ForEach $defaultTemplateFiles {
            $Output.Lines | Should -Contain "at '$PWD\default-package\$_'"
        }

        It "Creates expected file at <_>" -ForEach $defaultTemplateFiles {
            "$PWD\default-package\$_" | Should -Exist
        }

        It "Do not contain `$uninstalled variable" -Skip:$(-not (Test-ChocolateyVersionEqualOrHigherThan "0.10.16-beta")) {
            "$PWD\default-package\tools\chocolateyuninstall.ps1" | Should -Not -FileContentMatch "[$]uninstalled\s*=\s*[$]false"
        }

        # Issue: https://github.com/chocolatey/choco/issues/1364
        It "Create nuspec without BOM inserted" -Skip:$(-not (Test-ChocolateyVersionEqualOrHigherThan "0.10.16-beta")) {
            $bom = Test-ByteOrderMark -Path "$PWD\default-package\default-package.nuspec"
            $bom | Should -BeFalse
        }

        It "Creates powershell scripts with UTF-8 BOM" {
            $scripts = Get-ChildItem "$PWD\default-package" -Filter "*.ps1" -Recurse

            $scripts | ForEach-Object {
                $encoding = Test-ByteOrderMark -Path $_.FullName

                $encoding | Should -Be ([System.Text.Encoding]::UTF8)
            }
        }
    }

    Context "Create new package with default template, specifying as automatic package, custom version, custom maintainer and custom url" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot -SetWorkDir

            $Output = Invoke-Choco new override --auto --version=5.2.1 '--maintainer="Test User"' url=https://test-url.com
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Creates metadata with version set as 5.2.1" {
            "override\override.nuspec" | Should -FileContentMatchExactly "<version>5\.2\.1</version>"
        }

        It "Creates metadata with owners set as Test User" {
            "override\override.nuspec" | Should -FileContentMatchExactly "<owners>Test User</owners>"
        }

        It "Creates metadata with note about being an automatic package" {
            "override\override.nuspec" | Should -FileContentMatchExact "\*\*Please Note\*\*: This is an automatically updated package\."
        }

        # The following test is currently being skipped on 0.10.15 due to a bug
        # in the program that causes the key-value pairs not being replaced.
        It "Creates install script with download url set to https://test-url.com" -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan "0.10.16-beta")) {
            "override\tools\chocolateyinstall.ps1" | Should -FileContentMatchExactly "\`$url\s*= 'https://test-url.com'"
        }

        Context "Create new package with a custom template" {
            BeforeAll {
                Restore-ChocolateyInstallSnapshot -SetWorkDir
                $null = Invoke-Choco install package.template -y

                $Output = Invoke-Choco new custom-package --template package
            }

            It "Exits with Success (0)" {
                $Output.ExitCode | Should -Be 0
            }

            It "Displays chocolatey name with version" {
                $Output.Lines | Should -Contain $expectedHeader
            }

            It "Creates the file or directory <_>" -ForEach @(
                "chocolateyinstall.ps1"
                "notice.txt"
                "tools" # This folder always gets created by Chocolatey
            ) {
                "custom-package\$_" | Should -Exist
            }

            It "Does not create any files in tools folder" {
                "custom-package\tools\*" | Should -Not -Exist
            }
        }
    }

    # https://github.com/chocolatey/choco/issues/1003
    Context "Create new package with template containing empty folders" -Foreach @{ EmptyFolders = $EmptyFolders } {
        BeforeAll {
            New-ChocolateyInstallSnapshot -SetWorkDir

            $null = Invoke-Choco install zip.template
            foreach ($Folder in $EmptyFolders) {
                New-Item $env:ChocolateyInstall\templates\zip\$Folder -ItemType Directory -Force
            }
            $Output = Invoke-Choco new emptyfolder --template=zip
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Creates the empty folders expected (<_>)" -ForEach $EmptyFolders {
            "$PWD\emptyfolder\$_" | Should -Exist
        }
    }
}
