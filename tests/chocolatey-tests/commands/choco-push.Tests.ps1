Import-Module helpers/common-helpers

# TODO: All tests that is expected to fail now succeed when pushing to CCR
# even when CCR returns an non-successful status code.
# This is probably something that needs to be fixed in NuGet.Client.
Describe "choco push" -Tag Chocolatey, PushCommand, Broken -Skip:($null -eq $env:API_KEY) {
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
                Name     = $PackageUnderTest
                Version  = $VersionUnderTest
            }
            New-ChocolateyTestPackage @NewChocolateyTestPackage

            $PackagePath = "$($snapshotPath.PackagesPath)\$PackageUnderTest.$VersionUnderTest.nupkg"

            $Output = Invoke-Choco push $PackagePath --source $RepositoryToUse --api-key $ApiKey
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
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
                Name     = $PackageUnderTest
                Version  = $VersionUnderTest
            }
            New-ChocolateyTestPackage @NewChocolateyTestPackage

            $PackagePath = "$($snapshotPath.PackagesPath)\$PackageUnderTest.$VersionUnderTest.nupkg"

            $Output = Invoke-Choco push $PackagePath --source $RepositoryToUse --api-key $ApiKey
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
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
                Name     = $PackageUnderTest
                Version  = $VersionUnderTest
            }
            New-ChocolateyTestPackage @NewChocolateyTestPackage

            $PackagePath = "$($snapshotPath.PackagesPath)\$PackageUnderTest.$VersionUnderTest.nupkg"

            $Output = Invoke-Choco push $PackagePath --source $RepositoryToUse --api-key $ApiKey
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It "Should Report the actual cause of the error" {
            $Output.Lines | Should -Contain "Attempting to push $PackageUnderTest.$VersionUnderTest.nupkg to $RepositoryToUse"
            $Output.Lines | Should -Contain "An error has occurred. It's possible the package version already exists on the repository or a nuspec element is invalid. See error below..."
            $Output.String | Should -Match "Failed to process request. '"
            $Output.Lines | Should -Contain "The remote server returned an error: (409) Conflict.."
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}

Describe 'choco push nuget <_> repository' -Tag Chocolatey, PushCommand -Skip:($null -eq $env:NUGET_API_KEY -or $null -eq $env:NUGET_PUSH_REPO) -ForEach @('v2', 'v3') {
    BeforeDiscovery {
        $TestCases = @(
            @{ Wording = 'using config' ; UseConfig = $true }
            @{ Wording = 'using command line parameters' ; UseConfig = $false }
        )
    }

    BeforeAll {
        $RepositoryEndpoint = switch ($_) {
            'v2' { '' }
            'v3' { 'index.json' }
        }

        $ApiKey = $env:NUGET_API_KEY
        $RepositoryToUse = $env:NUGET_PUSH_REPO

        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Pushing package successfully <Wording>" -ForEach $TestCases {
        BeforeAll {
            $snapshotPath = New-ChocolateyInstallSnapshot
            $PackageUnderTest = "chocolatey-dummy-package"
            $TestPath = "$PSScriptRoot\testpackages"
            $VersionUnderTest = '1.0.0'
            $AddedVersion = "a$(((New-Guid) -split '-')[0])"
            $NewChocolateyTestPackage = @{
                TestPath     = $TestPath
                Name         = $PackageUnderTest
                Version      = $VersionUnderTest
                AddedVersion = $AddedVersion
            }
            $PackagePath = New-ChocolateyTestPackage @NewChocolateyTestPackage

            if ($UseConfig) {
                # TODO: These really should use the full parameter names
                $null = Invoke-Choco apikey -s $RepositoryToUse$RepositoryEndpoint -k $ApiKey
                # Ensure the key is null (should always be, but scoping can be wonky)
                $KeyParameter = $null
            } else {
                # PowerShell requires this for reasons that only PowerShell knows
                $KeyParameter = @("--api-key", $ApiKey)
            }

            $Output = Invoke-Choco push $PackagePath --source $RepositoryToUse$RepositoryEndpoint @KeyParameter
            $VerifyPackagesSplat = @(
                "find"
                "$PackageUnderTest"
                "--pre"
                "--source"
                "$RepositoryToUse$RepositoryEndpoint"
                "--api-key"
                "$ApiKey"
                "--version"
                "$VersionUnderTest-$AddedVersion"
            )
            $Packages = (Invoke-Choco @VerifyPackagesSplat --Limit-Output).Lines | ConvertFrom-ChocolateyOutput -Command List
        }

        AfterAll {
            $null = Invoke-Choco install nuget.commandline -y
            & "$env:ChocolateyInstall/bin/nuget.exe" delete $PackageUnderTest "$VersionUnderTest-$AddedVersion" -Source $RepositoryToUse -ApiKey $ApiKey -NonInteractive
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Successfully pushed the package to the repository' {
            $Packages | Should -Not -BeNullOrEmpty
        }
    }
}
