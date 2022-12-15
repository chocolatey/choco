Import-Module helpers/common-helpers

# https://github.com/chocolatey/choco/blob/master/src/chocolatey.tests.integration/scenarios/InstallScenarios.cs

Describe "choco install" -Tag Chocolatey, InstallCommand {
    BeforeDiscovery {
        $isLicensed30OrMissingVersion = Test-PackageIsEqualOrHigher 'chocolatey.extension' '3.0.0-beta' -AllowMissingPackage
        $licensedProxyFixed = Test-PackageIsEqualOrHigher 'chocolatey.extension' 2.2.0-beta -AllowMissingPackage
        # The destination alias was implemented in Chocolatey v0.10.16/0.11.0,
        # but was not implemenented in Chocolatey Licensed at the time.
        # Implementation in Chocolatey Licensed is scheduled for v3.0.0.
        $destinationAliasAvailable = $isLicensed30OrMissingVersion -and (Test-ChocolateyVersionEqualOrHigherThan '0.10.16-beta')
        $hasBeforeInstallBlock = $isLicensed30OrMissingVersion -and (Test-ChocolateyVersionEqualOrHigherThan '0.11.0')
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Using NOOP when installing a Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $Output = Invoke-Choco install $PackageUnderTest --noop
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Doesn't install the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }

        It "Displays that it would have used Nuget to install a package" {
            $Output.Lines | Should -Contain "Chocolatey would have used NuGet to install packages (if they are not already installed):"
        }

        It "Displays that it would have run a PowerShell script" {
            $Output.Lines | Should -Contain "Would have run 'chocolateyinstall.ps1':"
        }

        It "Doesn't show that it would have run a BeforeModify script" {
            $Output.Lines | Should -Not -Contain "Would have run 'chocolateyBeforeModify.ps1':"
        }
    }

    Context "Using NOOP when installing a Package that doesn't exist" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "somethingnonexisting"

            $Output = Invoke-Choco install $PackageUnderTest --noop
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Doesn't install the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }

        It "Displays that it would have used Nuget to install a package" {
            $Output.Lines | Should -Contain "Chocolatey would have used NuGet to install packages (if they are not already installed):"
        }

        It "Shows that it was unable to find this package" {
            $Output.Lines | Should -Contain "$PackageUnderTest not installed. The package was not found with the source(s) listed."
        }
    }

    Context "Installing a Package (Happy Path)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed a package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Exist
        }

        # We are skipping this for now, until we have stabilized the directory
        # path reporting functionality. There are times that this test will
        # fail due to Chocolatey not reporting the path.
        # This failure seems to happen randomly, and is therefore not a
        # reliable test we can make.
        It "Outputs the installation directory (which should exist)" -Skip {
            $directoryPath = "$env:ChocolateyInstall\lib\$PackageUnderTest"
            $lineRegex = [regex]::Escape($directoryPath)

            $foundPath = $Output.Lines -match $lineRegex
            $foundPath | Should -Not -BeNullOrEmpty
            $foundPath | Should -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Creates a Console Shim in the Bin Directory" {
            "$env:ChocolateyInstall\bin\console.exe" | Should -Exist
        }

        It "Creates a Graphical Shim in the Bin Directory" {
            "$env:ChocolateyInstall\bin\graphical.exe" | Should -Exist
        }

        It "Does not create a Shim for Ignored Executable in the Bin Directory" {
            "$env:ChocolateyInstall\bin\not.installed.exe" | Should -Not -Exist
        }

        It "Does not create a Shim for Ignored Executable (with mismatched case) in the Bin Directory" {
            "$env:ChocolateyInstall\bin\casemismatch.exe" | Should -Not -Exist
        }

        It "Does not create an extensions folder for the package" {
            "$env:ChocolateyInstall\extensions\$PackageUnderTest" | Should -Not -Exist
        }

        It "Contains the output of the ChocolateyInstall.ps1 script" {
            $Output.Lines | Should -Contain "Ya!"
        }

        It "Outputs a message showing that installation was successful" {
            $Output.String | Should -Match "Chocolatey installed 1/1 packages\."
        }
    }

    Context "Installing a Package with Packages.config" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
  <package id="installpackage" />
  <package id="hasdependency" version="1.0.0" />
  <package id="missingpackage" />
  <package id="upgradepackage" version="1.0.0"
           installArguments="hi"
           packageParameters="yo" forceX86="true" allowMultipleVersions="false"
           ignoreDependencies="true"
           />
</packages>
"@ | Out-File -FilePath $env:CHOCOLATEY_TEST_PACKAGES_PATH\test.packages.config -Encoding utf8

            $Output = Invoke-Choco install $env:CHOCOLATEY_TEST_PACKAGES_PATH\test.packages.config --confirm
        }

        AfterAll {
            $null = Invoke-Choco uninstall installpackage hasdependency upgradepackage -x --confirm
        }

        It "Exits with Failure (1), due to the missing 'missingpackage'" {
            $Output.ExitCode | Should -Be 1
        }

        It "Installs the package '<_>' in the Lib directory" -ForEach @("InstallPackage"; "HasDependency"; "IsDependency"; "UpgradePackage") {
            "$env:ChocolateyInstall\lib\$_" | Should -Exist
        }

        It "Outputs a message indicating that it installed N-1 packages successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 5/6 packages. 1 packages failed."
        }

        It "Outputs a message indicating that UpgradePackage with the expected version was installed" {
            $Output.Lines | Should -Contain "upgradepackage 1.0.0 Installed"
        }

        It "Outputs a message indicating that '<_>' package was installed" -ForEach @(
            "The install of installpackage was successful."
            "hasdependency 1.0.0 Installed"
            "isexactversiondependency 1.0.0 Installed"
            "isdependency 1.1.0 Installed"
            "upgradepackage 1.0.0 Installed"
        ) {
            $Output.Lines | Should -Contain $_
        }

        It "Outputs a message indicating that missingpackage was not installed" {
            $Output.Lines | Should -Contain "missingpackage not installed. The package was not found with the source(s) listed."
        }

        Context "packages.config containing all options" {
            BeforeAll {
                @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
    <package
        id="installpackage"
        prerelease="true"
        overrideArguments="true"
        notSilent="true"
        allowDowngrade="true"
        forceDependencies="true"
        skipAutomationScripts="true"
        user="user"
        password="string"
        cert="cert"
        certPassword="string"
        ignoreChecksums="true"
        allowEmptyChecksums="true"
        allowEmptyChecksumsSecure="true"
        requireChecksums="true"
        downloadChecksum="downloadChecksum"
        downloadChecksum64="downloadChecksum64"
        downloadChecksumType="downloadChecksumType"
        downloadChecksumType64="downloadChecksumType64"
        ignorePackageExitCodes="true"
        usePackageExitCodes="true"
        stopOnFirstFailure="true"
        exitWhenRebootDetected="true"
        ignoreDetectedReboot="true"
        disableRepositoryOptimizations="true"
        acceptLicense="true"
        confirm="true"
        limitOutput="true"
        cacheLocation="Z:\"
        failOnStderr="true"
        useSystemPowershell="true"
        noProgress="true"
        force="true"
        executionTimeout="1000"
    />
</packages>
"@ | Out-File $env:CHOCOLATEY_TEST_PACKAGES_PATH\alloptions.packages.config -Encoding utf8

                $Output = Invoke-Choco install $env:CHOCOLATEY_TEST_PACKAGES_PATH\alloptions.packages.config --confirm --verbose --debug

                # This is based on two observations: The addition explicitly outputs that it's the Package Configuration.
                # The configuration output is about 80 lines.
                $StartOfPackageConfiguration = [array]::IndexOf($Output.Lines, "Package Configuration Start:")
                $EndOfPackageConfiguration = [array]::IndexOf($Output.Lines, "Package Configuration End")

                $PackageConfigurationOutput = $Output.Lines[$StartofPackageConfiguration..$EndOfPackageConfiguration] -join [Environment]::NewLine
            }

            # We are explicitly passing in a bad username and password here.
            # Therefore it cannot find the package to install and fails the install.
            # That doesn't matter because we just need to test that the configuration is set properly.
            It "Should exit Failure (1)" {
                $Output.ExitCode | Should -Be 1 -Because $Output.String
            }

            It "Should contain the expected configuration option (<Option>) set correctly (<ExpectedValue>)" -ForEach @(
                @{ Option = "Prerelease" ; ExpectedValue = "True" }
                @{ Option = "OverrideArguments" ; ExpectedValue = "True" }
                @{ Option = "NotSilent" ; ExpectedValue = "True" }
                @{ Option = "AllowDowngrade" ; ExpectedValue = "True" }
                @{ Option = "ForceDependencies" ; ExpectedValue = "True" }
                # SkipAutomationScripts sets configuration option SkipPackageInstallProvider
                @{ Option = "SkipPackageInstallProvider" ; ExpectedValue = "True" }
                # User is expanded to Username
                @{ Option = "Username" ; ExpectedValue = "user" }
                # Password should *not* be output in the logging
                # @{ Option = "Password" ; ExpectedValue = "string" }
                # Cert is expanded to Certificate
                @{ Option = "Certificate" ; ExpectedValue = "cert" }
                # CertPassword should *not* be output in the logging
                # @{ Option = "CertPassword" ; ExpectedValue = "string" }
                # IgnoreChecksums sets ChecksumFiles to False
                @{ Option = "ChecksumFiles" ; ExpectedValue = "False" }
                # RequireChecksums is evaluated after allowing empty. It sets both allow options to False
                # @{ Option = "RequireChecksums" ; ExpectedValue = "True" }
                @{ Option = "AllowEmptyChecksums" ; ExpectedValue = "False" }
                @{ Option = "AllowEmptyChecksumsSecure" ; ExpectedValue = "False" }
                @{ Option = "DownloadChecksum" ; ExpectedValue = "downloadChecksum" }
                @{ Option = "DownloadChecksum64" ; ExpectedValue = "downloadChecksum64" }
                @{ Option = "DownloadChecksumType" ; ExpectedValue = "downloadChecksumType" }
                @{ Option = "DownloadChecksumType64" ; ExpectedValue = "downloadChecksumType64" }
                # UsePackageExitCodes and IgnorePackageExitCodes set the same setting, but are opposite of each other.
                # UsePackageExitCodes is evaluated last, so takes precidence.
                # @{ Option = "IgnorePackageExitCodes" ; ExpectedValue = "True" }
                @{ Option = "UsePackageExitCodes" ; ExpectedValue = "True" }
                # StopOnFirstFailure is expanded to StopOnFirstPackageFailure
                @{ Option = "StopOnFirstPackageFailure" ; ExpectedValue = "True" }
                # ExitWhenRebootDetected and IgnoreDetectedReboot both set ExitOnRebootDetected.
                # IgnoreDetectedReboot is evaluated last, so takes precidence.
                # @{ Option = "ExitWhenRebootDetected" ; ExpectedValue = "True" }
                # @{ Option = "IgnoreDetectedReboot" ; ExpectedValue = "True" }
                @{ Option = "ExitOnRebootDetected" ; ExpectedValue = "False" }
                # DisableRepositoryOptimizations sets UsePackageRepositoryOptimizations to false
                @{ Option = "UsePackageRepositoryOptimizations" ; ExpectedValue = "False" }
                @{ Option = "AcceptLicense" ; ExpectedValue = "True" }
                # Confirm is negated into PromptForConfirmation
                @{ Option = "PromptForConfirmation" ; ExpectedValue = "False" }
                # LimitOutput is negated into Regular Output
                @{ Option = "RegularOutput" ; ExpectedValue = "False" }
                @{ Option = "CacheLocation" ; ExpectedValue = "Z:\\" }
                @{ Option = "FailOnStandardError" ; ExpectedValue = "True" }
                # UseSystemPowerShell sets UsePowerShellHost to False
                @{ Option = "UsePowerShellHost" ; ExpectedValue = "False" }
                # NoProgress sets ShowDownloadProgress to False
                @{ Option = "ShowDownloadProgress" ; ExpectedValue = "False" }
                @{ Option = "Force" ; ExpectedValue = "True" }
                # ExecutionTimeout is expanded to CommandExecutionTimeoutSeconds
                @{ Option = "CommandExecutionTimeoutSeconds" ; ExpectedValue = "1000" }
            ) {
                $PackageConfigurationOutput | Should -Match "$Option='$ExpectedValue'"
            }
        }
    }

    Context "Installing a Package that is already installed" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm

            $Output = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Should still have a package in the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Exist
        }

        It "Should still have the expected version installed" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Outputs a message indicating that it was unable to install any package(s)" {
            $Output.Lines | Should -Contain "$PackageUnderTest v1.0.0 already installed."
            $Output.Lines | Should -Contain "Chocolatey installed 0/1 packages."
        }

        It "Outputs a message about using --force to reinstall" {
            # Warning gets repeated in the summary
            $Output.String | Should -Match 'Use --force to reinstall'
        }
    }

    Context "Force Installing a Package that is already installed" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm
            "FileNotOverwritten" | Add-Content -Path "$env:ChocolateyInstall\lib\$PackageUnderTest\tools\chocolateyInstall.ps1"

            $Output = Invoke-Choco install $PackageUnderTest --version 1.0.0 --force --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Should install the package in the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Exist
        }

        It "Should still have the expected version installed" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Should remove and readd the package files in the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\tools\chocolateyInstall.ps1" | Should -Not -FileContentMatch "FileNotOverwritten"
        }

        It "Deletes the rollback files" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        It "Outputs a message indicating that it installed the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    Context "Force Installing a Package that is already installed (that errors)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm
            "FileNotOverwritten" | Add-Content -Path "$env:ChocolateyInstall\lib\$PackageUnderTest\tools\chocolateyInstall.ps1"

            $Output = Invoke-Choco install $PackageUnderTest --version 1.0.0 --force --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Should install the package in the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Exist
        }

        It "Should still have the expected version installed" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Should remove and readd the package files in the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\tools\chocolateyInstall.ps1" | Should -Not -FileContentMatch "FileNotOverwritten"
        }

        It "Deletes the rollback files" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        It "Outputs a message indicating that it installed the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    Context "Force Installing a Package that is already installed (with a read/delete locked file)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            Invoke-Choco install $PackageUnderTest --confirm

            $LockedFile = [System.IO.File]::Open(
                "$env:ChocolateyInstall\lib\$PackageUnderTest\tools\chocolateyInstall.ps1",
                "OpenOrCreate",
                "ReadWrite",
                "Read, Delete"
            )

            $Output = Invoke-Choco install installpackage --force --confirm
        }

        AfterAll {
            $LockedFile.Close()
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Has successfully retained an install of the original package" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\"
        }

        It "Has successfully retained the original version" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        # This behaviour was fixed in 0.10.16
        It "Should not have been able to delete the rollback" -Tag ExpectBroken -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan "0.10.16-beta")) {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Exist
        }

        It "Outputs a message showing that installation succeeded." {
            $Output.String | Should -Match "Chocolatey installed 1/1 packages\."
        }
    }

    Context "Force Installing a Package that is already installed (with an exclusively locked file)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            Invoke-Choco install $PackageUnderTest --confirm

            $LockedFile = [System.IO.File]::Open(
                "$env:ChocolateyInstall\lib\$PackageUnderTest\tools\chocolateyInstall.ps1",
                "OpenOrCreate",
                "ReadWrite",
                "None"
            )

            $Output = Invoke-Choco install installpackage --force --confirm
        }

        AfterAll {
            $LockedFile.Close()
        }

        It "Exits with Success (0)" -Tag FossOnly, ExpectBroken {
            $Output.ExitCode | Should -Be 0
        }

        It "Exits with Failure (1)" -Tag Licensed {
            $Output.ExitCode | Should -Be 1
        }

        It "Has successfully retained an install of the original package" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\"
        }

        It "Has successfully retained the original version" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Should not have been able to delete the rollback" -Tag FossOnly, ExpectBroken {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Exist
        }

        It "Should have been able to delete the rollback" -Tag Licensed {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        It "Outputs a message showing that installation succeeded." {
            $Output.String | Should -Match "Chocolatey installed 0/1 packages\."
        }
    }

    Context "Installing a Package that exists (with a version that does not)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $Output = Invoke-Choco install $PackageUnderTest --version 1.0.1 --confirm
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Doesn't install the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }

        It "Outputs a message showing that installation failed." {
            $Output.String | Should -Match "Chocolatey installed 0/1 packages\."
        }
    }

    Context "Installing a Package that does not exist" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "nonexisting"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Doesn't install the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }

        It "Outputs a message showing that installation failed." {
            $Output.String | Should -Match "Chocolatey installed 0/1 packages\."
        }
    }

    Context "Installing a Package that errors" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "badpackage"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Failure (-1)" {
            $Output.ExitCode | Should -Be -1
        }

        It "Doesn't install the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }

        It "Puts the package in the lib-bad directory" {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest" | Should -Exist
        }

        It "Outputs a message showing that installation failed." {
            $Output.String | Should -Match "Chocolatey installed 0/1 packages\."
        }
    }

    Context "Installing a Package that has non-terminating errors" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco feature disable -n failOnStandardError

            $PackageUnderTest = "nonterminatingerror"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed a package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0"
        }

        It "Outputs a message showing that installation was successful" {
            $Output.String | Should -Match "Chocolatey installed 1/1 packages\."
        }
    }

    Context "Installing a Package that has non-terminating errors (with fail on STDERR)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco feature enable -n FailOnStandardError

            $PackageUnderTest = "nonterminatingerror"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Doesn't install the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }

        It "Puts the package in the lib-bad directory" {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest" | Should -Exist
        }

        It "Outputs a message showing that installation failed." {
            $Output.String | Should -Match "Chocolatey installed 0/1 packages\."
        }
    }

    Context "Installing a side-by-side Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $Output = Invoke-Choco install $PackageUnderTest --confirm --allowmultipleversions
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
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
            $Output.Lines | Should -Contain "Installing the same package with multiple versions is deprecated and will be removed in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it installed the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    Context "Switching a normal Package to a side-by-side Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco install $PackageUnderTest --confirm

            $Output = Invoke-Choco install $PackageUnderTest --confirm --force --allowmultipleversions
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0" | Should -Exist
        }

        It "Removed the previous version of the package from the lib directory" -Tag ExpectBroken {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Not -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest).1.0.0\$($PackageUnderTest).1.0.0.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Outputs a warning message about side by side installs are deprecated" {
            $Output.Lines | Should -Contain "Installing the same package with multiple versions is deprecated and will be removed in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it installed the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    Context "Switching a side-by-side Package to a normal Package" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            $null = Invoke-Choco install $PackageUnderTest --confirm --allowmultipleversion

            $Output = Invoke-Choco install $PackageUnderTest --confirm --force
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
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
            $Output.Lines | Should -Not -Contain "Installing the same package with multiple versions is deprecated and will be removed in v2.0.0." -Because $Output.String
        }

        It "Does not output a warning message that installed side by side package is deprecated" {
            $Output.Lines | Should -Not -Contain "installpackage has been installed as a side by side installation." -Because $Output.String
            $Output.Lines | Should -Not -Contain "Side by side installations are deprecated and is pending removal in v2.0.0." -Because $Output.String
        }

        It "Outputs a message indicating that it installed the package successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    Context "Installing a Package with dependencies (Happy Path)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasdependency"

            $Output = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Installed the dependency to the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Exist
        }

        It "Installs the expected version of the dependency" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "1.1.0"
        }

        It "Outputs a message indicating that it installed the package(s) successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 3/3 packages."
        }
    }

    Context "Force Installing a Package that is already installed (with dependencies)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasdependency"
            $null = Invoke-Choco install isdependency --version 1.1.0 --confirm
            $null = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm

            $Output = Invoke-Choco install $PackageUnderTest --force --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Installed the exact same version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Has still got the dependency to the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Exist
        }

        It "Should not have upgraded the dependency in the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "1.1.0"
        }

        It "Outputs a message indicating that it installed the package(s) successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    Context "Force Installing a Package that is already installed (forcing dependencies)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasdependency"
            $null = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm

            $Output = Invoke-Choco install $PackageUnderTest --force --forcedependencies --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Installed the exact same version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Has still got the dependency to the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Exist
        }

        It "Should have installed the floating dependency in the lib directory with the latest appropriate version" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "1.1.0"
        }

        It "Has still got the exactdependency to the lib directory" {
            "$env:ChocolateyInstall\lib\isexactversiondependency" | Should -Exist
        }

        It "Should have installed the exact exactdependency in the lib directory" {
            "$env:ChocolateyInstall\lib\isexactversiondependency\isexactversiondependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isexactversiondependency\isexactversiondependency.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Outputs a message indicating that it installed the package(s) successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 3/3 packages."
        }
    }

    Context "Force Installing a Package that is already installed (ignoring dependencies)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasdependency"
            $null = Invoke-Choco install isdependency --version 1.0.0 --confirm
            $null = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm

            $Output = Invoke-Choco install $PackageUnderTest --force --ignoredependencies --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Installed the exact same version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Has still got the floating dependency installed to the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Exist
        }

        It "Should not have upgraded the floating dependency in the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Has still got the exact dependency installed to the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Exist
        }

        It "Should not have upgraded the exact dependency in the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Outputs a message indicating that it installed the package(s) successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    Context "Force Installing a Package that is already installed (forcing and ignoring dependencies)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasdependency"
            $null = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm

            $Output = Invoke-Choco install $PackageUnderTest --force --ignoredependencies --forcedependencies --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Installed the exact same version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        # This only gets removed on Open Source
        It "Has removed the floating dependency installed in the lib directory" -Tag FossOnly {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Not -Exist
        }

        # This only gets removed on Open Source
        It "Has removed the exact dependency installed in the lib directory" -Tag FossOnly {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Not -Exist
        }

        It "Outputs a message indicating that it installed the package(s) successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    Context "Installing a Package (with dependencies that cannot be found)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "ismissingpackage"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Doesn't install the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }

        It "Doesn't install the package to the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Not -Exist
        }

        It "Outputs a message indicating that it failed to install the package" {
            $Output.Lines | Should -Contain "Chocolatey installed 0/1 packages. 1 packages failed."
        }
    }

    Context "Installing a Package (ignoring dependencies that cannot be found)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasdependency"

            $Output = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm --ignoredependencies
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Has installed a package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Exist
        }

        It "Installed the right version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Doesn't install the package to the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Not -Exist
        }

        It "Outputs a message indicating that it install the package" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    # TODO: Need to get this test working
    Context "Installing a Package that depends on a newer version of an installed dependency" -Skip {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            Invoke-Choco install "isdependency" --version 1.0.0 --confirm
            $PackageUnderTest = "hasdependency"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Installed the exact same version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec"
            $XML.package.metadata.version | Should -Be "2.1.0"
        }

        It "Has the dependency installed in the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency" | Should -Exist
        }

        It "Upgraded the dependency in the lib directory" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "2.1.0"
        }

        It "Outputs a message indicating that it installed the package(s) successfully" {
            $Output.Lines | Should -Contain "Chocolatey installed 3/3 packages."
        }
    }

    Context "Installing a Package that depends on a newer version of an installed dependency (that is unavailable)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install "hasdependency" --version 1.0.0 --confirm
            $PackageUnderTest = "hasoutofrangedependency"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Has not installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Not -Exist
        }

        It "Outputs a message indicating that it failed to install the package(s)" {
            $Output.Lines | Should -Contain "Chocolatey installed 0/1 packages. 1 packages failed."
        }
    }

    Context "Installing a Package that depends on a newer version of an installed dependency (that is unavailable, ignoring dependencies)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install "hasdependency" --version 1.0.0 --confirm
            $PackageUnderTest = "hasoutofrangedependency"

            $Output = Invoke-Choco install $PackageUnderTest --confirm --ignoredependencies
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Has installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Installed the right version of the package" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$($PackageUnderTest)\$($PackageUnderTest).nuspec"
            $XML.package.metadata.version | Should -Be "2.0.3"
        }

        It "Outputs a message indicating that it successfully installed the package(s)" {
            $Output.Lines | Should -Contain "Chocolatey installed 1/1 packages."
        }
    }

    Context "Force Installing a Package that depends on a newer version of an installed dependency (that is unavailable, forcing depedencies)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install "hasdependency" --version 1.0.0 --confirm
            $PackageUnderTest = "hasoutofrangedependency"

            $Output = Invoke-Choco install $PackageUnderTest --confirm --force --forcedependencies
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Has not installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Not -Exist
        }

        It "Has not upgraded the dependency" {
            "$env:ChocolateyInstall\lib\hasdependency\hasdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\hasdependency\hasdependency.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Outputs a message indicating that it failed to install the package(s)" {
            $Output.Lines | Should -Contain "Chocolatey installed 0/1 packages. 1 packages failed."
            $Output.String | Should -Match "Unable to resolve dependency 'hasdependency"
        }
    }

    # TODO: Add tests for version handling when dependency is specified as a lower
    # version than what is available (hasoutofrangedependency v2.0.0 should cover this)

    # TODO: Add tests for version handling when dependency is specifies as a
    # version that is between available versions (hasoutofrangedependency v2.0.1 should cover this)

    # TODO: Add tests for version handling when dependency is specified as a
    # version that has an exact version that do not exist (hasoutofrangedependency v2.0.2 should cover this)

    Context "Installing a Package that depends on a newer version of a package than an existing package has with that dependency" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install isdependency --version 2.1.0 --confirm
            $PackageUnderTest = "conflictingdependency"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Has installed the package to the lib directory" {
            "$env:ChocolateyInstall\lib\$($PackageUnderTest)" | Should -Exist
        }

        It "Has upgraded the dependency" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "2.1.0"
        }

        It "Outputs a message indicating that it succeeded to install the package(s)" {
            $Output.Lines | Should -Contain "Chocolatey installed 2/2 packages."
        }
    }

    Context "Installing a Package from a nupkg file" {
        BeforeAll {
            $snapshotPath = New-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            New-ChocolateyTestPackage `
                -TestPath "$PSScriptRoot\testpackages" `
                -Name $PackageUnderTest `
                -Version "1.0.0"

            $PackagePath = "$($snapshotPath.PackagesPath)\$PackageUnderTest.1.0.0.nupkg"

            $Output = Invoke-Choco install $PackagePath --confirm --params '/ParameterOne:FirstOne /ParameterTwo:AnotherOne'
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed a package to the lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Exist
        }

        # We are skipping this for now, until we have stabilized the directory
        # path reporting functionality. There are times that this test will
        # fail due to Chocolatey not reporting the path.
        # This failure seems to happen randomly, and is therefore not a
        # reliable test we can make.
        It "Outputs the installation directory" -Skip {
            $directoryPath = "$env:ChocolateyInstall\lib\$PackageUnderTest"
            $lineRegex = [regex]::Escape($directoryPath)

            $Output.String | Should -Match "$lineRegex"
        }

        It "Has created the installation directory" {
            $Output.Lines -Match "$([Regex]::Escape((Join-Path $env:ChocolateyInstall "lib\$PackageUnderTest")))" | Should -Exist
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Creates a Console Shim in the Bin Directory" {
            "$env:ChocolateyInstall\bin\console.exe" | Should -Exist
        }

        It "Creates a Graphical Shim in the Bin Directory" {
            "$env:ChocolateyInstall\bin\graphical.exe" | Should -Exist
        }

        It "Does not create a Shim for Ignored Executable in the Bin Directory" {
            "$env:ChocolateyInstall\bin\not.installed.exe" | Should -Not -Exist
        }

        It "Does not create a Shim for Ignored Executable (with mismatched case) in the Bin Directory" {
            "$env:ChocolateyInstall\bin\casemismatch.exe" | Should -Not -Exist
        }

        It "Does not create an extensions folder for the package" {
            "$env:ChocolateyInstall\extensions\$PackageUnderTest" | Should -Not -Exist
        }

        It "Contains the output of the ChocolateyInstall.ps1 script" {
            $Output.Lines | Should -Contain "Ya!"
        }

        # https://github.com/chocolatey/choco/issues/2089
        It "Reports the Package Parameters expected" {
            $Output.Lines | Should -Contain "Package Parameters:"
            $Output.Lines | Should -Contain "ParameterOne - FirstOne"
            $Output.Lines | Should -Contain "ParameterTwo - AnotherOne"
        }

        It "Outputs a message showing that installation was successful" {
            $Output.String | Should -Match "Chocolatey installed 1/1 packages\."
        }
    }

    Context "Installing a Pure Portable Package" {
        BeforeAll {
            $snapshotPath = New-ChocolateyInstallSnapshot
            $PackageUnderTest = 'pureportable'

            $Output = Invoke-Choco install $PackageUnderTest
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs a message showing that installation was successful" {
            $Output.String | Should -Match "Chocolatey installed 1/1 packages\."
        }

        It "Outputs installation to the correct location" {
            $Output.Lines | Should -Contain "Software installed to '$($env:ChocolateyInstall)\lib\$PackageUnderTest'"
        }
    }

    # TODO: Create tests surrounding this stubbed out test
    Context "Installing a Package with config transforms" -Skip {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
        }
    }

    Context "Installing a Package with no sources enabled" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            Disable-ChocolateySource -All

            $PackageUnderTest = "installpackage"

            $Output = Invoke-Choco install $PackageUnderTest --confirm
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Outputs a message indicating that there were no sources enabled" {
            $Output.String | Should -Match "Installation was NOT successful. There are no sources enabled for"
        }

        It "Does not install a package" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }
    }

    # Issue: https://gitlab.com/chocolatey/collaborators/choco-licensed/-/issues/530 (NOTE: Proxy bypassing also works on Chocolatey FOSS)
    Context "Installing a Package with proxy and proxy bypass list" -Skip:(!$licensedProxyFixed) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $null = Invoke-Choco config set --name=proxy --value="https://invalid.chocolatey.org/"
            $null = Invoke-Choco config set --name=proxyBypassList --value="hermes.chocolatey.org"

            $Output = Invoke-Choco install installpackage --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays package files install completed" {
            $Output.Lines | Should -Contain "installpackage package files install completed. Performing other installation steps."
        }
    }

    # Issue: https://gitlab.com/chocolatey/collaborators/choco-licensed/-/issues/530 (NOTE: Proxy bypassing also works on Chocolatey FOSS)
    Context "Installing a Package with proxy and proxy bypass list on command" -Skip:(!$licensedProxyFixed) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $null = Invoke-Choco config set --name=proxy --value="https://invalid.chocolatey.org/"

            $Output = Invoke-Choco install installpackage --confirm "--proxy-bypass-list=hermes.chocolatey.org"
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays package files install completed" {
            $Output.Lines | Should -Contain "installpackage package files install completed. Performing other installation steps."
        }
    }

    # TODO: Need to figure out how to test out toggling proxy bypassing on the
    # source itself. This should be possible to do with the hermes source,
    # but we need to extract the source, user and password before we can
    # do that.

    # Issue: https://github.com/chocolatey/choco/issues/2203
    # Issue: https://gitlab.com/chocolatey/collaborators/choco-licensed/-/issues/552
    Context "Installing a Package with Embedded Zip Archive and using -UnzipLocation" -Skip:(!$destinationAliasAvailable) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install get-chocolateyunzip-test --version 0.0.2 --confirm
        }

        AfterAll {
            $null = Invoke-Choco uninstall get-chocolateyunzip-test --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        # TODO: Background service only works with Chocolatey Licensed Extension. Assess moving this test to CLE test suite
        It "Runs under background Service" -Tag Background {
            $Output.Lines | Should -Contain 'Running in background mode'
        }

        It "Displays installed message" {
            $Output.Lines | Should -Contain "The install of get-chocolateyunzip-test was successful."
        }
    }

    # Issue: https://github.com/chocolatey/chocolatey-licensed-issues/issues/284
    Context "Installing a Package with Embedded Zip Archive and specifying destination <Name>" -ForEach @(
        @{ Name = 'Root of UNC share' ; Path = '\\localhost\c$\' }
        @{ Name = 'UNC share path' ; Path = '\\localhost\c$\temp\' }
        @{ Name = 'Root of drive with trailing slash' ; Path = 'C:\' }
        @{ Name = 'Root of drive without trailing slash' ; Path = 'C:' }
        @{ Name = 'Windows System32' ; Path = 'C:\Windows\System32' }
        @{ Name = 'Non-existentfolder' ; Path = "$(Get-TempDirectory)$(New-Guid)" }
    ) {
        param($Name, $Path)
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install get-chocolateyunzip-custom-paths --params "'/Destination:$Path'" --confirm
            $packageFile = Join-Path -Path $Path -ChildPath 'get-chocolateyunzip-custom-paths.txt'
        }

        AfterAll {
            $null = Invoke-Choco uninstall get-chocolateyunzip-custom-paths --confirm
            $null = Remove-Item $packageFile -Force -ErrorAction Ignore
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        # TODO: Background service only works with Chocolatey Licensed Extension. Assess moving this test to CLE test suite
        It "Runs under background Service" -Tag Background {
            $Output.Lines | Should -Contain 'Running in background mode'
        }

        It "Displays installed message" {
            $Output.Lines | Should -Contain "The install of get-chocolateyunzip-custom-paths was successful."
        }

        It "Installed the file as expected" {
            $packageFile | Should -Exist -Because "Package was instructed to be installed to $Path."
        }
    }

    Context "Installing commercial package with custom commandlets" -Tag FossOnly {
        BeforeAll {
            New-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install get-chocolateyunzip-licensed --confirm
        }

        It "Exits with Failure (-1)" {
            $Output.ExitCode | Should -Be -1
        }
    }

    Context "Installing license only package on open source" -Tag FossOnly {
        BeforeAll {
            New-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install business-only-license --confirm
        }

        It "Exits with Failure (-1)" {
            $Output.ExitCode | Should -Be -1
        }

        It "Outputs warning message about needing commercial edition" {
            $Output.Lines | Should -Contain "WARNING: Package Requires Commercial License - Installation cannot continue as Package Builder use require endpoints to be licensed with Chocolatey Licensed Extension v3.0.0+ (chocolatey.extension). Please see error below for details and correction instructions."
        }

        It "Outputs error message about needing commercial edition" {
            $Output.Lines | Should -Contain "ERROR: This package requires a commercial edition of Chocolatey as it was built/internalized with commercial features. Please install the license and install/upgrade to Chocolatey Licensed Extension v3.0.0+ as per https://docs.chocolatey.org/en-us/licensed-extension/setup."
        }
    }

    Context "Installing package with beforeInstall scriptblock defined" -Skip:(!$hasBeforeInstallBlock) {
        BeforeAll {
            New-ChocolateyInstallSnapshot
            Remove-Item "$env:ChocolateyInstall\logs\*" -ErrorAction Ignore

            $Output = Invoke-Choco install hasbeforeinstallblock --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Outputs string defined in before installation block" {
            $Output.Lines | Should -Contain "Running necessary Pre-Install step"
        }

        It "Logs beforeInstall block in log file" {
            "$env:ChocolateyInstall\logs\chocolatey.log" | Should -FileContentMatchMultiline "\s*-?beforeInstall:? '[\s\r\n]*# This is just to notify that the before install script[\s\r\n]*# block have been ran[\s\r\n]*Write-Host `"Running necessary Pre-Install step`"[\s\r\n]*'"
        }
    }

    Context "Installing package with circular dependencies" -Tag CircularDependency {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot -SetWorkDir

            $result1 = Invoke-Choco install circulardependency1 -y
            $result2 = Invoke-Choco install circulardependency2 -y
        }

        It "Exits with Failure (1)" {
            $result1.ExitCode | Should -Be 1
            $result2.ExitCode | Should -Be 1
        }

        It "should identify a circular dependency" {
            $result1.Lines | Should -Contain "Circular dependency detected 'circulardependency1 0.0.1 => circulardependency2 0.0.1 => circulardependency1 0.0.1'."
            $result2.Lines | Should -Contain "Circular dependency detected 'circulardependency1 0.0.1 => circulardependency2 0.0.1 => circulardependency1 0.0.1'."
        }
    }

    Context "Install '<Package>' package with (<Command>) specified" -ForEach @(
        @{ Command = '--pin' ; Package = 'installpackage' ; Contains = $true }
        @{ Command = '' ; Package = 'installpackage' ; Contains = $false }
        @{ Command = '' ; Package = 'packages.config' ; Contains = $true }
    ) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            if ($Package -eq 'packages.config') {
                @"
<?xml version="1.0" encoding="utf-8"?>
<packages>
    <package id="installpackage" pinPackage="true" />
</packages>
"@ | Set-Content $PWD/packages.config
            }

            $null = Invoke-Choco install $Package $Command --confirm
            $Output = Invoke-Choco pin list
        }

        It "Output should include pinned package" {
            if ($Contains) {
                $Output.String | Should -Match "installpackage|1.0.0"
            }
            else {
                $Output.String | Should -Not -Match "installpackage|1.0.0"
            }
        }
    }

    Context "Installing package that extracts local zip archive while disabling logging" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install zip-log-disable-test --verbose --debug -y
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Does not output extracted file path '<_>'" -ForEach @('tools\'; 'tools\chocolateybeforemodify.ps1'; 'tools\chocolateyinstall.ps1'; 'tools\chocolateyuninstall.ps1'; 'zip-log-disable-test.nuspec') {
            $Output.String | Should -Not -Match "- $([regex]::Escape($_))"
        }
    }

    Context "Installing package that extracts external zip archive while disabling logging" -Tag Internal {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install zip-log-disable-test-external --verbose --debug -y
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Does not output extracted file path '<_>'" -ForEach @('tools\'; 'tools\chocolateybeforemodify.ps1'; 'tools\chocolateyinstall.ps1'; 'tools\chocolateyuninstall.ps1'; 'zip-log-disable-test.nuspec') {
            $Output.String | Should -Not -Match "- $([regex]::Escape($_))"
        }
    }

    Context "Installing package that makes use of new Get Chocolatey Path helper" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            Enable-ChocolateySource -Name 'local'

            $Output = Invoke-Choco install test-chocolateypath -y
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Outputs message <_>" -ForEach @(
            'Package Path in Install Script: <installPath>\lib\test-chocolateypath'
            'Install Path in Install Script: <installPath>'
        ) {
            $Output.Lines | Should -Contain "$($_ -replace '<installPath>',$env:ChocolateyInstall)" -Because $Output.String
        }
    }


    Context "Installing multiple packages at once without allowGlobalConfirmation" {

        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            Disable-ChocolateyFeature -Name allowGlobalConfirmation

            $PackageUnderTest = "installpackage", "packagewithscript"

            $Output = "a`n"*2 | Invoke-Choco install @PackageUnderTest
        }

        It "Installs successfully and exits with success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Installed the packages to the lib directory" {
            $PackageUnderTest | ForEach-Object {
                "$env:ChocolateyInstall\lib\$_" | Should -Exist
            }
        }

        It "Ran both installation scripts after selecting [A] Yes to all at the first prompt" {
            $promptLine = "Do you want to run the script?([Y]es/[A]ll - yes to all/[N]o/[P]rint):"
            $prompts = $Output.Lines | Where-Object { $_ -eq $promptLine }

            $prompts.Count | Should -Be 1
        }
    }
}
