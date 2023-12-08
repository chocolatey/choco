Import-Module helpers/common-helpers

$TestCases = @(
    @{
        Name                 = 'None'
        ConfigurationsToTest = @{
            System              = $false
            EnvironmentVariable = $false
            ConfigFile          = $false
            CliArgument         = $false
        }
    }
    @{
        Name                 = 'System'
        ConfigurationsToTest = @{
            System              = $true
            EnvironmentVariable = $false
            ConfigFile          = $false
            CliArgument         = $false
        }
    }
    @{
        Name                 = 'SystemConfig'
        ConfigurationsToTest = @{
            System              = $true
            EnvironmentVariable = $false
            ConfigFile          = $true
            CliArgument         = $false
        }
    }
    @{
        Name                 = 'SystemEnvironmentVariable'
        ConfigurationsToTest = @{
            System              = $true
            EnvironmentVariable = $true
            ConfigFile          = $false
            CliArgument         = $false
        }
    }
    @{
        Name                 = 'SystemCliArgument'
        ConfigurationsToTest = @{
            System              = $true
            EnvironmentVariable = $true
            ConfigFile          = $false
            CliArgument         = $false
        }
    }
    @{
        Name                 = 'SystemConfigFileEnvironmentVariable'
        ConfigurationsToTest = @{
            System              = $true
            EnvironmentVariable = $true
            ConfigFile          = $true
            CliArgument         = $false
        }
    }
    @{
        Name                 = 'SystemConfigFileCliArgument'
        ConfigurationsToTest = @{
            System              = $true
            EnvironmentVariable = $false
            ConfigFile          = $true
            CliArgument         = $true
        }
    }
    @{
        Name                 = 'SystemEnvironmentVariableCliAgrument'
        ConfigurationsToTest = @{
            System              = $true
            EnvironmentVariable = $true
            ConfigFile          = $false
            CliArgument         = $true
        }
    }
    @{
        Name                 = 'SystemConfigFileEnvironmentVariableCliArgument'
        ConfigurationsToTest = @{
            System              = $true
            EnvironmentVariable = $true
            ConfigFile          = $true
            CliArgument         = $true
        }
    }
    @{
        Name                 = 'ConfigFile'
        ConfigurationsToTest = @{
            System              = $false
            EnvironmentVariable = $false
            ConfigFile          = $true
            CliArgument         = $false
        }
    }
    @{
        Name                 = 'ConfigFileEnvironmentVariable'
        ConfigurationsToTest = @{
            System              = $false
            EnvironmentVariable = $true
            ConfigFile          = $true
            CliArgument         = $false
        }
    }
    @{
        Name                 = 'ConfigFileCliArgument'
        ConfigurationsToTest = @{
            System              = $false
            EnvironmentVariable = $false
            ConfigFile          = $true
            CliArgument         = $true
        }
    }
    @{
        Name                 = 'ConfigFileEnvironmentVariableCliArgument'
        ConfigurationsToTest = @{
            System              = $false
            EnvironmentVariable = $true
            ConfigFile          = $true
            CliArgument         = $true
        }
    }
    @{
        Name                 = 'EnvironmentVariable'
        ConfigurationsToTest = @{
            System              = $false
            EnvironmentVariable = $true
            ConfigFile          = $false
            CliArgument         = $false
        }
    }
    @{
        Name                 = 'EnvironmentVariableCliArgument'
        ConfigurationsToTest = @{
            System              = $false
            EnvironmentVariable = $true
            ConfigFile          = $false
            CliArgument         = $true
        }
    }
    @{
        Name                 = 'CliArgument'
        ConfigurationsToTest = @{
            System              = $false
            EnvironmentVariable = $false
            ConfigFile          = $false
            CliArgument         = $true
        }
    }
)

$CommandsToTest = @(
    @{ Command = 'install'; ExtraArguments = @('dummypackage') }
    @{ Command = 'upgrade'; ExtraArguments = @('dummypackage') }
    @{ Command = 'search'; ExtraArguments = @('') }
    @{ Command = 'find'; ExtraArguments = @('') }
    @{ Command = 'outdated'; ExtraArguments = @('') }
    @{ Command = 'push'; ExtraArguments = @("--source='https://example.com'", "--api-key='my-key'") }
)

# Skip when not on Test Kitchen as this changes the system proxy
# Skip when run in Proxy Test Kitchen as the Proxy test kitchen sets some of these...
Describe "Proxy configuration (<Name>)" -Tag Proxy, ProxySkip -ForEach $TestCases -Skip:(-not $env:TEST_KITCHEN) {
    BeforeAll {
        Initialize-ChocolateyTestInstall
        New-ChocolateyInstallSnapshot
        $arguments = $null

        $SystemSet = "SystemSetProxy"
        $ConfigFileSet = "ConfigFileSetProxy"
        $EnvironmentVariableSet = "EnvironmentVariableSetProxy"
        $CliArgumentSet = "CliArgumentSetProxy"

        if ($ConfigurationsToTest.System) {
            Set-ItemProperty -Path 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Internet Settings' -Name ProxyServer -Value "https=$SystemSet;ftp=someFtp;socks=socksProxy"
            Set-ItemProperty -Path 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Internet Settings' -Name ProxyEnable -Value 1
        }

        if ($ConfigurationsToTest.ConfigFile) {
            Invoke-Choco config set proxy $ConfigFileSet
            Invoke-Choco config set proxyBypassList $ConfigFileSet
        }

        if ($ConfigurationsToTest.EnvironmentVariable) {
            $env:https_proxy = $EnvironmentVariableSet
            $env:no_proxy = $EnvironmentVariableSet
        }

        if ($ConfigurationsToTest.CliArgument) {
            $arguments = @("--proxy='$CliArgumentSet'", "--proxy-bypass-list='$CliArgumentSet'")
        }
    }

    AfterAll {
        Remove-ChocolateyTestInstall
        Remove-ItemProperty -Path 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Internet Settings' -Name ProxyServer -ErrorAction Ignore
        Remove-ItemProperty -Path 'HKCU:/Software/Microsoft/Windows/CurrentVersion/Internet Settings' -Name ProxyEnable -ErrorAction Ignore
        $env:https_proxy = $null
    }

    Context "Configured for command (<Command>)" -ForEach $CommandsToTest {
        BeforeAll {
            $Output = Invoke-Choco $Command @arguments @ExtraArguments --debug --verbose --noop
        }

        It "Should output the correct Proxy setting" {
            switch ($true) {
                $ConfigurationsToTest.CliArgument {
                    $Output.String | Should -MatchExactly "Proxy\.Location='$CliArgumentSet'"
                    $Output.String | Should -MatchExactly "Proxy\.BypassList='$CliArgumentSet'"
                    continue
                }

                $ConfigurationsToTest.ConfigFile {
                    $Output.String | Should -MatchExactly "Proxy\.Location='$ConfigFileSet'"
                    $Output.String | Should -MatchExactly "Proxy\.BypassList='$ConfigFileSet'"
                    continue
                }

                $ConfigurationsToTest.EnvironmentVariable {
                    $Output.String | Should -MatchExactly "Proxy\.Location='$EnvironmentVariableSet'"
                    $Output.String | Should -MatchExactly "Proxy\.BypassList='$EnvironmentVariableSet'"
                    continue
                }

                default {
                    $Output.String | Should -Not -Match "Proxy\.Location"
                    $Output.String | Should -Not -Match "Proxy\.BypassList"
                }
            }
        }
    }
}
