Describe 'Test-ProcessAdminRights helper function tests' -Tags Cmdlets, TestProcessAdminRights {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"
    }

    It 'should report true in an admin context' {
        Test-ProcessAdminRights | Should -BeTrue -Because "We should run these tests exclusively as admin"
    }
}