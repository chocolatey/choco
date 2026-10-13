#load nuget:?package=Chocolatey.Cake.Recipe&version=0.32.0
#tool nuget:?package=WiX&version=3.11.2

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// SCRIPT
///////////////////////////////////////////////////////////////////////////////

// ILMerge has been removed for the .NET 10 migration: choco.exe is published as a
// self-contained, single-file executable (see Prepare-Chocolatey-Packages) instead of
// merging dependencies. shouldRunILMerge is set to false in BuildParameters.SetParameters.

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
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.json", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/CREDITS.json");
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.pdf", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/CREDITS.pdf");
    CopyFile(BuildParameters.Paths.Directories.PublishedApplications + "/choco/net10.0-windows/LICENSE.txt", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/LICENSE.txt");

    // The application manifest is embedded into choco.exe now (no side-by-side file to copy).

    // Copy external file resources
    EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/helpers");
    CopyFiles(GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/net10.0-windows/helpers/**/*"), BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/helpers", true);
    EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/redirects");
    CopyFiles(GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/net10.0-windows/redirects/**/*"), BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/redirects", true);
    EnsureDirectoryExists(BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/tools");
    CopyFiles(GetFiles(BuildParameters.Paths.Directories.PublishedApplications + "/choco/net10.0-windows/tools/**/*"), BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/tools", true);

    // Publish a self-contained, single-file choco.exe so it runs on a clean machine with no
    // .NET runtime installed (no framework, no reboot). PowerShell SDK needs its files on
    // disk, so IncludeAllContentForSelfExtract extracts the bundle to a temp dir at startup.
    var selfContainedDirectory = BuildParameters.Paths.Directories.PublishedApplications.FullPath + "/choco-selfcontained";
    DotNetCorePublish(BuildParameters.RootDirectoryPath + "/src/chocolatey.console/chocolatey.console.csproj", new DotNetCorePublishSettings {
        Configuration = BuildParameters.Configuration,
        OutputDirectory = selfContainedDirectory,
        Runtime = "win-x64",
        SelfContained = true,
        MSBuildSettings = new DotNetCoreMSBuildSettings()
            .WithProperty("PublishSingleFile", "true")
            .WithProperty("IncludeAllContentForSelfExtract", "true")
            .WithProperty("Version", BuildParameters.Version.SemVersion)
            .WithProperty("AssemblyVersion", BuildParameters.Version.FileVersion)
            .WithProperty("FileVersion", BuildParameters.Version.FileVersion)
            .WithProperty("AssemblyInformationalVersion", BuildParameters.Version.InformationalVersion)
            .WithProperty("Copyright", BuildParameters.ProductCopyright)
    });

    CopyFile(selfContainedDirectory + "/choco.exe", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/choco.exe");

    // Copy Chocolatey.PowerShell.dll and its help.xml file
    CopyFile(BuildParameters.Paths.Directories.PublishedLibraries + "/Chocolatey.PowerShell/net10.0-windows/Chocolatey.PowerShell.dll", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/helpers/Chocolatey.PowerShell.dll");
    CopyFile(BuildParameters.Paths.Directories.PublishedLibraries + "/Chocolatey.PowerShell/net10.0-windows/Chocolatey.PowerShell.dll-help.xml", BuildParameters.Paths.Directories.ChocolateyNuspecDirectory + "/tools/chocolateyInstall/helpers/Chocolatey.PowerShell.dll-help.xml");

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
    EnsureDirectoryExists(BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/net10.0");

    // Copy legal documents
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.md", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/CREDITS.txt");
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.json", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/CREDITS.json");
    CopyFile(BuildParameters.RootDirectoryPath + "/docs/legal/CREDITS.pdf", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/CREDITS.pdf");

    // No ILMerge: ship chocolatey.dll itself (consumers resolve its dependencies via NuGet).
    CopyFile(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/net10.0-windows/chocolatey.dll", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/net10.0/chocolatey.dll");
    CopyFile(BuildParameters.Paths.Directories.PublishedLibraries + "/chocolatey/net10.0-windows/chocolatey.xml", BuildParameters.Paths.Directories.NuGetNuspecDirectory + "/chocolatey.lib/lib/net10.0/chocolatey.xml");
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
    .IsDependentOn("DotNetBuild")
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
            WorkingDirectory = BuildParameters.Paths.Directories.PublishedApplications.FullPath + "/choco/net10.0-windows/"
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
                            shouldRunILMerge: false,
                            preferDotNetGlobalToolUsage: !IsRunningOnWindows(),
                            shouldBuildMsi: false,
                            msiUsedWithinNupkg: false,
                            shouldAuthenticodeSignMsis: true,
                            shouldRunNuGet: IsRunningOnWindows(),
                            shouldAuthenticodeSignPowerShellScripts: IsRunningOnWindows(),
                            shouldPublishAwsLambdas: false,
                            chocolateyNupkgGlobbingPattern: "/**/chocolatey*.nupkg",
                            shouldRunInspectCode: false);

ToolSettings.SetToolSettings(context: Context);

BuildParameters.PrintParameters(Context);

Build.RunDotNet();
