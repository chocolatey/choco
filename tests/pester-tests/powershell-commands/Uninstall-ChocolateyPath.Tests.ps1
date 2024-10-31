Describe 'Uninstall-ChocolateyPath helper function tests' -Tags Cmdlets, UninstallChocolateyPath {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    Context 'Adding and removing PATH values' -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        Context 'Path "<_>"' -ForEach @("C:\test", "C:\tools") {
            BeforeEach {
                Install-ChocolateyPath -Path $_ -Scope $Scope
            }

            It 'removes a stored PATH value in the desired PATH scope' {
                Uninstall-ChocolateyPath -Path $_ -Scope $Scope
                [Environment]::GetEnvironmentVariable('PATH', $Scope) -split [IO.Path]::PathSeparator | Should -Not -Contain $_
            }
        }
    }

    Context 'Edge cases' {
        It 'successfully detects that a path is present and removes it when it is missing a trailing slash that is present in PATH' {
            Install-ChocolateyPath -Path "C:\test\" -Scope Process
            Uninstall-ChocolateyPath -Path "C:\test" -Scope Process

            @($env:PATH -split [IO.Path]::PathSeparator) -match "C:\\test" | Should -BeNullOrEmpty
        }

        It 'successfully detects that a path is present and removes it when it has a trailing slash that is not present in PATH' {
            Install-ChocolateyPath -Path "C:\test" -Scope Process
            Uninstall-ChocolateyPath -Path "C:\test\" -Scope Process

            @($env:PATH -split [IO.Path]::PathSeparator) -match "C:\\test" | Should -BeNullOrEmpty
        }
    }
}

Describe 'Uninstall-ChocolateyPath end-to-end tests with add-path package' -Tags Cmdlet, UninstallChocolateyPath -ForEach @(
    @{ Scope = 'User' }
    @{ Scope = 'Machine' }
) {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        $PackageUnderTest = 'add-path'
        Restore-ChocolateyInstallSnapshot
        $OriginalPath = [Environment]::GetEnvironmentVariable('PATH', $Scope)
        $install = Invoke-Choco install $PackageUnderTest --confirm --params "/Path=C:\test /Scope=$Scope"

        if ($install.ExitCode -ne 0) {
            throw "Setup failed, could not install ${PackageUnderTest}: $($install.String)"
        }

        $Output = Invoke-Choco uninstall $PackageUnderTest --confirm --params "/Path=C:\test /Scope=$Scope"
    }

    AfterAll {
        Remove-ChocolateyInstallSnapshot
        [Environment]::SetEnvironmentVariable('PATH', $OriginalPath, $Scope)
    }

    It 'Exits with Success (0)' {
        $Output.ExitCode | Should -Be 0 -Because $Output.String
    }

    It 'Shows the path being added and the scope' {
        $Output.String | Should -MatchExactly "Removing 'C:\\test' from PATH at scope $Scope"
    }

    It 'Correctly modifies the PATH' {
        [Environment]::GetEnvironmentVariable("PATH", $Scope) | Should -BeExactly $OriginalPath
    }
}