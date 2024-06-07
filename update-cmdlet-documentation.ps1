<#
.SYNOPSIS
Generates Markdown documentation for the Chocolatey.PowerShell portion of Chocolatey's installer module commands.

.DESCRIPTION
Use this script when modifying or adding commands into the Chocolatey.PowerShell project.
You will need the chocolatey/docs repository cloned locally in order to use this script.
When all documentation files this script is monitoring have been filled out (contain no remaining {{ template tokens }}), this script will generate/update the external help xml files for PowerShell.
#>
[CmdletBinding()]
param(
    # Specify the path to the chocolatey/docs repository root locally. Defaults to ../docs
    [Parameter()]
    [string]
    $DocsRepositoryPath = "$PSScriptRoot/../docs",

    # Specify the new commands' names to generate new documentation pages for them.
    [Parameter()]
    [Alias('NewCommands')]
    [string[]]
    $NewCommand,

    # Opens any new or incomplete files in the default editor for Markdown (.md) files for editing.
    [Parameter()]
    [switch]
    $OpenUnfinished
)

if (-not (Get-Module -ListAvailable PlatyPS)) {
    Write-Warning "PlatyPS module not found, attempting to install from PSGallery"
    Install-Module PlatyPS -Scope CurrentUser
}

$documentationPath = Join-Path $DocsRepositoryPath -ChildPath "src\content\docs\en-us\create\cmdlets"
if (-not (Test-Path $DocsRepositoryPath)) {
    throw "PowerShell commands docs folder was not found at '$documentationPath'. Please clone the chocolatey/docs repository locally first, and/or provide the path to the repo root as -DocsRepositoryPath to this script."
}

$dllPath = "$PSScriptRoot/code_drop/temp/_PublishedLibs/Chocolatey.PowerShell/Chocolatey.PowerShell.dll"

if (-not (Test-Path $dllPath)) {
    throw "Please run this repository's build.ps1 file before trying to build markdown help for this module."
}

# Rename .mdx to .md and transform anything platyps doesn't like and can't handle
$renamedFiles = Get-ChildItem -Path $documentationPath -Filter '*.md*' |
    Where-Object Name -notlike "index.*" |
    Rename-Item -NewName { $_.BaseName + ".md" } -PassThru |
    ForEach-Object {
        $content = Get-Content -Path $_.FullName
        $content = $content -replace '<Xref title="(?<label>[^"]+)" value="(?<xref>[^"]+)" classes="(?<classes>[^"]+)" />', '[${label}](${xref},${classes})'
        $content | Set-Content -Path $_.FullName
    }

# Import the module .dll to generate / update help from.
Import-Module $dllPath

if (-not (Get-Module Chocolatey.PowerShell)) {
    throw "The Chocolatey.PowerShell module was not able to be loaded, exiting documentation generation."
}

$newOrUpdatedFiles = [System.Collections.Generic.HashSet[System.IO.FileSystemInfo]] @(
    if ($NewCommand) {
        New-MarkdownHelp -Command $NewCommand -OutputFolder "$PSScriptRoot\docs" -ExcludeDontShow
    }

    Update-MarkdownHelp -Path $documentationPath -ExcludeDontShow
)

$incompleteFiles = $newOrUpdatedFiles | Select-String '\{\{[^}]+}}' | Select-Object -ExpandProperty Path

if ($incompleteFiles) {
    Write-Warning "The following files contain {{ template tokens }} from PlatyPS that must be replaced with help content before they are committed to the repository:"
    $incompleteFiles | Write-Warning

    if ($OpenUnfinished) {
        $incompleteFiles | Invoke-Item
    }

    Write-Warning "Run this script again once these files have been updated in order to generate the XML help documentation for the module."
}
else {
    New-ExternalHelp -Path $documentationPath -OutputPath "$PSScriptRoot/src/Chocolatey.PowerShell" -Force

    $newOrUpdatedFiles = $newOrUpdatedFiles |
        Rename-Item -NewName { $_.BaseName + ".mdx" } -PassThru |
        ForEach-Object {
            $content = Get-Content -Path $_.FullName
            $content = $content -replace '\[(?<name>[^\]]+)\]\((?<xref>[^,]+),(?<classes>[^)]+)\)', '<Xref title="${name}" value="${xref}" classes="${classes}" />'

            $frontMatterBounds = 0
            $content = $content | ForEach-Object {
                $_

                if ($_ -eq '---') {
                    $frontMatterBounds++

                    if ($frontMatterBounds -eq 2) {
                        "import Xref from '@components/Xref.astro';"
                    }
                }
            }

            $content | Set-Content -Path $_.FullName

            $_
        }

}

# Output the new/updated files so calling user knows what files the script has touched.
$newOrUpdatedFiles