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
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using NuGet;
    using SimpleInjector;
    using chocolatey.infrastructure.app.builders;
    using chocolatey.infrastructure.app.configuration;
    using chocolatey.infrastructure.app.domain;
    using chocolatey.infrastructure.filesystem;
    using chocolatey.infrastructure.services;

    public class Scenario
    {
        private static readonly DotNetFileSystem _fileSystem = new DotNetFileSystem();

        public static string get_top_level()
        {
            return _fileSystem.get_directory_name(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", string.Empty));
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
            config.RegularOuptut = true;
            config.SkipPackageInstallProvider = false;
            config.Sources = _fileSystem.get_full_path(_fileSystem.combine_paths(get_top_level(), "packages"));
            config.Version = null;

            return config;
        }

        private static ChocolateyConfiguration set_baseline()
        {
            var config = baseline_configuration();

            string packagesInstallPath = _fileSystem.combine_paths(get_top_level(), "lib");

            _fileSystem.delete_directory_if_exists(config.CacheLocation, recursive: true);
            _fileSystem.delete_directory_if_exists(config.Sources, recursive: true);
            _fileSystem.delete_directory_if_exists(packagesInstallPath, recursive: true);
            
            _fileSystem.create_directory(config.CacheLocation);
            _fileSystem.create_directory(config.Sources);
            _fileSystem.create_directory(packagesInstallPath);
         
            return config;
        }

        private static void set_files_in_source(ChocolateyConfiguration config, string pattern)
        {
            var contextDir = _fileSystem.combine_paths(get_top_level(), "context");
            var files = _fileSystem.get_files(contextDir, pattern, SearchOption.AllDirectories);

            foreach (var file in files.or_empty_list_if_null())
            {
                _fileSystem.copy_file(_fileSystem.get_full_path(file), _fileSystem.combine_paths(config.Sources, _fileSystem.get_file_name(file)), overwriteExisting: true);
            }
        }

        public static ChocolateyConfiguration install()
        {
            var config = set_baseline();
            config.CommandName = CommandNameType.install.to_string();
            config.PackageNames = config.Input = "installpackage";

            set_files_in_source(config, config.Input + "*" + Constants.PackageExtension);
            set_files_in_source(config, "badpackage*" + Constants.PackageExtension);

            return config;
        }

        public static ChocolateyConfiguration upgrade()
        {
            var config = set_baseline();
            config.CommandName = CommandNameType.install.to_string();
            config.PackageNames = config.Input = "upgradepackage";

            //arrange for upgrade


            return config;
        }
    }
}