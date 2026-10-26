Describe 'Get-PackageParameters helper function tests' -Tags Cmdlets, GetPackageParameters {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        $modulePath = "$testLocation\helpers\chocolateyInstaller.psm1"
        Import-Module $modulePath

        # The tests set the variables they need, so the ones from the machine running them are put aside.
        $managedVariables = 'ChocolateyPackageParameters', 'ChocolateyPackageParametersSensitive'
        $originalValues = @{}
        foreach ($name in $managedVariables) {
            $originalValues[$name] = [Environment]::GetEnvironmentVariable($name)
        }

        function Get-DebugMessages {
            param([scriptblock]$Script)

            & {
                $DebugPreference = 'Continue'
                & $Script
            } 5>&1 |
                Where-Object { $_ -is [System.Management.Automation.DebugRecord] } |
                ForEach-Object { $_.Message }
        }
    }

    AfterAll {
        foreach ($name in $managedVariables) {
            [Environment]::SetEnvironmentVariable($name, $originalValues[$name])
        }
    }

    BeforeEach {
        foreach ($name in $managedVariables) {
            [Environment]::SetEnvironmentVariable($name, $null)
        }
    }

    Context 'Command' {
        It 'Is a cmdlet' {
            (Get-Command Get-PackageParameters -CommandType Cmdlet -ErrorAction Stop).CommandType | Should -Be 'Cmdlet'
        }

        It 'Accepts the parameters as a positional argument' {
            $result = Get-PackageParameters '/Positional:1'

            $result['Positional'] | Should -BeExactly '1'
        }

        It 'Accepts the previous name of the parameter, -params' {
            $result = Get-PackageParameters -params '/Alias:1'

            $result['Alias'] | Should -BeExactly '1'
        }

        It 'Accepts arguments that do not apply' {
            { Get-PackageParameters -Parameters '/A:1' -Unrelated 'value' -ErrorAction Stop } | Should -Not -Throw
        }
    }

    Context 'Previous names and overrides' {
        AfterEach {
            Remove-Item -Path Function:\Get-PackageParameters -ErrorAction SilentlyContinue
        }

        It 'Can still be called by its previous name, Get-PackageParametersBuiltIn' {
            $result = Get-PackageParametersBuiltIn -Parameters '/BuiltIn:1'

            $result['BuiltIn'] | Should -BeExactly '1'
        }

        It 'Has the previous name, Get-PackageParametersBuiltIn, as an alias for the command' {
            $alias = Get-Alias -Name 'Get-PackageParametersBuiltIn'

            $alias.Definition | Should -BeExactly 'Chocolatey.PowerShell\Get-PackageParameters'
            $alias.ResolvedCommand.CommandType | Should -Be 'Cmdlet'
        }

        It 'Still reaches the command when a function of the same name is defined' {
            function global:Get-PackageParameters { 'the function from an extension' }

            $result = Get-PackageParameters -Parameters '/FromCommand:1'

            $result | Should -BeOfType [hashtable]
            $result['FromCommand'] | Should -BeExactly '1'
        }

        It 'Still reaches the command from inside a script when a function of the same name is defined' {
            function global:Get-PackageParameters { 'the function from an extension' }
            $script = Join-Path $TestDrive 'package-script.ps1'
            Set-Content -Path $script -Value '(Get-PackageParameters -Parameters "/FromScript:1")["FromScript"]'

            & $script | Should -BeExactly '1'
        }

        It 'Reads the sensitive environment variable even when a function of the same name is defined' {
            function global:Get-PackageParameters { 'the function from an extension' }
            $env:ChocolateyPackageParametersSensitive = '/Password:s3cret'

            (Get-PackageParameters)['Password'] | Should -BeExactly 's3cret'
        }
    }

    Context 'Parsing a string that is given' {
        BeforeAll {
            $result = @(Get-PackageParameters -Parameters "/Shortcut /InstallDir:'c:\program files\xyz' /NoStartup /Count=3 /Label=`"two words`"")
        }

        It 'Writes one hashtable, not its entries' {
            $result.Count | Should -Be 1
            $result[0] | Should -BeOfType [hashtable]
        }

        It 'Gives a parameter without a value the boolean true' {
            $result[0]['Shortcut'] | Should -BeOfType [bool]
            $result[0]['Shortcut'] | Should -BeTrue
            $result[0]['NoStartup'] | Should -BeTrue
        }

        It 'Gives a value as a string, without the quotes round it' {
            $result[0]['InstallDir'] | Should -BeExactly 'c:\program files\xyz'
            $result[0]['Label'] | Should -BeExactly 'two words'
        }

        It 'Splits a name from its value at a colon or an equals sign' {
            $result[0]['Count'] | Should -BeExactly '3'
        }

        It 'Makes the parameters available as properties' {
            $result[0].InstallDir | Should -BeExactly 'c:\program files\xyz'
        }

        It 'Looks up names without regard to case' {
            $result[0]['installdir'] | Should -BeExactly 'c:\program files\xyz'
            $result[0].ContainsKey('NOSTARTUP') | Should -BeTrue
        }

        It 'Has no other entries' {
            $result[0].Count | Should -Be 5
        }
    }

    Context 'Parsing values' {
        It 'Keeps a value that has a colon and equals signs in it' {
            (Get-PackageParameters -Parameters '/Conn:Server=x;Db=y /Next')['Conn'] | Should -BeExactly 'Server=x;Db=y'
        }

        It 'Keeps a value with spaces that is not quoted, up to the next parameter' {
            $result = Get-PackageParameters -Parameters '/A:one two /B'

            $result['A'] | Should -BeExactly 'one two'
            $result['B'] | Should -BeTrue
        }

        It 'Keeps a Windows path that ends with a backslash' {
            (Get-PackageParameters -Parameters '/Dir:C:\x\ /Next')['Dir'] | Should -BeExactly 'C:\x\'
        }

        It 'Gives true for an empty value' {
            $result = Get-PackageParameters -Parameters "/A: /B= /C:'' "

            $result['A'] | Should -BeTrue
            $result['B'] | Should -BeTrue
            $result['C'] | Should -BeTrue
        }

        It 'Keeps zero and false as the text they are' {
            $result = Get-PackageParameters -Parameters '/A:0 /B:false'

            $result['A'] | Should -BeExactly '0'
            $result['B'] | Should -BeExactly 'false'
        }

        It 'Keeps the last value when a name is given twice, whatever the case' {
            $result = Get-PackageParameters -Parameters '/Key:first /KEY:second'

            $result.Count | Should -Be 1
            $result['key'] | Should -BeExactly 'second'
        }

        It 'Returns nothing in the table for text with no parameters' {
            (Get-PackageParameters -Parameters 'no slash here').Count | Should -Be 0
        }

        It 'Reads names and values that are not ASCII' {
            $result = Get-PackageParameters -Parameters "/Name:Zo$([char]0xEB) /$([char]0x41A)$([char]0x43B):$([char]0x437)"

            $result['Name'] | Should -BeExactly "Zo$([char]0xEB)"
            $result["$([char]0x41A)$([char]0x43B)"] | Should -BeExactly "$([char]0x437)"
        }
    }

    Context 'Using the environment variables' {
        It 'Reads the package parameters from ChocolateyPackageParameters' {
            $env:ChocolateyPackageParameters = '/A:1 /B:2'

            $result = Get-PackageParameters

            $result.Count | Should -Be 2
            $result['A'] | Should -BeExactly '1'
            $result['B'] | Should -BeExactly '2'
        }

        It 'Reads the sensitive parameters from ChocolateyPackageParametersSensitive' {
            $env:ChocolateyPackageParametersSensitive = '/Password:s3cret'

            (Get-PackageParameters)['Password'] | Should -BeExactly 's3cret'
        }

        It 'Merges the two, and the sensitive value wins' {
            $env:ChocolateyPackageParameters = '/A:1 /B:2'
            $env:ChocolateyPackageParametersSensitive = '/B:secret /C'

            $result = Get-PackageParameters

            $result.Count | Should -Be 3
            $result['A'] | Should -BeExactly '1'
            $result['B'] | Should -BeExactly 'secret'
            $result['C'] | Should -BeTrue
        }

        It 'Writes an empty hashtable when neither is set' {
            $result = @(Get-PackageParameters)

            $result.Count | Should -Be 1
            $result[0] | Should -BeOfType [hashtable]
            $result[0].Count | Should -Be 0
        }

        It 'Uses the environment variables when the given text is an empty string' {
            $env:ChocolateyPackageParameters = '/FromEnvironment:1'

            (Get-PackageParameters -Parameters '')['FromEnvironment'] | Should -BeExactly '1'
        }

        It 'Ignores the environment variables when text is given' {
            $env:ChocolateyPackageParameters = '/FromEnvironment:1'
            $env:ChocolateyPackageParametersSensitive = '/AlsoFromEnvironment:1'

            $result = Get-PackageParameters -Parameters '/Given:1'

            $result.Count | Should -Be 1
            $result['Given'] | Should -BeExactly '1'
        }

        It 'Ignores the environment variables when the given text is only whitespace' {
            $env:ChocolateyPackageParameters = '/FromEnvironment:1'

            (Get-PackageParameters -Parameters '   ').Count | Should -Be 0
        }
    }

    Context 'Logging' {
        It 'Says where the parameters are read from, and logs each one' {
            $env:ChocolateyPackageParameters = '/A:1 /B'

            $messages = Get-DebugMessages { $null = Get-PackageParameters }

            $messages | Should -Contain 'Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters'
            $messages | Should -Contain "Adding package param 'A'='1'"
            $messages | Should -Contain "Adding package param 'B'='True'"
        }

        It 'Logs each parameter that is given' {
            $messages = Get-DebugMessages { $null = Get-PackageParameters -Parameters '/A:1' }

            $messages | Should -Contain "Adding package param 'A'='1'"
            $messages | Should -Not -Contain 'Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters'
        }

        It 'Does not log any parameter when there are sensitive parameters, and says so' {
            $env:ChocolateyPackageParameters = '/A:1'
            $env:ChocolateyPackageParametersSensitive = '/Password:s3cret'

            $messages = Get-DebugMessages { $null = Get-PackageParameters }

            $messages | Should -Contain 'Sensitive parameters detected, no logging of parameters.'
            $messages | Where-Object { $_ -like 'Adding package param*' } | Should -BeNullOrEmpty
        }

        It 'Does not write a sensitive value to the debug stream' {
            $env:ChocolateyPackageParametersSensitive = '/Password:s3cret'

            $messages = Get-DebugMessages { $null = Get-PackageParameters }

            ($messages -join "`n") | Should -Not -Match 's3cret'
        }

        It 'Writes the call to the debug stream' {
            $messages = Get-DebugMessages { $null = Get-PackageParameters -Parameters '/A:1' }

            $messages | Should -Contain "Running Get-PackageParameters -Parameters '/A:1'"
        }
    }
}
