function Invoke-DownloadOrCacheFile {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        # The url to the resource if it do not already exist in a cache directory.
        [parameter(Mandatory)]
        [string]$Url,
        # The checksum of the resource, if not found or the found file do not match
        # the checksum the file will be redownloaded.
        # The checksum must be in 'sha1'
        [string]$Checksum,

        # The name of the file
        [string]$FileName,

        # The output path that any files will be copied to.
        [string]$OutFile
    )

    $cacheDirectory = "$(Get-TempDirectory)ChocolateyDownloadCache"
    # we do a naive implementation of acquiring the file name.
    if (!$fileName) {
        $fileName = Split-Path $url -Leaf
    }

    $fullPath = "$cacheDirectory\$fileName"

    if (Test-Path $fullPath) {
        # We use SHA1 as the checksum, as this is the highest checksum
        # visible on hermes.
        $actualChecksum = Get-FileHash -Path $fullPath -Algorithm SHA1 | Select-Object -ExpandProperty Hash
        if ($actualChecksum -eq $checksum) {
            if ($OutFile) {
                $null = Copy-Item $fullPath $OutFile
                return $OutFile
            }

            return $fullPath
        }

        $null = Remove-Item $fullPath
    }
    elseif (!(Test-Path $cacheDirectory)) {
        $null = New-Item $cacheDirectory -ItemType Directory
    }

    $client = New-Object -TypeName System.Net.WebClient
    $null = $client.DownloadFile($Url, $fullPath)

    $actualChecksum = Get-FileHash -Path $fullPath -Algorithm SHA1 | Select-Object -ExpandProperty Hash

    if ($actualChecksum -ne $checksum) {
        throw 'Checksum of downloaded file do not match the expected checksum!'
    }

    if ($OutFile) {
        $null = Copy-Item $fullPath $OutFile
        $OutFile
    }
    else {
        $fullPath
    }
}
