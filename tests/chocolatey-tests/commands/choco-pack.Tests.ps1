Import-Module helpers/common-helpers

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
    $emptyFailures | ForEach-Object {
        @{id = $_; message = "Invalid URI: The format of the URI could not be determined." }
    }
    @{id = "version"; message = "An error occured while trying to parse the value 'INVALID' of property 'version' in the manifest file." }
    @{id = "no-content"; message = "Cannot create a package that has no dependencies nor content." }
    @{id = "id"; message = "The package ID 'invalid id' contains invalid characters. Examples of valid package IDs include 'MyPackage' and 'MyPackage.Sample'." }
    @{id = "requirelicenseacceptance"; message = "Enabling license acceptance requires a license or a licenseUrl to be specified. The licenseUrl will be deprecated, consider using the license metadata." }
)

Describe "choco pack" -Tag Chocolatey, PackCommand {
    BeforeAll {
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
    }

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
            $Output.Lines | Should -Contain "$_ is required."
        }

        It "Does not create the nuget package" {
            "required.1.0.0.nupkg" | Should -Not -Exist
        }
    }

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
            $Output.Lines | Should -Contain "$_ cannot be empty."
        }

        It "Does not create the nuget package" {
            "empty.1.0.0.nupkg" | Should -Not -Exist
        }
    }

    # This empty element must be in a seperate nuspec file as it will be a serializing error
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

        It "Displays possible element message <_>" -ForEach $missingFailures {
            $Output.String | Should -Match "The element 'metadata' in namespace.*has incomplete content\..*$_"
        }

        It "Does not create the nuget package" {
            "missing.1.0.0.nupkg" | Should -Not -Exist
        }
    }

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
    }
}
