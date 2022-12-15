#load nuget:?package=Chocolatey.Cake.Recipe&version=0.20.1

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// SCRIPT
///////////////////////////////////////////////////////////////////////////////

Func<List<ILMergeConfig>> getILMergeConfigs = () =>
{
    var mergeConfigs = new List<ILMergeConfig>();

    var targetPlatform = "v4,C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.8";
    var assembliesToILMerge = GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/*.{exe|dll}")
                            - GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/choco.exe")
                            - GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/System.Management.Automation.dll");

    Information("The following assemblies have been selected to be ILMerged for choco.exe...");
    foreach (var assemblyToILMerge in assembliesToILMerge)
    {
        Information(assemblyToILMerge.FullPath);
    }

    mergeConfigs.Add(new ILMergeConfig() {
        KeyFile = BuildParameters.StrongNameKeyPath,
        LogFile = BuildParameters.Paths.Directories.Build + "/ilmerge-chocoexe.log",
        TargetPlatform = targetPlatform,
        Target = "exe",
        Internalize = BuildParameters.RootDirectoryPath + "/src/chocolatey.console/ilmerge.internalize.ignore.txt",
        Output = BuildParameters.Paths.Directories.PublishedApplications + "/choco_merged/choco.exe",
        PrimaryAssemblyName = BuildParameters.Paths.Directories.PublishedApplications + "/choco/choco.exe",
        AssemblyPaths = assembliesToILMerge });

    assembliesToILMerge = GetFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/*.{exe|dll}")
                            - GetFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/choco.exe")
                            - GetFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/chocolatey.dll")
                            - GetFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/log4net.dll")
                            - GetFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/System.Management.Automation.dll");

    Information("The following assemblies have been selected to be ILMerged for chocolatey.dll...");
    foreach (var assemblyToILMerge in assembliesToILMerge)
    {
        Information(assemblyToILMerge.FullPath);
    }

    mergeConfigs.Add(new ILMergeConfig() {
        KeyFile = BuildParameters.StrongNameKeyPath,
        LogFile = BuildParameters.Paths.Directories.Build + "/ilmerge-chocolateydll.log",
        TargetPlatform = targetPlatform,
        Target = "dll",
        Internalize = BuildParameters.RootDirectoryPath + "/src/chocolatey/ilmerge.internalize.ignore.dll.txt",
        Output = BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey_merged/chocolatey.dll",
        PrimaryAssemblyName = BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/chocolatey.dll",
        AssemblyPaths = assembliesToILMerge });

    if (DirectoryExists(BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip/"))
    {
        var no7zAssembliesToILMerge = GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip/*.{exe|dll}")
                                - GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip/choco.exe")
                                - GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip/System.Management.Automation.dll")
                                - GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip/chocolatey.tests*.dll")
                                - GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip/{Moq|nunit|Should|testcentric}*.dll");

        Information("The following assemblies have been selected to be ILMerged for choco.exe No7zip Version...");
        foreach (var assemblyToILMerge in no7zAssembliesToILMerge)
        {
            Information(assemblyToILMerge.FullPath);
        }

        mergeConfigs.Add(new ILMergeConfig() {
            KeyFile = BuildParameters.StrongNameKeyPath,
            LogFile = BuildParameters.Paths.Directories.Build + "/ilmerge-chocono7zipexe.log",
            TargetPlatform = targetPlatform,
            Target = "exe",
            Internalize = BuildParameters.RootDirectoryPath + "/src/chocolatey.console/ilmerge.internalize.ignore.txt",
            Output = BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip_merged/choco.exe",
            PrimaryAssemblyName = BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip/choco.exe",
            AssemblyPaths = no7zAssembliesToILMerge });
    }

    return mergeConfigs;
};

Func<FilePathCollection> getScriptsToSign = () =>
{
    var scriptsToSign = GetFiles(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/**/*.{ps1|psm1}") +
                        GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/**/*.{ps1|psm1}");

    if (DirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "-no7zip"))
    {
        scriptsToSign += GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "-no7zip/**/*.{ps1|psm1}");
    }

    Information("The following PowerShell scripts have been selected to be signed...");
    foreach (var scriptToSign in scriptsToSign)
    {
        Information(scriptToSign.FullPath);
    }

    return scriptsToSign;
};

Func<FilePathCollection> getFilesToSign = () =>
{
    var filesToSign = GetFiles(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib/chocolatey.dll")
                    + GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/choco.exe")
                    + GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/tools/{checksum|shimgen}.exe")
                    + GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/redirects/*.exe");

    if (DirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "-no7zip"))
    {
        filesToSign += GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "-no7zip/tools/chocolateyInstall/choco.exe")
                    + GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "-no7zip/tools/chocolateyInstall/tools/{checksum|shimgen}.exe")
                    + GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "-no7zip/tools/chocolateyInstall/redirects/*.exe");
    }

    Information("The following assemblies have been selected to be signed...");
    foreach (var fileToSign in filesToSign)
    {
        Information(fileToSign.FullPath);
    }

    return filesToSign;
};

///////////////////////////////////////////////////////////////////////////////
// CUSTOM TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Prepare-Chocolatey-Packages")
    .IsDependeeOf("Create-Chocolatey-Packages")
    .IsDependeeOf("Sign-PowerShellScripts")
    .IsDependeeOf("Sign-Assemblies")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping because not running on Windows")
    .WithCriteria(() => BuildParameters.ShouldRunChocolatey, "Skipping because execution of Chocolatey has been disabled")
    .Does(() =>
{
    // Copy legal documents
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.md", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/CREDITS.txt");

    // Run Chocolatey Unpackself
    CopyFile(BuildParameters.Paths.Directories.PublishedApplications + "/choco_merged/choco.exe", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/choco.exe");

    StartProcess(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/choco.exe", new ProcessSettings{ Arguments = "unpackself -f -y --allow-unofficial-build" });

    // Tidy up logs and config folder which are not required
    var logsDirectory = BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/logs";
    var configDirectory = BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/config";

    if (DirectoryExists(logsDirectory))
    {
        DeleteDirectory(logsDirectory, new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        });
    }

    if (DirectoryExists(configDirectory))
    {
        DeleteDirectory(configDirectory, new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        });
    }
});

Task("Build-ChocolateyNo7zip")
    .WithCriteria(() => BuildParameters.Configuration == "ReleaseOfficial", "Skipping No7zip because this isn't an official release")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependeeOf("Run-ILMerge")
    .Does<BuildData>(data => RequireTool(ToolSettings.MSBuildExtensionPackTool, () =>
{
    Information("Building {0} with No7zip", BuildParameters.SolutionFilePath);

    CleanDirectory(BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip/");

    var no7zLogPath = BuildParameters.Paths.Files.BuildLogFilePath.ToString().Replace("\\.(\\S+)$", "-no7zip.${1}");

    if (BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows)
    {
        var msbuildSettings = new MSBuildSettings()
            {
                ToolPath = ToolSettings.MSBuildToolPath
            }
            .SetPlatformTarget(ToolSettings.BuildPlatformTarget)
            .UseToolVersion(ToolSettings.BuildMSBuildToolVersion)
            .WithProperty("OutputPath", MakeAbsolute(new DirectoryPath(BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip/")).FullPath)
            .WithProperty("TreatWarningsAsErrors", BuildParameters.TreatWarningsAsErrors.ToString())
            .WithTarget("Build")
            .SetMaxCpuCount(ToolSettings.MaxCpuCount)
            .SetConfiguration("ReleaseOfficialNo7zip")
            .WithLogger(
                Context.Tools.Resolve("MSBuild.ExtensionPack.Loggers.dll").FullPath,
                "XmlFileLogger",
                string.Format(
                    "logfile=\"{0}\";invalidCharReplacement=_;verbosity=Detailed;encoding=UTF-8",
                    no7zLogPath
                )
            );

        MSBuild(BuildParameters.SolutionFilePath, msbuildSettings);
    }

    if (FileExists(no7zLogPath))
    {
        BuildParameters.BuildProvider.UploadArtifact(no7zLogPath);
    }
}));

Task("Prepare-ChocolateyNo7zip-Package")
    .WithCriteria(() => BuildParameters.Configuration == "ReleaseOfficial", "Skipping No7zip because this isn't an official release")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping because not running on Windows")
    .WithCriteria(() => BuildParameters.ShouldRunChocolatey, "Skipping because execution of Chocolatey has been disabled")
    .IsDependentOn("Build-ChocolateyNo7zip")
    .IsDependeeOf("Sign-Assemblies")
    .IsDependeeOf("Sign-PowerShellScripts")
    .IsDependeeOf("Create-ChocolateyNo7zip-Package")
    .Does(() =>
{
    var nuspecDirectory = BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "-no7zip";
    // Copy the Nuget/Chocolatey directory from Root Folder to temp/nuspec/chocolatey-no7zip
    EnsureDirectoryExists(nuspecDirectory);
    CopyFiles(GetFiles("./nuspec/chocolatey/**/*"), nuspecDirectory, true);

    // Copy legal documents
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.md", nuspecDirectory + "/tools/chocolateyInstall/CREDITS.txt");

    // Run Chocolatey Unpackself
    CopyFile(BuildParameters.Paths.Directories.PublishedApplications + "/choco-no7zip_merged/choco.exe", nuspecDirectory + "/tools/chocolateyInstall/choco.exe");

    StartProcess(nuspecDirectory + "/tools/chocolateyInstall/choco.exe", new ProcessSettings{ Arguments = "unpackself -f -y --allow-unofficial-build" });

    // Tidy up logs and config folder which are not required
    var logsDirectory = nuspecDirectory + "/tools/chocolateyInstall/logs";
    var configDirectory = nuspecDirectory + "/tools/chocolateyInstall/config";

    if (DirectoryExists(logsDirectory))
    {
        DeleteDirectory(logsDirectory, new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        });
    }

    if (DirectoryExists(configDirectory))
    {
        DeleteDirectory(configDirectory, new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        });
    }
});

Task("Create-ChocolateyNo7zip-Package")
    .WithCriteria(() => BuildParameters.Configuration == "ReleaseOfficial", "Skipping No7zip because this isn't an official release")
    .WithCriteria(() => BuildParameters.ShouldRunChocolatey, "Skipping because execution of Chocolatey has been disabled")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping because not running on Windows")
    .IsDependentOn("Prepare-ChocolateyNo7zip-Package")
    .IsDependeeOf("Package")
    .Does(() =>
{
    var nuspecDirectory = BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "-no7zip/";
    var nuspecFile = nuspecDirectory + "chocolatey.nuspec";

    ChocolateyPack(nuspecFile, new ChocolateyPackSettings {
        AllowUnofficial = true,
        Version = BuildParameters.Version.PackageVersion,
        OutputDirectory = nuspecDirectory,
        WorkingDirectory = BuildParameters.Paths.Directories.PublishedApplications
    });

    MoveFile(
        nuspecDirectory + "chocolatey." + BuildParameters.Version.PackageVersion + ".nupkg",
        BuildParameters.Paths.Directories.ChocolateyPackages + "/chocolatey-no7zip." + BuildParameters.Version.PackageVersion + ".nupkg"
    );

    // Due to the fact that we have chosen to ignore the no7zip package via the chocolateyNupkgGlobbingPattern, it will
    // no longer be automatically uploaded via Chocolatey.Cake.Recipe, so we need to handle that work here.
    BuildParameters.BuildProvider.UploadArtifact(BuildParameters.Paths.Directories.ChocolateyPackages + "/chocolatey-no7zip." + BuildParameters.Version.PackageVersion + ".nupkg");
});

Task("Prepare-NuGet-Packages")
    .WithCriteria(() => BuildParameters.ShouldRunNuGet, "Skipping because execution of NuGet has been disabled")
    .IsDependeeOf("Create-NuGet-Packages")
    .IsDependeeOf("Sign-PowerShellScripts")
    .IsDependeeOf("Sign-Assemblies")
    .Does(() =>
{
    CleanDirectory(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib");
    EnsureDirectoryExists(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib/net48");

    // Copy legal documents
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.md", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib/CREDITS.txt");

    CopyFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey_merged/*", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib/net48");
    CopyFile(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/chocolatey.xml", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib/net48/chocolatey.xml");
});

///////////////////////////////////////////////////////////////////////////////
// RECIPE SCRIPT
///////////////////////////////////////////////////////////////////////////////

Environment.SetVariableNames();

BuildParameters.SetParameters(context: Context,
                            buildSystem: BuildSystem,
                            sourceDirectoryPath: "./src",
                            solutionFilePath: "./src/chocolatey.sln",
                            solutionDirectoryPath: "./src/chocolatey",
                            resharperSettingsFileName: "chocolatey.sln.DotSettings",
                            title: "Chocolatey",
                            repositoryOwner: "chocolatey",
                            repositoryName: "choco",
                            productName: "Chocolatey",
                            productDescription: "chocolatey is a product of Chocolatey Software, Inc. - All Rights Reserved.",
                            productCopyright: string.Format("Copyright © 2017 - {0} Chocolatey Software, Inc. Copyright © 2011 - 2017, RealDimensions Software, LLC - All Rights Reserved.", DateTime.Now.Year),
                            shouldStrongNameSignDependentAssemblies: false,
                            treatWarningsAsErrors: false,
                            getScriptsToSign: getScriptsToSign,
                            getFilesToSign: getFilesToSign,
                            getILMergeConfigs: getILMergeConfigs,
                            preferDotNetGlobalToolUsage: !IsRunningOnWindows(),
                            shouldRunNuGet: IsRunningOnWindows(),
                            shouldPublishAwsLambdas: false,
                            chocolateyNupkgGlobbingPattern: "/**/chocolatey[!-no7zip]*.nupkg");

ToolSettings.SetToolSettings(context: Context);

BuildParameters.PrintParameters(Context);

Build.Run();