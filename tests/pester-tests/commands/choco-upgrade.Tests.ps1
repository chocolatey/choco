Describe "choco upgrade" -Tag Chocolatey, UpgradeCommand {
    BeforeAll {
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
        $features = Get-ChocolateyFeatures
    }

    AfterAll {
        Remove-ChocolateyTestInstall
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
            Enable-ChocolateySource -Name hermes-setup
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
            Enable-ChocolateySource -Name hermes-setup
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

    Context "Upgrading a Package from a nupkg file" {
        BeforeAll {
            $snapshotPath = New-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            New-ChocolateyTestPackage `
                -TestPath "$PSScriptRoot\testpackages" `
                -Name $PackageUnderTest `
                -Version "1.0.0"

            $PackagePath = "$($snapshotPath.PackagesPath)\$PackageUnderTest.1.0.0.nupkg"

            $Output = Invoke-Choco upgrade $PackagePath --confirm --params '/ParameterOne:FirstOne /ParameterTwo:AnotherOne'
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Not Installed a package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }

        It "Not Installed a package to the lib bad directory" {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest" | Should -Not -Exist
        }

        It "Not Installed a package to the lib backup directory" {
            "$env:ChocolateyInstall\lib-backup\$PackageUnderTest" | Should -Not -Exist
        }

        It "Outputs expected error message" {
            $Output.String | Should -Match @"
Package name cannot be a path to a file on a remote, or local file system.

To upgrade a local, or remote file, you may use:
  choco upgrade $packageUnderTest --version="1.0.0" --source="$([regex]::Escape($snapshotPath.PackagesPath))
"@
        }
    }

    # We are marking this test as internal, as the package we need to make use
    # of downloads a zip archive from a internal server, and the package is also
    # only located on an internal feed.
    Context "Upgrading non-existing package while specifying a cache location (Arg: <_>)" -ForEach '-c', '--cache', '--cachelocation', '--cache-location' -Tag Internal, LongPaths, CacheLocation {
        BeforeAll {
            $paths = Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco upgrade install-chocolateyzip --version 3.21.2 --confirm "$_" "$($paths.CachePathLong)" --no-progress
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
                "Extracting $($paths.CachePathLong)\install-chocolateyzip\3.21.2\cmake-3.21.2-windows-x86_64.zip to $env:ChocolateyInstall\lib\install-chocolateyzip\tools..."
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
            "$($paths.CachePathLong)\install-chocolateyzip\3.21.2\cmake-3.21.2-windows-x86_64.zip" | Should -Exist
        }

        It 'Installed software to expected directory' {
            "$env:ChocolateyInstall\lib\install-chocolateyzip\tools\cmake-3.21.2-windows-x86_64\bin\cmake.exe" | Should -Exist
        }
    }

    Context "Upgrading a package when invalid package source is being used" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install upgradepackage --version 1.0.0 --confirm
            $InvalidSource = "https://invalid.chocolatey.org/api/v2/"
            $null = Invoke-Choco source add -n "invalid" -s $InvalidSource

            $Output = Invoke-Choco upgrade upgradepackage --confirm
        }


        # `upgradepackage` contains a beforeModify that throws, which triggers an incorrect -1 exit code.
        # See https://app.clickup.com/t/20540031/PROJ-615
        It 'Exits with Success (0)' -Tag Broken {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Outputs warning about unable to load service index' {
            $Output.Lines | Should -Contain "Unable to load the service index for source $InvalidSource."
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
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$PackageVersion\$_" | Should -Exist
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

        It "Keeps file '<_>' in the lib directory" -ForEach @('hasinnoinstaller.nuspec', 'hasinnoinstaller.nupkg', 'tools\chocolateyinstall.ps1', 'tools\chocolateyuninstall.ps1', 'tools\helloworld-1.0.0.exe', 'tools\helloworld-1.0.0.exe.ignore') {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$_" | Should -Exist
        }

        It "Doesn't keep a package backup in lib-bkp" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        # Only two files are backed up as we was not able to download and extract the new package
        It "Creates backup of file '<_>' in lib-bad" -ForEach @('.chocolateyPending', 'tools\helloworld-1.0.0.exe') {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$PackageVersion\$_" | Should -Exist
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

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Keeps file '<_>' in the lib directory" -ForEach @('hasinnoinstaller.nuspec', 'hasinnoinstaller.nupkg', 'tools\chocolateyinstall.ps1', 'tools\chocolateyuninstall.ps1', 'tools\helloworld-1.0.0.exe', 'tools\helloworld-1.0.0.exe.ignore') {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$_" | Should -Exist
        }

        It "Doesn't keep a package backup in lib-bkp" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        It "Creates backup of file '<_>' in lib-bad" -ForEach @('.chocolateyPending', 'tools\a-locked-file.txt') {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$PackageVersion\$_" | Should -Exist
        }

        It "Did not create backup of file '<_>' in lib-bad" -ForEach @('hasinnoinstaller.nuspec', 'tools\chocolateyinstall.ps1', 'tools\chocolateyuninstall.ps1', 'tools\helloworld-1.0.0.exe', 'helloworld-1.0.0.exe.ignore') {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$PackageVersion\$_" | Should -Not -Exist
        }

        It "Did not create backup of file 'hasinnoinstaller.nupkg' in lib-bad" -Tag FossOnly {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$PackageVersion\hasinnoinstaller.nupkg" | Should -Not -Exist
        }

        It "Outputs a message showing that installation failed." {
            $Output.Lines | Should -Contain "Chocolatey upgraded 0/1 packages. 1 packages failed." -Because $Output.String
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

        # This was broken in v1.3.1
        It "Exits with Success (0)" -Tag Broken {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs a message showing that installation was successful" {
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages."
        }

        It "Outputs additiontal warning about before modify script" {
            $Output.Lines | Should -Contain "- upgradepackage - v1.0.0 - Error while running the 'chocolateyBeforeModify.ps1'." -Because $Output.String
        }
    }

    Context "Upgrading a package when user specifies non-conforming case and is latest available version (no-op)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install isdependency --confirm

            $Output = Invoke-Choco upgrade IsDePeNDency --noop -r
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs line with package name version and old version" {
            $Output.String | Should -MatchExactly "isdependency\|2\.1\.0\|2\.1\.0\|false"
        }
    }

    Context "Upgrading a package with a non-normalized version number" -Tag VersionNormalization {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $PackageUnderTest = 'nonnormalizedversions'
            $VersionUnderTest = '004.0.01.0'
            $ExpectedPackageVersion = '4.0.1'
            $null = Invoke-Choco install $PackageUnderTest --version 1.0.0
            $Output = Invoke-Choco upgrade $PackageUnderTest
        }

        It "Should exit with success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Should report successful upgrade" {
            $Output.Lines | Should -Contain "$PackageUnderTest v$ExpectedPackageVersion" -Because $Output.String
            $Output.Lines | Should -Contain 'Chocolatey upgraded 1/1 packages.' -Because $Output.String
        }

        It "Should have upgraded the correct files" {
            $ExpectedFiles = "${env:ChocolateyInstall}/lib/$PackageUnderTest/$PackageUnderTest"
            "$ExpectedFiles.Nupkg" | Should -Exist -Because $Output.String
            $NuspecContents = [xml](Get-Content "$ExpectedFiles.nuspec")
            $NuspecContents.package.metadata.version | Should -Be $VersionUnderTest
        }
    }

    Context 'Upgrading a package where parent only contain pre-releases' -Tag Testing {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install isdependency --version 1.0.0
            $null = Invoke-Choco install hasstabledependency

            $Output = Invoke-Choco upgrade isdependency --version 2.0.0
        }

        It "Should exit with sucess (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Should report successful upgrade" {
            $Output.Lines | Should -Contain "isdependency v2.0.0" -Because $Output.String
            $Output.Lines | Should -Contain "Chocolatey upgraded 1/1 packages." -Because $Output.String
        }

        It "Should have upgraded the correct files" {
            $ExpectedFile = "${env:ChocolateyInstall}/lib/isdependency/isdependency"
            "$ExpectedFile.nupkg" | Should -Exist
            $NuspecContent = [xml](Get-Content "$ExpectedFile.nuspec")
            $NuspecContent.package.metadata.version | Should -Be "2.0.0"
        }
    }

    # This needs to be (almost) the last test in this block, to ensure NuGet configurations aren't being created.
    # Any tests after this block are expected to generate the configuration as they're explicitly using the NuGet CLI
    Test-NuGetPaths

    Context 'Upgrading a package with unsupported nuspec elements shows a warning' {

        BeforeDiscovery {
            $testCases = @(
                '<license>'
                '<packageTypes>'
                '<readme>'
                '<repository>'
                '<serviceable>'
            )
        }

        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $nuspec = @'
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2011/08/nuspec.xsd">
  <metadata>
    <id>unsupportedmetadata</id>
    <version>1.0.0</version>
    <title>unsupportedmetadata (Install)</title>
    <authors>Chocolatey Software</authors>
    <tags>unsupportedmetadata</tags>

    <license type="expression">MIT</license>
    <packageTypes>
        <packageType name="Unsupported" />
    </packageTypes>
    <readme>readme.md</readme>
    <repository type="git" url="https://github.com/chocolatey/choco.git" />
    <serviceable>true</serviceable>

    <summary>Test of unsupported metadata</summary>
    <description>Some metadata fields are not supported by chocolatey. `choco pack` should fail to pack them, while `choco install` or `upgrade` should allow them with a warning.</description>
  </metadata>
</package>
'@
            $tempPath = "$env:TEMP/$(New-Guid)"
            $packageName = 'unsupportedmetadata'
            $nuspecPath = "$tempPath/$packageName/$packageName.nuspec"

            $null = New-Item -Path "$tempPath/$packageName" -ItemType Directory
            $nuspec | Set-Content -Path $nuspecPath
            "readme content" | Set-Content -Path "$tempPath/$packageName/readme.md"

            $null = Invoke-Choco install nuget.commandline
            $null = & "$env:ChocolateyInstall/bin/nuget.exe" pack $nuspecPath

            $Output = Invoke-Choco upgrade $packageName --source .
        }

        AfterAll {
            Remove-Item $tempPath -Recurse -Force
        }

        It 'Installs successfully and exits with success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Shows a warning about the unsupported nuspec metadata element "<_>"' -TestCases $testCases {
            $Output.String | Should -Match "$_ elements are not supported in Chocolatey CLI"
        }
    }

    # Do not add tests here unless they use the NuGet CLI. All Chocolatey tests should be above the Test-NuGetPaths call.
}
