Import-Module helpers/common-helpers

Describe "choco limit-output tests" -Tag Chocolatey, LimitOutputFeature {
    BeforeDiscovery {
        $Flags = @(
            '-r'
            '--limitoutput'
            '--limit-output'
        )
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context 'Running with flag <_> with wrong license files existing' -ForEach $Flags {
        BeforeAll {
            $paths = Restore-ChocolateyInstallSnapshot
            New-Item -Path "$($paths.InstallPath)\license" -ItemType Directory
            Set-Content -Path "$($paths.InstallPath)\license\test.xml" -Value ""

            $Output = Invoke-Choco help $_
        }

        It "Should not output Files not found in license directory." {
            [array]$foundLine = $Output.Lines | Where-Object { $_ -match "Files found in directory|valid license file. License should be named"}
            $foundLine.Count | Should -Be 0 -Because $Output.String
        }
    }

    Context 'Running with <_> with license file in wrong location' -ForEach $Flags {
        BeforeAll {
            $paths = Restore-ChocolateyInstallSnapshot
            Set-Content -Path "$($paths.InstallPath)\chocolatey.license.xml" -Value ""

            $Output = Invoke-Choco help $_
        }

        It "Should not output license file being in wrong location" {
            $Output.Lines | Should -Not -Contain "Chocolatey license found in the wrong location. File must be located at" -Because $Output.String
        }
    }

    Context 'Running with <_> with invalid license file in correct location' -ForEach $Flags {
        BeforeAll {
            $paths = Restore-ChocolateyInstallSnapshot
            New-Item -Path "$($paths.InstallPath)\license" -ItemType Directory
            Set-Content -Path "$($paths.InstallPath)\license\chocolatey.license.xml" -Value ""

            $Output = Invoke-Choco help $_
        }

        It "Should not output license being invalid" {
            $Output.Lines | Should -Not -Contain "A license was found for a licensed version of Chocolatey, but is invalid:"
            $Output.Lines | Should -Not -Contain "Could not validate existing license"
        }
    }
}