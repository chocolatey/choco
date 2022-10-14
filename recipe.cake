#load nuget:?package=Chocolatey.Cake.Recipe&version=0.17.1

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////

// This is needed in order to allow NUnit v2 tested to be executed by the NUnit
// v3 Test Runner
#tool nuget:?package=NUnit.Extension.NUnitV2Driver&version=3.9.0

///////////////////////////////////////////////////////////////////////////////
// SCRIPT
///////////////////////////////////////////////////////////////////////////////

Func<List<ILMergeConfig>> getILMergeConfigs = () =>
{
    var mergeConfigs = new List<ILMergeConfig>();

    var targetPlatform = "v4,C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework\\v4.0";
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

    return mergeConfigs;
};

Func<FilePathCollection> getScriptsToSign = () =>
{
    var scriptsToSign = GetFiles(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/**/*.{ps1|psm1}") +
                        GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/**/*.{ps1|psm1}");

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

Task("Prepare-NuGet-Packages")
    .IsDependeeOf("Create-NuGet-Packages")
    .IsDependeeOf("Sign-PowerShellScripts")
    .IsDependeeOf("Sign-Assemblies")
    .Does(() =>
{
    // Copy legal documents
    CleanDirectory(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib");
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.md", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib/CREDITS.txt");

    CopyFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey_merged/*", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib");
    CopyFile(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/chocolatey.xml", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/lib/chocolatey.xml");
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
                            preferDotNetGlobalToolUsage: !IsRunningOnWindows());

ToolSettings.SetToolSettings(context: Context,
                            buildMSBuildToolVersion: MSBuildToolVersion.NET40);

BuildParameters.PrintParameters(Context);

Build.Run();
