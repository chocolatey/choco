function Clear-ChocoLogData {
    Remove-Item (Get-ChocoLogPath)
}