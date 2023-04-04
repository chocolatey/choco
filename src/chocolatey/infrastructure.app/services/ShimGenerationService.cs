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

namespace chocolatey.infrastructure.app.services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using configuration;
    using filesystem;
    using infrastructure.commands;
    using results;

    public class ShimGenerationService : IShimGenerationService
    {
        private readonly IFileSystem _fileSystem;
        private readonly ICommandExecutor _commandExecutor;
        private const string PathToken = "{{path}}";
        private const string IconPathToken = "{{icon_path}}";
        private const string OutputToken = "{{output}}";
        private readonly string _shimGenExePath = ApplicationParameters.Tools.ShimGenExe;
        private readonly IDictionary<string, ExternalCommandArgument> _shimGenArguments = new Dictionary<string, ExternalCommandArgument>(StringComparer.InvariantCultureIgnoreCase);


        public ShimGenerationService(IFileSystem fileSystem, ICommandExecutor commandExecutor)
        {
            _fileSystem = fileSystem;
            _commandExecutor = commandExecutor;
            SetupShimgenArgsDictionary();
        }

        /// <summary>
        /// Sets up shimgen arguments
        /// </summary>
        private void SetupShimgenArgsDictionary()
        {
            _shimGenArguments.Add("_file_path_", new ExternalCommandArgument
                {
                    ArgumentOption = "--path=",
                    ArgumentValue = PathToken,
                    QuoteValue = true,
                    Required = true
                });

            _shimGenArguments.Add("_output_directory_", new ExternalCommandArgument
                {
                    ArgumentOption = "--output=",
                    ArgumentValue = OutputToken,
                    QuoteValue = true,
                    Required = true
                });

            _shimGenArguments.Add("_icon_path_", new ExternalCommandArgument
                {
                    ArgumentOption = " --iconpath=",
                    ArgumentValue = IconPathToken,
                    QuoteValue = true,
                    Required = true
                });

            //_shimGenArguments.Add("_gui_", new ExternalCommandArgument { ArgumentOption = "--gui", Required = false });
        }


        public void Install(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            _fileSystem.EnsureDirectoryExists(ApplicationParameters.ShimsLocation);

            if (packageResult.InstallLocation.IsEqualTo(ApplicationParameters.InstallLocation) || packageResult.InstallLocation.IsEqualTo(ApplicationParameters.PackagesLocation))
            {
                var logMessage = "Install location is not specific enough, cannot run shimgen:{0} Erroneous install location captured as '{1}'".FormatWith(Environment.NewLine, packageResult.InstallLocation);
                packageResult.Messages.Add(new ResultMessage(ResultType.Warn, logMessage));
                this.Log().Error(logMessage.EscapeCurlyBraces());
                return;
            }

            //gather all .exes in the folder
            var exeFiles = _fileSystem.GetFiles(packageResult.InstallLocation, pattern: "*.exe", option: SearchOption.AllDirectories);
            foreach (string file in exeFiles.OrEmpty())
            {
                if (_fileSystem.FileExists(file + ".ignore")) continue;
                bool isGui = _fileSystem.FileExists(file + ".gui");
                //todo: #2586 v2 be able to determine gui automatically

                var args = ExternalCommandArgsBuilder.BuildArguments(configuration, _shimGenArguments);
                var shimLocation = _fileSystem.CombinePaths(ApplicationParameters.ShimsLocation, _fileSystem.GetFileName(file));
                var argsForPackage = args.Replace(PathToken, file.Replace(ApplicationParameters.InstallLocation, "..\\")).Replace(OutputToken, shimLocation).Replace(IconPathToken, file);
                if (isGui)
                {
                    argsForPackage += " --gui";
                }

                var exitCode = _commandExecutor.Execute(
                    _shimGenExePath, argsForPackage, configuration.CommandExecutionTimeoutSeconds,
                    (s, e) =>
                        {
                            if (string.IsNullOrWhiteSpace(e.Data)) return;
                            this.Log().Debug(() => " [ShimGen] {0}".FormatWith(e.Data.EscapeCurlyBraces()));
                        },
                    (s, e) =>
                        {
                            if (string.IsNullOrWhiteSpace(e.Data)) return;
                            this.Log().Error(() => " [ShimGen] {0}".FormatWith(e.Data.EscapeCurlyBraces()));
                        },
                    updateProcessPath: true
                    );

                if (exitCode != 0)
                {
                    Environment.ExitCode = exitCode;
                }
                else
                {
                    this.Log().Info(() => " ShimGen has successfully created a {0}shim for {1}".FormatWith(isGui ? "gui " : string.Empty, _fileSystem.GetFileName(file)));
                    this.Log().Debug(() => "  Created: {0}{1}  Targeting: {2}{1}  IsGui:{3}{1}".FormatWith(shimLocation, Environment.NewLine, file, isGui));
                }
            }
        }

        public void Uninstall(ChocolateyConfiguration configuration, PackageResult packageResult)
        {
            //gather all .exes in the folder
            var exeFiles = _fileSystem.GetFiles(packageResult.InstallLocation, pattern: "*.exe", option: SearchOption.AllDirectories);
            foreach (string file in exeFiles.OrEmpty())
            {
                if (_fileSystem.FileExists(file + ".ignore")) continue;

                var shimLocation = _fileSystem.CombinePaths(ApplicationParameters.ShimsLocation, _fileSystem.GetFileName(file));
                this.Log().Debug(() => "Removing shim for {0} at '{1}".FormatWith(_fileSystem.GetFileName(file), shimLocation));
                _fileSystem.DeleteFile(shimLocation);
            }
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void install(ChocolateyConfiguration configuration, PackageResult packageResult)
            => Install(configuration, packageResult);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void uninstall(ChocolateyConfiguration configuration, PackageResult packageResult)
            => Uninstall(configuration, packageResult);
#pragma warning restore IDE1006
    }
}
