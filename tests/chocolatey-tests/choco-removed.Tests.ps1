Import-Module helpers/common-helpers

Describe "Ensuring removed things are removed" -Tag Removed, Chocolatey {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        # Ensure that we do not have any compatibility layer package installed
        $null = Invoke-Choco uninstall chocolatey-compatibility.extension -y --force
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context 'Helper function (<FunctionName>)' -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0')) -Foreach @(
        @{ FunctionName = 'Write-FileUpdateLog' }
        @{ FunctionName = 'Write-ChocolateySuccess' }
        @{ FunctionName = 'Write-ChocolateyFailure' }
        @{ FunctionName = 'Install-ChocolateyDesktopLink' }
    ) {
        BeforeAll {
            $TestScript = @'
$packageName = $env:ChocolateyPackageName
Write-Host "Checking for command $packageName"
$command = Get-Command $packageName -All
Write-Host "Found: $($command.Count)"
exit $command.Count
'@
            $snapshotPath = New-ChocolateyInstallSnapshot
            Push-Location $snapshotPath.PackagesPath
            $null = Invoke-Choco new "$FunctionName" --version 1.0.0
            $TestScript > $FunctionName/tools/chocolateyInstall.ps1
            $null = Invoke-Choco pack "$FunctionName/$FunctionName.nuspec"
            $Output = Invoke-Choco install "$FunctionName" --source $snapshotPath.PackagesPath
            Pop-Location
        }

        It 'Exits with Success (0)' {
            $Output.ExitCode | Should -Be 0
        }

        It 'Reports the correct command name' {
            $Output.Lines | Should -Contain "Checking for command $FunctionName" -Because "Expected output to contain information: $($Output.String)"
        }

        It 'Reports the correct number found (0)' {
            $Output.Lines | Should -Contain "Found: 0" -Because "Expected output to contain information: $($Output.String)"
        }
    }

    Context 'Ensure -t removed from push commands help message' -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0')) {
        BeforeAll {
            $Output = Invoke-Choco push -?
        }

        It 'No longer reports the -t flag' {
            $Output.Lines | Should -Not -Contain '\s-t=VALUE'
        }
    }

    Context 'Ensure choco push apikey fallback removal' -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0')) {
        BeforeAll {
            $null = Invoke-Choco new testPackage --version 1.0.0
            $null = Invoke-Choco pack testPackage/testPackage.nuspec
            $null = Invoke-Choco apikey add -s https://chocolatey.org -k None
            $Output = Invoke-Choco push ./testPackage.1.0.0.nupkg
        }

        It 'Exits with Failure (1)' {
            $Output.ExitCode | Should -Be 1
        }

        It 'Reports missing API key' {
            $Output.Lines | Should -Contain "An ApiKey was not found for 'https://push.chocolatey.org/'. You must either set an api key in the configuration or specify one with --api-key."
        }
    }

    Context 'Ensure <Command> removed from Chocolatey' -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0')) -Foreach @(
        @{ Command = 'update' }
        @{ Command = 'version' }
    ) {
        BeforeAll {
            $Output = Invoke-Choco $Command
        }

        It 'Exits with Failure (1)' {
            $Output.ExitCode | Should -Be 1
        }

        It "Reports that the command doesn't exist" {
            $Output.Lines | Should -Contain "Could not find a command registered that meets '$Command'."
        }
    }

    It 'Does not have shim (<Name>) created' -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan '1.0.0')) -ForEach @(
        @{ Name = 'cver' }
        @{ Name = 'cpack' }
    ) {
        Get-ChildItem -Path $env:ChocolateyInstall -Name "$Name.exe" -Recurse -ErrorAction SilentlyContinue | Should -HaveCount 0
    }
}
