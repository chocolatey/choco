Describe 'Assert-ValidChecksum helper function tests' -Tags Cmdlets, AssertValidChecksum {
    BeforeAll {
        Initialize-ChocolateyTestInstall

        $testLocation = Get-ChocolateyTestLocation
        Import-Module "$testLocation\helpers\chocolateyInstaller.psm1"

        # Generate a random 100kb file to verify the checksum of
        $data = [byte[]]::new(100kb)
        $random = [random]::new()
        $random.NextBytes($data)
        $File = New-TemporaryFile
        [System.IO.File]::WriteAllBytes($File.FullName, $data)
    }

    AfterAll {
        Remove-Item -Path $file.FullName
    }

    Context 'Exported Aliases' {
        It 'Exports an alias for Get-ChecksumValid' {
            (Get-Alias -Name 'Get-ChecksumValid').Definition | Should -BeExactly 'Assert-ValidChecksum'
        }
    }

    $checksumTypes = @(
        @{ ChecksumType = 'Md5' }
        @{ ChecksumType = 'Sha1' }
        @{ ChecksumType = 'Sha256' }
        @{ ChecksumType = 'Sha512' }
    )
    Context 'Checksum Validation (<ChecksumType>)' -ForEach $checksumTypes {
        BeforeAll {
            $checksum = (Get-FileHash -Path $File.FullName -Algorithm $ChecksumType).Hash
        }

        It 'Correctly validates the proper checksum' {
            { Assert-ValidChecksum -Path $File.FullName -Checksum $checksum -ChecksumType $ChecksumType } | Should -Not -Throw -Because "The checksum of the file should match $checksum"
        }

        It 'Throws when given an invalid checksum' {
            $invalidChecksum = $checksum.Substring(10) + ("F" * 10)
            { Assert-ValidChecksum -Path $File.FullName -Checksum $invalidChecksum -ChecksumType $ChecksumType } | Should -Throw -Because "The checksum of the file should not match $invalidChecksum"
        }

        It 'Throws when given the incorrect checksum type' {
            $badType = if ($ChecksumType -eq 'Md5') { 'Sha256' } else { 'Md5' }
            { Assert-ValidChecksum -Path $File.FullName -Checksum $invalidChecksum -ChecksumType $ChecksumType } | Should -Throw -Because "$checksum is not a $badType checksum"
        }

        It 'Defaults to MD5 if the checksum type is unspecified' {
            if ($ChecksumType -eq 'Md5') {
                { Assert-ValidChecksum -Path $File.FullName -Checksum $checksum } | Should -Not -Throw -Because "$checksum should be the valid Md5 checksum"
            }
            else {
                { Assert-ValidChecksum -Path $File.FullName -Checksum $checksum } | Should -Throw -Because "$checksum is not an Md5 checksum"
            }
        }

        Context 'Bypassing Checksum Verification' {
            AfterEach {
                $env:chocolateyIgnoreChecksums = ''
                $env:chocolateyAllowEmptyChecksums = ''
                $env:chocolateyAllowEmptyChecksumsSecure = ''
            }

            It 'Skips checksum verification if $env:chocolateyIgnoreChecksums is set to "true"' {
                $env:chocolateyIgnoreChecksums = 'true'
                { Assert-ValidChecksum -Path $File.FullName } | Should -Not -Throw -Because "The verification should be bypassed"
            }

            It 'Skips checksum verification when empty checksums are passed and $env:chocolateyAllowEmptyChecksums is set to "true"' {
                $env:chocolateyAllowEmptyChecksums = 'true'
                { Assert-ValidChecksum -Path $File.FullName -Checksum "" } | Should -Not -Throw -Because "The verification should be bypassed"
            }

            It 'Throws if empty checksums are passed, $env:chocolateyAllowEmptyChecksumsSecure is set to "true", and there is no URL passed' {
                $env:chocolateyAllowEmptyChecksumsSecure = 'true'
                { Assert-ValidChecksum -Path $File.FullName -Checksum "" } | Should -Throw -Because 'URL was not provided'
            }

            It 'Throws if empty checksums are passed, $env:chocolateyAllowEmptyChecksumsSecure is set to "true", and the url passed is not HTTPS' {
                $env:chocolateyAllowEmptyChecksumsSecure = 'true'
                { Assert-ValidChecksum -Path $File.FullName -Checksum "" -Url 'http://example.com/application.exe' } | Should -Throw -Because 'URL passed was not HTTPS'
            }

            It 'Skips checksum verification when empty checksums are passed and $env:chocolateyAllowEmptyChecksumsSecure is set to "true" if a HTTPS url is provided' {
                $env:chocolateyAllowEmptyChecksumsSecure = 'true'
                { Assert-ValidChecksum -Path $File.FullName -Checksum "" -Url 'https://example.com/application.exe' } | Should -Not -Throw -Because 'URL passed was HTTPS'
            }
        }
    }

    Context 'When checksum.exe is not present' {
        BeforeAll {
            Rename-Item -Path "$testLocation\tools\checksum.exe" -NewName 'checksum.exe.old'
        }

        AfterAll {
            Rename-Item -Path "$testLocation\tools\checksum.exe.old" -NewName 'checksum.exe'
        }

        It 'Throws if checksum.exe is not present' {
            { Assert-ValidChecksum -Path $File.FullName -Checksum "" -Url 'https://example.com/application.exe' } | Should -Throw -Because 'URL passed was not HTTPS'
        }
    }
}