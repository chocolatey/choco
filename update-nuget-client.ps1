param(
    [string]$sourceLocation = $env:NUGET_SOURCE_LOCATION,
    [Parameter(ParameterSetName = 'build')]
    [switch]$build,
    [Parameter(ParameterSetName = 'build')]
    [int]$buildNumber
)

if (!$sourceLocation) {
    Write-Error "The Source location of NuGet.Client has not been set. Unable to continue..."
    return
}

$thisLocation = $PSScriptRoot

Push-Location $sourceLocation

if ($build -or !(Test-Path "$sourceLocation\artifacts")) {
    if (!$buildNumber) {
        [int]$buildNumber = Get-ChildItem "$thisLocation\src\packages\Chocolatey.Nuget.*" | ? {
            $_.Name -match "-\d+$"
        } | % {
            $_.Name -replace '^.*-(\d+)$',"`$1"
        } | Select-Object -First 1
    }

    "Configuring NuGet.Client repository dependencies"
    #.\configure.ps1
    
    "Calling .\build.ps1 -CI -SkipUnitTest -ChocolateyBuild -Configuration Debug -BuildNumber $buildNumber -ReleaseLabel 'zlocal'"
    .\build.ps1 -CI -SkipUnitTest -ChocolateyBuild -Configuration Debug -BuildNumber $buildNumber -ReleaseLabel 'zlocal'
}

Get-ChildItem "$thisLocation\src\packages\Chocolatey.NuGet.*" | ForEach-Object {
    $name = $_.Name -replace "^Chocolatey\.NuGet([^\d]+)(\.\d.*)$", "NuGet`$1"

    $destination = "$($_.FullName)\lib"
    Remove-Item "$destination\*\*"

    Get-ChildItem "$sourceLocation\artifacts\$name\bin\Debug" -Directory | ForEach-Object {
        $directoryName = $_.Name
        $files = "$($_.FullName)\Chocolatey.$name*"
        if (Test-Path $files) {
            "Copying Chocolatey.$name to $destination\$directoryName"
            $null = New-Item -ItemType Directory -Path "$destination\$directoryName" -Force -ErrorAction SilentlyContinue
            Copy-Item $files -Destination "$destination\$directoryName" -Force
        }
    }
}


Pop-Location