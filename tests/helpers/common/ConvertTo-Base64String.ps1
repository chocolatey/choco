function ConvertTo-Base64String {
    <#
        .Synopsis
            Helper function to Convert a string to a Base64 encoded string.
    #>
    [CmdletBinding()]
    param(
        # The string to be converted to base64.
        [Parameter(ValueFromPipeline)]
        [string]$InputObject
    )
    $bytes = [system.text.encoding]::Unicode.GetBytes($InputObject)
    [Convert]::ToBase64String($bytes)
}
