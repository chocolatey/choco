Describe 'New-Shim helper function tests' -Tags NewShim, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        $binLocation = Join-Path $testLocation 'bin'
        $packageTools = Join-Path $testLocation 'lib\shim-tests\tools'
        $null = New-Item -Path $packageTools -ItemType Directory -Force
        $target = Join-Path $packageTools 'whoami.exe'
        Copy-Item -Path "$env:SystemRoot\System32\whoami.exe" -Destination $target
        $expectedOutput = & "$env:SystemRoot\System32\whoami.exe"
    }

    AfterAll {
        Remove-Item -Path (Join-Path $testLocation 'lib\shim-tests') -Recurse -Force -ErrorAction Ignore
        Remove-Module "chocolateyInstaller" -Force
    }

    Context 'Unit tests' -Tags WhatIf {
        BeforeAll {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1' -Global")
        }

        It 'Asks to create the shim' {
            $Command = [scriptblock]::Create("New-Shim -Name 'shim-whatif' -Path '$target' -WhatIf")

            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf | Should -HaveCount 1 -Because $results.Output
            $results.WhatIf[0] | Should -BeExactly "What if: Performing the operation ""Create shim"" on target ""$binLocation\shim-whatif.exe""." -Because $results.Output
        }

        It 'Does not create the shim' {
            Join-Path $binLocation 'shim-whatif.exe' | Should -Not -Exist
        }
    }

    Context 'Creating a shim for a file <Description>' -ForEach @(
        @{ Description = 'in the install folder'; Name = 'shim-relative'; InInstallFolder = $true }
        @{ Description = 'outside the install folder'; Name = 'shim-absolute'; InInstallFolder = $false }
    ) {
        BeforeAll {
            $path = if ($InInstallFolder) { $target } else { "$env:SystemRoot\System32\whoami.exe" }
            New-Shim -Name $Name -Path $path
        }

        AfterAll {
            Remove-Shim -Name $Name
        }

        It 'Creates the shim' {
            Join-Path $binLocation "$Name.exe" | Should -Exist
        }

        It 'Does not create the batch redirect files' {
            Join-Path $binLocation "$Name.bat" | Should -Not -Exist
            Join-Path $binLocation $Name | Should -Not -Exist
        }

        It 'Runs the target file' {
            & (Join-Path $binLocation "$Name.exe") | Should -Be $expectedOutput
        }
    }

    Context 'Replacing existing batch redirect files' {
        BeforeAll {
            $Name = 'shim-replace'
            Set-Content -Path (Join-Path $binLocation "$Name.bat") -Value 'old batch'
            Set-Content -Path (Join-Path $binLocation $Name) -Value 'old bash'

            New-Shim -Name $Name -Path $target
        }

        AfterAll {
            Remove-Shim -Name $Name
        }

        It 'Removes the batch redirect files' {
            Join-Path $binLocation "$Name.bat" | Should -Not -Exist
            Join-Path $binLocation $Name | Should -Not -Exist
        }

        It 'Creates the shim' {
            Join-Path $binLocation "$Name.exe" | Should -Exist
        }
    }

    Context 'Deprecated aliases' {
        It 'Can be called as <_>' -ForEach @('Generate-BinFile', 'Add-BinFile', 'Install-BinFile') {
            (Get-Alias -Name $_).ResolvedCommandName | Should -Be 'New-Shim'
        }

        It 'Warns that <_> is deprecated' -ForEach @('Generate-BinFile', 'Add-BinFile', 'Install-BinFile') {
            & $_ -Name 'shim-alias' -Path $target -WhatIf -WarningVariable warnings -WarningAction SilentlyContinue

            $warnings | Should -HaveCount 1
            $warnings[0].Message | Should -BeExactly "The '$_' alias is deprecated and will be removed in a future version. Use 'New-Shim' to ensure compatibility with future versions of Chocolatey."
        }
    }
}
