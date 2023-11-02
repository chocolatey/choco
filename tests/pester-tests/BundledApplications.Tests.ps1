Import-Module helpers/common-helpers

Describe 'Ensuring correct version of <Name> is installed' -Tag BundledApplications -ForEach @(
    @{ Name = 'shimgen' ; Version = '1.0.0' ; ChocolateyVersion = '1.0.0' ; IsSigned = $true }
    @{ Name = '7z' ; Version = '21.07' ; ChocolateyVersion = '1.1.0' ; IsSigned = $false }
) -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan "1.0.0")) {
    Context '<Name> is correctly installed' -Skip:(-not (Test-ChocolateyVersionEqualOrHigherThan $ChocolateyVersion)) {
        BeforeAll {
            # Because we're not modifying the install in any way, there is no need to Initialize-ChocolateyTestInstall
            $ToolPath = "$env:ChocolateyInstall/tools/$Name.exe"
            # TODO: Encapsulate in an environment variable once kitchen-pester has new version - https://github.com/chocolatey/choco/issues/2692
            $Thumbprint = '83AC7D88C66CB8680BCE802E0F0F5C179722764B'
        }

        It 'Should be in Chocolatey tools directory' {
            $ToolPath | Should -Exist
        }

        It 'Should be appropriately signed' -Skip:(-not $IsSigned) {
            $signature = Get-AuthenticodeSignature -FilePath $ToolPath
            $signature.Status | Should -Be 'Valid'
            $signature.SignerCertificate.Thumbprint | Should -Be $Thumbprint
        }

        It 'Should be version <Version>' {
            $fileInfo = Get-ChildItem $ToolPath
            $fileInfo.VersionInfo.ProductVersion | Should -Be $Version
        }
    }
}
