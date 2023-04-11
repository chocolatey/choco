Import-Module helpers/common-helpers

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

        It 'Exits with Success (0)' {
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
