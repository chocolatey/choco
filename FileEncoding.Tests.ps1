Describe "Verifying integrity of module files" {
    $moduleRoot = (Resolve-Path "$PSScriptRoot").Path
    BeforeDiscovery {
        $FilesToVerify = Get-ChildItem -Include '*.psm1', '*.ps1' -Recurse
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

    Context "Validating PS1 Script files" {
        It "Should have Byte Order Mark (<_.FullName>)" -ForEach $FilesToVerify {
            Get-FileEncoding -Path $_.FullName | Should -Be 'UTF8 BOM'
        }
    }
}
