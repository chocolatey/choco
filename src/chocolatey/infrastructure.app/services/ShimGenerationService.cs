// Copyright © 2017 - 2018 Chocolatey Software, Inc
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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using configuration;
    using filesystem;
    using infrastructure.app.domain;
    using infrastructure.app.utility;
    using infrastructure.commands;
    using results;

    public class ShimGenerationService : IShimGenerationService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICommandExecutor _commandExecutor;
        private const string PATH_TOKEN = "{{path}}";
        private const string ICON_PATH_TOKEN = "{{icon_path}}";
        private const string OUTPUT_TOKEN = "{{output}}";
        private readonly string _shimGenExePath = ApplicationParameters.Tools.ShimGenExe;
        private readonly IDictionary<string, ExternalCommandArgument> _shimGenArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);
        private readonly ShimRegistry _registry;

        public ShimGenerationService(IFileSystem fileSystem, ICommandExecutor commandExecutor)
        {
            _fileSystem = fileSystem;
            _commandExecutor = commandExecutor;
            set_shimgen_args_dictionary();
            _registry = new ShimRegistry(_fileSystem);
        }

        /// <summary>
        ///   Sets webpicmd install dictionary
        /// </summary>
        private void set_shimgen_args_dictionary()
        {
            _shimGenArguments.Add("_file_path_", new ExternalCommandArgument
                {
                    ArgumentOption = "--path=",
                    ArgumentValue = PATH_TOKEN,
                    QuoteValue = true,
                    Required = true
                });

            _shimGenArguments.Add("_output_directory_", new ExternalCommandArgument
                {
                    ArgumentOption = "--output=",
                    ArgumentValue = OUTPUT_TOKEN,
                    QuoteValue = true,
                    Required = true
                });

            _shimGenArguments.Add("_icon_path_", new ExternalCommandArgument
                {
                    ArgumentOption = " --iconpath=",
                    ArgumentValue = ICON_PATH_TOKEN,
                    QuoteValue = true,
                    Required = true
                });

            //_shimGenArguments.Add("_gui_", new ExternalCommandArgument { ArgumentOption = "--gui", Required = false });
        }


        public void install(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            _fileSystem.create_directory_if_not_exists(ApplicationParameters.ShimsLocation);

            if (packageResult.InstallLocation.is_equal_to(ApplicationParameters.InstallLocation) || packageResult.InstallLocation.is_equal_to(ApplicationParameters.PackagesLocation))
            {
                var logMessage = "Install location is not specific enough, cannot run shimgen:{0} Erroneous install location captured as '{1}'".format_with(Environment.NewLine, packageResult.InstallLocation);
                packageResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                this.Log().Error(logMessage.escape_curly_braces());
                return;
            }

            var existingShims = _registry.get_snapshot(packageResult.Name);
            var noShims = is_shimming_disabled(configuration, packageResult);

            if (noShims)
            {
                this.Log().Debug(() => "Automatic package shimming disabled for {0}".format_with(packageResult.Name));
            }

            if (existingShims.Count > 0)
            {
                this.Log().Debug(() => "Removing previous shims for {0}".format_with(packageResult.Name));
                uninstall_shims(existingShims);
            }

            if (!noShims)
            {
                //gather all relevant .exes in the folder
                var exeFiles = get_exe_files_to_shim(packageResult.InstallLocation);
                install_shims(configuration, packageResult.Name, exeFiles);
            }
        }

        public void uninstall(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            var existingShims = _registry.get_all(packageResult.Name);
            uninstall_shims(existingShims);
        }

        public void take_snapshot()
        {
            _registry.create_snapshot();
        }

        /// <summary>
        ///   Installs shimgens for the package
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="packageName">The package name.</param>
        /// <param name="exeFiles">The exe files to shim</param>
        private void install_shims(ChocolateyConfiguration configuration, string packageName, IList<string> exeFiles)
        {
            var args = ExternalCommandArgsBuilder.build_arguments(configuration, _shimGenArguments);

            foreach (string file in exeFiles)
            {
                bool isGui = _fileSystem.file_exists(file + ".gui");
                //todo: v2 be able to determine gui automatically

                var shimLocation = _fileSystem.combine_paths(ApplicationParameters.ShimsLocation, _fileSystem.get_file_name(file));
                var argsForPackage = args.Replace(PATH_TOKEN, file.Replace(ApplicationParameters.InstallLocation, "..\\")).Replace(OUTPUT_TOKEN, shimLocation).Replace(ICON_PATH_TOKEN, file);
                if (isGui)
                {
                    argsForPackage += " --gui";
                }

                // check for an existing shim
                ShimRecord record = _registry.get(shimLocation);
                if (record != null)
                {
                    this.Log().Error(() => " ShimGen cannot overwrite shim {0} in {1} package".format_with(_fileSystem.get_file_name(file), record.PackageName));
                    this.Log().Debug(() => "  Shim: {0}{1}  Target: {2}{1}  New target: {3}{1}".format_with(shimLocation, Environment.NewLine, record.TargetFile, file));
                    Environment.ExitCode = 1;
                    continue;
                }

                var exitCode = _commandExecutor.execute(
                    _shimGenExePath, argsForPackage, configuration.CommandExecutionTimeoutSeconds,
                    (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(e.Data)) return;
                        this.Log().Debug(() => " [ShimGen] {0}".format_with(e.Data.escape_curly_braces()));
                    },
                    (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(e.Data)) return;
                        this.Log().Error(() => " [ShimGen] {0}".format_with(e.Data.escape_curly_braces()));
                    },
                    updateProcessPath: true
                    );

                if (exitCode != 0)
                {
                    Environment.ExitCode = exitCode;
                }
                else
                {
                    _registry.add(shimLocation, packageName, file);
                    this.Log().Info(() => " ShimGen has successfully created a {0}shim for {1}".format_with(isGui ? "gui " : string.Empty, _fileSystem.get_file_name(file)));
                    this.Log().Debug(() => "  Created: {0}{1}  Targeting: {2}{1}  IsGui: {3}{1}".format_with(shimLocation, Environment.NewLine, file, isGui));
                }
            }
        }

        /// <summary>
        ///   Uninstalls shimgens for the package
        /// </summary>
        /// <param name="existingShims">The shim records for the package.</param>
        private void uninstall_shims(IList<ShimRecord> existingShims)
        {
            foreach (ShimRecord record in existingShims)
            {
                this.Log().Debug(() => "Removing shim {0} targetting '{1}'".format_with(_fileSystem.get_file_name(record.ExeFile), record.TargetFile));
                _fileSystem.delete_file(record.ExeFile);

                if (!_fileSystem.file_exists(record.ExeFile))
                {
                    _registry.remove(record.ExeFile);
                }
            }
        }

        /// <summary>
        ///   Returns the exe files to shim
        /// </summary>
        /// <param name="installLocation">The package install location.</param>
        /// <returns></returns>
        private IList<string> get_exe_files_to_shim(string installLocation)
        {
            var exeFiles = new List<string>();

            if (!_fileSystem.directory_exists(installLocation) || installLocation.is_equal_to(ApplicationParameters.InstallLocation) || installLocation.is_equal_to(ApplicationParameters.PackagesLocation))
            {
                return exeFiles;
            }

            var targetFiles = _fileSystem.get_files(installLocation, pattern: "*.exe", option: SearchOption.AllDirectories);
            foreach (string file in targetFiles.or_empty_list_if_null())
            {
                if (!_fileSystem.file_exists(file + ".ignore"))
                {
                    exeFiles.Add(file);
                }
            }

            return exeFiles;
        }

        /// <summary>
        ///   Checks if shimming is disabled for the package
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="packageResult">The package result.</param>
        /// <returns>true if shimming is disabled</returns>
        private bool is_shimming_disabled(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            return configuration.NoShimsGlobal || (configuration.NoShims
                && !PackageUtility.package_is_a_dependency(configuration, packageResult.Package.Id));
        }
    }
}
