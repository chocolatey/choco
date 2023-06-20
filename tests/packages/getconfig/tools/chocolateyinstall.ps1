$ErrorActionPreference = 'Stop'

$ValuesToTest = @(
    'addedTextValue'
    'addedNumberValue'
    'commandExecutionTimeoutSeconds'
    'cacheLocation'
    'nonExistentKey'
)

foreach ($Value in $ValuesToTest) {
    $Result = Get-ChocolateyConfigValue -configKey $Value
    Write-Host "${Value}: $Result"
}
