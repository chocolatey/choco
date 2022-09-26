Import-Module helpers/common-helpers

Describe "choco push" -Tag Chocolatey, PushCommand -Skip:($null -eq $env:API_KEY) {
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
        # Ideally this comes from an environment variable, but that's proving harder to put into the tests than is desired.
        $ApiKey = $env:API_KEY
        # Using Chocolatey Community Repository for pushing as choco-test could be blown away at any time, and we'd have to reset up the user and packages.
        $RepositoryToUse = if ($env:PUSH_REPO) {
            $env:PUSH_REPO
        }
        else {
            "https://push.chocolatey.org"
        }
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Pushing package version that already exists" {
        BeforeAll {
            $snapshotPath = New-ChocolateyInstallSnapshot

            $PackageUnderTest = "chocolatey-dummy-package"
            $VersionUnderTest = "1.0.0"

            $NewChocolateyTestPackage = @{
                TestPath = "$PSScriptRoot\testpackages"
                Name = $PackageUnderTest
                Version = $VersionUnderTest
            }
            New-ChocolateyTestPackage @NewChocolateyTestPackage

            $PackagePath = "$($snapshotPath.PackagesPath)\$PackageUnderTest.$VersionUnderTest.nupkg"

            $Output = Invoke-Choco push $PackagePath --source $RepositoryToUse --api-key $ApiKey
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Should Report the actual cause of the error" {
            $Output.Lines | Should -Contain "Attempting to push $PackageUnderTest.$VersionUnderTest.nupkg to $RepositoryToUse"
            $Output.Lines | Should -Contain "An error has occurred. It's possible the package version already exists on the repository or a nuspec element is invalid. See error below..."
            $Output.String | Should -Match "Failed to process request. '"
            $Output.Lines | Should -Contain "The remote server returned an error: (409) Conflict.."
        }
    }

    Context "Pushing package description more than 4000 characters" {
        BeforeAll {
            $snapshotPath = New-ChocolateyInstallSnapshot

            $PackageUnderTest = "too-long-description"
            $VersionUnderTest = "1.0.0"

            $NewChocolateyTestPackage = @{
                TestPath = "$PSScriptRoot\testpackages"
                Name = $PackageUnderTest
                Version = $VersionUnderTest
            }
            New-ChocolateyTestPackage @NewChocolateyTestPackage

            $PackagePath = "$($snapshotPath.PackagesPath)\$PackageUnderTest.$VersionUnderTest.nupkg"

            $Output = Invoke-Choco push $PackagePath --source $RepositoryToUse --api-key $ApiKey
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Should Report the actual cause of the error" {
            $Output.Lines | Should -Contain "Attempting to push $PackageUnderTest.$VersionUnderTest.nupkg to $RepositoryToUse"
            $Output.Lines | Should -Contain "An error has occurred. It's possible the package version already exists on the repository or a nuspec element is invalid. See error below..."
            $Output.String | Should -Match "Failed to process request. '"
            $Output.Lines | Should -Contain "The remote server returned an error: (409) Conflict.."
        }
    }

    Context "Pushing package title more than 256 characters" {
        BeforeAll {
            $snapshotPath = New-ChocolateyInstallSnapshot

            $PackageUnderTest = "too-long-title"
            $VersionUnderTest = "1.0.0"

            $NewChocolateyTestPackage = @{
                TestPath = "$PSScriptRoot\testpackages"
                Name = $PackageUnderTest
                Version = $VersionUnderTest
            }
            New-ChocolateyTestPackage @NewChocolateyTestPackage

            $PackagePath = "$($snapshotPath.PackagesPath)\$PackageUnderTest.$VersionUnderTest.nupkg"

            $Output = Invoke-Choco push $PackagePath --source $RepositoryToUse --api-key $ApiKey
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Should Report the actual cause of the error" {
            $Output.Lines | Should -Contain "Attempting to push $PackageUnderTest.$VersionUnderTest.nupkg to $RepositoryToUse"
            $Output.Lines | Should -Contain "An error has occurred. It's possible the package version already exists on the repository or a nuspec element is invalid. See error below..."
            $Output.String | Should -Match "Failed to process request. '"
            $Output.Lines | Should -Contain "The remote server returned an error: (409) Conflict.."
        }
    }
}
