﻿Import-Module helpers/common-helpers

Describe 'Ensuring correct version of <Name> is installed' -Tag BundledApplications -ForEach @(
    @{ Name = 'shimgen' ; Version = '2.0.0' ; ChocolateyVersion = '2.0.0-alpha' ; IsSigned = $true }
    @{ Name = 'checksum' ; Version = '0.3.1' ; ChocolateyVersion = '2.0.0-alpha' ; IsSigned = $true }
    @{ Name = '7z' ; Version = '23.01' ; ChocolateyVersion = '2.2.1-alpha' ; IsSigned = $false }
) -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan "1.0.0")) {
    Context '<Name> is correctly installed' -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan $ChocolateyVersion)) {
        BeforeAll {
            # Because we're not modifying the install in any way, there is no need to Initialize-ChocolateyTestInstall
            $ToolPath = "$env:ChocolateyInstall/tools/$Name.exe"
        }

        It 'Should be in Chocolatey tools directory' {
            $ToolPath | Should -Exist
        }

        It 'Should be appropriately signed' -Skip:(-not $IsSigned) {
            $signature = Get-AuthenticodeSignature -FilePath $ToolPath

            # For non production builds, the official signing certificate is not in play, so need to
            # alter the assestion slightly, to account for the fact that UnknownError, is making the
            # underlying problem, i.e. "A certificate chain processed, but terminated in a root
            # certificate which is not trusted by the trust provider"
            if ($signature.SignerCertificate.Issuer -match 'Chocolatey Software, Inc') {
                $signature.Status | Should -Be 'UnknownError'
            }
            elseif ($signature.SignerCertificate.Issuer -match 'DigiCert') {
                $signature.Status | Should -Be 'Valid'
            }
        }

        It 'Should be version <Version>' {
            $fileInfo = Get-ChildItem $ToolPath
            $fileInfo.VersionInfo.ProductVersion | Should -Be $Version
        }
    }
}
