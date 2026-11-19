Describe 'Install-ChocolateyExplorerMenuItem helper function tests' -Tags InstallChocolateyExplorerMenuItem, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        function Get-MenuValue([string] $Path) {
            $key = [Microsoft.Win32.Registry]::ClassesRoot.OpenSubKey($Path)
            if ($null -ne $key) {
                try {
                    $key.GetValue('')
                }
                finally {
                    $key.Dispose()
                }
            }
        }

        function Remove-MenuKey([string] $Path) {
            [Microsoft.Win32.Registry]::ClassesRoot.DeleteSubKeyTree($Path, $false)
        }
    }

    AfterAll {
        Remove-Module "chocolateyInstaller" -Force
    }

    Context 'Unit tests' -Tags WhatIf -ForEach @(
        @{ Type = 'File'; ItemKey = '*' }
        @{ Type = 'Directory'; ItemKey = 'directory' }
    ) {
        BeforeAll {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1' -Global")
            $Key = 'chocolatey-pester-whatif'
        }

        It 'Asks to create the <Type> menu item' {
            $Command = [scriptblock]::Create("Install-ChocolateyExplorerMenuItem -Key '$Key' -Label 'Pester' -Command 'C:\tool.exe' -Type $Type -WhatIf")

            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf | Should -HaveCount 1 -Because $results.Output
            $results.WhatIf[0] | Should -BeExactly "What if: Performing the operation ""Create explorer menu item"" on target ""HKEY_CLASSES_ROOT\$ItemKey\shell\$Key""." -Because $results.Output
        }

        It 'Does not create the <Type> menu item' {
            Get-MenuValue "$ItemKey\shell\$Key" | Should -BeNullOrEmpty
        }
    }

    Context 'Creating a <Type> menu item' -Tag VMOnly -ForEach @(
        @{ Type = 'File'; ItemKey = '*' }
        @{ Type = 'Directory'; ItemKey = 'directory' }
    ) {
        BeforeAll {
            $Key = "chocolatey-pester-$Type"
            Install-ChocolateyExplorerMenuItem -Key $Key -Label "Open with Pester's tool" -Command 'C:\Program Files\Pester\tool.exe' -Type $Type -WarningVariable warnings
        }

        AfterAll {
            Remove-MenuKey "$ItemKey\shell\$Key"
        }

        It 'Does not warn' {
            $warnings | Should -BeNullOrEmpty
        }

        It 'Sets the label' {
            Get-MenuValue "$ItemKey\shell\$Key" | Should -BeExactly "Open with Pester's tool"
        }

        It 'Sets the command with the quoted item path' {
            Get-MenuValue "$ItemKey\shell\$Key\command" | Should -BeExactly 'C:\Program Files\Pester\tool.exe "%1"'
        }
    }

    Context 'Calling with the previous parameter names' -Tag VMOnly {
        BeforeAll {
            $Key = 'chocolatey-pester-aliases'
        }

        AfterAll {
            Remove-MenuKey "*\shell\$Key"
            Remove-MenuKey "directory\shell\$Key"
        }

        It 'Accepts -MenuKey and -MenuLabel' {
            Install-ChocolateyExplorerMenuItem -MenuKey $Key -MenuLabel 'Named' -Command 'C:\tool.exe'

            Get-MenuValue "*\shell\$Key" | Should -BeExactly 'Named'
        }

        It 'Accepts positional arguments with a lower case type' {
            Install-ChocolateyExplorerMenuItem $Key 'Positional' 'C:\tool.exe' 'directory'

            Get-MenuValue "directory\shell\$Key" | Should -BeExactly 'Positional'
            Get-MenuValue "directory\shell\$Key\command" | Should -BeExactly 'C:\tool.exe "%1"'
        }
    }
}
