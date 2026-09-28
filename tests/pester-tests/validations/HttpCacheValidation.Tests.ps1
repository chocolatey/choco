Describe "CacheFolderLockdownValidation" -Tag Chocolatey, Validations, HttpCache -Skip:(-not $env:TEST_KITCHEN) {
    BeforeDiscovery {
        $installedPackages = Get-ChocolateyInstalledPackages
        $agentInstalled = $installedPackages.Name -contains 'chocolatey-agent'
    }

    BeforeAll {
        Initialize-ChocolateyTestInstall

        $script:TestFile = $null
        $script:CacheFolder = "$env:ProgramData/ChocolateyHttpCache"

        function Assert-HttpCacheLockdown {
            $observedErrors = @(
                if (-not (Test-Path $script:CacheFolder)) {
                    "The cache folder does not exist"
                }

                $acl = Get-Acl -Path $script:CacheFolder
                if ($acl.Owner -cne "BUILTIN\Administrators") {
                    "The cache folder is owned by $($acl.Owner), not Administrators"
                }

                $rules = $acl.Access
                if ($rules.Count -ne 3) {
                    "Unexpected access rules are present on the cache folder:`n$($rules | Format-List | Out-String)"
                }

                $expectedRules = @(
                    @{
                        Identifier = "BUILTIN\Administrators"
                        AclType = "Allow"
                        Rights = "FullControl"
                    }
                    @{
                        Identifier = "NT AUTHORITY\SYSTEM"
                        AclType = "Allow"
                        Rights = "FullControl"
                    }
                    @{
                        Identifier = "BUILTIN\Users"
                        AclType = "Allow"
                        Rights = "ReadAndExecute, Synchronize"
                    }
                )

                foreach ($rule in $expectedRules) {
                    $actual = $rules.Where{$_.IdentityReference.Value -eq $rule.Identifier}
                    if ($actual) {
                        if ($actual.AccessControlType -ne $rule.AclType) {
                            "Access control type for $($rule.Identifier) rule should have '$($rule.AclType)', but has '$($actual.AccessControlType)'"
                        }
                        
                        if ($actual.FileSystemRights -ne $rule.Rights) {
                            "File system rights for $($rule.Identifier) rule should have '$($rule.Rights)', but has '$($actual.FileSystemRights)'"
                        }
                    }
                    else {
                        "There is no access rule for $($rule.Identifier)"
                    }
                }
            ) -join "`n"

            $observedErrors = $observedErrors.Trim()
            if ($observedErrors) {
                throw $observedErrors
            }
        }

        function Initialize-TestCacheFolder {
            # Remove any original cache folder, if there is one.
            if (Test-Path $script:CacheFolder -PathType Container) {
                Remove-Item -Path $script:CacheFolder -Force -Recurse
            }

            # Create normal folder and file with no special ACLs in its place.
            New-Item -Path $script:CacheFolder -ItemType Directory
            $script:TestFile = New-Item -Path "$script:CacheFolder/test-file.dat"
        }

        function Reset-ReadOnlyCacheFiles {
            # Ensure no files within the cache folder are set read-only so it can be removed
            Get-ChildItem -Path $script:CacheFolder -Recurse -File |
                Where-Object { $_.IsReadOnly } |
                ForEach-Object { $_.IsReadOnly = $false }
        }
    }

    AfterAll {
        Remove-ChocolateyTestInstall

        if (Test-Path $script:CacheFolder) {
            Remove-Item -Recurse -Force $script:CacheFolder
        }
    }

    Context 'Normal Admin Context' {

        Describe 'With a suspicious HttpCache directory' {
            BeforeAll {
                Initialize-TestCacheFolder
                $result = Invoke-Choco search chocolatey
            }

            It 'exits with 0' {
                $result.ExitCode | Should -Be 0 -Because $result.String
            }

            It 'warns that the pre-existing cache folder has been purged' {
                $result.Lines | Should -Contain 'Validation Warnings:'
                $result.String | Should -Match 'The HTTP Cache was not correctly locked down, purging the cache'
            }

            It 'creates a new cache directory with the correct permissions' {
                Assert-HttpCacheLockdown
            }
        }

        Describe 'With Read-Only files in the suspicious HttpCache directory' {
            BeforeAll {
                Initialize-TestCacheFolder

                # Make the file read only so the removal can't work
                $script:TestFile.IsReadOnly = $true

                $result = Invoke-Choco search chocolatey
            }

            AfterAll {
                Reset-ReadOnlyCacheFiles
            }

            It 'exits with 1' {
                $result.ExitCode | Should -Be 1 -Because $result.String
            }

            It 'fails with an error message indicating that the cache folder could not be purged' {
                $result.Lines | Should -Contain 'Validation Errors:'
                $result.String | Should -Match 'System Cache directory exists, but could not be locked down'
            }

            It 'fails to purge the cache directory' {
                $script:TestFile | Should -Exist -Because 'Chocolatey should not have been able to purge the cache folder'
            }
        }

        Describe 'With Read-Only files in the suspicious HttpCache directory and --ignore-http-cache' {
            BeforeAll {
                Initialize-TestCacheFolder

                # Make the file read only so the removal can't work
                $script:TestFile.IsReadOnly = $true

                $result = Invoke-Choco search chocolatey --ignore-http-cache
            }

            AfterAll {
                Reset-ReadOnlyCacheFiles
            }

            It 'exits with 0' {
                $result.ExitCode | Should -Be 0
            }

            It 'warns that the cache folder could not be purged' {
                $result.Lines | Should -Contain 'Validation Warnings:'
                $result.String | Should -Match 'System Cache directory exists, but could not be locked down'
            }

            It 'fails to purge the cache directory' {
                $script:TestFile | Should -Exist -Because 'Chocolatey should not have been able to purge the cache folder'
            }

            It 'fails to lock down the cache directory' {
                { Assert-HttpCacheLockdown } | Should -Throw -Because 'we expect the cache directory to not be locked down'
            }
        }
    }

    Context 'Background Service' -Tag Background {
        BeforeAll {
            New-ChocolateyInstallSnapshot

            # enable background service and make sure it's used for this operation
            Enable-ChocolateyFeature -Name useBackgroundService
            Disable-ChocolateyFeature -Name useBackgroundServiceWithNonAdministratorsOnly
            Disable-ChocolateyFeature -Name useBackgroundServiceWithSelfServiceSourcesOnly
            Invoke-Choco config set --name backgroundServiceAllowedCommands --value "install,upgrade,uninstall,download"

            function Clear-DownloadDirectory {
                if (Test-Path "$env:ProgramData\download") {
                    Remove-Item "$env:ProgramData\download" -Recurse -Force
                }
            }
        }

        AfterAll {
            Remove-ChocolateyInstallSnapshot
        }

        Describe 'With a suspicious HttpCache directory' {
            BeforeAll {
                Initialize-TestCacheFolder
                $result = Invoke-Choco download chocolatey
            }

            AfterAll {
                Clear-DownloadDirectory
            }

            It 'exits with 0' {
                $result.ExitCode | Should -Be 0
            }

            It 'should run in background mode' {
                $result.Lines | Should -Contain "Running in background mode" -Because $result.String
            }

            It 'warns that the pre-existing cache folder has been purged' {
                $result.Lines | Should -Contain 'Validation Warnings:'
                $result.String | Should -Match 'The HTTP Cache was not correctly locked down, purging the cache'
            }

            It 'creates a new cache directory with the correct permissions' {
                Assert-HttpCacheLockdown
            }
        }

        Describe 'With Read-Only files in the suspicious HttpCache directory' {
            BeforeAll {
                Initialize-TestCacheFolder

                # Make the file read only so the removal can't work
                $script:TestFile.IsReadOnly = $true

                $result = Invoke-Choco download chocolatey
            }

            AfterAll {
                Reset-ReadOnlyCacheFiles
                Clear-DownloadDirectory
            }

            It 'exits with 1' {
                $result.ExitCode | Should -Be 1
            }

            It 'should run in background mode' {
                $result.Lines | Should -Contain "Running in background mode" -Because $result.String
            }

            It 'fails with an error message indicating that the cache folder could not be purged' {
                $result.Lines | Should -Contain 'Validation Errors:'
                $result.String | Should -Match 'System Cache directory exists, but could not be locked down'
            }

            It 'fails to purge the cache directory' {
                $script:TestFile | Should -Exist -Because 'Chocolatey should not have been able to purge the cache folder'
            }
        }

        Describe 'With Read-Only files in the suspicious HttpCache directory and --ignore-http-cache' {
            BeforeAll {
                Initialize-TestCacheFolder
                
                # Make the file read only so the removal can't work
                $script:TestFile.IsReadOnly = $true

                $result = Invoke-Choco download chocolatey --ignore-http-cache
            }

            AfterAll {
                Reset-ReadOnlyCacheFiles
                Clear-DownloadDirectory
            }

            It 'exits with 0' {
                $result.ExitCode | Should -Be 0
            }

            It 'should run in background mode' {
                $result.Lines | Should -Contain "Running in background mode" -Because $result.String
            }

            It 'warns that the cache folder could not be purged' {
                $result.Lines | Should -Contain 'Validation Warnings:'
                $result.String | Should -Match 'System Cache directory exists, but could not be locked down'
            }

            It 'fails to purge the cache directory' {
                $script:TestFile | Should -Exist -Because 'Chocolatey should not have been able to purge the cache folder'
            }

            It 'fails to lock down the cache directory' {
                { Assert-HttpCacheLockdown } | Should -Throw -Because 'we expect the cache directory to not be locked down'
            }
        }
    }
}