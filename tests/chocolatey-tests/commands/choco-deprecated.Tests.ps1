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

    Context 'help for command <Command> mentions that <Deprecation> is deprecated' -ForEach $DeprecatedShims {
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
