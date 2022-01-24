#Requires -Module @{ ModuleName = 'pester'; ModuleVersion = '5.3.1' }

Describe "Verifying integrity of module files" {
    BeforeDiscovery {
        $FilesToVerify = Get-ChildItem -Include '*.psm1', '*.ps1' -Recurse
        $DirectoriesToExclude = @(
            # These directories contain dependencies we bring in, we don't control their file formats
            'lib'
            'packages'
            '.nuget'
            # These directories contain output of builds, if there's anything in these with "bad" formatting it's likely an old build, or not relevant
            'bin'
            'obj'
            # This directory currently contains scripts to assist in creating the docker container. It is not formatted and should not require signing (at this time)
            'docker'
            # These directories contain uppercut configurations. Here be dragons
            '.build'
            '.build.custom'
            # GitHub/git configs. No PowerShell here?
            '.github'
            '.git'
        )
        $Slash = [System.IO.Path]::DirectorySeparatorChar
        $FilesBeingTested = $FilesToVerify | Where-Object { $null -ne $env:CHOCO_TEST_ALL -or $_.FullName -notmatch "\$Slash($($DirectoriesToExclude -join '|'))\$Slash" }
    }

    BeforeAll {
        function Get-FileEncoding {
            <#
			.SYNOPSIS
				Tests a file for encoding.

			.DESCRIPTION
				Tests a file for encoding.

			.PARAMETER Path
				The file to test
		#>
            [CmdletBinding()]
            Param (
                [Parameter(Mandatory = $True, ValueFromPipelineByPropertyName = $True)]
                [Alias('FullName')]
                [string]
                $Path
            )

            if ($PSVersionTable.PSVersion.Major -lt 6) {
                [byte[]]$byte = get-content -Encoding byte -ReadCount 4 -TotalCount 4 -Path $Path
            }
            else {
                [byte[]]$byte = Get-Content -AsByteStream -ReadCount 4 -TotalCount 4 -Path $Path
            }

            if ($byte[0] -eq 0xef -and $byte[1] -eq 0xbb -and $byte[2] -eq 0xbf) { 'UTF8 BOM' }
            elseif ($byte[0] -eq 0xfe -and $byte[1] -eq 0xff) { 'Unicode' }
            elseif ($byte[0] -eq 0 -and $byte[1] -eq 0 -and $byte[2] -eq 0xfe -and $byte[3] -eq 0xff) { 'UTF32' }
            elseif ($byte[0] -eq 0x2b -and $byte[1] -eq 0x2f -and $byte[2] -eq 0x76) { 'UTF7' }
            else { 'Unknown' }
        }
    }

    Context "Validating PowerShell file <_.FullName>" -Foreach $FilesBeingTested {
        BeforeAll {
            $FileUnderTest = $_
        }

        It "Should have Byte Order Mark" {
            Get-FileEncoding -Path $FileUnderTest.FullName | Should -Be 'UTF8 BOM'
        }

        It "Should have 'CRLF' Line Endings" {
            (Get-Content $FileUnderTest -Raw) -match '([^\r]\n|\r[^\n])' | Should -BeFalse
        }
    }
}
