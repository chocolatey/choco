Describe 'Get-UninstallRegistryKey helper function tests' -Tags Cmdlets, GetUninstallRegistryKey {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        # Keys made for these tests in the user's uninstall location, with names no installed software has. They are
        # removed afterwards.
        $uninstallLocation = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall'
        $testId = [guid]::NewGuid().ToString('N')
        $displayName = "Chocolatey Pester Test $testId"
        $keyName = "ChocolateyPesterTest-$testId"
        $emptyKeyName = "ChocolateyPesterTestEmpty-$testId"
        $testKey = Join-Path $uninstallLocation $keyName
        $emptyKey = Join-Path $uninstallLocation $emptyKeyName

        New-Item -Path $testKey -Force > $null
        New-ItemProperty -Path $testKey -Name DisplayName -Value $displayName > $null
        New-ItemProperty -Path $testKey -Name UninstallString -Value 'C:\test\uninstall.exe' > $null
        New-Item -Path $emptyKey -Force > $null
    }

    AfterAll {
        Remove-Item -Path $testKey, $emptyKey -Recurse -Force -ErrorAction SilentlyContinue
    }

    It 'Is a cmdlet' {
        (Get-Command Get-UninstallRegistryKey).CommandType | Should -Be 'Cmdlet'
    }

    It 'Finds a key by its display name' {
        $result = @(Get-UninstallRegistryKey -SoftwareName $displayName)

        $result | Should -HaveCount 1
        $result[0].DisplayName | Should -BeExactly $displayName
        $result[0].UninstallString | Should -BeExactly 'C:\test\uninstall.exe'
        $result[0].PSChildName | Should -BeExactly $keyName
        $result[0].PSObject.TypeNames[0] | Should -Be 'System.Management.Automation.PSCustomObject'
    }

    It 'Ignores case' {
        $result = @(Get-UninstallRegistryKey -SoftwareName $displayName.ToUpperInvariant())

        $result.PSChildName | Should -Be @($keyName)
    }

    It 'Accepts wildcards' {
        $result = @(Get-UninstallRegistryKey -SoftwareName "Chocolatey Pester T?st *$testId")

        $result.PSChildName | Should -Be @($keyName)
    }

    It 'Takes the name by position and from the pipeline' {
        @(Get-UninstallRegistryKey $displayName).PSChildName | Should -Be @($keyName)
        @($displayName | Get-UninstallRegistryKey).PSChildName | Should -Be @($keyName)
    }

    It 'Searches for each name from the pipeline' {
        $result = @("Missing $testId", $displayName | Get-UninstallRegistryKey -WarningVariable warnings 3> $null)

        $result.PSChildName | Should -Be @($keyName)
        $warnings | Should -HaveCount 1
        "$($warnings[0])" | Should -BeExactly "No registry key found based on 'Missing $testId'"
    }

    It 'Does not return a key that has no values' {
        $names = @(Get-UninstallRegistryKey -SoftwareName '*').PSChildName

        $names | Should -Contain $keyName
        $names | Should -Not -Contain $emptyKeyName
    }

    It 'Warns and returns nothing when no key matches' {
        $result = Get-UninstallRegistryKey -SoftwareName "Missing $testId" -WarningVariable warnings 3> $null

        $result | Should -BeNullOrEmpty
        $warnings | Should -HaveCount 1
        "$($warnings[0])" | Should -BeExactly "No registry key found based on 'Missing $testId'"
    }

    It 'Is available as Get-InstallRegistryKey' {
        @(Get-InstallRegistryKey -SoftwareName $displayName).PSChildName | Should -Be @($keyName)
    }

    It 'Rejects an empty name' {
        { Get-UninstallRegistryKey -SoftwareName '' } | Should -Throw -ErrorId 'ParameterArgumentValidationErrorEmptyStringNotAllowed,*'
    }

    It 'Fails for a name that is not a valid wildcard pattern' {
        { Get-UninstallRegistryKey -SoftwareName 'Foo[' 3> $null } | Should -Throw -ErrorId 'GetUninstallRegistryKeyCommandError.InvalidPattern,*'
    }
}
