﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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

namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NuGet;
    using attributes;
    using commandline;
    using configuration;
    using domain;
    using infrastructure.commands;
    using logging;
    using nuget;
    using services;

    [CommandFor(CommandNameType.pin)]
    public sealed class ChocolateyPinCommand : ICommand
    {
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly ILogger _nugetLogger;
        private readonly INugetService _nugetService;

        public ChocolateyPinCommand(IChocolateyPackageInformationService packageInfoService, ILogger nugetLogger, INugetService nugetService)
        {
            _packageInfoService = packageInfoService;
            _nugetLogger = nugetLogger;
            _nugetService = nugetService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("n=|name=",
                     "Name - the name of the package. Required with some actions. Defaults to empty.",
                     option => configuration.PinCommand.Name = option.remove_surrounding_quotes())
                .Add("version=",
                     "Version - Used when multiple versions of a package are installed.  Defaults to empty.",
                     option => configuration.Version = option.remove_surrounding_quotes())
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single pin command must be listed. Please see the help menu for those commands");
            }

            var command = PinCommandType.unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
            Enum.TryParse(unparsedCommand, true, out command);

            if (command == PinCommandType.unknown) 
            {
                this.Log().Warn("Unknown command {0}. Setting to list.".format_with(unparsedCommand));
                command = PinCommandType.list;
            }
            
            configuration.PinCommand.Command = command;
            configuration.Sources = ApplicationParameters.PackagesLocation;
            configuration.ListCommand.LocalOnly = true;
            configuration.AllVersions = true;
            configuration.Prerelease = true;
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
            if (configuration.PinCommand.Command != PinCommandType.list && string.IsNullOrWhiteSpace(configuration.PinCommand.Name))
            {
                throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name.".format_with(configuration.PinCommand.Command.to_string()));
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Pin Command");
            this.Log().Info(@"
Pin a package to suppress upgrades.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco pin [list]|add|remove [<options/switches>]
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco pin   
    choco pin list  
    choco pin add -n=git
    choco pin add -n=git --version 1.2.3
    choco pin remove --name git

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("Pin would have called {0} with other options:{1} Name={2}{1} Version={3}".format_with(configuration.PinCommand.Command.to_string(),Environment.NewLine,configuration.PinCommand.Name.to_string(),configuration.Version.to_string()));
        }

        public void run(ChocolateyConfiguration configuration)
        {
            var packageManager = NugetCommon.GetPackageManager(configuration, _nugetLogger,
                                                               installSuccessAction: null,
                                                               uninstallSuccessAction: null,
                                                               addUninstallHandler: false);

            switch (configuration.PinCommand.Command)
            {
                case PinCommandType.list:
                    list_pins(packageManager, configuration);
                    break;
                case PinCommandType.add:
                case PinCommandType.remove:
                    set_pin(packageManager, configuration);
                    break;
            }
        }

        public void list_pins(IPackageManager packageManager, ChocolateyConfiguration config)
        {
            foreach (var pkg in _nugetService.list_run(config, logResults: true))
            {
                var pkgInfo = _packageInfoService.get_package_information(pkg.Package);
                if (pkgInfo != null && pkgInfo.IsPinned)
                {
                    this.Log().Info(() => "{0}|{1}".format_with(pkgInfo.Package.Id,pkgInfo.Package.Version));
                }
            }
        }

        public void set_pin(IPackageManager packageManager, ChocolateyConfiguration config)
        {
            var versionUnspecified = string.IsNullOrWhiteSpace(config.Version);
            SemanticVersion semanticVersion = versionUnspecified ? null : new SemanticVersion(config.Version);
            IPackage installedPackage = packageManager.LocalRepository.FindPackage(config.PinCommand.Name, semanticVersion);
            if (installedPackage == null)
            {
                throw new ApplicationException("Unable to find package named '{0}'{1} to pin. Please check to ensure it is installed.".format_with(config.PinCommand.Name, versionUnspecified ? "" : " (version '{0}')".format_with(config.Version)));
            }

            var pkgInfo = _packageInfoService.get_package_information(installedPackage);

            pkgInfo.IsPinned = config.PinCommand.Command == PinCommandType.add;
            _packageInfoService.save_package_information(pkgInfo);
        }
    }
}