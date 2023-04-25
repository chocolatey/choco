Import-Module helpers/common-helpers

Describe "choco uninstall" -Tag Chocolatey, UninstallCommand {
    BeforeAll {
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Uninstalling package that makes use of new Get Chocolatey Path helper" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            Enable-ChocolateySource -Name 'local'

            $null = Invoke-Choco install test-chocolateypath -y

            $Output = Invoke-Choco uninstall test-chocolateypath -y
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Outputs message <_>" -ForEach @(
            'Package Path in Before Modify Script: <installPath>\lib\test-chocolateypath'
            'Install Path in Before Modify Script: <installPath>'
            'Package Path in Uninstall Script: <installPath>\lib\test-chocolateypath'
            'Install Path in Uninstall Script: <installPath>'
        ) {
            $Output.Lines | Should -Contain ($_ -replace '<installPath>', $env:ChocolateyInstall) -Because $Output.String
        }
    }

    # Broken in latest Chocolatey CLI v2.0.0-beta
    Context "Uninstalling a package when chocolateyBeforeModify fails" -Tag Broken {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install upgradepackage --version 1.0.0 --confirm

            $Output = Invoke-Choco uninstall upgradepackage --confirm
        }

        # Broken since v1.3.1
        It "Exits with Success (0)" -Tag Broken {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Should have removed lib package directory" {
            "$env:ChocolateyInstall\lib\upgradepackage" | Should -Not -Exist
        }

        It "Should not have created lib-bad directory" {
            "$env:ChocolateyInstall\lib-bad\upgradepackage" | Should -Not -Exist
        }

        It "Should have removed lib-bkp directory" {
            "$env:ChocolateyInstall\lib-bkp\upgradepackage" | Should -Not -Exist
        }

        It "Outputs Successful uninstall" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 1/1 packages."
        }
    }

    Context "Uninstalling a package with a failing uninstall script" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install uninstallfailure --confirm --no-progress

            "Test file" | Out-File "$env:ChocolateyInstall\lib\uninstallfailure\test-file.txt"

            $Output = Invoke-Choco uninstall uninstallfailure --confirm
        }

        It "Exits with Failure (-1)" {
            $Output.ExitCode | Should -Be -1 -Because $Output.String
        }

        It "Should have kept file '<_>' in lib directory" -ForEach @('uninstallfailure.nupkg', 'uninstallfailure.nuspec', 'tools\chocolateyuninstall.ps1', "test-file.txt") {
            "$env:ChocolateyInstall\lib\uninstallfailure\$_" | Should -Exist
        }

        It "Should have created backup of file '<_>' in lib directory" -ForEach @('uninstallfailure.nupkg', 'uninstallfailure.nuspec', 'tools\chocolateyuninstall.ps1', "test-file.txt") {
            "$env:ChocolateyInstall\lib-bad\uninstallfailure\1.0.0\$_" | Should -Exist
        }

        It "Should not have kept backup files" {
            "$env:ChocolateyInstall\lib-bkp\uninstallfailure" | Should -Not -Exist
        }

        It "Outputs no package uninstalled" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 0/1 packages. 1 packages failed."
        }
    }

    Context "Uninstalling a package where non-package file is locked" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install installpackage --confirm --no-progress

            $LockedFile = [System.IO.File]::Open("$env:ChocolateyInstall\lib\installpackage\a-locked-file.txt", 'OpenOrCreate', 'Read', 'Read')

            $Output = Invoke-Choco uninstall installpackage --confirm
        }

        AfterAll {
            $LockedFile.Dispose()
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Should have kept locked file in lib directory" {
            "$env:ChocolateyInstall\lib\installpackage\a-locked-file.txt" | Should -Exist
        }

        It "Should have removed file '<_>' in lib directory" -ForEach @('installpackage.nupkg', 'installpackage.nuspec', 'tools\casemismatch.exe', 'tools\Casemismatch.exe.ignore', 'tools\chocolateyBeforeModify.ps1', 'tools\chocolateyinstall.ps1', 'tools\chocolateyuninstall.ps1', 'tools\console.exe', 'tools\graphical.exe', 'tools\graphical.exe.gui', 'tools\not.installed.exe', 'tools\not.installed.exe.ignore', 'tools\simplefile.txt') {
            "$env:ChocolateyInstall\lib\installpackage\$_" | Should -Not -Exist
        }

        It "Should not have created lib-bad directory" {
            "$env:ChocolateyInstall\lib-bad\upgradepackage" | Should -Not -Exist
        }

        It "Should have removed lib-bkp directory" {
            "$env:ChocolateyInstall\lib-bkp\upgradepackage" | Should -Not -Exist
        }

        It "Outputs Successful uninstall" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 1/1 packages."
        }
    }

    # When a file exists before initial installation, it will be considered as part of the
    # package files. This is NuGet behavior. This happens during existing files for upgrades as well.
    # We might want to rollback files in this case, but it is not possible as the backup has been removed before
    # any locked files are being tried to be removed.
    Context "Uninstalling a package where non-package file is locked before initial installation" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            mkdir "$env:ChocolateyInstall\lib\installpackage"
            $LockedFile = [System.IO.File]::Open("$env:ChocolateyInstall\lib\installpackage\a-locked-file.txt", 'OpenOrCreate', 'Read', 'Read')

            $null = Invoke-Choco install installpackage --confirm --no-progress

            $Output = Invoke-Choco uninstall installpackage --confirm
        }

        AfterAll {
            $LockedFile.Dispose()
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Should have kept locked file in lib directory" {
            "$env:ChocolateyInstall\lib\installpackage\a-locked-file.txt" | Should -Exist
        }

        It "Should have removed file '<_>' in lib directory" -ForEach @('installpackage.nupkg', 'installpackage.nuspec', 'tools\casemismatch.exe', 'tools\Casemismatch.exe.ignore', 'tools\chocolateyBeforeModify.ps1', 'tools\chocolateyinstall.ps1', 'tools\chocolateyuninstall.ps1', 'tools\console.exe', 'tools\graphical.exe', 'tools\graphical.exe.gui', 'tools\not.installed.exe', 'tools\not.installed.exe.ignore', 'tools\simplefile.txt') {
            "$env:ChocolateyInstall\lib\installpackage\$_" | Should -Not -Exist
        }

        It "Should not have created lib-bad directory" {
            "$env:ChocolateyInstall\lib-bad\upgradepackage" | Should -Not -Exist
        }

        It "Should have removed lib-bkp directory" {
            "$env:ChocolateyInstall\lib-bkp\upgradepackage" | Should -Not -Exist
        }

        It "Reports no package uninstalled" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 0/1 packages. 1 packages failed."
        }

        It "Outputs not able to remove all package files" {
            $Output.String | Should -Match "installpackage - Unable to delete all existing package files. There will be leftover files requiring manual cleanup"
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
