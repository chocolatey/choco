Describe 'Get-OSArchitectureWidth helper function tests' -Tags Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    It 'Returns a width of 32 or 64' {
        Get-OSArchitectureWidth | Should -BeIn @(32, 64)
    }

    It 'Matches the bit-width of the current process' {
        # The width reflects the current process, which is 64 whether Chocolatey runs
        # natively as ARM64 or under x64 emulation on Windows on ARM.
        $expected = 64
        if ([System.IntPtr]::Size -eq 4 -and -not (Test-Path env:\PROCESSOR_ARCHITEW6432)) {
            $expected = 32
        }
        Get-OSArchitectureWidth | Should -Be $expected
    }

    It 'Returns 64 on a 64-bit operating system (including Windows on ARM)' -Skip:(-not [System.Environment]::Is64BitOperatingSystem) {
        Get-OSArchitectureWidth | Should -Be 64
    }

    Context 'When a comparison value is supplied' {
        BeforeAll {
            $width = Get-OSArchitectureWidth
            $otherWidth = if ($width -eq 32) { 64 } else { 32 }
        }

        It 'Returns true when the comparison matches the width' {
            Get-OSArchitectureWidth -Compare $width | Should -BeTrue
        }

        It 'Returns false when the comparison does not match the width' {
            Get-OSArchitectureWidth -Compare $otherWidth | Should -BeFalse
        }
    }

    It 'Is aliased as Get-ProcessorBits' {
        Get-ProcessorBits | Should -Be (Get-OSArchitectureWidth)
    }
}
