Import-Module helpers/common-helpers

Describe "choco uninstall" -Tag Chocolatey, UninstallCommand {
    BeforeAll {
        Remove-NuGetPaths
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Uninstalling package that makes use of new Get Chocolatey Path helper" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            Enable-ChocolateySource -Name 'local'

            $null = Invoke-Choco install test-chocolateypath -y

            $Output = Invoke-Choco uninstall test-chocolateypath -y
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Outputs message <_>" -ForEach @(
            'Package Path in Before Modify Script: <installPath>\lib\test-chocolateypath'
            'Install Path in Before Modify Script: <installPath>'
            'Package Path in Uninstall Script: <installPath>\lib\test-chocolateypath'
            'Install Path in Uninstall Script: <installPath>'
        ) {
            $Output.Lines | Should -Contain ($_ -replace '<installPath>', $env:ChocolateyInstall) -Because $Output.String
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
