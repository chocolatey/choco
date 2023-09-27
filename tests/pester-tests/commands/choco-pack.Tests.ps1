
Describe "choco pack" -Tag Chocolatey, PackCommand {
    BeforeDiscovery {
        $successPack = @('basic'; 'basic-dependencies'; "cdata"; "full")
        # Required elements, that can also not be empty
        $missingFailures = @('id'; 'version'; 'authors'; 'description')
        # Elements that can not be set to an empty string, but are not required
        $emptyFailures = @(
            "projectUrl"
            "projectSourceUrl"
            "docsUrl"
            "bugTrackerUrl"
            "mailingListUrl"
            "iconUrl"
            "licenseUrl"
        )
        # Elements that will return an invalid failure (usually due to serialization)
        $invalidFailures = @(
            @{id = 'projectUrl'; message = "ERROR: CHCU0001: 'invalid project url' is not a valid URL for the projectUrl element in the package nuspec file." }
            @{id = 'projectSourceUrl'; message = "ERROR: CHCU0001: 'invalid project source url' is not a valid URL for the projectSourceUrl element in the package nuspec file." }
            @{id = 'docsUrl'; message = "ERROR: CHCU0001: 'invalid docs url' is not a valid URL for the docsUrl element in the package nuspec file." }
            @{id = 'bugTrackerUrl'; message = "ERROR: CHCU0001: 'invalid bug tracker url' is not a valid URL for the bugTrackerUrl element in the package nuspec file." }
            @{id = 'mailingListUrl'; message = "ERROR: CHCU0001: 'invalid mailing list url' is not a valid URL for the mailingListUrl element in the package nuspec file." }
            @{id = 'iconUrl'; message = "ERROR: CHCU0001: 'invalid icon url' is not a valid URL for the iconUrl element in the package nuspec file." }
            @{id = 'licenseUrl'; message = "ERROR: CHCU0001: 'invalid license url' is not a valid URL for the licenseUrl element in the package nuspec file." }
            @{id = "version"; message = "ERROR: CHCU0001: 'INVALID' is not a valid version string in the package nuspec file." }
            @{id = "no-content"; message = "Cannot create a package that has no dependencies nor content." } # This is a message from NuGet.Client, we may want to take ownership of it eventually.
            @{id = "id"; message = "The package ID 'invalid id' contains invalid characters. Examples of valid package IDs include 'MyPackage' and 'MyPackage.Sample'." } # This is a message from NuGet.Client, we may want to take ownership of it eventually.
            @{id = "requirelicenseacceptance"; message = "ERROR: CHCR0002: Enabling license acceptance requires a license url." }
        )
    }

    BeforeAll {
        Remove-NuGetPaths
        $testPackageLocation = "$(Get-TempDirectory)ChocolateyTests\packages"
        Initialize-ChocolateyTestInstall -Source $testPackageLocation

        Push-Location "$PSScriptRoot\testnuspecs"
        $expectedHeader = Get-ExpectedChocolateyHeader

        # NOTE: Only functions that changes any configuration value,
        # or state uses the 'Restore-ChocolateyInstallSnapshot' helper.
        # In other cases we do not need it, and can do manual
        # push/pop.
    }

    AfterAll {
        # Remove all packaged nupkg files
        Get-ChildItem "*.nupkg" -Recurse | ForEach-Object {
            Remove-Item $_.FullName
        }

        Pop-Location

        Remove-ChocolateyTestInstall
    }

    Context "No nuspec file available" {
        BeforeAll {
            $Output = Invoke-Choco pack
        }

        It "'choco pack' exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Outputs Error Message" {
            $Output.String | Should -Match "No \.nuspec files \(or more than 1\) were found to build in .*Please specify the \.nuspec file or try in a different directory\."
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }
    }

    Context "Package <_> metadata in current directory" -ForEach $successPack {
        BeforeAll {
            Push-Location $_
            $Output = Invoke-Choco pack
            Pop-Location
        }

        It "'choco pack' exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays created package message" {
            $Output.String | Should -Match "Successfully created package.*\.nupkg"
        }

        It "Creates nuget package" {
            "$_\$_.1.0.0.nupkg" | Should -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }
    }

    Context "Package <_> metadata with path" -ForEach $successPack {
        BeforeAll {
            $Output = Invoke-Choco pack "$_\$_.nuspec"
        }

        It "'choco pack' exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays created package message" {
            $Output.String | Should -Match "Successfully created package.*\.nupkg"
        }

        It "Creates nuget package" {
            "$_.1.0.0.nupkg" | Should -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }
    }

    # Message has changed and is missing almost all items in the validation message
    Context "Package with required elements" {
        BeforeAll {
            $Output = Invoke-Choco pack "required.nuspec"
        }

        It "'choco pack' exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays required error message for <_>" -ForEach $missingFailures {
            $Output.Lines | Should -Contain "ERROR: CHCR0001: $_ is a required element in the package nuspec file."
        }

        It "Does not create the nuget package" {
            "required.1.0.0.nupkg" | Should -Not -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }

        It "Should output message about validation issues found" {
            $Output.Lines | Should -Contain "One or more issues found with required.nuspec, please fix all validation items above listed as errors." -Because $Output.String
        }
    }

    # TODO: Validation messages are incomplete and are missing some items
    Context "Package with empty elements" {
        BeforeAll {
            $Output = Invoke-Choco pack "empty.nuspec"
        }

        It "'choco pack' exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays empty error message for <_>" -ForEach $emptyFailures {
            $Output.Lines | Should -Contain "ERROR: CHCR0001: The $_ element in the package nuspec file cannot be empty."
        }

        It "Does not create the nuget package" {
            "empty.1.0.0.nupkg" | Should -Not -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }

        It "Should output message about validation issues found" {
            $Output.Lines | Should -Contain "One or more issues found with empty.nuspec, please fix all validation items above listed as errors."
        }
    }

    # This empty element must be in a separate nuspec file as it will be a serializing error
    Context "Package with empty requireLicenseAcceptance" {
        BeforeAll {
            $Output = Invoke-Choco pack "empty-requireLicenseAcceptance.nuspec"
        }

        It "'choco pack' exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays serialize error" {
            $Output.Lines | Should -Contain "The 'http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd:requireLicenseAcceptance' element is invalid - The value '' is invalid according to its datatype 'http://www.w3.org/2001/XMLSchema:boolean' - The string '' is not a valid Boolean value. This validation error occurred in a 'requireLicenseAcceptance' element."
        }

        It "Does not create the nuget package" {
            "requireLicenseAcceptance.1.0.0.nupkg" | Should -Not -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }

        # We currently do not have a custom validation message for this element.
        # As such we do not know when or if it failed because of an empty/invalid require license acceptance value.
        It "Should not output message about validation issues found" {
            $Output.Lines | Should -Not -Contain "One or more issues found with empty-requireLicenseAcceptance.nuspec, please fix all validation items above listed as errors."
        }
    }

    Context "Package with missing elements" {
        BeforeAll {
            $Output = Invoke-Choco pack "missing.nuspec"
        }

        It "'choco pack' exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays empty error message for <_>" -ForEach $missingFailures {
            $Output.Lines | Should -Contain "ERROR: CHCR0001: $_ is a required element in the package nuspec file."
        }

        It "Does not create the nuget package" {
            "missing.1.0.0.nupkg" | Should -Not -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }

        It "Should output message about validation issues found" {
            $Output.Lines | Should -Contain "One or more issues found with missing.nuspec, please fix all validation items above listed as errors."
        }
    }

    # TODO: Message about verifying version string has changed, and should be fixed
    Context "Package with invalid <_.id>" -ForEach $invalidFailures {
        BeforeAll {
            $Output = Invoke-Choco pack "invalid-$($_.id).nuspec"
        }

        It "'choco pack' exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays error message '$($_.message)'" {
            $Output.Lines | Should -Contain $_.message
        }

        It "Does not create the nuget package" {
            "invalid-$($_.id).1.0.0.nupkg" | Should -Not -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }

        It "Should output message about validation issues found if needed" {
            $message = "One or more issues found with invalid-$($_.id).nuspec, please fix all validation items above listed as errors."

            if ($_.id -in @('no-content', 'id')) {
                $Output.Lines | Should -Not -Contain $message
            } else {
                $Output.Lines | Should -Contain $message
            }
        }
    }

    Context "Package with invalid character '&'" {
        BeforeAll {
            $Output = Invoke-Choco pack "invalid-character-and.nuspec"
        }

        It "'choco pack' exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays error message" {
            $Output.Lines | Should -Contain "An error occurred while parsing EntityName. Line 8, position 69."
        }

        It "Does not create the nuget package" {
            "invalid-character-and.1.0.0.nupkg" | Should -Not -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }
    }

    Context "Package with invalid character '<'" {
        BeforeAll {
            $Output = Invoke-Choco pack "invalid-character-lesser.nuspec"
        }

        It "'choco pack' exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays error message" {
            $Output.Lines | Should -Contain "Name cannot begin with the '.' character, hexadecimal value 0x2E. Line 8, position 69."
        }

        It "Does not create the nuget package" {
            "invalid-character-lesser.1.0.0.nupkg" | Should -Not -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }
    }

    Context "Package with version override" {
        BeforeAll {
            $Output = Invoke-Choco pack "basic/basic.nuspec" --version="3.0.4"
        }

        It "'choco pack' exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays created package message" {
            $Output.String | Should -Match "Successfully created package.*\.nupkg"
        }

        It "Does create the nuget package" {
            "basic.3.0.4.nupkg" | Should -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }
    }

    Context "Package with custom space out directory" -ForEach @(
        '--out', '--outdir', '--outputdirectory', '--output-directory'
    ) {
        BeforeAll {
            $OutDirectory = "$(Get-TempDirectory)ChocoPackOutput"
            if (Test-Path $OutDirectory) {
                Remove-Item "$OutDirectory\*.nupkg" -ErrorAction SilentlyContinue
            }
            else {
                New-Item -ItemType Directory -Path $OutDirectory
            }

            $Output = Invoke-Choco pack "basic/basic.nuspec" $_ $OutDirectory
        }

        It "'choco pack' exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays created package message" {
            $Output.Lines | Should -Contain "Successfully created package '$OutDirectory\basic.1.0.0.nupkg'"
        }

        It "Does create the nuget package" {
            "$OutDirectory\basic.1.0.0.nupkg" | Should -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }
    }

    Context "Package with custom equals out directory" -ForEach @(
        '--out={0}', '--outdir={0}', '--outputdirectory={0}', '--output-directory={0}'
    ) {
        BeforeAll {
            $OutDirectory = "$(Get-TempDirectory)ChocoPackOutput"
            if (Test-Path $OutDirectory) {
                Remove-Item "$OutDirectory\*.nupkg" -ErrorAction SilentlyContinue
            }

            $Output = Invoke-Choco pack "basic/basic.nuspec" ($_ -f $OutDirectory)
        }

        It "'choco pack' exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays Chocolatey name with version" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Displays created package message" {
            $Output.Lines | Should -Contain "Successfully created package '$OutDirectory\basic.1.0.0.nupkg'"
        }

        It "Does create the nuget package" {
            "$OutDirectory\basic.1.0.0.nupkg" | Should -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }
    }

    # Issue: https://github.com/chocolatey/choco/issues/2166
    Context "Package with forward slash" -Skip:(-Not (Test-ChocolateyVersionEqualOrHigherThan "0.10.16-beta")) {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot -SetWorkDir

            $Output = Invoke-Choco pack "$PSScriptRoot\testnuspecs\forward-slash\forward-slash.nuspec" "--output-directory=$PWD"
        }

        It "Displays created package message" {
            $Output.Lines | Should -Contain "Successfully created package '$PWD\forward-slash.1.0.0.nupkg'"
        }

        It "Does create the nuget package" {
            "$PWD\forward-slash.1.0.0.nupkg" | Should -Exist
        }

        It "Extract archive with correct paths" {
            # We leave the extraction here, as we test for the existence
            # in a previous test,
            # otherwise if the extraction fails, so will the previous test
            Expand-ZipArchive "$PWD\forward-slash.1.0.0.nupkg" "archiveContents"
            "$PWD\archiveContents\tools\purpose.txt" | Should -Exist
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }
    }

    Context 'Attempting to pack a package with unsupported nuspec elements throws an error' {

        BeforeDiscovery {
            $testCases = @(
                '<license>'
                '<packageTypes>'
                '<readme>'
                '<repository>'
                '<serviceable>'
            )
        }

        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $nuspec = @'
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2015/06/nuspec.xsd">
  <metadata>
    <id>unsupportedmetadata</id>
    <version>1.0.0</version>
    <title>unsupportedmetadata (Install)</title>
    <authors>Chocolatey Software</authors>
    <tags>unsupportedmetadata</tags>

    <license type="expression">MIT</license>
    <packageTypes>
        <packageType name="Unsupported" />
    </packageTypes>
    <readme>readme.md</readme>
    <repository type="git" url="https://github.com/chocolatey/choco.git" />
    <serviceable>true</serviceable>

    <summary>Test of unsupported metadata</summary>
    <description>Some metadata fields are not supported by chocolatey. `choco pack` should fail to pack them, while `choco install` or `upgrade` should allow them with a warning.</description>
  </metadata>
</package>
'@
            $packageName = 'unsupportedmetadata'
            $nuspecPath = "$packageName\$packageName.nuspec"

            $null = New-Item -Path $packageName -ItemType Directory
            $nuspec | Set-Content -Path $nuspecPath
            "readme content" | Set-Content -Path "$packageName\readme.md"

            $Output = Invoke-Choco pack $nuspecPath
        }

        It 'Fails to pack and exits with an error (1)' {
            $Output.ExitCode | Should -Be 1
        }

        It 'Shows an error about the unsupported nuspec metadata element "<_>"' -TestCases $testCases {
            $Output.String | Should -Match "ERROR: CHCU0002: $_ elements are not supported in Chocolatey CLI"
        }

        It "Should not output message about license url being deprecated" {
            $Output.String | Should -Not -Match "The 'licenseUrl' element will be deprecated"
        }

        It "Should not output message about iconUrl being deprecated" {
            $Output.String | Should -Not -Match "The 'PackageIconUrl'/'iconUrl' element is deprecated"
        }

        It "Should output message about validation issues found" {
            $Output.Lines | Should -Contain "One or more issues found with $nuspecPath, please fix all validation items above listed as errors."
        }
    }

    Context 'Packing a package with non-normalized versions generates normalized versions' -ForEach @(
        @{ ExpectedPackageVersion = '0.1.0' ; ProvidedVersion = '0.1.0.0' }
        @{ ExpectedPackageVersion = '1.2.3.4' ; ProvidedVersion = '01.02.03.04' }
        @{ ExpectedPackageVersion = '1.2.4' ; ProvidedVersion = '01.02.04' }
        @{ ExpectedPackageVersion = '1.2.0' ; ProvidedVersion = '01.02' }
        @{ ExpectedPackageVersion = '1.2.3' ; ProvidedVersion = '0001.0002.0003' }
        @{ ExpectedPackageVersion = '2.0.0' ; ProvidedVersion = '02' }
    ) -Tag VersionNormalization {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot
            $PackageUnderTest = 'nonnormalizedversions'
            Push-Location (New-Item "$(Get-TempDirectory)/$(New-Guid)" -ItemType Directory)
            $null = Invoke-Choco new $PackageUnderTest --version $ProvidedVersion
            $SourceNuspec = "$PWD/$PackageUnderTest/$PackageUnderTest.nuspec"
            $Output = Invoke-Choco pack $SourceNuspec
        }

        AfterAll {
            Pop-Location
        }

        It "Should exit with success (0)" {
            $Output.ExitCode | Should -Be 0 -Because $Output.String
        }

        It "Should report successful installation" {
            $Output.Lines | Should -Contain "Successfully created package '$PWD\$PackageUnderTest.$ExpectedPackageVersion.nupkg'" -Because $Output.String
        }

        It "Should have generated the correct files" {
            $ExpectedNupkg = "$PWD/$PackageUnderTest.$ExpectedPackageVersion.nupkg"
            $ExpectedNupkg | Should -Exist -Because $Output.String
            Expand-ZipArchive -Source $ExpectedNupkg -Destination "$PWD/$PackageUnderTest-expanded"
            $SourceNuspecContents = [xml](Get-Content $SourceNuspec)
            $PackedNuspecContents = [xml](Get-Content "$PWD/$PackageUnderTest-expanded/$PackageUnderTest.nuspec")
            $SourceNuspecContents.package.metadata.version | Should -Be $ProvidedVersion
            $PackedNuspecContents.package.metadata.version | Should -Be $ExpectedPackageVersion
        }
    }

    # This needs to be the last test in this block, to ensure NuGet configurations aren't being created.
    Test-NuGetPaths
}
