function Expand-ZipArchive {
    <#
    .Synopsis
        Helper function to extract the contents of an archive without a .zip extension.
#>
    [CmdletBinding()]
    param(
        # The intunewin file to extract
        [Parameter(Position = 1, Mandatory = $true)]
        $Source,
        # The location to put the files when done.
        [Parameter(Position = 2, Mandatory = $true)]
        $Destination
    )
    $zipFile = "$Source.zip"
    Rename-Item -Path $Source -NewName $zipFile
    try {
        Expand-Archive -Path $zipFile -DestinationPath $Destination
    }
    finally {
        Rename-Item -Path $zipFile -NewName $Source
    }
}
