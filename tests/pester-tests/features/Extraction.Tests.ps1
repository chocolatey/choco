Import-Module helpers/common-helpers

# These tests are skipped when not in Test Kitchen as the packages for this test are not available in this repository.
# These tests also modify files outside of the tests.
Describe "Extraction tests <_> command" -Skip:(-not $env:TEST_KITCHEN) -ForEach @(
    'install'
    'upgrade'
    if (Test-PackageIsEqualOrHigher -PackageName chocolatey.extension -Version 5.0.0) {
        'download'
    }
) -Tag Extraction {
    BeforeDiscovery {
        $Type = @(
            'combined-extracted-path'
            'absolute-extracted-path'
            'relative-extracted-path'
        )
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
        $Command = $_
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context '<_> extraction' -ForEach $Type {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $Output = Invoke-Choco $Command $_ --confirm
        }

        AfterAll {
            Remove-Item -Path C:\demonstration -Force -Recurse -ErrorAction SilentlyContinue
        }

        It 'Exits correctly' {
            $Output.ExitCode | Should -Be 1 -Because $Output.String
        }

        It 'Should output correctly' {
            $Output.String | Should -Match "The package contains an entry '.*' which is unsafe for extraction."
        }

        It 'Should not create the file.' {
            'C:\demonstration\demo.txt' | Should -Not -Exist -Because $Output.String
        }

    }
}
