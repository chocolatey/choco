function Get-ChocoLogData {
    # Strip out the date and other incidental data from the beginning of log lines
    $chocoLogData = (Get-Content (Get-ChocoLogPath)) -replace '^\d.* \[', '['
    [PSCustomObject]@{
        Lines = $chocoLogData
        String = $chocoLogData -join "`r`n"
    }
}