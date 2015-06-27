// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using NuGet;
    using chocolatey.infrastructure.app;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.app.nuget;
    using chocolatey.infrastructure.app.services;
    using chocolatey.infrastructure.commands;
    using chocolatey.infrastructure.filesystem;

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

            _fileSystem.delete_directory_if_exists(config.CacheLocation, recursive: true);
            _fileSystem.delete_directory_if_exists(config.Sources, recursive: true);
            _fileSystem.delete_directory_if_exists(packagesInstallPath, recursive: true);
            _fileSystem.delete_directory_if_exists(shimsPath, recursive: true);
            _fileSystem.delete_directory_if_exists(badPackagesPath, recursive: true);
            _fileSystem.delete_directory_if_exists(backupPackagesPath, recursive: true);
            _fileSystem.delete_directory_if_exists(_fileSystem.combine_paths(get_top_level(), ".chocolatey"), recursive: true);
            _fileSystem.delete_directory_if_exists(_fileSystem.combine_paths(get_top_level(), "extensions"), recursive: true);

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

        public static void install_package(ChocolateyConfiguration config, string packageId, string version)
        {
            if (_service == null)
            {
                _service= NUnitSetup.Container.GetInstance<IChocolateyPackageService>();
            }

            var originalPackageName = config.PackageNames;
            var originalPackageVersion = config.Version;

            config.PackageNames = packageId;
            config.Version = version;
            _service.install_run(config);
            config.PackageNames = originalPackageName;
            config.Version = originalPackageVersion;
            //var pattern = "{0}.{1}{2}".format_with(packageId, string.IsNullOrWhiteSpace(version) ? "*" : version, Constants.PackageExtension);
            //var files = _fileSystem.get_files(config.Sources, pattern);
            //foreach (var file in files)
            //{
            //    var packageManager = NugetCommon.GetPackageManager(config, new ChocolateyNugetLogger(), null, null, false);
            //    packageManager.InstallPackage(new OptimizedZipPackage(file), false,false);
            //}
        }

        private static ChocolateyConfiguration baseline_configuration()
        {
            var config = NUnitSetup.Container.GetInstance<ChocolateyConfiguration>();

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
            config.IgnoreDependencies = false;
            config.InstallArguments = string.Empty;
            config.Noop = false;
            config.OverrideArguments = false;
            config.Prerelease = false;
            config.PromptForConfirmation = false;
            config.RegularOutput = true;
            config.SkipPackageInstallProvider = false;
            config.Sources = _fileSystem.get_full_path(_fileSystem.combine_paths(get_top_level(), "packages"));
            config.Version = null;
            config.Debug = true;

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
    }
}