Describe "Get-PackageScriptParameters helper function tests" -Tags Cmdlets, GetPackageScriptParameters {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        if (-not $env:ChocolateyInstall) {
            $env:ChocolateyInstall = $testLocation
        }
    }

    Context 'Functional' {
        It "A <Title> Should return <Expected>" -ForEach @(
            @{
                Title = 'Script with Parameters, No Package Parameters'
                ExpectedInput = @{
                    ScriptPath = "$PSScriptRoot\..\..\packages\hasparameters\chocolateyInstall.ps1"
                }
                Expected = 'nothing'
                ExpectedOutput = @{}
            }
            @{
                Title = "Script with Parameters, Non-matching Package Parameters"
                ExpectedInput = @{
                    ScriptPath = "$PSScriptRoot\..\..\packages\hasparameters\chocolateyInstall.ps1"
                    Parameters = "/InvalidOption /SomethingElse='$(($TestGuid = "$(New-Guid)"))'"
                }
                Expected = "nothing"
                ExpectedOutput = @{}
            }
            @{
                Title = "Script with Parameters, Some Matching Package Parameters"
                ExpectedInput = @{
                    ScriptPath = "$PSScriptRoot\..\..\packages\hasparameters\chocolateyInstall.ps1"
                    Parameters = "/SomethingElse=$(New-Guid) /ValidStringParameter=$(($TestGuid = "$(New-Guid)"))"
                }
                Expected = "matching parameters only"
                ExpectedOutput = @{
                    ValidStringParameter = "$TestGuid"
                }
            }
            @{
                Title = "Script with Parameters, All Matching Package Parameters"
                ExpectedInput = @{
                    ScriptPath = "$PSScriptRoot\..\..\packages\hasparameters\chocolateyInstall.ps1"
                    Parameters = "/ValidStringParameter=$(($TestGuid = "$(New-Guid)")) /AnotherValidParameter"
                }
                Expected = "all matching parameters"
                ExpectedOutput = @{
                    ValidStringParameter = "$TestGuid"
                    AnotherValidParameter = $true
                }
            }
            @{
                Title = "Script without Parameters, No Package Parameters"
                ExpectedInput = @{
                    ScriptPath = "$PSScriptRoot\..\..\packages\add-path\tools\chocolateyinstall.ps1"
                    Parameters = "/ValidStringParameter=$(($TestGuid = "$(New-Guid)")) /AnotherValidParameter"
                }
                Expected = "nothing"
                ExpectedOutput = @{}
            }
            @{
                Title = "Script without Parameters, Some Package Parameters"
                ExpectedInput = @{
                    ScriptPath = "$PSScriptRoot\..\..\packages\add-path\tools\chocolateyinstall.ps1"
                    Parameters = "/InvalidOption /SomethingElse=$(($TestGuid = "$(New-Guid)"))"
                }
                Expected = "nothing"
                ExpectedOutput = @{}
            }
        ) {
            Get-PackageScriptParameters @ExpectedInput | ConvertTo-Json | Should -Be ($ExpectedOutput | ConvertTo-Json)
        }
    }
}