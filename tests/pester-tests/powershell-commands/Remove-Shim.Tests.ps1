Describe 'Remove-Shim helper function tests' -Tags RemoveShim, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        $binLocation = Join-Path $testLocation 'bin'
    }

    AfterAll {
        Remove-Module "chocolateyInstaller" -Force
    }

    Context 'Unit tests' -Tags WhatIf {
        BeforeAll {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1' -Global")
            $Name = 'shim-whatif'
            $files = @("$Name.bat", $Name, "$Name.exe") | ForEach-Object { Join-Path $binLocation $_ }
            $files | ForEach-Object { Set-Content -Path $_ -Value 'test' }
        }

        AfterAll {
            $files | Remove-Item -Force -ErrorAction Ignore
        }

        It 'Asks to remove each file' {
            $Command = [scriptblock]::Create("Remove-Shim -Name '$Name' -WhatIf")

            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf | Should -HaveCount 3 -Because $results.Output
            $results.WhatIf[0] | Should -BeExactly "What if: Performing the operation ""Remove batch file"" on target ""$($files[0])""." -Because $results.Output
            $results.WhatIf[1] | Should -BeExactly "What if: Performing the operation ""Remove bash file"" on target ""$($files[1])""." -Because $results.Output
            $results.WhatIf[2] | Should -BeExactly "What if: Performing the operation ""Remove shim"" on target ""$($files[2])""." -Because $results.Output
        }

        It 'Does not remove the files' {
            $files | Should -Exist
        }
    }

    Context 'Removing a shim and batch redirect files' {
        BeforeAll {
            $Name = 'shim-remove'
            $files = @("$Name.bat", $Name, "$Name.exe") | ForEach-Object { Join-Path $binLocation $_ }
            $files | ForEach-Object { Set-Content -Path $_ -Value 'test' }

            Remove-Shim -Name $Name
        }

        It 'Removes the <Kind>' -ForEach @(
            @{ Kind = 'batch file'; Extension = '.bat' }
            @{ Kind = 'bash file'; Extension = '' }
            @{ Kind = 'shim'; Extension = '.exe' }
        ) {
            Join-Path $binLocation "$Name$Extension" | Should -Not -Exist
        }
    }

    Context 'Removing a shim that does not exist' {
        It 'Does not fail' {
            { Remove-Shim -Name 'shim-missing' -Path 'C:\Other\Missing.exe' } | Should -Not -Throw
        }
    }

    Context 'Deprecated aliases' {
        It 'Can be called as <_>' -ForEach @('Remove-BinFile', 'Uninstall-BinFile') {
            (Get-Alias -Name $_).ResolvedCommandName | Should -Be 'Remove-Shim'
        }

        It 'Warns that <_> is deprecated' -ForEach @('Remove-BinFile', 'Uninstall-BinFile') {
            & $_ -Name 'shim-alias' -WarningVariable warnings -WarningAction SilentlyContinue

            $warnings | Should -HaveCount 1
            $warnings[0].Message | Should -BeExactly "The '$_' alias is deprecated and will be removed in a future version. Use 'Remove-Shim' to ensure compatibility with future versions of Chocolatey."
        }
    }
}
