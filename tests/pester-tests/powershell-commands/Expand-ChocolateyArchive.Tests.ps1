Describe 'Expand-ChocolateyArchive helper function tests' -Tags ExpandChocolateyArchive, Cmdlets {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
    }

    Context 'Unit tests' -Tags WhatIf {
        BeforeAll {
            $Guid = New-Guid
            $Path = "$env:TEMP\$Guid.zip"
            $Path64 = "$env:TEMP\$Guid-x64.zip"
            $Destination = "$env:TEMP\$Guid"
            $LogPath = "$env:TEMP\$Guid-packagefolder"
            $PackageName = "$Guid"

            $tempFile = New-Item -Path $Path
            $tempFile64 = New-Item -Path $Path64
        }

        AfterAll {
            $tempFile, $tempFile64 | Remove-Item -Force
        }

        It 'extracts the target zip file specified by <_> to the expected location' -TestCases @('-Path', '-Path64') {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'")
            $Command = [scriptblock]::Create("Expand-ChocolateyArchive $_ '$Path' -Destination '$Destination' -WhatIf")

            $expectedResults = @(
                "What if: Performing the operation `"Create Directory`" on target `"$Destination`"."
                "What if: Performing the operation `"Extract zip file contents to '$Destination' with 7-Zip`" on target `"$Path`"."
            )

            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf | Should -BeExactly $expectedResults
        }

        It 'always uses -Path if the chocolateyForceX86 environment variable is set' {
            $Preamble = [scriptblock]::Create(@"
            `$env:chocolateyForceX86 = 'true'
            Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'
"@)
            $Command = [scriptblock]::Create("Expand-ChocolateyArchive -Path '$Path' -Path64 '$Path64' -Destination '$Destination' -WhatIf")
            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command

            $expectedResults = @(
                "What if: Performing the operation `"Create Directory`" on target `"$Destination`"."
                "What if: Performing the operation `"Extract zip file contents to '$Destination' with 7-Zip`" on target `"$Path`"."
            )
            $results.WhatIf | Should -BeExactly $expectedResults
        }

        It 'creates a lib folder to store the log files if needed when -PackageName is set' {
            $Preamble = [scriptblock]::Create(@"
                Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'
                `$env:chocolateyPackageFolder = '$LogPath'
"@)
            $Command = [scriptblock]::Create("Expand-ChocolateyArchive -Path '$Path' -Destination '$Destination' -PackageName $PackageName -WhatIf")
            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command

            $expectedResult = "What if: Performing the operation `"Create Directory`" on target `"$LogPath`"."
            $results.WhatIf | Should -Contain $expectedResult
        }

        It 'creates a lib folder to store the log files if needed when chocolateyPackageName environment variable is set' {
            $Preamble = [scriptblock]::Create(@"
                Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'
                `$env:chocolateyPackageFolder = '$LogPath'
                `$env:chocolateyPackageName = '$PackageName'
"@)
            $Command = [scriptblock]::Create("Expand-ChocolateyArchive -Path '$Path' -Destination '$Destination' -WhatIf")
            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command

            $expectedResult = "What if: Performing the operation `"Create Directory`" on target `"$LogPath`"."
            $results.WhatIf | Should -Contain $expectedResult
        }

        It 'uses 7zip to decompress by default' {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'")
            $Command = [scriptblock]::Create("Expand-ChocolateyArchive -Path '$Path' -Destination '$Destination' -WhatIf")
            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command

            $expectedResult = "What if: Performing the operation `"Extract zip file contents to '$Destination' with 7-Zip`" on target `"$Path`"."
            $results.WhatIf | Should -Contain $expectedResult
        }

        It 'will use the fallback builtin extraction method if using -UseBuiltinCompression' {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'")
            $Command = [scriptblock]::Create("Expand-ChocolateyArchive -Path '$Path' -Destination '$Destination' -UseBuiltInCompression -WhatIf")
            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command

            $expectedResult = "What if: Performing the operation `"Extract zip file contents to '$Destination' with built-in decompression`" on target `"$Path`"."
            $results.WhatIf | Should -Contain $expectedResult
        }

        It 'will use the fallback builtin extraction method if specified by chocolateyUseBuiltinCompression environment variable' {
            $Preamble = [scriptblock]::Create(@"
                Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'
                `$env:chocolateyUseBuiltinCompression = 'true'
"@)
            $Command = [scriptblock]::Create("Expand-ChocolateyArchive -Path '$Path' -Destination '$Destination' -WhatIf")
            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command

            $expectedResult = "What if: Performing the operation `"Extract zip file contents to '$Destination' with built-in decompression`" on target `"$Path`"."
            $results.WhatIf | Should -Contain $expectedResult
        }

        It 'applies a -FilesToExtract filter when provided' {
            $Preamble = [scriptblock]::Create("Import-Module '$testLocation\helpers\chocolateyInstaller.psm1'")
            $Command = [scriptblock]::Create("Expand-ChocolateyArchive -Path '$Path' -Destination '$Destination' -WhatIf -FilesToExtract '*.exe'")

            $expectedResults = @(
                "What if: Performing the operation `"Create Directory`" on target `"$Destination`"."
                "What if: Performing the operation `"Extract zip file contents matching pattern *.exe to '$Destination' with 7-Zip`" on target `"$Path`"."
            )

            $results = Get-WhatIfResult -Preamble $Preamble -Command $Command
            $results.WhatIf | Should -BeExactly $expectedResults
        }
    }

    Context 'Integration tests' {
        BeforeAll {
            Import-Module "$testLocation\helpers\chocolateyInstaller.psm1" -Force

            $Guid = New-Guid
            $Path = "$env:TEMP\$Guid.zip"
            $Path64 = "$env:TEMP\$Guid-x64.zip"
            $Destination = "$env:TEMP\$Guid"

            $LogPath = "$env:TEMP\$Guid-packagefolder"
            $PackageName = "$Guid"

            $TempFiles = 1..10 | ForEach-Object { @{ Id = $_; File = New-TemporaryFile } }
            foreach ($file in $TempFiles) {
                # Populate the file with random data
                $file.Content = 1..100 | ForEach-Object { (New-Guid).ToString() }
                $file.Content | Set-Content -Path $file.File
            }

            Compress-Archive -Path $TempFiles.File.FullName -DestinationPath $Path
            New-Item -ItemType Directory -Path $LogPath > $null

            $env:chocolateyPackageName = $PackageName
            $env:chocolateyPackageFolder = $LogPath
        }

        AfterAll {
            $cleanupItems = @(
                $TempFiles.File.FullName
                $Path
                $Destination
                $LogPath
            )
            Remove-Item -Path $cleanupItems -Force -Recurse -ErrorAction Ignore

            $env:chocolateyPackageName = ''
            $env:chocolateyPackageFolder = ''
        }

        Describe 'Invalid parameters' {
            BeforeAll {
                $env:chocolateyForceX86 = 'true'
            }

            AfterAll {
                $env:chocolateyForceX86 = ''
            }

            It 'throws an error if using only -Path64 with the chocolateyForceX86 environment variable set' {
                { Expand-ChocolateyArchive -Path64 $Path64 -Destination $Destination } |
                    Should -Throw -ExpectedMessage "32-bit archive is not supported for $PackageName" -ExceptionType 'System.NotSupportedException'
            }
        }

        Describe 'Using <Mode> decompression' -ForEach @(
            @{ Mode = '7zip' }
            @{ Mode = 'fallback' }
        ) {
            Context 'No filter' {
                It 'completes successfully and returns the destination path' {
                    $params = @{
                        Path = $Path
                        Destination = $Destination
                        UseBuiltinCompression = $Mode -eq 'fallback'
                    }
                    $result = Expand-ChocolateyArchive @params

                    $result | Should -BeExactly $Destination
                }

                It 'extracts the files from the archive' {
                    $extractedFiles = Get-ChildItem -Path $Destination
                    $extractedFiles.Count | Should -Be $TempFiles.Count

                    foreach ($file in $extractedFiles) {
                        $expectedFile = $TempFiles | Where-Object { $_.File.Name -eq $file.Name }
                        $expectedFile | Should -Not -BeNullOrEmpty -Because 'we should not have any unexpected files extracted'

                        $content = Get-Content -Path $file.FullName
                        $content | Should -BeExactly $expectedFile.Content -Because "$($file.Name) should have the same content as it originally did"
                    }
                }

                It 'writes a log file to the package folder' {
                    $log = Get-ChildItem -Path $LogPath -File

                    $log | Should -Not -BeNullOrEmpty -Because 'the command should have written an extraction log'

                    $expectedContent = $TempFiles.File.Name | ForEach-Object { Join-Path $Destination -ChildPath $_ }
                    Get-Content -Path $log | Should -BeExactly $expectedContent
                }
            }

            Context 'Filter for specific files' {
                BeforeAll {
                    $TargetFile = $TempFiles | Select-Object -First 1
                    $SpecificDestination = "$Destination-test"
                }

                AfterAll {
                    Remove-Item -Path $SpecificDestination -Force -Recurse -ErrorAction Ignore
                }

                It 'completes successfully and returns the destination path' {
                    $params = @{
                        Path = $Path
                        Destination = $SpecificDestination
                        UseBuiltinCompression = $Mode -eq 'fallback'
                        FilesToExtract = $TargetFile.File.Name
                    }
                    $result = Expand-ChocolateyArchive @params

                    $result | Should -BeExactly $SpecificDestination
                }

                It 'extracts the file from the archive' {
                    $extractedFiles = Get-ChildItem -Path $SpecificDestination
                    @($extractedFiles).Count | Should -Be 1

                    $extractedFiles.Name | Should -BeExactly $TargetFile.File.Name

                    $extractedContent = Get-Content -Path $extractedFiles.FullName
                    $extractedContent | Should -BeExactly $TargetFile.Content -Because "$($extractedFiles.Name) should have the same content as it originally did"
                }

                It 'writes a log file to the package folder' {
                    $log = Get-ChildItem -Path $LogPath -File

                    $log | Should -Not -BeNullOrEmpty -Because 'the command should have written an extraction log'

                    $expectedContent = Join-Path $SpecificDestination -ChildPath $TargetFile.File.Name
                    Get-Content -Path $log | Should -BeExactly $expectedContent
                }
            }
        }
    }
}