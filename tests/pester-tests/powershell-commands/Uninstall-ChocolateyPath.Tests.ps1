Describe 'Uninstall-ChocolateyPath helper function tests' -Tags UninstallChocolateyPath, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    Context 'Unit tests' -Tags WhatIf -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        It 'removes a stored PATH value in the desired PATH scope' {
            $targetPathEntry = [Environment]::GetEnvironmentVariable('PATH', $Scope) -split ';' | Select-Object -First 1
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'")
            $Command = [scriptblock]::Create("Uninstall-ChocolateyPath -Path '$targetPathEntry' -Scope $Scope -WhatIf")
            
            $results = @( Get-WhatIfResult -Preamble $Preamble -Command $Command )
            $results[0] | Should -BeExactly "What if: Performing the operation ""Set $Scope environment variable"" on target ""PATH""."

            if ($Scope -ne 'Process') {
                $results[1] | Should -BeExactly 'What if: Performing the operation "Notify system of changes" on target "Environment variables".'
                $results[2] | Should -BeExactly 'What if: Performing the operation "Refresh all environment variables" on target "Current process".'
            }
        }

        It 'skips removing the value if it is not present' {
            $targetPathEntry = [Environment]::GetEnvironmentVariable('PATH', $Scope) -split ';' | Select-Object -First 1
            $Command = [scriptblock]::Create("Uninstall-ChocolateyPath -Path 'C:\ThisShouldNotBePresent' -Scope $Scope -WhatIf")
            Get-WhatIfResult -Preamble $Preamble -Command $Command | Should -BeNullOrEmpty -Because 'we should skip removing a value that does not exist'
        }
    }

    Context 'Adding and removing PATH values' -Tags VMOnly -ForEach @(
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

Describe 'Uninstall-ChocolateyPath end-to-end tests with add-path package' -Tags Cmdlet, UninstallChocolateyPath, VMOnly -ForEach @(
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