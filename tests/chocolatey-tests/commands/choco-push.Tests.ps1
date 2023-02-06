Import-Module helpers/common-helpers

Describe "choco push" -Tag Chocolatey, PushCommand -Skip:($null -eq $env:API_KEY -or $null -eq $env:PUSH_REPO) {
    BeforeAll {
        Remove-NuGetPaths
        $ApiKey = $env:API_KEY
        $RepositoryToUse = $env:PUSH_REPO
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
            $Output.Lines | Should -Contain "Response status code does not indicate success: 409 (Conflict)."
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
            $Output.Lines | Should -Contain "Response status code does not indicate success: 409 (Conflict)."
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
            $Output.Lines | Should -Contain "Response status code does not indicate success: 409 (Conflict)."
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}

Describe 'choco push nuget <_> repository' -Tag Chocolatey, PushCommand -Skip:($null -eq $env:NUGET_SOURCE_USERNAME -or $null -eq $env:NUGET_SOURCE_PASSWORD -or $null -eq $env:NUGET_API_KEY -or $null -eq $env:NUGET_PUSH_REPO) -ForEach @('v2', 'v3') {
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
                "--pre"
                "--source"
                "$RepositoryToUse$RepositoryEndpoint"
                "--user"
                $env:NUGET_SOURCE_USERNAME
                "--password"
                $env:NUGET_SOURCE_PASSWORD
                "--version"
                "$VersionUnderTest-$AddedVersion"
            )

            # Nexus can take a moment to index the package, but we want to validate that it was successfully pushed
            $Timer =  [System.Diagnostics.Stopwatch]::StartNew()
            while ($Timer.Elapsed.TotalSeconds -lt 300 -and -not (
                $Packages = (Invoke-Choco find $PackageUnderTest @VerifyPackagesSplat --Limit-Output).Lines | ConvertFrom-ChocolateyOutput -Command List
            )) {
                Write-Verbose "$($PackageUnderTest) was not found on $($RepositoryToUse)$($RepositoryEndpoint). Waiting for 5 seconds before trying again."
                Start-Sleep -Seconds 5
            }
        }

        AfterAll {
            if ($Packages) {
                $null = Invoke-Choco install nuget.commandline -y
                & "$env:ChocolateyInstall/bin/nuget.exe" delete $PackageUnderTest "$VersionUnderTest-$AddedVersion" -Source $RepositoryToUse -ApiKey $ApiKey -NonInteractive
            }
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It 'Successfully pushed the package to the repository' {
            $Packages | Should -Not -BeNullOrEmpty -Because "Package $PackageUnderTest with version '$VersionUnderTest-$AddedVersion' should be found on $RepositoryToUse$RepositoryEndpoint"
        }
    }
}
