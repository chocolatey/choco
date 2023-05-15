function Get-NuGetPaths {
    @(
        # "$env:localappdata\NuGet" # The directory gets created during running v1 commands
        "$(Get-TempDirectory)\NuGetScratch"
        "$env:userprofile\.nuget"
        "${env:programfiles(x86)}\NuGet"
    )
}
