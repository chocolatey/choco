#load nuget:?package=Chocolatey.Cake.Recipe&version=0.30.1
#tool nuget:?package=WiX&version=3.11.2

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
                            - GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/System.Management.Automation.dll")
                            - GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/Chocolatey.PowerShell.dll");

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
                        - GetFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/System.Management.Automation.dll")
                        - GetFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/Chocolatey.PowerShell.dll");

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

Func<FilePathCollection> getScriptsToVerify = () =>
{
    var scriptsToVerify = GetFiles("./src/chocolatey.resources/**/*.{ps1|psm1|psd1}") +
                        GetFiles(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/**/*.{ps1|psm1|psd1}") +
                        GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/**/*.{ps1|psm1|psd1}");

    Information("The following PowerShell scripts have been selected to be verified...");
    foreach (var scriptToVerify in scriptsToVerify)
    {
        Information(scriptToVerify.FullPath);
    }

    return scriptsToVerify;
};

Func<FilePathCollection> getScriptsToSign = () =>
{
    var scriptsToSign = GetFiles("./nuspec/**/*.{ps1|psm1|psd1}") +
                        GetFiles("./src/chocolatey.resources/**/*.{ps1|psm1|psd1}");

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
                    + GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/redirects/*.exe")
                    + GetFiles(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/helpers/Chocolatey.PowerShell.dll");

    Information("The following assemblies have been selected to be signed...");
    foreach (var fileToSign in filesToSign)
    {
        Information(fileToSign.FullPath);
    }

    return filesToSign;
};

Func<FilePathCollection> getMsisToSign = () =>
{
    var msisToSign = GetFiles(BuildParameters.Paths.Directories.Build + "/MSIs/**/*.msi");

    Information("The following msi's have been selected to be signed...");
    foreach (var msiToSign in msisToSign)
    {
        Information(msiToSign.FullPath);
    }

    return msisToSign;
};

///////////////////////////////////////////////////////////////////////////////
// CUSTOM TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Prepare-Chocolatey-Packages")
    .IsDependeeOf("Create-Chocolatey-Packages")
    .IsDependeeOf("Verify-PowerShellScripts")
    .IsDependeeOf("Sign-Assemblies")
    .IsDependentOn("Copy-Nuspec-Folders")
    .WithCriteria(() => BuildParameters.BuildAgentOperatingSystem == PlatformFamily.Windows, "Skipping because not running on Windows")
    .WithCriteria(() => BuildParameters.ShouldRunChocolatey, "Skipping because execution of Chocolatey has been disabled")
    .Does(() =>
{
    // Copy legal documents
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.md", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/CREDITS.txt");
    CopyFile(BuildParameters.Paths.Directories.PublishedApplications + "/choco/LICENSE.txt", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/LICENSE.txt");

    // Copy choco.exe.manifest
    CopyFile(BuildParameters.Paths.Directories.PublishedApplications + "/choco/choco.exe.manifest", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/choco.exe.manifest");

    // Copy external file resources
    EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/helpers");
    CopyFiles(GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/helpers/**/*"), BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/helpers", true);
    EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/redirects");
    CopyFiles(GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/redirects/**/*"), BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/redirects", true);
    EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/tools");
    CopyFiles(GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/tools/**/*"), BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/tools", true);

    // Copy merged choco.exe
    CopyFile(BuildParameters.Paths.Directories.PublishedApplications + "/choco_merged/choco.exe", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/choco.exe");

    // Copy Chocolatey.PowerShell.dll and its help.xml file
    CopyFile(BuildParameters.Paths.Directories.PublishedLibraries + "/Chocolatey.PowerShell/Chocolatey.PowerShell.dll", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/helpers/Chocolatey.PowerShell.dll");
    CopyFile(BuildParameters.Paths.Directories.PublishedLibraries + "/Chocolatey.PowerShell/Chocolatey.PowerShell.dll-help.xml", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/helpers/Chocolatey.PowerShell.dll-help.xml");

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
    .WithCriteria(() => BuildParameters.ShouldRunNuGet, "Skipping because execution of NuGet has been disabled")
    .IsDependeeOf("Create-NuGet-Packages")
    .IsDependeeOf("Verify-PowerShellScripts")
    .IsDependeeOf("Sign-Assemblies")
    .Does(() =>
{
    CleanDirectory(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib");
    EnsureDirectoryExists(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/net48");

    // Copy legal documents
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.md", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/CREDITS.txt");
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.json", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/CREDITS.json");
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.pdf", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/CREDITS.pdf");

    CopyFiles(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey_merged/*", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/net48");
    CopyFile(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/chocolatey.xml", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/net48/chocolatey.xml");
});

Task("Prepare-MSI")
    .WithCriteria(() => BuildParameters.ShouldBuildMsi, "Skipping because creation of MSI has been disabled")
    .IsDependeeOf("Build-MSI")
    .Does(() =>
{
    var installScriptPath = BuildParameters.RootDirectoryPath + "/src/chocolatey.install/assets/Install.ps1";

    if (!FileExists(installScriptPath)) 
    {
        DownloadFile(
            "https://community.chocolatey.org/install.ps1",
            installScriptPath
        );
    }
});

Task("Create-TarGz-Packages")
    .IsDependentOn("Build")
    .IsDependeeOf("Package")
    .WithCriteria(!IsRunningOnWindows(), "Skipping because this is a Windows build")
    .Does(() =>
{
    EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyPackages);

    var outputFile = string.Format(
        "{0}/chocolatey.v{1}.tar.gz",
        MakeAbsolute(new DirectoryPath(BuildParameters.Paths.Directories.ChocolateyPackages.FullPath)),
        BuildParameters.Version.SemVersion
    );

    StartProcess(
        "tar",
        new ProcessSettings {
            Arguments = string.Format(
                "-czvf {0} .",
                outputFile
            ),
            WorkingDirectory = BuildParameters.Paths.Directories.PublishedApplications.FullPath + "/choco/"
        }
    );

    if (FileExists(outputFile))
    {
        BuildParameters.BuildProvider.UploadArtifact(outputFile);
    }
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
                            getScriptsToVerify: getScriptsToVerify,
                            getScriptsToSign: getScriptsToSign,
                            getFilesToSign: getFilesToSign,
                            getMsisToSign: getMsisToSign,
                            getILMergeConfigs: getILMergeConfigs,
                            preferDotNetGlobalToolUsage: !IsRunningOnWindows(),
                            shouldBuildMsi: false,
                            msiUsedWithinNupkg: false,
                            shouldAuthenticodeSignMsis: true,
                            shouldRunNuGet: IsRunningOnWindows(),
                            shouldAuthenticodeSignPowerShellScripts: IsRunningOnWindows(),
                            shouldPublishAwsLambdas: false,
                            chocolateyNupkgGlobbingPattern: "/**/chocolatey*.nupkg");

ToolSettings.SetToolSettings(context: Context);

BuildParameters.PrintParameters(Context);

Build.Run();
