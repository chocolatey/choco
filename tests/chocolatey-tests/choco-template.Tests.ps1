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
    }
}
