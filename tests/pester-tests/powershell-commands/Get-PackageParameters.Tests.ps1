Describe "Get-PackageParameters helper function tests" -Tags Cmdlets, GetPackageParameters {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        if (-not $env:ChocolateyInstall) {
            $env:ChocolateyInstall = $testLocation
        }

        function ConvertToComparableHashtableJson {
            <#
                .SYNOPSIS
                    Sorts the keys in a given hashtable to match a provided hashtable or ordered hashtable, then converts to JSON
            #>
            param(
                [Parameter(Mandatory, ValueFromPipeline)]
                [hashtable]$InputObject,

                $ComparisonHashtable = $Values
            )
            $OutputHashtable = [ordered]@{}
            $InputObject.GetEnumerator() | Sort-Object {
                @($ComparisonHashtable.Keys).IndexOf($_.Name)
            } | ForEach-Object {
                $OutputHashtable.Add($_.Key, $_.Value)
            }
            $OutputHashtable | ConvertTo-Json
        }
    }

    Context "Separators" {
        BeforeDiscovery {
            $ExampleParameters = @(
                @{
                    Values = @{
                        Something = "A Value"
                    }
                }
                @{
                    Values = @{
                        "1" = "Another Value"
                    }
                }
                @{
                    Values = @{
                        One = "$(New-Guid)"
                        Two = "$(New-Guid)"
                        Three = "$(New-Guid)"
                    }
                }
            )
        }

        Context "Mixed Formatting" -ForEach $ExampleParameters.Where{$_.Values.Keys.Count -gt 1} {
            It "Correctly identifies the parameters" {
                $Parameter = $Values.GetEnumerator().ForEach{"/$($_.Key)$(':','=' | Get-Random)$($_.Value)"} -join ' '
                Get-PackageParameters -Parameter $Parameter | ConvertToComparableHashtableJson | Should -Be ($Values | ConvertTo-Json)
            }
        }

        Context "Slash-Equals" -ForEach $ExampleParameters {
            It "Correctly identifies the parameters" {
                $Parameter = $Values.GetEnumerator().ForEach{"/$($_.Key)=$($_.Value)"} -join ' '
                Get-PackageParameters -Parameter $Parameter | ConvertToComparableHashtableJson | Should -Be ($Values | ConvertTo-Json)
            }
        }

        Context "Slash-Colon" -ForEach $ExampleParameters {
            It "Correctly identifies the parameters" {
                $Parameter = $Values.GetEnumerator().ForEach{"/$($_.Key):$($_.Value)"} -join ' '
                Get-PackageParameters -Parameter $Parameter | ConvertToComparableHashtableJson | Should -Be ($Values | ConvertTo-Json)
            }
        }

        Context "Reading from Environment Variable" -ForEach $ExampleParameters {
            It "Correctly identifies the parameters" {
                $env:ChocolateyPackageParameters = $Values.GetEnumerator().ForEach{"/$($_.Key)=$($_.Value)"} -join ' '
                Get-PackageParameters | ConvertToComparableHashtableJson | Should -Be ($Values | ConvertTo-Json)
            }
        }
    }

    Context "Parameter Count" {
        It "should <Title>" -ForEach @(
            @{
                Title = "accept one parameter"
                ParameterString = "/Key='Value'"
                Values = @{
                    Key = "Value"
                }
            }
            @{
                Title = "accept a single parameter with spaces included"
                ParameterString = "/User=Bob Example"
                Values = @{
                    User = "Bob Example"
                }
            }
            @{
                Title = "accept multiple parameters"
                ParameterString = "/Key='Value' /Door='Locked'"
                Values = @{
                    Key = "Value"
                    Door = "Locked"
                }
            }
            @{
                Title = "handle multiple parameters without quoting with spaces in the first parameter"
                ParameterString = "/User=Bob Example /Group=IT"
                Values = @{
                    User = "Bob Example"
                    Group = "IT"
                }
            }
        ) {
            $env:ChocolateyPackageParameters = if ($ParameterString) {
                $ParameterString
            } else {
                $Values.GetEnumerator().ForEach{"/$($_.Key):$($_.Value)"} -join ' '
            }
            Get-PackageParameters | ConvertToComparableHashtableJson | Should -Be ($Values | ConvertTo-Json)
        }
    }

    Context "Parameter Names" {
        It "Should <Title>" -ForEach @(
            @{
                Title = "accept names starting with letters"
                ParameterString = "/TestKeyName=$(($TestGuid = New-Guid))"
                Values = @{
                    TestKeyName = "$TestGuid"
                }
            }
            @{
                Title = "accept names starting with numbers"
                ParameterString = "/123Key=$(($TestGuid = New-Guid))"
                Values = @{
                    "123Key" = "$TestGuid"
                }
            }
        ) {
            $env:ChocolateyPackageParameters = if ($ParameterString) {
                $ParameterString
            } else {
                $Values.GetEnumerator().ForEach{"/$($_.Key):$($_.Value)"} -join ' '
            }
            Get-PackageParameters | ConvertToComparableHashtableJson | Should -Be ($Values | ConvertTo-Json)
        }
    }

    Context "Parameter Values" {
        It "Should <Title>" -ForEach @(
            @{
                Title = "accept string values"
                Values = @{
                    StringTest = "HelloWorld"
                }
            }
            @{
                Title = "accept 'switch' style (missing) values, and return '`$true'"
                ParameterString = "/InstallOnly"
                Values = @{
                    "InstallOnly" = $true
                }
            }
            @{
                Title = "strip either kind of quotes from the values"
                ParameterString = "/DisplayName='Bob Example' /ConnectionString=""Localhost"""
                Values = @{
                    DisplayName = "Bob Example"
                    ConnectionString = "Localhost"
                }
            }
        ) {
            $env:ChocolateyPackageParameters = if ($ParameterString) {
                $ParameterString
            } else {
                $Values.GetEnumerator().ForEach{"/$($_.Key):$($_.Value)"} -join ' '
            }
            Get-PackageParameters | ConvertToComparableHashtableJson | Should -Be ($Values | ConvertTo-Json)
        }
    }
}