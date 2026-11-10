Describe 'Get-OSArchitecture helper function tests' -Tags Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    It 'Returns one of the known architecture values' {
        Get-OSArchitecture | Should -BeIn @('x86', 'x64', 'arm64')
    }

    It 'Returns the value in lower case' {
        Get-OSArchitecture | Should -MatchExactly '^(x86|x64|arm64)$'
    }

    It 'Reports the native processor architecture' {
        # Determine the native architecture independently of the function under test, so
        # the expectation is correct on both x64 and Windows on ARM (arm64) runners. WMI
        # reports the native architecture even when the process is running under emulation.
        $osArchitecture = (Get-WmiObject -Class Win32_OperatingSystem).OSArchitecture
        $expectedArchitecture = if ($osArchitecture -match 'ARM') {
            'arm64'
        }
        elseif ($osArchitecture -match '64') {
            'x64'
        }
        else {
            'x86'
        }

        Get-OSArchitecture | Should -Be $expectedArchitecture
    }

    It 'Is aliased as Get-ProcessorArchitecture' {
        Get-ProcessorArchitecture | Should -Be (Get-OSArchitecture)
    }
}
