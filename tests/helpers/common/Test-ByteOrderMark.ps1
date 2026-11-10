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
    # Read the first 4 bytes via System.IO so this works on both Windows PowerShell 5.1
    # and PowerShell 7+. The 5.1-only `-Encoding Byte` switch was removed in PowerShell 6
    # (it's `-AsByteStream` there), so the previous form silently broke the BOM check on PS7.
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    [byte[]]$bom = if ($bytes.Length -ge 4) { $bytes[0..3] } else { $bytes }

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
