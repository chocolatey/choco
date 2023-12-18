Import-Module helpers/common-helpers

Describe "choco upgrade" -Tag Chocolatey, UpgradeCommand {
    BeforeAll {
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Upgrading a side-by-side Package (non-existing)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $Output = Invoke-Choco upgrade $PackageUnderTest --confirm --allowmultipleversions
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Installed a package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0" | Should -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Outputs a warning message about side by side installs are deprecated" {
            $Output.Lines | Should -Contain "Upgrading the same package with multiple versions is deprecated and will be removed in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it upgraded the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages." -Because $Output.String
        }
    }

    Context "Switching a normal Package to a side-by-side Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco install $PackageUnderTest --confirm

            $Output = Invoke-Choco upgrade $PackageUnderTest --confirm --force --allowmultipleversions
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0" | Should -Exist
        }

        It "Removed the previous version of the package from the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Not -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Outputs a warning message about side by side installs are deprecated" {
            $Output.Lines | Should -Contain "Upgrading the same package with multiple versions is deprecated and will be removed in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it upgraded the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages." -Because $Output.String
        }
    }

    Context "Switching a side-by-side Package to a normal Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco install $PackageUnderTest --confirm --allowmultipleversion

            $Output = Invoke-Choco upgrade $PackageUnderTest --confirm --force
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Does not output a warning message about side by side installs are deprecated" {
            $Output.Lines | Should -Not -Contain "Upgrading the same package with multiple versions is deprecated and will be removed in v2.0.0." -Because $Output.String
        }

        It "Does not output a warning message that installed side by side package is deprecated" {
            $Output.Lines | Should -Not -Contain "installpackage has been installed as a side by side installation." -Because $Output.String
            $Output.Lines | Should -Not -Contain "Side by side installations are deprecated and is pending removal in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it upgraded the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages." -Because $Output.String
        }
    }

    Context "Upgrade package with (<Command>) specified" -ForEach @(
        @{ Command = '--pin' ; Contains = $true }
        @{ Command = '' ; Contains = $false }
    ) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Package = 'upgradepackage'
            $null = Invoke-Choco install $Package --version 1.0.0 --confirm
            $null = Invoke-Choco upgrade $Package $Command --confirm
            $Output = Invoke-Choco pin list
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Output should include pinned package" {
            if ($Contains) {
                $Output.String | Should -Match "$Package|1.1.0"
            }
            else {
                $Output.String | Should -Not -Match "$Package|1.1.0"
            }
        }
    }

    Context "Upgrading packages while remembering arguments with only one package using arguments" -Tag Internal {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Enable-ChocolateyFeature useRememberedArgumentsForUpgrades
            $null = Invoke-Choco install curl --package-parameters="'/CurlOnlyParam'" --version="7.77.0" --ia="'/CurlIAParam'" --x86 -y
            $null = Invoke-Choco install wget --version=1.21.1 -y

            $Output = Invoke-Choco upgrade all -y --debug
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Outputs running curl script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*curl\\tools" } | Select-Object -Last 1

            $line | Should -Not -BeNullOrEmpty
            $line | Should -MatchExactly "\/CurlIAParam"
            $line | Should -MatchExactly "\/CurlOnlyParam"
            $line | Should -Match "-forceX86"
        }

        It 'Outputs running wget script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*wget\\tools" }

            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "installArguments:? ''"
            $line | Should -Match "packageParameters:? ''"
            $line | Should -Not -Match "-forceX86"
        }
    }

    # We exclude this test when running CCM, as it will install and remove
    # the firefox package which is used through other tests that will be affected.
    Context "Upgrading packages while remembering arguments with multiple packages using arguments" -Tag CCMExcluded, Internal, VMOnly {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Enable-ChocolateyFeature useRememberedArgumentsForUpgrades
            $null = Invoke-Choco install curl --package-parameters="'/CurlOnlyParam'" --version="7.77.0" --ia="'/CurlIAParam'" --forcex86 -y
            $null = Invoke-Choco install wget --version=1.21.1 -y --forcex86
            $null = Invoke-Choco install firefox --version=99.0.1 --package-parameters="'/l=eu'" -y --ia="'/RemoveDistributionDir=true'"

            $Output = Invoke-Choco upgrade all -y --debug
        }

        AfterAll {
            $null = Invoke-Choco uninstall firefox -y
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Outputs running curl script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*curl\\tools" } | Select-Object -Last 1

            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "installArguments:? '/CurlIAParam'"
            $line | Should -Match "packageParameters:? '/CurlOnlyParam'"
            $line | Should -Match "-forceX86"
        }

        It 'Outputs running wget script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*wget\\tools" } | Select-Object -Last 1

            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "installArguments:? ''"
            $line | Should -Match "packageParameters:? ''"
            $line | Should -Match "-forceX86"
        }

        It 'Outputs firefox using eu as language locale' {
            $Output.Lines | Should -Contain "Using locale 'eu'..." -Because $Output.String
        }

        It 'Outputs running firefox script with correct arguments' {
            $line = $Output.Lines | Where-Object { $_ -match "packageScript.*firefox\\tools" }

            $line | Should -Not -BeNullOrEmpty
            $line | Should -Match "installArguments:? '\/RemoveDistributionDir=true'"
            $line | Should -Match "packageParameters:? '\/l=eu'"
            $line | Should -Not -Match "-forceX86"
        }
    }


    Context "Upgrading multiple packages at once" {

        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage", "packagewithscript"

            $Output = Invoke-Choco upgrade @PackageUnderTest --confirm
        }

        It "Installs successfully and exits with success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Installed the packages to the lib directory" {
            $PackageUnderTest | ForEach-Object {
                "$env:ChocolateyInstall\lib\$_" | Should -Exist
            }
        }
    }

    # We are marking this test as internal, as the package we need to make use
    # of downloads a zip archive from a internal server, and the package is also
    # only located on an internal feed.
    Context "Upgrading non-existing package while specifying a cache location (Arg: <_>)" -ForEach '-c', '--cache', '--cachelocation', '--cache-location' -Tag Internal, LongPaths, CacheLocation {
        BeforeAll {
            $paths = Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco upgrade install-chocolateyzip --version 3.21.2 --confirm "$_" "$($paths.CachePath)" --no-progress
        }

        AfterAll {
            $null = Invoke-Choco uninstall install-chocolateyzip --confirm
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Runs under background Service' -Tag Background {
            $Output.Lines | Should -Contain 'Running in background mode' -Because $Output.String
        }

        It 'Outputs downloading 64bit package' {
            $Output.Lines | Should -Contain 'Downloading install-chocolateyzip 64 bit' -Because $Output.String
        }

        It 'Outputs download completed' {
            $testMessage = if ($features.License) {
                "Download of 'cmake-3.21.2-windows-x86_64.zip' (36.01 MB) completed."
            } else {
                "Download of cmake-3.21.2-windows-x86_64.zip (36.01 MB) completed."
            }
            $Output.Lines | Should -Contain $testMessage -Because $Output.String
        }

        It 'Outputs extracting correct archive' {
            $testMessage = if ($features.License) {
                "Extracting cmake-3.21.2-windows-x86_64.zip to $env:ChocolateyInstall\lib\install-chocolateyzip\tools..."
            } else {
                "Extracting $($paths.CachePath)\install-chocolateyzip\3.21.2\cmake-3.21.2-windows-x86_64.zip to $env:ChocolateyInstall\lib\install-chocolateyzip\tools..."
            }
            $Output.Lines | Should -Contain $testMessage -Because $Output.String
        }

        It 'Created shim for <_>' -ForEach 'cmake-gui.exe', 'cmake.exe', 'cmcldeps.exe', 'cpack.exe', 'ctest.exe' {
            $Output.Lines | Should -Contain "ShimGen has successfully created a shim for $_"
            "$env:ChocolateyInstall\bin\$_" | Should -Exist
        }

        It 'Outputs upgrading was successful' {
            $Output.Lines | Should -Contain 'The upgrade of install-chocolateyzip was successful.' -Because $Output.String
        }

        It 'Outputs software installation directory' {
            $Output.Lines | Should -Contain "Software installed to '$env:ChocolateyInstall\lib\install-chocolateyzip\tools'" -Because $Output.String
        }

        It 'Should have cached installed directory in custom cache' {
            # Need to be verified, but the file may not exist on licensed edition
            "$($paths.CachePath)\install-chocolateyzip\3.21.2\cmake-3.21.2-windows-x86_64.zip" | Should -Exist
        }

        It 'Installed software to expected directory' {
            "$env:ChocolateyInstall\lib\install-chocolateyzip\tools\cmake-3.21.2-windows-x86_64\bin\cmake.exe" | Should -Exist
        }
    }

    # We are marking this test as internal, as the package we need to make use
    # of downloads a zip archive from a internal server, and the package is also
    # only located on an internal feed.
    Context "Upgrading non-existing package while specifying a cache location (Arg: <_>)" -ForEach '-c', '--cache', '--cachelocation', '--cache-location' -Tag Internal, LongPaths, CacheLocation {
        BeforeAll {
            $paths = Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco upgrade install-chocolateyzip --version 3.21.2 --confirm "$_" "$($paths.CachePath)" --no-progress
        }

        AfterAll {
            $null = Invoke-Choco uninstall install-chocolateyzip --confirm
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Runs under background Service' -Tag Background {
            $Output.Lines | Should -Contain 'Running in background mode' -Because $Output.String
        }

        It 'Outputs downloading 64bit package' {
            $Output.Lines | Should -Contain 'Downloading install-chocolateyzip 64 bit' -Because $Output.String
        }

        It 'Outputs download completed' {
            $testMessage = if ($features.License) {
                "Download of 'cmake-3.21.2-windows-x86_64.zip' (36.01 MB) completed."
            } else {
                "Download of cmake-3.21.2-windows-x86_64.zip (36.01 MB) completed."
            }
            $Output.Lines | Should -Contain $testMessage -Because $Output.String
        }

        It 'Outputs extracting correct archive' {
            $testMessage = if ($features.License) {
                "Extracting cmake-3.21.2-windows-x86_64.zip to $env:ChocolateyInstall\lib\install-chocolateyzip\tools..."
            } else {
                "Extracting $($paths.CachePath)\install-chocolateyzip\3.21.2\cmake-3.21.2-windows-x86_64.zip to $env:ChocolateyInstall\lib\install-chocolateyzip\tools..."
            }
            $Output.Lines | Should -Contain $testMessage -Because $Output.String
        }

        It 'Created shim for <_>' -ForEach 'cmake-gui.exe', 'cmake.exe', 'cmcldeps.exe', 'cpack.exe', 'ctest.exe' {
            $Output.Lines | Should -Contain "ShimGen has successfully created a shim for $_"
            "$env:ChocolateyInstall\bin\$_" | Should -Exist
        }

        It 'Outputs upgrading was successful' {
            $Output.Lines | Should -Contain 'The upgrade of install-chocolateyzip was successful.' -Because $Output.String
        }

        It 'Outputs software installation directory' {
            $Output.Lines | Should -Contain "Software installed to '$env:ChocolateyInstall\lib\install-chocolateyzip\tools'" -Because $Output.String
        }

        It 'Should have cached installed directory in custom cache' {
            # Need to be verified, but the file may not exist on licensed edition
            "$($paths.CachePath)\install-chocolateyzip\3.21.2\cmake-3.21.2-windows-x86_64.zip" | Should -Exist
        }

        It 'Installed software to expected directory' {
            "$env:ChocolateyInstall\lib\install-chocolateyzip\tools\cmake-3.21.2-windows-x86_64\bin\cmake.exe" | Should -Exist
        }
    }

    Context "Upgrading a package when invalid package source is being used" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install upgradepackage --version 1.0.0 --confirm

            $null = Invoke-Choco source add -n "invalid" -s "https://invalid.chocolatey.org/api/v2/"

            $Output = Invoke-Choco upgrade upgradepackage --confirm
        }

        It 'Exits with Success (-1)' {
            $Output.ExitCode | Should -Be -1
        }

        It 'Outputs warning about unable to load service index' {
            $Output.String | Should -Match "Error retrieving packages from source 'https://invalid.chocolatey.org/api/v2/':"
        }

        It 'Outputs successful installation of single package' {
            $Output.Lines | Should -Contain 'Chocolatey upgraded 1/1 packages.'
        }
    }

    Context "Upgrading package should not downgrade existing package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $DependentPackageName = 'isdependency'

            $null = Invoke-Choco install $DependentPackageName --version 1.1.0 --confirm
            $null = Invoke-Choco install hasdependency --version 1.0.0 --confirm

            $Output = Invoke-Choco upgrade hasdependency
            $Packages = (Invoke-Choco list -r).Lines | ConvertFrom-ChocolateyOutput -Command List
            $DependentPackage = $Packages | Where-Object Name -EQ $DependentPackageName
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'should not have downgraded isdependency' {
            Test-VersionEqualOrHigher -InstalledVersion $DependentPackage.Version -CompareVersion 1.1.0 | Should -BeTrue
        }
    }

    Context "Upgrading a failing package creates creates bad backup and rolls back lib directory" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "failingdependency"
            $PackageVersion = '1.0.0'

            $null = Invoke-Choco install $PackageUnderTest --version 0.9.9 -n

            $Output = Invoke-Choco upgrade $PackageUnderTest --version $PackageVersion --confirm
        }

        It "Exits with Failure (15608)" {
            $Output.ExitCode | Should -Be 15608 -Because $Output.String
        }

        It "Doesn't keep a package backup in lib-bkp" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        It "Creates backup of file '<_>' in lib-bad" -ForEach @('failingdependency.nupkg', 'failingdependency.nuspec', '.chocolateyPending', 'tools\chocolateyinstall.ps1') {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$_" | Should -Exist
        }

        It "Outputs a message showing that installation failed." {
            $Output.String | Should -Match "Chocolatey upgraded 0/1 packages\."
        }
    }

    Context "Upgrading a package when installer is locked" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasinnoinstaller"
            $PackageVersion   = '6.2.0.3'

            # We are purposely using the --reduce-nupkg-only option (both here and in the next call to Invoke-Choco), to make the
            # test as close to default operation, when running both in the context of OSS and CLE. It was found during testing, that
            # the Package Optimizer would remove the application installer, which is the file that is being locked during the test,
            # which means then that the test doesn't actually test what we want it to. We are locking the exe here instead of say a
            # PowerShell script, is to simulate the exe being used when the test is running.
            $null = Invoke-Choco install $PackageUnderTest --version 6.2.0.0 --confirm --no-progress --reduce-nupkg-only

            $LockedFile = [System.IO.File]::Open("$env:ChocolateyInstall\lib\$PackageUnderTest\tools\helloworld-1.0.0.exe", 'Open', 'Read',
            'Read')

            $Output = Invoke-Choco upgrade $PackageUnderTest --version $PackageVersion --confirm --no-progress --reduce-nupkg-only
        }

        AfterAll {
            $LockedFile.Dispose()
            $null = Invoke-Choco uninstall $PackageUnderTest --confirm
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Keeps file '<_>' in the lib directory" -ForEach @('hasinnoinstaller.nuspec', 'hasinnoinstaller.nupkg', 'tools\chocolateyinstall.ps1', 'tools\chocolateyuninstall.ps1', 'tools\helloworld-1.0.0.exe') {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$_" | Should -Exist
        }

        It "Doesn't keep a package backup in lib-bkp" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        # Only two files are backed up as we was not able to download and extract the new package
        It "Creates backup of file '<_>' in lib-bad" -ForEach @('.chocolateyPending') {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$_" | Should -Exist
        }

        It "Outputs a message showing that installation failed." {
            $Output.Lines | Should -Contain "Chocolatey upgraded 0/1 packages. 1 packages failed."
        }
    }

    Context "Upgrading a package when non-package file is locked before initial installation" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasinnoinstaller"
            $PackageVersion   = '6.2.0.3'

            mkdir "$env:ChocolateyInstall\lib\$PackageUnderTest\tools"
            $LockedFile = [System.IO.File]::Open("$env:ChocolateyInstall\lib\$PackageUnderTest\tools\a-locked-file.txt", 'OpenOrCreate', 'Read',
            'Read')

            # We are purposely using the --reduce-nupkg-only option here to make the test as close to default operation, when running
            # both in the context of OSS and CLE. It was found during testing, that the Package Optimizer can remove files that are
            # normally left in place, and this test is specifically for testing the back-up/restore process.
            $null = Invoke-Choco install $PackageUnderTest --version 6.2.0.0 --confirm --no-progress --reduce-nupkg-only

            $Output = Invoke-Choco upgrade $PackageUnderTest --version $PackageVersion --confirm --no-progress
        }

        AfterAll {
            $LockedFile.Dispose()
            $null = Invoke-Choco uninstall $PackageUnderTest --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Keeps file '<_>' in the lib directory" -ForEach @('hasinnoinstaller.nuspec', 'hasinnoinstaller.nupkg', 'tools\chocolateyinstall.ps1', 'tools\chocolateyuninstall.ps1', 'tools\helloworld-1.0.0.exe', 'tools\helloworld-1.0.0.exe.ignore') {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$_" | Should -Exist
        }

        It "Doesn't keep a package backup in lib-bkp" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        It "Did not create backup of file '<_>' in lib-bad" -ForEach @('hasinnoinstaller.nuspec', 'tools\chocolateyinstall.ps1', 'tools\chocolateyuninstall.ps1', 'tools\helloworld-1.0.0.exe', 'helloworld-1.0.0.exe.ignore') {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$PackageVersion\$_" | Should -Not -Exist
        }

        It "Did not create backup of file 'hasinnoinstaller.nupkg' in lib-bad" -Tag FossOnly {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$PackageVersion\hasinnoinstaller.nupkg" | Should -Not -Exist
        }
    }

    Context "Upgrading a package when non-package file is locked after initial installation" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasinnoinstaller"
            $PackageVersion   = '6.2.0.3'

            $null = Invoke-Choco install $PackageUnderTest --version 6.2.0.0 --confirm --no-progress

            $LockedFile = [System.IO.File]::Open("$env:ChocolateyInstall\lib\$PackageUnderTest\a-locked-file.txt", 'OpenOrCreate', 'Read',
            'Read')

            $Output = Invoke-Choco upgrade $PackageUnderTest --version $PackageVersion --confirm --no-progress
        }

        AfterAll {
            $LockedFile.Dispose()
            $null = Invoke-Choco uninstall $PackageUnderTest --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Keeps have file '<_>' in the lib directory" -ForEach @('hasinnoinstaller.nuspec', 'hasinnoinstaller.nupkg', 'tools\chocolateyinstall.ps1', 'tools\chocolateyuninstall.ps1', 'tools\helloworld-1.0.0.exe', 'tools\helloworld-1.0.0.exe.ignore') {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$_" | Should -Exist
        }

        It "Doesn't keep a package backup in lib-bkp" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        It "Doesn't keep a package backup in lib-bad" {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest" | Should -Not -Exist
        }

        It "Outputs a message showing that package was upgraded." {
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages." -Because $Output.String
        }
    }

    Context "Upgrading a package where beforeModify fails still succeeds the installation" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = 'upgradepackage'

            $null = Invoke-Choco install upgradepackage --version 1.0.0 --confirm

            $Output = Invoke-Choco upgrade upgradepackage --confirm
        }

        # This was broken in v1.0.0
        It "Exits with Success (0)" -Tag Broken {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Outputs a message showing that installation was successful" {
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages."
        }
    }

    # This needs to be (almost) the last test in this block, to ensure NuGet configurations aren't being created.
    # Any tests after this block are expected to generate the configuration as they're explicitly using the NuGet CLI
    Test-NuGetPaths
}
