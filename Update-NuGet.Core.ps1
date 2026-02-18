param(
    [Parameter(Mandatory)]
    $NuGetPath,
    $ExistingNuGetVersion = '20260219',
    $Target = 'Debug'
)

Push-Location $NuGetPath
.\build.cmd $Target
mkdir nuget/Chocolatey-NuGet.Core/lib/net4
Copy-Item .\src\Core\bin\$Target\* .\nuget\Chocolatey-NuGet.Core\lib\net4\
Push-Location nuget/Chocolatey-NuGet.Core
.\strongname.cmd
Pop-Location
Pop-Location

Copy-Item $NuGetPath/nuget/Chocolatey-NuGet.Core/output/* "$PSScriptRoot/lib/Chocolatey-NuGet.Core.2.11.0.$ExistingNuGetVersion/lib/net4/"