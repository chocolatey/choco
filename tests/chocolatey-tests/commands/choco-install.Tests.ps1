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
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
        $features = Get-ChocolateyFeatures
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
           packageParameters="yo" forceX86="true"
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
            $Output.ExitCode | Should -Be 1 -Because $Output.String
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
                # UsePackageExitCodes is evaluated last, so takes precedence.
                # @{ Option = "IgnorePackageExitCodes" ; ExpectedValue = "True" }
                @{ Option = "UsePackageExitCodes" ; ExpectedValue = "True" }
                # StopOnFirstFailure is expanded to StopOnFirstPackageFailure
                @{ Option = "StopOnFirstPackageFailure" ; ExpectedValue = "True" }
                # ExitWhenRebootDetected and IgnoreDetectedReboot both set ExitOnRebootDetected.
                # IgnoreDetectedReboot is evaluated last, so takes precedence.
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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

    Context "Force Installing a Package that is already installed (with a delete locked file)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "installpackage"

            Invoke-Choco install $PackageUnderTest --confirm

            $LockedFile = [System.IO.File]::Open(
                "$env:ChocolateyInstall\lib\$PackageUnderTest\tools\chocolateyInstall.ps1",
                "OpenOrCreate",
                "ReadWrite",
                "Delete"
            )

            $Output = Invoke-Choco install installpackage --force --confirm
        }

        AfterAll {
            $LockedFile.Close()
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Has successfully retained an install of the original package" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\"
        }

        It "Has successfully retained the original version" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Should not have been able to delete the rollback" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Exist
        }

        It "Outputs a message showing that installation succeeded." {
            $Output.String | Should -Match "Chocolatey installed 1/1 packages\."
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
        It "Should not have been able to delete the rollback" -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan "0.10.16-beta")) {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Exist
        }

        It "Outputs a message showing that installation succeeded." {
            $Output.String | Should -Match "Chocolatey installed 1/1 packages\."
        }
    }

    Context "Force Installing a Package that is already installed (with an exclusively locked file)" {
        BeforeDiscovery {
            $PackageUnderTest = "installpackage"
        }

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

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Has successfully retained an install of the original package" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\" | Should -Exist
        }

        It "Has successfully retained the original version" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\$PackageUnderTest\$PackageUnderTest.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }

        It "Should have been able to delete the rollback" {
            # It is important to know that the behavior may be different on different operating
            # systems. It depends on how the operating system implements locking of files.
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        It "Should have created the package in lib-bad" {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest" | Should -Exist
        }

        It "Should have stored pending file in bad folder" {
            # Because the package itself fails to extract, due to the install script being locked.
            # The only file in the original package that could be copied over is the pending file (can't copy the exclusively locked file either).
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\1.0.0\.chocolateyPending" | Should -Exist
        }

        It "Outputs a message showing that installation failed." {
            $Output.String | Should -Match "Chocolatey installed 0/1 packages\."
        }
    }

    Context "Installing a failing package created backup in lib-bad" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "failingdependency"
            $PackageVersion = '0.9.9'

            $Output = Invoke-Choco install $PackageUnderTest --version $PackageVersion --confirm
        }

        It "Exits with Failure (15608)" {
            $Output.ExitCode | Should -Be 15608 -Because $Output.String
        }

        It "Doesn't install the package to lib directory" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }

        It "Doesn't keep a package backup in lib-bkp" {
            "$env:ChocolateyInstall\lib-bkp\$PackageUnderTest" | Should -Not -Exist
        }

        It "Creates backup of file '<_>' in lib-bad" -ForEach @('failingdependency.nupkg', 'failingdependency.nuspec', '.chocolateyPending', 'tools\chocolateyinstall.ps1', 'tools\test-file.txt') {
            "$env:ChocolateyInstall\lib-bad\$PackageUnderTest\$PackageVersion\$_" | Should -Exist
        }

        It "Outputs a message showing that installation failed." {
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
            $Output.ExitCode | Should -Be 1 -Because $Output.String
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
            $Output.ExitCode | Should -Be 1 -Because $Output.String
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
            $Output.ExitCode | Should -Be -1 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 1 -Because $Output.String
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

    Context "Installing a Package with dependencies (Happy Path)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $PackageUnderTest = "hasdependency"

            $Output = Invoke-Choco install $PackageUnderTest --version 1.0.0 --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 1 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 1 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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

    Context "Force Installing a Package that depends on a newer version of an installed dependency (that is unavailable, forcing dependencies)" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install "hasdependency" --version 1.0.0 --confirm
            $PackageUnderTest = "hasoutofrangedependency"

            $Output = Invoke-Choco install $PackageUnderTest --confirm --force --forcedependencies
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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

To install a local, or remote file, you may use:
  choco install $packageUnderTest --version="1.0.0" --source="$([regex]::Escape($snapshotPath.PackagesPath))
"@
        }
    }

    Context "Installing a Pure Portable Package" {
        BeforeAll {
            $snapshotPath = New-ChocolateyInstallSnapshot
            $PackageUnderTest = 'pureportable'

            $Output = Invoke-Choco install $PackageUnderTest
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Outputs a message indicating that there were no sources enabled" {
            $Output.String | Should -Match "Installation was NOT successful. There are no sources enabled for"
        }

        It "Does not install a package" {
            "$env:ChocolateyInstall\lib\$PackageUnderTest" | Should -Not -Exist
        }
    }

    # Issue: https://gitlab.com/chocolatey/collaborators/choco-licensed/-/issues/530 (NOTE: Proxy bypassing also works on Chocolatey FOSS)
    # These are skipped on Proxy tests because the proxy server can't be bypassed in that test environment.
    Context "Installing a Package with proxy and proxy bypass list" -Tag ProxySkip -Skip:(!$licensedProxyFixed) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $null = Invoke-Choco config set --name=proxy --value="https://invalid.chocolatey.org/"
            $null = Invoke-Choco config set --name=proxyBypassList --value="hermes.chocolatey.org"

            $Output = Invoke-Choco install installpackage --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Displays package files install completed" {
            $Output.Lines | Should -Contain "installpackage package files install completed. Performing other installation steps."
        }
    }

    # Issue: https://gitlab.com/chocolatey/collaborators/choco-licensed/-/issues/530 (NOTE: Proxy bypassing also works on Chocolatey FOSS)
    # These are skipped on Proxy tests because the proxy server can't be bypassed in that test environment.
    Context "Installing a Package with proxy and proxy bypass list on command" -Tag ProxySkip -Skip:(!$licensedProxyFixed) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $null = Invoke-Choco config set --name=proxy --value="https://invalid.chocolatey.org/"

            $Output = Invoke-Choco install installpackage --confirm "--proxy-bypass-list=hermes.chocolatey.org"
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install get-chocolateyunzip-licensed --confirm
        }

        It "Exits with Failure (-1)" {
            $Output.ExitCode | Should -Be -1 -Because $Output.String
        }
    }

    Context "Installing license only package on open source" -Tag FossOnly {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            Enable-ChocolateySource -Name hermes-setup

            $Output = Invoke-Choco install business-only-license --confirm
        }

        It "Exits with Failure (-1)" {
            $Output.ExitCode | Should -Be -1 -Because $Output.String
        }

        It "Outputs warning message about needing commercial edition" {
            $Output.Lines | Should -Contain "WARNING: Package Requires Commercial License - Installation cannot continue as Package Builder use require endpoints to be licensed with Chocolatey Licensed Extension v3.0.0+ (chocolatey.extension). Please see error below for details and correction instructions."
        }

        It "Outputs error message about needing commercial edition" {
            $Output.Lines | Should -Contain "ERROR: This package requires a commercial edition of Chocolatey as it was built/internalized with commercial features. Please install the license and install/upgrade to Chocolatey Licensed Extension v3.0.0+ as per https://docs.chocolatey.org/en-us/licensed-extension/setup."
        }
    }

    # These are skipped in the Proxy Test environment because the beforeInstall reaches out to GitHub which is not permitted through our proxy.
    Context "Installing package with beforeInstall scriptblock defined" -Tag ProxySkip -Skip:(!$hasBeforeInstallBlock) {
        BeforeAll {
            New-ChocolateyInstallSnapshot
            Remove-Item "$env:ChocolateyInstall\logs\*" -ErrorAction Ignore

            $Output = Invoke-Choco install hasbeforeinstallblock --confirm
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $result1.ExitCode | Should -Be 1 -Because $result1.String
            $result2.ExitCode | Should -Be 1 -Because $result2.String
        }

        It "should identify a circular dependency" {
            $result1.Lines | Should -Contain "Unable to resolve dependency 'circulardependency1': Circular dependency detected 'circulardependency1 0.0.1 => circulardependency2 0.0.1 => circulardependency1 0.0.1'." -Because $result1.String
            $result2.Lines | Should -Contain "Unable to resolve dependency 'circulardependency1': Circular dependency detected 'circulardependency1 0.0.1 => circulardependency2 0.0.1 => circulardependency1 0.0.1'." -Because $result2.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Does not output extracted file path '<_>'" -ForEach @('tools\'; 'tools\chocolateybeforemodify.ps1'; 'tools\chocolateyinstall.ps1'; 'tools\chocolateyuninstall.ps1'; 'zip-log-disable-test.nuspec') {
            $Output.String | Should -Not -Match "- $([regex]::Escape($_))"
        }
    }

    Context "Installing package that makes use of new Get Chocolatey Path helper" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

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
            $Output.ExitCode | Should -Be 0 -Because $Output.String
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

    # We are marking this test as internal, as the package we need to make use
    # of downloads a zip archive from a internal server, and the package is also
    # only located on an internal feed.
    Context "Installing package while specifying a cache location (Arg: <_>)" -ForEach '-c', '--cache', '--cachelocation', '--cache-location' -Tag Internal, LongPaths, CacheLocation {
        BeforeAll {
            $paths = Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install install-chocolateyzip --version 3.21.2 --confirm "$_" "$($paths.CachePathLong)" --no-progress
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

        It 'Outputs installation was successful' {
            $Output.Lines | Should -Contain 'The install of install-chocolateyzip was successful.' -Because $Output.String
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

    Context 'Installing a package while passing in an uppercase letter as the identifier' {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install IsDependency --confirm
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Installs package to expected directory' {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
        }

        It 'Outputs installation was successful' {
            $Output.Lines | Should -Contain 'The install of isdependency was successful.' -Because $Output.String
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "2.1.0"
        }
    }

    Context 'Installing a package while passing in an uppercase letter as the identifier (Repository optimization disabled)' {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install IsDependency --confirm --disable-repository-optimizations
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Installs package to expected directory' {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
        }

        It 'Outputs installation was successful' {
            $Output.Lines | Should -Contain 'The install of isdependency was successful.' -Because $Output.String
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "2.1.0"
        }
    }

    Context 'Installing a package while passing in an uppercase letter as the identifier and specific version' {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install IsDependency --confirm --disable-repository-optimizations --version 1.0.0
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Installs package to expected directory' {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
        }

        It 'Outputs installation was successful' {
            $Output.Lines | Should -Contain 'The install of isdependency was successful.' -Because $Output.String
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }
    }

    Context 'Installing a package while passing in an uppercase letter as the identifier and specific version (Repository optimization disabled)' {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install IsDependency --confirm --disable-repository-optimizations --version 1.0.0
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Installs package to expected directory' {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
        }

        It 'Outputs installation was successful' {
            $Output.Lines | Should -Contain 'The install of isdependency was successful.' -Because $Output.String
        }

        It "Installs the expected version of the package" {
            "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec" | Should -Exist
            [xml]$XML = Get-Content "$env:ChocolateyInstall\lib\isdependency\isdependency.nuspec"
            $XML.package.metadata.version | Should -Be "1.0.0"
        }
    }

    Context "Installing a package when invalid package source is being used" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $InvalidSource = "https://invalid.chocolatey.org/api/v2/"
            $null = Invoke-Choco source add -n "invalid" -s $InvalidSource

            $Output = Invoke-Choco install installpackage --confirm
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Outputs warning about unable to load service index' {
            $Output.Lines | Should -Contain "Unable to load the service index for source $InvalidSource."
        }

        It 'Outputs successful installation of single package' {
            $Output.Lines | Should -Contain 'Chocolatey installed 1/1 packages.'
        }
    }

    Context "Installing a package when the user specifies a non-conforming casing of the id" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install InstAlLpAckaGe --confirm
        }

        It 'Exits with Success' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Outputs successful installation of single package' {
            $Output.Lines | Should -Contain 'Chocolatey installed 1/1 packages.' -Because $Output.String
        }

        It 'Installed package to expected location' {
            "$env:ChocolateyInstall\lib\installpackage" | Should -Exist
        }
    }

    Context "Installing a package with a non-normalized version number" -ForEach @(
        @{ ExpectedPackageVersion = '1.0.0' ; SearchVersion = '1' ; NuspecVersion = '01.0.0.0'}
        @{ ExpectedPackageVersion = '1.0.0' ; SearchVersion = '1.0' ; NuspecVersion = '01.0.0.0'}
        @{ ExpectedPackageVersion = '1.0.0' ; SearchVersion = '1.0.0' ; NuspecVersion = '01.0.0.0' }
        @{ ExpectedPackageVersion = '4.0.1' ; SearchVersion = '4.0.1' ; NuspecVersion = '004.0.01.0' }
        @{ ExpectedPackageVersion = '1.0.0' ; SearchVersion = '01.0.0.0' ; NuspecVersion = '01.0.0.0' }
        @{ ExpectedPackageVersion = '4.0.1' ; SearchVersion = '004.0.01.0' ; NuspecVersion = '004.0.01.0' }
        @{ ExpectedPackageVersion = '4.0.1' ; SearchVersion = '0000004.00000.00001.0000' ; NuspecVersion = '004.0.01.0' }
    ) -Tag VersionNormalization {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $PackageUnderTest = 'nonnormalizedversions'
            $Output = Invoke-Choco install $PackageUnderTest --Version $SearchVersion
        }

        It "Should exit with success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Should report successful installation" {
            $Output.Lines | Should -Contain "$PackageUnderTest v$ExpectedPackageVersion" -Because $Output.String
            $Output.Lines | Should -Contain 'Chocolatey installed 1/1 packages.' -Because $Output.String
        }

        It "Should have installed the correct files" {
            $ExpectedFiles = "${env:ChocolateyInstall}/lib/$PackageUnderTest/$PackageUnderTest"
            "$ExpectedFiles.nupkg" | Should -Exist -Because $Output.String
            $NuspecContents = [xml](Get-Content "$ExpectedFiles.nuspec")
            $NuspecContents.package.metadata.version | Should -Be $NuspecVersion
        }
    }

    # Tagged as Internal since this package is only available internally and downloads from internal infrastructure.
    Context 'Installing package with Open Source Get-ChocolateyWebFile, Get-WebFileName and Get-WebHeaders' -Tag Internal, FossOnly {
        BeforeAll {
            $paths = New-ChocolateyInstallSnapshot

            # Cache directory is set here to prevent assertion failures
            $Output = Invoke-Choco install get-chocolateywebfile "--cache-location=$($paths.PackagesPath)" --confirm
        }

        AfterAll {
            $null = Invoke-Choco uninstall get-chocolateywebfile --confirm
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Runs under background Service' -Tag Background {
            $Output.Lines | Should -Contain 'Running in background mode' -Because $Output.String
        }

        It 'Outputs name of remote file' {
            $Output.Lines | Should -Contain 'FileName: ChocolateyGUI.msi' -Because $Output.String
        }

        # We only get an output of System.Collections.Hashtable here,
        # but that is enough for us to assert against the call to
        # Get-WebHeaders
        It 'Outputs information from web headers' {
            $Output.Lines | Should -Contain 'System.Collections.Hashtable' -Because $Output.String
        }

        It 'Outputs downloading software' {
            $Output.Lines | Should -Contain 'Downloading get-chocolateywebfile' -Because $Output.String
        }

        It 'Outputs download completed' {
            $Output.Lines | Should -Contain "Download of ChocolateyGUI.msi (16.23 MB) completed." -Because $Output.String
        }

        It 'Outputs path to msi executable' {
            $Output.Lines | Should -Contain "Path: $env:ChocolateyInstall\lib\get-chocolateywebfile\tools\ChocolateyGUI.msi" -Because $Output.String
        }

        It 'Outputs installing msi executable' {
            $Output.Lines | Should -Contain 'Installing get-chocolateywebfile...' -Because $Output.String
        }

        It 'Outputs installation was successful' {
            $Output.Lines | Should -Contain 'get-chocolateywebfile has been installed.' -Because $Output.String
        }

        It 'Installs software to expected directory' {
            "${env:ProgramFiles(x86)}\Chocolatey GUI\ChocolateyGui.exe" | Should -Exist
        }
    }

    # Tagged as Internal as this package needs to be packaged by an older version of Chocolatey CLI to have the nuspec version
    # not be normalized.
    Context 'Installing non-normalized package outputting all environment variables' -Tag Internal {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco install test-environment --version 0.9 --confirm
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Outputs <Name> as <Value>' -ForEach @(@{
            Name = 'chocolateyPackageVersion'
            Value= '0.9.0'
        }
        @{
            Name = 'packageVersion'
            Value= '0.9.0'
        }
        @{
            Name = 'chocolateyPackageNuspecVersion'
            Value= '0.9'
        }
        @{
            Name = 'packageNuspecVersion'
            Value= '0.9'
        }) {
            $Output.Lines | Should -Contain "$Name=$Value"
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    # Any tests after this block are expected to generate the configuration as they're explicitly using the NuGet CLI
    Test-NuGetPaths

    Context 'Installing a package with unsupported nuspec elements shows a warning' {

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

            Disable-ChocolateySource -Name hermes-setup

            $Output = Invoke-Choco install $packageName --source .
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
