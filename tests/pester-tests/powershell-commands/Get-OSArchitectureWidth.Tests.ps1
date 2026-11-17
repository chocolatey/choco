Describe 'Get-OSArchitectureWidth helper function tests' -Tags Cmdlets, GetOSArchitectureWidth {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        $expectedWidth = if ([IntPtr]::Size -eq 4 -and -not $env:PROCESSOR_ARCHITEW6432) { 32 } else { 64 }
        if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64' -or $env:PROCESSOR_ARCHITEW6432 -eq 'ARM64') {
            $expectedWidth = 32
        }
        $otherWidth = if ($expectedWidth -eq 64) { 32 } else { 64 }
    }

    Context 'Command and aliases' {
        It 'Is a cmdlet' {
            (Get-Command Get-OSArchitectureWidth).CommandType | Should -Be 'Cmdlet'
        }

        It 'Exports an alias <Name>' -ForEach @(
            @{ Name = 'Get-ProcessorBits' }
            @{ Name = 'Get-OSBitness' }
        ) {
            (Get-Alias -Name $Name).Definition | Should -BeExactly 'Get-OSArchitectureWidth'
        }
    }

    Context 'Without a comparison' {
        It 'Returns the width as a number' {
            $width = Get-OSArchitectureWidth

            $width | Should -BeOfType [int]
            $width | Should -Be $expectedWidth
        }

        It 'Returns the width when the comparison is empty' -ForEach @(
            @{ Value = $null }
            @{ Value = '' }
        ) {
            Get-OSArchitectureWidth -Compare $Value | Should -Be $expectedWidth
        }
    }

    Context 'With a comparison' {
        It 'Accepts the comparison as a positional argument, as install scripts do' {
            Get-OSArchitectureWidth $expectedWidth | Should -BeTrue
            Get-OSArchitectureWidth $otherWidth | Should -BeFalse
        }

        It 'Accepts the comparison by the name <Name>' -ForEach @(
            @{ Name = 'Compare' }
            @{ Name = 'CompareTo' }
        ) {
            $arguments = @{ $Name = $expectedWidth }

            Get-OSArchitectureWidth @arguments | Should -BeTrue
        }

        It 'Compares a width given as text' {
            Get-OSArchitectureWidth "$expectedWidth" | Should -BeTrue
            Get-OSArchitectureWidth "$otherWidth" | Should -BeFalse
        }

        It 'Returns a Boolean' {
            Get-OSArchitectureWidth $expectedWidth | Should -BeOfType [bool]
        }

        It 'Can be used directly as a condition' {
            if (Get-OSArchitectureWidth $otherWidth) {
                throw 'The width should not match.'
            }

            if (-not (Get-OSArchitectureWidth $expectedWidth)) {
                throw 'The width should match.'
            }
        }
    }
}