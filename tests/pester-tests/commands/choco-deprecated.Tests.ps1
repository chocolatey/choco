Import-Module helpers/common-helpers

Describe "choco deprecated shims" -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan "1.0.0")) -Tag Chocolatey, DeprecatedShims {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    BeforeDiscovery {
        $DeprecatedShims = @(
            @{ Command = 'help'; Deprecation = 'none' }
            @{ Command = 'install'; Deprecation = 'cinst' }
            @{ Command = 'push'; Deprecation = 'cpush' }
            @{ Command = 'list'; Deprecation = 'clist' }
            @{ Command = 'search'; Deprecation = 'none' }
            @{ Command = 'uninstall'; Deprecation = 'cuninst' }
            @{ Command = 'upgrade'; Deprecation = 'cup' }
        )
    }

    Context 'help for command <Command> mentions that <Deprecation> is deprecated' -Foreach $DeprecatedShims {
        BeforeAll {
            # -? needs to be wrapped in quotes or PowerShell consumes it on us.
            $Output = Invoke-Choco $Command "-?"
        }

        Context 'help command deprecation notice' -Skip:($Command -ne 'help') {
            It 'should list the shims deprecated' {
                $Output.Lines | Should -Contain 'The shims `chocolatey`, `cinst`, `clist`, `cpush`, `cuninst` and `cup` are deprecated.'
            }

            It 'should list deprecation as being in 2.0.0' {
                $Output.Lines | Should -Contain 'removed in v2.0.0 of Chocolatey.'
            }
        }

        It 'should contain DEPRECATION NOTICE' -Skip:($Command -eq 'search') {
            $Output.Lines | Should -Contain 'DEPRECATION NOTICE'
        }

        It 'should mention <Deprecation> is deprecated' -Skip:($Deprecation -eq 'none') {
            $Output.Lines | Should -Contain "Starting in v2.0.0 the shortcut ``$Deprecation`` will be removed and can not be used"
            $Output.Lines | Should -Contain "use the full command going forward (``choco $Command``)."
        }

        It 'should show deprecation in usage' -Skip:($Command -notin 'search', 'list') {
            $Output.Lines | Should -Contain 'clist <filter> [<options/switches>] (DEPRECATED, will be removed in v2.0.0)'
        }
    }
}

Describe "Deprecated Chocolatey Helper Commands" -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan "1.0.0")) -Tag Chocolatey, DeprecatedHelpers {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot

        $PackageName = New-Guid
        $HelperUnderTest = "Get-BinRoot"
        $null = Invoke-Choco new $PackageName --version 1.0.0
        $TemplateOutput = Get-ChildItem -Path $PackageName -Recurse | Select-String -Pattern 'Get-BinRoot'
        $HelperUnderTest > "$PackageName/tools/chocolateyInstall.ps1"
        $null = Invoke-Choco pack "$PackageName/$PackageName.nuspec"
        $Output = Invoke-Choco install $PackageName --source . -y
    }

    AfterAll {
        Remove-Item "./$PackageName" -Recurse -Force -ErrorAction Ignore
        Remove-ChocolateyTestInstall
    }
    It 'should not mention Get-BinRoot in any of the generated files' {
        $TemplateOutput | Should -BeNullOrEmpty -Because 'Get-BinRoot has been deprecated and removed from the template'
    }

    It 'should exit success (0)' {
        $Output.ExitCode | Should -Be 0
    }

    It 'should warn that Get-BinRoot is deprecated' {
        $Output.Lines | Should -Contain 'WARNING: Get-BinRoot was deprecated in v1 and will be removed in v2. It has been replaced with Get-ToolsLocation (starting with v0.9.10), however many packages no longer require a special separate directory since package folders no longer have versions on them. Some do though and should continue to use Get-ToolsLocation.'
    }
}
