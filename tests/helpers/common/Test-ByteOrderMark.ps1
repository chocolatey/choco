# This function is based on the function found here: https://vertigion.com/2015/02/04/powershell-get-fileencoding/

function Test-ByteOrderMark {
    <#
        .SYNOPSIS
            Checks a single file if it contains a bye order mark (BOM) or
            not. If BOM is found, returns the encoding that were found.
            Otherwise, $false is returned.
    #>
    [CmdletBinding()]
    param(
        [ValidateScript( { Test-Path $_ })]
        [Parameter(Mandatory)]
        [string]$Path
    )
    [byte[]]$bom = Get-Content -Encoding Byte -ReadCount 4 -TotalCount 4 -Path $Path

    $encoding_found = $false

    foreach ($encoding in [System.Text.Encoding]::GetEncodings().GetEncoding()) {
        $preamble = $encoding.GetPreamble()
        if ($preamble) {
            for ($i = 0; ($i -lt $bom.Length -and $i -lt $preamble.Length); $i++) {
                if ($bom[$i] -ne $preamble[$i]) {
                    break
                }
            }

            if ($i -eq $preamble.Length) {
                $encoding_found = $encoding
            }
        }

        if ($encoding_found) {
            break
        }
    }

    $encoding_found
}
