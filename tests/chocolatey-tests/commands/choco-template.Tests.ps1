Import-Module helpers/common-helpers

Describe "choco <_>" -ForEach @(
    "template"
    "templates"
) -Tag Chocolatey, TemplateCommand {
    BeforeDiscovery {

    }

    BeforeAll {
        Initialize-ChocolateyTestInstall

        New-ChocolateyInstallSnapshot
    }

    AfterAll {
        Remove-ChocolateyTestInstall
    }

    Context "Running without subcommand specified" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install msi.template zip.template

            $Output = Invoke-Choco $_
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays the templates expected" {
            $Output.Lines | Should -Contain 'msi 1.0.2'
            $Output.Lines | Should -Contain 'zip 1.0.0'
        }

        It "Displays how many custom templates are available" {
            $Output.Lines | Should -Contain "2 Custom templates found at $env:ChocolateyInstall\templates"
        }
    }

    Context "Running with list subcommand" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install msi.template zip.template

            $Output = Invoke-Choco $_ list
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays the templates expected" {
            $Output.Lines | Should -Contain 'msi 1.0.2'
            $Output.Lines | Should -Contain 'zip 1.0.0'
        }

        It "Displays how many custom templates are available" {
            $Output.Lines | Should -Contain "2 Custom templates found at $env:ChocolateyInstall\templates"
        }
    }

    Context "Running with info subcommand specified with no additional parameters" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install msi.template zip.template

            $Output = Invoke-Choco $_ info
        }

        It "Exits with Failure (1)" {
            $Output.ExitCode | Should -Be 1
        }

        It "Displays error with correct format" {
            $Output.Lines | Should -Contain "When specifying the subcommand 'info', you must also specify --name."
        }
    }

    Context "Running with info subcommand specified with --name parameters" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install msi.template zip.template

            $Output = Invoke-Choco $_ info --name msi
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays template information" {
            $Output.Lines | Should -Contain "Template name: msi"
            $Output.Lines | Should -Contain "Version: 1.0.2"
            $Output.Lines | Should -Contain "Default template: False"
            $Output.Lines | Should -Contain "Summary: MSI Chocolatey template"
            $Output.Lines | Should -Contain "### Chocolatey MSI template"
            $Output.Lines | Should -Contain "This adds a template for MSI packages."
            $Output.Lines | Should -Contain "List of files:"
            $Output.Lines | Should -Contain "$env:ChocolateyInstall\templates\msi\msi.nuspec"
            $Output.Lines | Should -Contain "$env:ChocolateyInstall\templates\msi\ReadMe.md"
            $Output.Lines | Should -Contain "$env:ChocolateyInstall\templates\msi\tools\chocolateybeforemodify.ps1"
            $Output.Lines | Should -Contain "$env:ChocolateyInstall\templates\msi\tools\chocolateyinstall.ps1"
            $Output.Lines | Should -Contain "$env:ChocolateyInstall\templates\msi\tools\chocolateyuninstall.ps1"
            $Output.Lines | Should -Contain "$env:ChocolateyInstall\templates\msi\tools\LICENSE.txt"
            $Output.Lines | Should -Contain "$env:ChocolateyInstall\templates\msi\tools\VERIFICATION.txt"
        }

        It "Displays section of parameters" {
            $Output.Lines | Should -Contain 'List of Parameters:'
        }

        It "Displays parameter name <_>" -Foreach @('PackageNameLower'; 'PackageVersion'; 'MaintainerRepo'; 'MaintainerName'; 'PackageName'; 'AutomaticPackageNotesNuspec'; 'AutomaticPackageNotesInstaller'; 'Url'; 'Url64'; 'Checksum'; 'Checksum64'; 'SilentArgs') {
            $Output.Lines | Should -Contain $_
        }

        It "Created parameters file for msi" {
            "$env:ChocolateyInstall\templates\msi\.parameters" | Should -Exist
        }

        It "Does not create parameters file for zip" {
            "$env:ChocolateyInstall\templates\zip\.parameters" | Should -Not -Exist
        }
    }

    Context "Running with no subcommand specified with --name parameter" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install msi.template zip.template

            $Output = Invoke-Choco $_ --name msi
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays template name and version" {
            $Output.Lines | Should -Contain "msi 1.0.2"
        }
    }

    Context "Running without subcommand specified after default template name is specified" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install msi.template zip.template

            $null = Invoke-Choco config set defaultTemplateName zip

            $Output = Invoke-Choco $_
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays the templates marking the default as expected" {
            $Output.Lines | Should -Contain '* zip 1.0.0'
            $Output.Lines | Should -Contain 'Built-in template is not default, it can be specified if the --built-in parameter is used'
        }

        It "Displays how many custom templates are available" {
            $Output.Lines | Should -Contain "2 Custom templates found at $env:ChocolateyInstall\templates"
        }
    }

    Context "Running without subcommand specified after an invalid default template name is specified" {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install msi.template zip.template

            $null = Invoke-Choco config set defaultTemplateName zp

            $Output = Invoke-Choco $_
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays the templates marking the default as expected" {
            $Output.Lines | Should -Contain 'zip 1.0.0'
            $Output.Lines | Should -Contain 'Built-in template is default.'
        }

        It "Displays how many custom templates are available" {
            $Output.Lines | Should -Contain "2 Custom templates found at $env:ChocolateyInstall\templates"
        }
    }

    Context 'Running <_> with verbose argument' {
        BeforeAll {
            Restore-ChocolateyInstallSnapshot

            $null = Invoke-Choco install msi.template zip.template

            $Output = Invoke-Choco $_ --verbose
        }

        It "Exits with Success (0)" {
            $Output.ExitCode | Should -Be 0
        }

        It "Displays <Name> template information" -ForEach @(
            @{
                Name  = 'msi'
                Lines = @(
                    'Template name: msi'
                    'Version: 1.0.2'
                    'Default template: False'
                    'Summary: MSI Chocolatey template'
                    '### Chocolatey MSI template'
                    'This adds a template for MSI packages.'
                    'List of files:'
                )
            }
            @{
                Name  = 'zip'
                Lines = @(
                    'Template name: zip'
                    'Version: 1.0.0'
                    'Default template: False'
                    'Summary: Zip Chocolatey template'
                    '### Chocolatey Zip template'
                    'This adds a template for archive (zipped) packages.'
                    'List of files:'
                )
            }
        ) {
            $Lines | ForEach-Object {
                $Output.Lines | Should -Contain $_ -Because $Output.String
            }
        }

        It "Displays <Name> template information for files" -Foreach @(
            @{
                Name = 'msi'
                Files = @(
                    'msi\msi.nuspec'
                    'msi\ReadMe.md'
                    'msi\tools\chocolateybeforemodify.ps1'
                    'msi\tools\chocolateyinstall.ps1'
                    'msi\tools\chocolateyuninstall.ps1'
                    'msi\tools\LICENSE.txt'
                    'msi\tools\VERIFICATION.txt'
                )
            }
            @{
                Name = 'zip'
                Files = @(
                    'zip\zip.nuspec'
                    'zip\ReadMe.md'
                    'zip\tools\chocolateybeforemodify.ps1'
                    'zip\tools\chocolateyinstall.ps1'
                    'zip\tools\LICENSE.txt'
                    'zip\tools\VERIFICATION.txt'
                )
            }
        ) {
            $Files | ForEach-Object {
                $Output.Lines | Should -Contain "$env:ChocolateyInstall\templates\$_" -Because $Output.String
            }
        }

        It "Displays how many custom templates are available" {
            $Output.Lines | Should -Contain "2 Custom templates found at $env:ChocolateyInstall\templates"
        }

        It "Displays section of parameters" {
            $items = $Output.Lines | Where-Object { $_ -eq 'List of Parameters:' }
            $items | Should -HaveCount 2 -Because $Output.String
        }

        It "Displays parameter name <_>" -Foreach @('PackageNameLower'; 'PackageVersion'; 'MaintainerRepo'; 'MaintainerName'; 'PackageName'; 'AutomaticPackageNotesNuspec'; 'AutomaticPackageNotesInstaller'; 'Url'; 'Url64'; 'Checksum'; 'Checksum64') {
            $parameter = $_
            $items = $Output.Lines | Where-Object { $_ -eq $parameter }
            $items | Should -HaveCount 2 -Because $Output.String
        }

        It "Displays parameter name <_>" -Foreach @('SilentArgs') {
            $parameter = $_
            $items = $Output.Lines | Where-Object { $_ -eq $parameter }
            $items | Should -HaveCount 1 -Because $Output.String
        }

        It "Created parameters file for <_>" -Foreach @('msi', 'zip') {
            "$env:ChocolateyInstall\templates\$_\.parameters" | Should -Exist
        }
    }
}
