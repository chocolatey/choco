Describe 'Install-ChocolateyPath helper function tests' -Tags InstallChocolateyPath, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    AfterAll {
        Remove-Module "chocolateyInstaller" -Force
    }

    Context 'Unit tests' -Tags WhatIf -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        BeforeAll {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1' -Global")
        }

        It 'stores the value <_> in the desired PATH scope' -TestCases @("C:\test", "C:\tools") {
            $Command = [scriptblock]::Create("Install-ChocolateyPath -Path '$_' -Scope $Scope -WhatIf")
            
            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf[0] | Should -BeExactly "What if: Performing the operation ""Set $Scope environment variable"" on target ""PATH""." -Because $results.Output

            if ($Scope -ne 'Process') {
                $results.WhatIf[1] | Should -BeExactly 'What if: Performing the operation "Notify system of changes" on target "Environment variables".' -Because $results.Output
                $results.WhatIf[2] | Should -BeExactly 'What if: Performing the operation "Refresh all environment variables" on target "Current process".' -Because $results.Output
            }
        }

        It 'skips adding the value if it is already present' {
            $targetPathEntry = [Environment]::GetEnvironmentVariable('PATH', $Scope) -split ';' | Select-Object -Last 1
            $Command = [scriptblock]::Create("Install-ChocolateyPath -Path '$targetPathEntry' -Scope $Scope -WhatIf")
            $result = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $result.WhatIf | Should -BeNullOrEmpty -Because "we should skip adding values that already exist. Output:`n$($result.Output)"
        }
    }

    Context 'Adding and removing PATH values' -Tag VMOnly -ForEach @(
        @{ Scope = 'Process' }
        @{ Scope = 'User' }
        @{ Scope = 'Machine' }
    ) {
        Context 'Path "<_>"' -ForEach @("C:\test", "C:\tools") {
            AfterEach {
                Uninstall-ChocolateyPath -Path $_ -Scope $Scope
            }

            It 'stores the value in the desired PATH scope' {
                Install-ChocolateyPath -Path $_ -Scope $Scope
                [Environment]::GetEnvironmentVariable('PATH', $Scope) -split [IO.Path]::PathSeparator | Should -Contain $_
            }
        }
    }

    Context 'Edge cases' {
        AfterEach {
            Uninstall-ChocolateyPath -Path "C:\test" -Scope Process
        }

        It 'successfully detects that a path is already present when it is missing a trailing slash that is present in PATH' {
            Install-ChocolateyPath -Path "C:\test\" -Scope Process
            Install-ChocolateyPath -Path "C:\test" -Scope Process

            @($env:PATH -split [IO.Path]::PathSeparator) -match "C:\\test" | Should -HaveCount 1 -Because "Install-ChocolateyPath should not add the same path more than once"
        }

        It 'successfully detects that a path is already present when it has a trailing slash that is not present in PATH' {
            Install-ChocolateyPath -Path "C:\test" -Scope Process
            Install-ChocolateyPath -Path "C:\test\" -Scope Process

            @($env:PATH -split [IO.Path]::PathSeparator) -match "C:\\test" | Should -HaveCount 1 -Because "Install-ChocolateyPath should not add the same path more than once"
        }

        It 'allows a subpath of a path already in PATH to be added' {
            Install-ChocolateyPath -Path "C:\test" -Scope Process
            Install-ChocolateyPath -Path "C:\test\subpath" -Scope Process

            @($env:PATH -split [IO.Path]::PathSeparator) | Should -Contain "C:\test\subpath"
        }
    }
}

Describe 'Install-ChocolateyPath end-to-end tests with add-path package modifying <Scope> PATH' -Tags Cmdlet, UninstallChocolateyPath, VMOnly -ForEach @(
    @{ Scope = 'User' }
    @{ Scope = 'Machine' }
) {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        $PackageUnderTest = 'add-path'
        Restore-ChocolateyInstallSnapshot
        $OriginalPath = [Environment]::GetEnvironmentVariable('PATH', $Scope)
        $Output = Invoke-Choco install $PackageUnderTest --confirm --params "/Path=C:\test /Scope=$Scope"
    }

    AfterAll {
        Remove-ChocolateyInstallSnapshot
        [Environment]::SetEnvironmentVariable('PATH', $OriginalPath, $Scope)
    }

    It 'Exits with Success (0)' {
        $Output.ExitCode | Should -Be 0 -Because $Output.String
    }

    It 'Shows the path being added and the scope' {
        $Output.String | Should -MatchExactly "Adding 'C:\\test' to PATH at scope $Scope"
    }

    It 'Correctly modifies the PATH' {
        [Environment]::GetEnvironmentVariable("PATH", $Scope) | Should -Match "C:\\test"
    }
}