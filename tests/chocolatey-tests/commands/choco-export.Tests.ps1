Import-Module helpers/common-helpers

Describe "choco export" -Tag Chocolatey, ExportCommand {
    BeforeDiscovery {
        # We create a temporary directory to export
        # any configurations to, this works faster
        # than creating/removing snapshots between each context
        # step.
        if (!(Test-Path "C:\temp")) {
            New-Item -Path "C:\temp" -ItemType Directory
        }
    }

    BeforeAll {
        $expectedHeader = Get-ExpectedChocolateyHeader
        Initialize-ChocolateyTestInstall

        # TODO: Consolidate these recurring package lists - https://github.com/chocolatey/choco/issues/2691
        # Let us install some known packages
        $null = @(
            'installpackage=1.0.0'
            'upgradepackage=1.1.1-beta'
            'package.extension=1.0.0'
            'package.template=1.0.0'
        ) | ForEach-Object {
            $splits = $_.Split('=')
            Invoke-Choco install $splits[0] --version $splits[1] --confirm
        }

        New-ChocolateyInstallSnapshot -SetWorkDir
    }

    AfterAll {
        Remove-ChocolateyTestInstall

        if (Test-Path "C:\temp") {
            Remove-Item -Recurse -Path "C:\temp"
        }
    }

    Context "Listing help section" {
        BeforeDiscovery {
            $supportedArguments = @(
                @{
                    Arguments   = "-o, --output-file-path=VALUE"
                    Description = "Output File Path - the path to where the list of currently installed`npackages should be saved. Defaults to packages.config."
                }
                @{
                    Arguments   = "--include-version-numbers, --include-version"
                    Description = "Include Version numbers - controls whether or not version numbers for`neach package appear in generated file.  Defaults to false."
                }
            )

            $examples = @(
                "choco export"
                "choco export --include-version-numbers"
                "choco export `"'c:\temp\packages.config'`""
                "choco export `"'c:\temp\packages.config'`" --include-version-numbers"
                "choco export -o=`"'c:\temp\packages.config'`""
                "choco export -o=`"'c:\temp\packages.config'`" --include-version-numbers"
                "choco export --output-file-path=`"'c:\temp\packages.config'`""
                "choco export --output-file-path=`"'c:\temp\packages.config'`" --include-version-numbers"
            )

            $supportedExitCodes = @(
                "- 0: operation was successful, no issues detected"
                "- -1 or 1: an error has occurred"
            )
        }

        BeforeAll {
            # No need to create a new snapshot here
            $Output = Invoke-Choco export --help
            $Output.Lines = $Output.Lines
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays description of command" {
            $Output.Lines | Should -Contain "Export Command"
            $Output.Lines | Should -Contain "Export all currently installed packages to a file."
            $Output.Lines | Should -Contain "This is especially helpful when re-building a machine that was created"
            $Output.Lines | Should -Contain "using Chocolatey.  Export all packages to a file, and then re-install"
            $Output.Lines | Should -Contain "those packages onto new machine using ``choco install packages.config``."
        }

        It "Displays command usage" {
            $Output.Lines | Should -Contain "choco export [<options/switches>]"
        }

        It "Displays example '<_>'" -Foreach $examples {
            $Output.Lines | Should -Contain $_
        }

        It "Displays supported exit codes" -Foreach $supportedExitCodes {
            $Output.Lines | Should -Contain $_
        }

        It "Displays supported option and switches <Arguments>" -Foreach $supportedArguments {
            $Output.Lines | Should -Contain $Arguments
        }

        It "Displays description of option and switches <Arguments>" -Foreach $supportedArguments {
            $Description -split "`n" | ForEach-Object {
                $Output.Lines | Should -Contain $_
            }
        }
    }

    Context "Runs export without additional arguments" {
        BeforeDiscovery {
            # TODO: Consolidate these recurring package lists - https://github.com/chocolatey/choco/issues/2691
            # We set this here, so we can reuse them between different contexts
            $expectedExports = @(
                "chocolatey"
                "installpackage"
                "upgradepackage"
                "package.extension"
                "package.template"
            )
        }

        BeforeAll {
            if (Test-Path "packages.config") {
                Remove-Item "packages.config"
            }
            $expectedPath = "packages.config"

            $Output = Invoke-Choco export
        }

        It "Exits with success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        # NOTE: There is no output other than the header, and possibly the unofficial statement
        It "Displays chocolatey version header" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Exports expected package '<_>'" -Foreach $expectedExports {
            $expectedPath | Should -FileContentMatch "<package id=`"$([regex]::Escape($_))`" />"
        }
    }

    Context "Runs export with including versions (<_>)" -Foreach @("--include-version-numbers", "--include-version") {
        BeforeDiscovery {
            # TODO: Consolidate these recurring package lists - https://github.com/chocolatey/choco/issues/2691
            # We set this here, so we can reuse them between different contexts
            $expectedExports = @(
                @{
                    Name    = "installpackage"
                    Version = "1.0.0"
                }
                @{
                    Name    = "upgradepackage"
                    Version = "1.1.1-beta"
                }
                @{
                    Name    = "package.extension"
                    Version = "1.0.0"
                }
                @{
                    Name    = "package.template"
                    Version = "1.0.0"
                }
            )
        }

        BeforeAll {
            $expectedPath = "packages.config"

            # This is in case we are running the test directly
            # which in that case this file will not exist.
            # We don't care about the content, encoding, only the presence
            if (!(Test-Path $expectedPath)) {
                "Not Important" | Set-Content -Path $expectedPath
            }

            $Output = Invoke-Choco export $_
        }

        It "Exits with success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        # NOTE: There is no output other than the header, and possibly the unofficial statement
        It "Displays chocolatey version header" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Exports expected package '<Name>' with version '<Version>'" -Foreach $expectedExports {
            $expectedPath | Should -FileContentMatch "<package id=`"$([regex]::Escape($Name))`" version=`"$([regex]::Escape($Version))`" />"
        }

        It "Creates backup of previous package.config file" {
            # We are not interested in the content, only that it gets created
            "$expectedPath.backup" | Should -Exist
        }
    }

    Context "Runs export with output path" {
        BeforeDiscovery {
            # TODO: Consolidate these recurring package lists - https://github.com/chocolatey/choco/issues/2691
            # We set this here, so we can reuse them between different contexts
            $expectedExports = @(
                "chocolatey"
                "installpackage"
                "upgradepackage"
                "package.extension"
                "package.template"
            )
        }

        BeforeAll {
            $expectedPath = "C:\temp\packages.config"

            if (Test-Path $expectedPath) {
                Remove-Item $expectedPath
            }

            $Output = Invoke-Choco export $expectedPath
        }

        It "Exits with success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        # NOTE: There is no output other than the header, and possibly the unofficial statement
        It "Displays chocolatey version header" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Exports expected package '<_>'" -Foreach $expectedExports {
            $expectedPath | Should -FileContentMatch "<package id=`"$([regex]::Escape($_))`" />"
        }
    }

    Context "Runs export with including versions (<_>)" -Foreach @("--include-version-numbers", "--include-version") {
        BeforeDiscovery {
            # TODO: Consolidate these recurring package lists - https://github.com/chocolatey/choco/issues/2691
            # We set this here, so we can reuse them between different contexts
            $expectedExports = @(
                @{
                    Name    = "installpackage"
                    Version = "1.0.0"
                }
                @{
                    Name    = "upgradepackage"
                    Version = "1.1.1-beta"
                }
                @{
                    Name    = "package.extension"
                    Version = "1.0.0"
                }
                @{
                    Name    = "package.template"
                    Version = "1.0.0"
                }
            )
        }

        BeforeAll {
            $expectedPath = "C:\temp\packages.config"

            # This is in case we are running the test directly
            # which in that case this file will not exist.
            # We don't care about the content, encoding, only the presence
            if (!(Test-Path $expectedPath)) {
                "Not Important" | Set-Content -Path $expectedPath
            }

            $Output = Invoke-Choco export $expectedPath $_
        }

        It "Exits with success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        # NOTE: There is no output other than the header, and possibly the unofficial statement
        It "Displays chocolatey version header" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Exports expected package '<Name>' with version '<Version>'" -Foreach $expectedExports {
            $expectedPath | Should -FileContentMatch "<package id=`"$([regex]::Escape($Name))`" version=`"$([regex]::Escape($Version))`" />"
        }

        It "Creates backup of previous package.config file" {
            # We are not interested in the content, only that it gets created
            "$expectedPath.backup" | Should -Exist
        }
    }

    Context "Runs export with output path argument '<_>" -Foreach @("--output-file-path={0}"; "-o {0}") {
        BeforeDiscovery {
            # TODO: Consolidate these recurring package lists - https://github.com/chocolatey/choco/issues/2691
            # We set this here, so we can reuse them between different contexts
            $expectedExports = @(
                "chocolatey"
                "installpackage"
                "upgradepackage"
                "package.extension"
                "package.template"
            )
        }

        BeforeAll {
            $expectedPath = "C:\temp\packages.config"

            if (Test-Path $expectedPath) {
                Remove-Item $expectedPath
            }

            $Output = Invoke-Choco export ($_ -f $expectedPath)
        }

        It "Exits with success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        # NOTE: There is no output other than the header, and possibly the unofficial statement
        It "Displays chocolatey version header" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Exports expected package '<_>'" -Foreach $expectedExports {
            $expectedPath | Should -FileContentMatch "<package id=`"$([regex]::Escape($_))`" />"
        }
    }

    Context "Runs export with path argument (<PathArgument>) including versions (<VersionsArgument>)" -Foreach @(
        @{
            PathArgument     = "--output-file-path={0}"
            VersionsArgument = "--include-version-numbers"
        }
        @{
            PathArgument     = "--output-file-path={0}"
            VersionsArgument = "--include-version"
        }
        @{
            PathArgument     = "-o {0}"
            VersionsArgument = "--include-version-numbers"
        }
        @{
            PathArgument     = "-o {0}"
            VersionsArgument = "--include-version-numbers"
        }
    ) {
        BeforeDiscovery {
            # TODO: Consolidate these recurring package lists - https://github.com/chocolatey/choco/issues/2691
            # We set this here, so we can reuse them between different contexts
            $expectedExports = @(
                @{
                    Name    = "installpackage"
                    Version = "1.0.0"
                }
                @{
                    Name    = "upgradepackage"
                    Version = "1.1.1-beta"
                }
                @{
                    Name    = "package.extension"
                    Version = "1.0.0"
                }
                @{
                    Name    = "package.template"
                    Version = "1.0.0"
                }
            )
        }

        BeforeAll {
            $expectedPath = "C:\temp\packages.config"

            # This is in case we are running the test directly
            # which in that case this file will not exist.
            # We don't care about the content, encoding, only the presence
            if (!(Test-Path $expectedPath)) {
                "Not Important" | Set-Content -Path $expectedPath
            }

            $Output = Invoke-Choco export ($PathArgument -f $expectedPath) $VersionsArgument
        }

        It "Exits with success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        # NOTE: There is no output other than the header, and possibly the unofficial statement
        It "Displays chocolatey version header" {
            $Output.Lines | Should -Contain $expectedHeader
        }

        It "Exports expected package '<Name>' with version '<Version>'" -Foreach $expectedExports {
            $expectedPath | Should -FileContentMatch "<package id=`"$([regex]::Escape($Name))`" version=`"$([regex]::Escape($Version))`" />"
        }

        It "Creates backup of previous package.config file" {
            # We are not interested in the content, only that it gets created
            "$expectedPath.backup" | Should -Exist
        }
    }

    Context "Exporting to a path that do not exist" {
        BeforeAll {
            if (Test-Path "$(Get-TempDirectory)TestInvalidPath") {
                Remove-Item -Recurse -Path "$(Get-TempDirectory)TestInvalidPath"
            }

            $Output = Invoke-Choco export "$(Get-TempDirectory)TestInvalidPath\packages.config"
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Reports unable to export packages" {
            $Output.Lines | Should -Contain "Error exporting currently installed packages:"

            # Actual error after above line can be localized on a computer,
            # as such, it is not tested here.
        }
    }

    Context "Exporting with a misspelled argument name" {
        BeforeAll {
            # Just to be certain that we allow misspelled names
            Enable-ChocoFeature ignoreInvalidOptionsSwitches # Usually this is enabled by default, but just to be sure
            $path = "C:\temp\packages.config"
            if (Test-Path "$path") {
                Remove-Item -Recurse -Path "$path"
            }

            $Output = Invoke-Choco export --outpu-file-path=$path
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Reports unable to export packages" {
            $Output.Lines | Should -Contain "Error exporting currently installed packages:"

            # Actual error after above line can be localized on a computer,
            # as such, it is not tested here.
        }
    }

    Context "Exporting with a misspelled argument name and disallowing unknown switches" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot -SetWorkDir
            Disable-ChocoFeature ignoreInvalidOptionsSwitches

            $Output = Invoke-Choco export --include-version-numers
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays help page" {
            # We just use the summary for asserting whether help page was shown or not
            $Output.Lines | Should -Contain "Export all currently installed packages to a file."
        }
    }
}
