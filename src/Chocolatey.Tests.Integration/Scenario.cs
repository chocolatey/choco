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

namespace Chocolatey.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Xml.Linq;
    using Chocolatey.Infrastructure.App;
    using Chocolatey.Infrastructure.App.Configuration;
    using Chocolatey.Infrastructure.App.Domain;
    using Chocolatey.Infrastructure.App.Services;
    using Chocolatey.Infrastructure.Commands;
    using Chocolatey.Infrastructure.Filesystem;
    using Chocolatey.Infrastructure.Guards;
    using Chocolatey.Infrastructure.Platforms;
    using global::NuGet.Configuration;
    using global::NuGet.Packaging;

    public class Scenario
    {
        private static IChocolateyPackageService _service;

        private static readonly DotNetFileSystem _fileSystem = new DotNetFileSystem();

        public static string get_top_level()
        {
            return _fileSystem.GetDirectoryName(_fileSystem.GetCurrentAssemblyPath());
        }

        public static string get_package_install_path()
        {
            return _fileSystem.CombinePaths(get_top_level(), "lib");
        }

        public static IEnumerable<string> get_installed_package_paths()
        {
            return _fileSystem.GetFiles(get_package_install_path(), "*" + NuGetConstants.PackageExtension, SearchOption.AllDirectories);
        }

        public static void reset(ChocolateyConfiguration config)
        {
            string packagesInstallPath = get_package_install_path();
            string badPackagesPath = get_package_install_path() + "-bad";
            string backupPackagesPath = get_package_install_path() + "-bkp";
            string shimsPath = ApplicationParameters.ShimsLocation;
            string hooksPath = ApplicationParameters.HooksLocation;

            _fileSystem.DeleteDirectoryChecked(config.CacheLocation, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(config.Sources, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(packagesInstallPath, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(shimsPath, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(badPackagesPath, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(backupPackagesPath, recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(_fileSystem.CombinePaths(get_top_level(), ".chocolatey"), recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(_fileSystem.CombinePaths(get_top_level(), "extensions"), recursive: true, overrideAttributes: true);
            _fileSystem.DeleteDirectoryChecked(hooksPath, recursive: true, overrideAttributes: true);

            _fileSystem.CreateDirectory(config.CacheLocation);
            _fileSystem.CreateDirectory(config.Sources);
            _fileSystem.CreateDirectory(packagesInstallPath);
            _fileSystem.CreateDirectory(shimsPath);
            _fileSystem.CreateDirectory(badPackagesPath);
            _fileSystem.CreateDirectory(backupPackagesPath);
            _fileSystem.CreateDirectory(_fileSystem.CombinePaths(get_top_level(), ".chocolatey"));
            _fileSystem.CreateDirectory(_fileSystem.CombinePaths(get_top_level(), "extensions"));

            PowershellExecutor.AllowUseWindow = false;
        }

        public static void add_packages_to_source_location(ChocolateyConfiguration config, string pattern)
        {
            _fileSystem.EnsureDirectory(config.Sources);
            var contextDir = _fileSystem.CombinePaths(get_top_level(), "context");
            var files = _fileSystem.GetFiles(contextDir, pattern, SearchOption.AllDirectories);

            foreach (var file in files.OrEmpty())
            {
                _fileSystem.CopyFile(_fileSystem.GetFullPath(file), _fileSystem.CombinePaths(config.Sources, _fileSystem.GetFileName(file)), overwriteExisting: true);
            }
        }

        public static void add_machine_source(ChocolateyConfiguration config, string name, string path = null, int priority = 0, bool createDirectory = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = _fileSystem.CombinePaths(get_top_level(), "PrioritySources", name);
            }

            if (createDirectory)
            {
                _fileSystem.EnsureDirectory(path);
            }

            var newSource = new MachineSourceConfiguration
            {
                Name = name,
                Key = path,
                Priority = priority
            };
            config.MachineSources.Add(newSource);
        }

        public static string add_packages_to_priority_source_location(ChocolateyConfiguration config, string pattern, int priority = 0, string name = null)
        {
            if (name == null)
            {
                name = "Priority" + priority;
            }

            var prioritySourceDirectory = _fileSystem.CombinePaths(get_top_level(), "PrioritySources", name);

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

            _fileSystem.EnsureDirectory(prioritySourceDirectory);

            var contextDir = _fileSystem.CombinePaths(get_top_level(), "context");
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

        public static void remove_packages_from_destination_location(ChocolateyConfiguration config, string pattern)
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

        public static void install_package(ChocolateyConfiguration config, string packageId, string version)
        {
            if (_service == null)
            {
                _service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }
            var installConfig = config.DeepCopy();

            installConfig.PackageNames = packageId;
            installConfig.Version = version;
            installConfig.CommandName = CommandNameType.Install.ToStringChecked();
            _service.Install(installConfig);

            NUnitSetup.MockLogger.Messages.Clear();
        }

        public static void add_files(IEnumerable<Tuple<string, string>> files)
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

        public static void create_directory(string directoryPath)
        {
            _fileSystem.CreateDirectory(directoryPath);
        }

        public static void add_changed_version_package_to_source_location(ChocolateyConfiguration config, string pattern, string newVersion)
        {
            _fileSystem.EnsureDirectory(config.Sources);
            var contextDir = _fileSystem.CombinePaths(get_top_level(), "context");
            var files = _fileSystem.GetFiles(contextDir, pattern, SearchOption.AllDirectories);

            foreach (var file in files.OrEmpty())
            {
                var copyToPath = _fileSystem.CombinePaths(config.Sources, _fileSystem.GetFileName(file));
                _fileSystem.CopyFile(_fileSystem.GetFullPath(file), copyToPath, overwriteExisting: true);
                change_package_version(copyToPath, newVersion);
            }
        }

        public static void change_package_version(string existingPackagePath, string newVersion)
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

        private static ChocolateyConfiguration baseline_configuration()
        {
            delete_test_package_directories();

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
            config.CacheLocation = _fileSystem.GetFullPath(_fileSystem.CombinePaths(get_top_level(), "cache"));
            config.CommandExecutionTimeoutSeconds = 2700;
            config.Force = false;
            config.ForceDependencies = false;
            config.ForceX86 = false;
            config.HelpRequested = false;
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
            config.Sources = _fileSystem.GetFullPath(_fileSystem.CombinePaths(get_top_level(), "packages"));
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
            config.MachineSources.Clear();

            return config;
        }

        public static ChocolateyConfiguration install()
        {
            var config = baseline_configuration();
            config.CommandName = CommandNameType.Install.ToStringChecked();

            return config;
        }

        public static ChocolateyConfiguration upgrade()
        {
            var config = baseline_configuration();
            config.CommandName = CommandNameType.Upgrade.ToStringChecked();

            return config;
        }

        public static ChocolateyConfiguration uninstall()
        {
            var config = baseline_configuration();
            config.CommandName = CommandNameType.Uninstall.ToStringChecked();

            return config;
        }

        public static ChocolateyConfiguration list()
        {
            var config = baseline_configuration();
            config.CommandName = "list";

            return config;
        }

        public static ChocolateyConfiguration search()
        {
            var config = baseline_configuration();
            config.CommandName = "search";

            return config;
        }

        public static ChocolateyConfiguration info()
        {
            var config = baseline_configuration();
            config.CommandName = "info";
            config.Verbose = true;
            config.ListCommand.Exact = true;

            return config;
        }

        public static ChocolateyConfiguration pin()
        {
            var config = baseline_configuration();
            config.CommandName = "pin";

            return config;
        }

        public static ChocolateyConfiguration pack()
        {
            var config = baseline_configuration();
            config.CommandName = "pack";

            return config;
        }

        private static void delete_test_package_directories()
        {
            var topDirectory = get_top_level();

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
