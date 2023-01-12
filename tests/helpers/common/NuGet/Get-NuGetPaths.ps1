function Get-NuGetPaths {
    @(
        "$env:localappdata\NuGet"
        "$(Get-TempDirectory)\NuGetScratch"
        "$env:userprofile\.nuget"
        "${env:programfiles(x86)}\NuGet"
    )
}
