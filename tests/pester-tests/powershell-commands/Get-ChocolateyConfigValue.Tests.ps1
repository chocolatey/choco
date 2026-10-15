Describe 'Get-ChocolateyConfigValue helper function tests' -Tags Cmdlets, GetChocolateyConfigValue {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    Context 'Configuration file is present' {
        It 'gets the <Name> un-encrypted config value' -ForEach @(
            # Default config values
            @{ Name = 'commandExecutionTimeoutSeconds'; TestValue = "500" }
            @{ Name = 'logRetentionPolicyInDays'; TestValue = "99" }
            @{ Name = 'backgroundServiceAllowedCommands'; TestValue = "install,config" }
            @{ Name = 'upgradeAllExceptions'; TestValue = "testpackage" }
            @{ Name = 'webRequestTimeoutSeconds'; TestValue = "72" }
            # Nonstandard/custom config values
            @{ Name = 'testValue'; TestValue = "100" }
            @{ Name = 'testValue2'; TestValue = "test string" }

        ) {
            Invoke-Choco config set --name $Name --value $TestValue

            Get-ChocolateyConfigValue -Name $Name | Should -BeExactly $TestValue -Because 'the current config value should be retrieved'
        }

        It 'retrieves values by name, ignoring the casing of the name' -ForEach @(
            @{ Name = 'commandExecutionTimeoutSeconds'; TestValue = "500" }
            @{ Name = 'logRetentionPolicyInDays'; TestValue = "99" }
        ) {
            Invoke-Choco config set --name $Name --value $TestValue

            Get-ChocolateyConfigValue -Name $Name.ToLower() | Should -BeExactly $TestValue -Because 'the current config value should be retrieved even if the casing differs'
        }

        # Stuff with "password" in the config name which should get encrypted
        It 'retrieves the <Name> encrypted value' -TestCases @(
            @{ Name = 'proxyPassword'; TestValue = "supersecretpassword" }
            @{ Name = 'customPasswordValue'; TestValue = "totally not just password" }
            @{ Name = 'superSecretPassword'; TestValue = "wrong horse staple battery" }
        ) {
            Invoke-Choco config set --name $Name --value $TestValue

            $value = Get-ChocolateyConfigValue -Name $Name
            $value | Should -Not -BeExactly $TestValue -Because 'the config value should be encrypted'
            # As these values are encrypted, we won't know the exact value, but we can confirm it "looks" like encrypted data does.
            $value | Should -Match '([a-z0-9+]*/)+[a-z0-9+]+={0,2}' -Because 'the config value should match the pattern for encrypted data'
        }
    }

    Context 'Config file is missing' {
        BeforeAll {
            $configPath = "$env:ChocolateyInstall\config\chocolatey.config"
            Move-Item -Path $configPath -Destination "$configPath.test"
        }

        AfterAll {
            Move-Item -Path "$configPath.test" -Destination $configPath
        }

        It 'throws an error' {
            { Get-ChocolateyConfigValue -Name 'commandExecutionTimeoutSeconds' } | Should -Throw -ExceptionType 'System.IO.FileNotFoundException'
        }
    }
}
