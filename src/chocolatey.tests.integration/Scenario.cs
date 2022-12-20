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
    using System.Linq;

    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.guards;
    using chocolatey.infrastructure.platforms;

    public class Scenario
    {
        private static IChocolateyPackageService _service;

        private static readonly DotNetFileSystem _fileSystem = new DotNetFileSystem();

        public static string get_top_level()
        {
            return _fileSystem.get_directory_name(_fileSystem.get_current_assembly_path());
        }

        public static string get_package_install_path()
        {
            return _fileSystem.combine_paths(get_top_level(), "lib");
        }

        public static void reset(ChocolateyConfiguration config)
        {
            string packagesInstallPath = get_package_install_path();
            string badPackagesPath = get_package_install_path() + "-bad";
            string backupPackagesPath = get_package_install_path() + "-bkp";
            string shimsPath = ApplicationParameters.ShimsLocation;
            string hooksPath = ApplicationParameters.HooksLocation;

            _fileSystem.delete_directory_if_exists(config.CacheLocation, recursive: true, overrideAttributes: true);
            _fileSystem.delete_directory_if_exists(config.Sources, recursive: true, overrideAttributes: true);
            _fileSystem.delete_directory_if_exists(packagesInstallPath, recursive: true, overrideAttributes: true);
            _fileSystem.delete_directory_if_exists(shimsPath, recursive: true, overrideAttributes: true);
            _fileSystem.delete_directory_if_exists(badPackagesPath, recursive: true, overrideAttributes: true);
            _fileSystem.delete_directory_if_exists(backupPackagesPath, recursive: true, overrideAttributes: true);
            _fileSystem.delete_directory_if_exists(_fileSystem.combine_paths(get_top_level(), ".chocolatey"), recursive: true, overrideAttributes: true);
            _fileSystem.delete_directory_if_exists(_fileSystem.combine_paths(get_top_level(), "extensions"), recursive: true, overrideAttributes: true);
            _fileSystem.delete_directory_if_exists(hooksPath, recursive: true, overrideAttributes: true);

            _fileSystem.create_directory(config.CacheLocation);
            _fileSystem.create_directory(config.Sources);
            _fileSystem.create_directory(packagesInstallPath);
            _fileSystem.create_directory(shimsPath);
            _fileSystem.create_directory(badPackagesPath);
            _fileSystem.create_directory(backupPackagesPath);
            _fileSystem.create_directory(_fileSystem.combine_paths(get_top_level(), ".chocolatey"));
            _fileSystem.create_directory(_fileSystem.combine_paths(get_top_level(), "extensions"));

            PowershellExecutor.AllowUseWindow = false;
        }

        public static void add_packages_to_source_location(ChocolateyConfiguration config, string pattern)
        {
            _fileSystem.create_directory_if_not_exists(config.Sources);
            var contextDir = _fileSystem.combine_paths(get_top_level(), "context");
            var files = _fileSystem.get_files(contextDir, pattern, SearchOption.AllDirectories);

            foreach (var file in files.or_empty_list_if_null())
            {
                _fileSystem.copy_file(_fileSystem.get_full_path(file), _fileSystem.combine_paths(config.Sources, _fileSystem.get_file_name(file)), overwriteExisting: true);
            }
        }

        public static void add_machine_source(ChocolateyConfiguration config, string name, string path = null, int priority = 0, bool createDirectory = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = _fileSystem.combine_paths(get_top_level(), "PrioritySources", name);
            }

            if (createDirectory)
            {
                _fileSystem.create_directory_if_not_exists(path);
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

            var prioritySourceDirectory = _fileSystem.combine_paths(get_top_level(), "PrioritySources", name);

            var machineSource = config.MachineSources.FirstOrDefault(m => m.Name.is_equal_to(name));

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

            _fileSystem.create_directory_if_not_exists(prioritySourceDirectory);

            var contextDir = _fileSystem.combine_paths(get_top_level(), "context");
            var files = _fileSystem.get_files(contextDir, pattern, SearchOption.AllDirectories).or_empty_list_if_null().ToList();

            if (files.Count == 0)
            {
                throw new ApplicationException("No files matching the pattern {0} could be found!".format_with(pattern));
            }

            foreach (var file in files)
            {
                _fileSystem.copy_file(_fileSystem.get_full_path(file), _fileSystem.combine_paths(prioritySourceDirectory, _fileSystem.get_file_name(file)), overwriteExisting: true);
            }

            return machineSource.Name;
        }

        public static void remove_packages_from_destination_location(ChocolateyConfiguration config, string pattern)
        {
            if (!_fileSystem.directory_exists(config.Sources))
            {
                return;
            }

            var files = _fileSystem.get_files(config.Sources, pattern, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                _fileSystem.delete_file(file);
            }
        }

        public static void install_package(ChocolateyConfiguration config, string packageId, string version)
        {
            if (_service == null)
            {
                _service = NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }
            var installConfig = config.deep_copy();

            installConfig.PackageNames = packageId;
            installConfig.Version = version;
            installConfig.CommandName = CommandNameType.install.to_string();
            _service.install_run(installConfig);

            NUnitSetup.MockLogger.Messages.Clear();
        }

        public static void add_files(IEnumerable<Tuple<string, string>> files)
        {
            foreach (var file in files)
            {
                if (_fileSystem.file_exists(file.Item1))
                {
                    _fileSystem.delete_file(file.Item1);
                }
                _fileSystem.write_file(file.Item1, file.Item2);
            }
        }

        public static void create_directory(string directoryPath)
        {
            _fileSystem.create_directory(directoryPath);
        }

        private static ChocolateyConfiguration baseline_configuration()
        {
            delete_test_package_directories();

            // note that this does not mean an empty configuration. It does get influenced by
            // prior commands, so ensure that all items go back to the default values here
            var config = NUnitSetup.Container.GetInstance<ChocolateyConfiguration>();

            config.Information.PlatformType = Platform.get_platform();
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
            config.AllowMultipleVersions = false;
            config.AllowUnofficialBuild = true;
            config.CacheLocation = _fileSystem.get_full_path(_fileSystem.combine_paths(get_top_level(), "cache"));
            config.CommandExecutionTimeoutSeconds = 2700;
            config.ContainsLegacyPackageInstalls = false;
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
            config.Sources = _fileSystem.get_full_path(_fileSystem.combine_paths(get_top_level(), "packages"));
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
            config.PinCommand.Command = PinCommandType.unknown;
            config.ListCommand.IdOnly = false;
            config.MachineSources.Clear();

            return config;
        }

        public static ChocolateyConfiguration install()
        {
            var config = baseline_configuration();
            config.CommandName = CommandNameType.install.to_string();

            return config;
        }

        public static ChocolateyConfiguration upgrade()
        {
            var config = baseline_configuration();
            config.CommandName = CommandNameType.upgrade.to_string();

            return config;
        }

        public static ChocolateyConfiguration uninstall()
        {
            var config = baseline_configuration();
            config.CommandName = CommandNameType.uninstall.to_string();

            return config;
        }

        public static ChocolateyConfiguration list()
        {
            var config = baseline_configuration();
            config.CommandName = "list";

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
                _fileSystem.delete_directory_if_exists(directory, recursive: true);
            }
        }
    }
}
