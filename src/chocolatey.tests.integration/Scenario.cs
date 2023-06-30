// Copyright © 2017 - 2021 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
//
// You may obtain a copy of the License at
//
// 	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace chocolatey.tests.integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Xml.Linq;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.guards;
    using chocolatey.infrastructure.platforms;
    using NuGet.Configuration;
    using NuGet.Packaging;

    public class Scenario
    {
        private static IChocolateyPackageService _service;

        private static readonly DotNetFileSystem _fileSystem = new DotNetFileSystem();

        public static string GetTopLevel()
        {
            return _fileSystem.GetDirectoryName(_fileSystem.GetCurrentAssemblyPath());
        }

        public static string GetPackageInstallPath()
        {
            return _fileSystem.CombinePaths(GetTopLevel(), "lib");
        }

        public static IEnumerable<string> GetInstalledPackagePaths()
        {
            return _fileSystem.GetFiles(GetPackageInstallPath(), "*" + NuGetConstants.PackageExtension, SearchOption.AllDirectories);
        }

        public static void Reset(ChocolateyConfiguration config)
        {
            string packagesInstallPath = GetPackageInstallPath();
            string badPackagesPath = GetPackageInstallPath() + "-bad";
            string backupPackagesPath = GetPackageInstallPath() + "-bkp";
            string shimsPath = ApplicationParameters.ShimsLocation;
            string hooksPath = ApplicationParameters.HooksLocation;

            _fileSystem.DeleteDirectoryChecked(config.CacheLocation, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(config.Sources, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(packagesInstallPath, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(shimsPath, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(badPackagesPath, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(backupPackagesPath, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(_fileSystem.CombinePaths(GetTopLevel(), ".chocolatey"), recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(_fileSystem.CombinePaths(GetTopLevel(), "extensions"), recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(hooksPath, recursive: true, overrideAttributes: true);

            _fileSystem.CreateDirectory(config.CacheLocation);
            _fileSystem.CreateDirectory(config.Sources);
            _fileSystem.CreateDirectory(packagesInstallPath);
            _fileSystem.CreateDirectory(shimsPath);
            _fileSystem.CreateDirectory(badPackagesPath);
            _fileSystem.CreateDirectory(backupPackagesPath);
            _fileSystem.CreateDirectory(_fileSystem.CombinePaths(GetTopLevel(), ".chocolatey"));
            _fileSystem.CreateDirectory(_fileSystem.CombinePaths(GetTopLevel(), "extensions"));

            PowershellExecutor.AllowUseWindow = false;
        }

        public static void AddPackagesToSourceLocation(ChocolateyConfiguration config, string pattern)
        {
            _fileSystem.EnsureDirectoryExists(config.Sources);
            var contextDir = _fileSystem.CombinePaths(GetTopLevel(), "context");
            var files = _fileSystem.GetFiles(contextDir, pattern, SearchOption.AllDirectories);

            foreach (var file in files.OrEmpty())
            {
                _fileSystem.CopyFile(_fileSystem.GetFullPath(file), _fileSystem.CombinePaths(config.Sources, _fileSystem.GetFileName(file)), overwriteExisting: true);
            }
        }

        public static void AddMachineSource(ChocolateyConfiguration config, string name, string path = null, int priority = 0, bool createDirectory = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = _fileSystem.CombinePaths(GetTopLevel(), "PrioritySources", name);
            }

            if (createDirectory)
            {
                _fileSystem.EnsureDirectoryExists(path);
            }

            var newSource = new MachineSourceConfiguration
            {
                Name = name,
                Key = path,
                Priority = priority
            };
            config.MachineSources.Add(newSource);
        }

        public static string AddPackagesToPrioritySourceLocation(ChocolateyConfiguration config, string pattern, int priority = 0, string name = null)
        {
            if (name == null)
            {
                name = "Priority" + priority;
            }

            var prioritySourceDirectory = _fileSystem.CombinePaths(GetTopLevel(), "PrioritySources", name);

            var machineSource = config.MachineSources.FirstOrDefault(m => m.Name.IsEqualTo(name));

            if (machineSource == null)
            {
                machineSource = new MachineSourceConfiguration
                {
                    Name = name,
                    Key = prioritySourceDirectory,
                    Priority = priority
                };
                config.MachineSources.Add(machineSource);
            }
            else
            {
                prioritySourceDirectory = machineSource.Key;
            }

            _fileSystem.EnsureDirectoryExists(prioritySourceDirectory);

            var contextDir = _fileSystem.CombinePaths(GetTopLevel(), "context");
            var files = _fileSystem.GetFiles(contextDir, pattern, SearchOption.AllDirectories).OrEmpty().ToList();

            if (files.Count == 0)
            {
                throw new ApplicationException("No files matching the pattern {0} could be found!".FormatWith(pattern));
            }

            foreach (var file in files)
            {
                _fileSystem.CopyFile(_fileSystem.GetFullPath(file), _fileSystem.CombinePaths(prioritySourceDirectory, _fileSystem.GetFileName(file)), overwriteExisting: true);
            }

            return machineSource.Name;
        }

        public static void RemovePackagesFromDestinationLocation(ChocolateyConfiguration config, string pattern)
        {
            if (!_fileSystem.DirectoryExists(config.Sources))
            {
                return;
            }

            var files = _fileSystem.GetFiles(config.Sources, pattern, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                _fileSystem.DeleteFile(file);
            }
        }

        public static void InstallPackage(ChocolateyConfiguration config, string packageId, string version)
        {
            if (_service == null)
            {
                _service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }
            var installConfig = config.DeepCopy();

            installConfig.PackageNames = packageId;
            installConfig.Version = version;
            installConfig.CommandName = CommandNameType.Install.ToStringSafe();
            _service.Install(installConfig);

            NUnitSetup.MockLogger.Messages.Clear();
        }

        public static void AddFiles(IEnumerable<Tuple<string, string>> files)
        {
            foreach (var file in files)
            {
                if (_fileSystem.FileExists(file.Item1))
                {
                    _fileSystem.DeleteFile(file.Item1);
                }
                _fileSystem.WriteFile(file.Item1, file.Item2);
            }
        }

        public static void CreateDirectory(string directoryPath)
        {
            _fileSystem.CreateDirectory(directoryPath);
        }

        public static void AddChangedVersionPackageToSourceLocation(ChocolateyConfiguration config, string pattern, string newVersion)
        {
            _fileSystem.EnsureDirectoryExists(config.Sources);
            var contextDir = _fileSystem.CombinePaths(GetTopLevel(), "context");
            var files = _fileSystem.GetFiles(contextDir, pattern, SearchOption.AllDirectories);

            foreach (var file in files.OrEmpty())
            {
                var copyToPath = _fileSystem.CombinePaths(config.Sources, _fileSystem.GetFileName(file));
                _fileSystem.CopyFile(_fileSystem.GetFullPath(file), copyToPath, overwriteExisting: true);
                ChangePackageVersion(copyToPath, newVersion);
            }
        }

        public static void ChangePackageVersion(string existingPackagePath, string newVersion)
        {
            string packageId;
            XDocument nuspecXml;

            using (var packageStream = new FileStream(existingPackagePath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (var packageReader = new PackageArchiveReader(packageStream, true))
                {
                    nuspecXml = packageReader.NuspecReader.Xml;
                    var metadataNode = nuspecXml.Root.Elements().FirstOrDefault(e => StringComparer.Ordinal.Equals(e.Name.LocalName, "metadata"));
                    var metadataNamespace = metadataNode.GetDefaultNamespace().NamespaceName;
                    var node = metadataNode.Elements(XName.Get("version", metadataNamespace)).FirstOrDefault();
                    node.Value = newVersion;
                    packageId = packageReader.GetIdentity().Id;
                }

                using (var zipArchive = new ZipArchive(packageStream, ZipArchiveMode.Update))
                {
                    var entry = zipArchive.GetEntry("{0}{1}".FormatWith(packageId, NuGetConstants.ManifestExtension));
                    using (var nuspecStream = entry.Open())
                    {
                        nuspecXml.Save(nuspecStream);
                    }
                }
            }

            var renamedPath = _fileSystem.CombinePaths(
                _fileSystem.GetDirectoryName(existingPackagePath),
                "{0}.{1}{2}".FormatWith(packageId, newVersion, NuGetConstants.PackageExtension));
            _fileSystem.MoveFile(existingPackagePath, renamedPath);
        }

        private static ChocolateyConfiguration BaselineConfiguration()
        {
            DeleteTestPackageDirectories();

            // note that this does not mean an empty configuration. It does get influenced by
            // prior commands, so ensure that all items go back to the default values here
            var config = NUnitSetup.Container.GetInstance<ChocolateyConfiguration>();

            config.Information.PlatformType = Platform.GetPlatform();
            config.Information.IsInteractive = false;
            config.Information.ChocolateyVersion = "1.2.3";
            config.Information.PlatformVersion = new Version(6, 1, 0, 0);
            config.Information.PlatformName = "Windows 7 SP1";
            config.Information.ChocolateyVersion = "1.2.3";
            config.Information.ChocolateyProductVersion = "1.2.3";
            config.Information.FullName = "choco something something";
            config.Information.Is64BitOperatingSystem = true;
            config.Information.IsInteractive = false;
            config.Information.IsUserAdministrator = true;
            config.Information.IsProcessElevated = true;
            config.Information.IsLicensedVersion = false;
            config.AcceptLicense = true;
            config.AllowUnofficialBuild = true;
            config.CacheLocation = _fileSystem.GetFullPath(_fileSystem.CombinePaths(GetTopLevel(), "cache"));
            config.CommandExecutionTimeoutSeconds = 2700;
            config.Force = false;
            config.ForceDependencies = false;
            config.ForceX86 = false;
            config.HelpRequested = false;
            config.ShowOnlineHelp = false;
            config.UnsuccessfulParsing = false;
            config.UnsuccessfulParsing = false;
            config.IgnoreDependencies = false;
            config.InstallArguments = string.Empty;
            config.Noop = false;
            config.OverrideArguments = false;
            config.Prerelease = false;
            config.UpgradeCommand.ExcludePrerelease = false;
            config.PromptForConfirmation = false;
            config.RegularOutput = true;
            config.SkipPackageInstallProvider = false;
            config.Sources = _fileSystem.GetFullPath(_fileSystem.CombinePaths(GetTopLevel(), "packages"));
            config.Version = null;
            config.Debug = true;
            config.AllVersions = false;
            config.Verbose = false;
            config.Trace = false;
            config.Input = config.PackageNames = string.Empty;
            config.ListCommand.LocalOnly = false;
            config.ListCommand.Exact = false;
            config.Features.UsePowerShellHost = true;
            config.Features.AutoUninstaller = true;
            config.Features.ChecksumFiles = true;
            config.OutputDirectory = null;
            config.Features.StopOnFirstPackageFailure = false;
            config.UpgradeCommand.PackageNamesToSkip = string.Empty;
            config.AllowDowngrade = false;
            config.Features.FailOnStandardError = false;
            config.ListCommand.IncludeVersionOverrides = false;
            config.UpgradeCommand.FailOnNotInstalled = false;
            config.PinCommand.Name = string.Empty;
            config.PinCommand.Command = PinCommandType.Unknown;
            config.ListCommand.IdOnly = false;
            config.ListCommand.PageSize = 25;
            config.ListCommand.ExplicitPageSize = false;
            config.MachineSources.Clear();

            return config;
        }

        public static ChocolateyConfiguration Install()
        {
            var config = BaselineConfiguration();
            config.CommandName = CommandNameType.Install.ToStringSafe();

            return config;
        }

        public static ChocolateyConfiguration Upgrade()
        {
            var config = BaselineConfiguration();
            config.CommandName = CommandNameType.Upgrade.ToStringSafe();

            return config;
        }

        public static ChocolateyConfiguration Uninstall()
        {
            var config = BaselineConfiguration();
            config.CommandName = CommandNameType.Uninstall.ToStringSafe();

            return config;
        }

        public static ChocolateyConfiguration List()
        {
            var config = BaselineConfiguration();
            config.CommandName = "list";

            return config;
        }

        public static ChocolateyConfiguration Search()
        {
            var config = BaselineConfiguration();
            config.CommandName = "search";

            return config;
        }

        public static ChocolateyConfiguration Info()
        {
            var config = BaselineConfiguration();
            config.CommandName = "info";
            config.Verbose = true;
            config.ListCommand.Exact = true;

            return config;
        }

        public static ChocolateyConfiguration Pin()
        {
            var config = BaselineConfiguration();
            config.CommandName = "pin";

            return config;
        }

        public static ChocolateyConfiguration Pack()
        {
            var config = BaselineConfiguration();
            config.CommandName = "pack";

            return config;
        }

        public static ChocolateyConfiguration Proxy()
        {
            return BaselineConfiguration();
        }

        public static void SetConfigurationFileSetting(string name, string value)
        {
            var config = BaselineConfiguration();
            config.ConfigCommand.Name = name;
            config.ConfigCommand.ConfigValue = value;
            config.ConfigCommand.Command = ConfigCommandType.Set;
            var configService = NUnitSetup.Container.GetInstance<IChocolateyConfigSettingsService>();
            configService.SetConfig(config);
        }

        private static void DeleteTestPackageDirectories()
        {
            var topDirectory = GetTopLevel();

            var directoriesToClean = new[]
            {
                Path.Combine(topDirectory, "PackageOutput"),
                Path.Combine(topDirectory, "PrioritySources")
            };

            foreach (var directory in directoriesToClean)
            {
                _fileSystem.DeleteDirectoryChecked(directory, recursive: true);
            }
        }
    }
}
