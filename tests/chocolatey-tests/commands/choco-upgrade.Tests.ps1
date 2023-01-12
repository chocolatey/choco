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
            $Output.ExitCode | Should -Be 0
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
            $Output.ExitCode | Should -Be 0
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
            $Output.ExitCode | Should -Be 0
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
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the packages to the lib directory" {
            $PackageUnderTest | ForEach-Object {
                "$env:ChocolateyInstall\lib\$_" | Should -Exist
            }
        }
    }

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
<package xmlns="http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd">
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

            $Output = Invoke-Choco upgrade $packageName --source $tempPath
        }

        AfterAll {
            Remove-Item $tempPath -Recurse -Force
        }

        It 'Installs successfully and exits with success (0)' {
            $Output.ExitCode | Should -Be 0
        }

        It 'Shows a warning about the unsupported nuspec metadata element "<_>"' -TestCases $testCases {
            $Output.String | Should -Match "$_ elements are not supported in Chocolatey CLI"
        }
    }
    
    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
