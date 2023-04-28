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

    Context "Uninstalling a package when chocolateyBeforeModify fails" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install upgradepackage --version 1.0.0 --confirm

            $Output = Invoke-Choco uninstall upgradepackage --confirm
        }

        # Broken since v1.0.0
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

    Context "When specifying multiple packages where one is a dependency should not fail uninstallation" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install hasdependency --confirm

            $Output = Invoke-Choco uninstall isdependency hasdependency --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        # When a file exists before initial installation, it will be considered as part of the
        # package files. This is NuGet behavior. This happens during existing files for upgrades as well.
        # We might want to rollback files in this case, but it is not possible as the backup has been removed before
        # any locked files are being tried to be removed.
        It "Should have removed <_>" -ForEach @('isdependency', 'hasdependency') -Tag FossOnly {
            "$env:ChocolateyInstall\lib\$_" | Should -Not -Exist -Because $Output.String
        }

        It "Should not have removed isexactversiondependency" {
            "$env:ChocolateyInstall\lib\isexactversiondependency" | Should -Exist -Because $Output.String
        }

        It "Outputs <_> was succcesfully uninstalled" -ForEach @('isdependency', 'hasdependency') -Tag FossOnly {
            $Output.Lines | Should -Contain "$_ has been successfully uninstalled." -Because $Output.String
        }

        It "Does not output isexactversiondependency being uninstalled" {
            $Output.Lines | Should -Not -Contain "isexactversiondependency has been successfully uninstalled." -Because $Output.String
        }

        It "Outputs 2/2 packages uninstalled" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 2/2 packages." -Because $Output.String
        }
    }

    Context "When specifying multiple packages where one is a dependency should not fail uninstallation (forced dependencies)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install hasdependency --confirm

            $Output = Invoke-Choco uninstall isdependency hasdependency --confirm --force-dependencies
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        # This does not work as expected when runnning in test kitchen and with Chocolatey Licensed Extension installed.
        # It is believed to be a problem with test kitchen, or the VM that we are using that is causing the issue.
        # The result is that versioned backup files of each file in the package is created, instead of the package being
        # removed. Consider the test partially broken.
        It "Should have removed <_>" -ForEach @('isdependency', 'hasdependency', 'isexactversiondependency') -Tag FossOnly {
            "$env:ChocolateyInstall\lib\$_" | Should -Not -Exist -Because $Output.String
        }

        It "Outputs <_> was succcesfully uninstalled" -ForEach @('isdependency', 'hasdependency', 'isexactversiondependency') {
            $Output.Lines | Should -Contain "$_ has been successfully uninstalled." -Because $Output.String
        }

        It "Outputs 3/3 packages uninstalled" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 3/3 packages." -Because $Output.String
        }
    }

    Context "When specifying non-existing package before and after failing package does not abort execution" {
        BeforeAll {
            $null = Invoke-Choco install uninstallfailure installpackage --confirm

            $Output = Invoke-Choco uninstall packageA uninstallfailure packageB installpackage --confirm
        }

        It "Exits with Failure (-1)" {
            $Output.ExitCode | Should -Be -1 -Because $Output.String
        }

        It "Outputs package not existing (<_>)" -ForEach @('packageA', 'packageB') {
            $Output.Lines | Should -Contain "$_ is not installed. Cannot uninstall a non-existent package." -Because $Output.String
            $Output.Lines | Should -Contain "- $_ - $_ is not installed. Cannot uninstall a non-existent package." -Because $Output.String
        }

        It "Outputs failing to uninstall package uninstallfailure" {
            $Output.Lines | Should -Contain "uninstallfailure not uninstalled. An error occurred during uninstall:" -Because $Output.String
            $Output.Lines | Should -Contain "uninstallfailure uninstall not successful." -Because $Output.String
            $Output.String | Should -Match "- uninstallfailure \(exited -1\) - Error while running"
        }

        # This does not work as expected when runnning in test kitchen and with Chocolatey Licensed Extension installed.
        # It is believed to be a problem with test kitchen, or the VM that we are using that is causing the issue.
        # The result is that versioned backup files of each file in the package is created, instead of the package being
        # removed. Consider the test partially broken.
        It "Should have uninstall package installpackage" -Tag FossOnly {
            "$env:ChocolateyInstall\lib\installpackage" | Should -Not -Exist -Because $Output.String
        }

        # This does not work as expected when runnning in test kitchen and with Chocolatey Licensed Extension installed.
        # It is believed to be a problem with test kitchen, or the VM that we are using that is causing the issue.
        # The result is that versioned backup files of each file in the package is created, instead of the package being
        # removed. Consider the test partially broken.
        It "Outputs successful uninstall of installpackage" -Tag FossOnly {
            $Output.Lines | Should -Contain "installpackage has been successfully uninstalled." -Because $Output.String
        }

        It "Outputs 1/3 successful uninstalls" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 1/4 packages. 3 packages failed." -Because $Output.String
        }
    }

    Context "When specifying multiple packages where one is a nested dependency should not fail uninstallation" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install toplevelwithnesteddependencies --confirm

            $Output = Invoke-Choco uninstall isdependency toplevelwithnesteddependencies --confirm
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        # This does not work as expected when runnning in test kitchen and with Chocolatey Licensed Extension installed.
        # It is believed to be a problem with test kitchen, or the VM that we are using that is causing the issue.
        # The result is that versioned backup files of each file in the package is created, instead of the package being
        # removed. Consider the test partially broken.
        It "Should have removed package toplevelwithnesteddependencies" -Tag FossOnly {
            "$env:ChocolateyInstall\lib\toplevelwithnesteddependencies" | Should -Not -Exist -Because $Output.String
        }

        It "Should not have removed <_>" -ForEach @('hasdependency', 'hasnesteddependency', 'isdependency', 'isexactversiondependency', 'toplevelhasexactversiondependency') {
            "$env:ChocolateyInstall\lib\$_" | Should -Exist -Because $Output.String
        }

        It "Outputs toplevelwithnesteddependencies was succcesfully uninstalled" {
            $Output.Lines | Should -Contain "toplevelwithnesteddependencies has been successfully uninstalled." -Because $Output.String
        }

        It "Does not output <_> being uninstalled" -ForEach @('hasdependency', 'hasnesteddependency', 'isdependency', 'isexactversiondependency', 'toplevelhasexactversiondependency') {
            $Output.Lines | Should -Not -Contain "$_ has been successfully uninstalled." -Because $Output.String
        }

        It "Outputs warning about package being unable to be uninstalled due to being a dependency" {
            $Output.Lines | Should -Contain "[NuGet]: Unable to uninstall 'isdependency.2.1.0' because 'hasdependency.2.0.1' depends on it."
        }

        It "Outputs 1/2 packages uninstalled with 1 failed package" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 1/2 packages. 1 packages failed." -Because $Output.String
        }

        It "Outputs failure to uninstall one of the packages" {
            $Output.Lines | Should -Contain "- isdependency - Unable to uninstall 'isdependency.2.1.0' because 'hasdependency.2.0.1' depends on it."
        }
    }

    Context "When specifying multiple packages where one is a nested dependency should not fail uninstallation (forced dependencies)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install toplevelwithnesteddependencies --confirm

            $Output = Invoke-Choco uninstall isdependency toplevelwithnesteddependencies --confirm --force-dependencies
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        # This does not work as expected when runnning in test kitchen and with Chocolatey Licensed Extension installed.
        # It is believed to be a problem with test kitchen, or the VM that we are using that is causing the issue.
        # The result is that versioned backup files of each file in the package is created, instead of the package being
        # removed. Consider the test partially broken.
        It "Should have removed <_>" -ForEach @('hasdependency', 'hasnesteddependency', 'isdependency', 'isexactversiondependency', 'toplevelhasexactversiondependency', 'toplevelwithnesteddependencies') -Tag FossOnly {
            "$env:ChocolateyInstall\lib\$_" | Should -Not -Exist -Because $Output.String
        }

        It "Outputs <_> was succcesfully uninstalled" -ForEach @('hasdependency', 'hasnesteddependency', 'isdependency', 'isexactversiondependency', 'toplevelhasexactversiondependency', 'toplevelwithnesteddependencies') {
            $Output.Lines | Should -Contain "$_ has been successfully uninstalled." -Because $Output.String
        }

        It "Outputs warning about package being unable to be uninstalled due to being a dependency" {
            $Output.Lines | Should -Contain "[NuGet]: Unable to uninstall 'isdependency.2.1.0' because 'hasdependency.2.0.1' depends on it."
        }

        It "Outputs 7/7 packages uninstalled" {
            $Output.Lines | Should -Contain "Chocolatey uninstalled 7/7 packages." -Because $Output.String
        }

        It "Does not output failure to uninstall one of the packages" {
            $Output.Lines | Should -Not -Contain "- isdependency - Unable to uninstall 'isdependency.2.1.0' because 'hasdependency.2.0.1' depends on it."
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
