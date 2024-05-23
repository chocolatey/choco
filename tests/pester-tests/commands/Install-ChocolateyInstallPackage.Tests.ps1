Describe 'Testing Install-ChocolateyInstallPackage' {
    BeforeDiscovery {
        # These tests are explicitly testing some scenarios that are not immediately
        # obvious. The test package sets 'SilentArguments' to a flag with a trailing space.
        # This is being tested both with and without Chocolatey adding additional
        # arguments. The package also sets the ChocolateyAdditionalArguments
        # environment variable to a string also with a trailing space.
        # Using exe as an example, that means the arguments passed are expected to be `/exe ` and then `/exe  /norestart ` (Note the extra spaces in both instances).
        $ExpectedOutput = @(
            @{
                OutputWithoutAdditionalArguments = '\["C:\\.*\\lib\\install-chocolateyinstallpackage-tests\\tools\\ConsoleApp1.exe" /exe \]'
                OutputWithAdditionalArguments = '\["C:\\.*\\lib\\install-chocolateyinstallpackage-tests\\tools\\ConsoleApp1.exe" /exe  /norestart \]'
                Type   = 'exe'
            }
            @{
                OutputWithoutAdditionalArguments = '\["C:\\Windows\\System32\\msiexec.exe" /i "C:\\.*\\lib\\install-chocolateyinstallpackage-tests\\tools\\ConsoleApp1.msi" /qn \]'
                OutputWithAdditionalArguments = '\["C:\\Windows\\System32\\msiexec.exe" /i "C:\\.*\\lib\\install-chocolateyinstallpackage-tests\\tools\\ConsoleApp1.msi" /qn  /norestart \]'
                Type   = 'msi'
            }
            @{
                OutputWithoutAdditionalArguments = '\["C:\\Windows\\System32\\wusa.exe" "C:\\.*\\lib\\install-chocolateyinstallpackage-tests\\tools\\ConsoleApp1.msu" /quiet \]'
                OutputWithAdditionalArguments = '\["C:\\Windows\\System32\\wusa.exe" "C:\\.*\\lib\\install-chocolateyinstallpackage-tests\\tools\\ConsoleApp1.msu" /quiet  /norestart \]'
                Type   = 'msu'
            }
        )
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
        $PackageUnderTest = 'install-chocolateyinstallpackage-tests'
        Restore-ChocolateyInstallSnapshot
        $Output = Invoke-Choco install $PackageUnderTest --confirm --debug
    }

    AfterAll {
        Remove-ChocolateyInstallSnapshot
    }

    It 'Exits with Success (0)' {
        $Output.ExitCode | Should -Be 0 -Because $Output.String
    }

    It 'Output is accurate for installer type <Type>' -ForEach $ExpectedOutput {
        $Output.String | Should -MatchExactly $OutputWithoutAdditionalArguments -Because ($Output.Lines | Select-String "ConsoleApp1.$Type")
        $Output.String | Should -MatchExactly $OutputWithAdditionalArguments -Because ($Output.Lines | Select-String "ConsoleApp1.$Type")
    }
}
