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
    using infrastructure.configuration;
    using logging;
    using nuget;
    using services;

    [CommandFor("pin", "suppress upgrades for a package")]
    public class ChocolateyPinCommand : ICommand
    {
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly ILogger _nugetLogger;
        private readonly INugetService _nugetService;
        private const string NO_CHANGE_MESSAGE = "Nothing to change. Pin already set or removed.";

        public ChocolateyPinCommand(IChocolateyPackageInformationService packageInfoService, ILogger nugetLogger, INugetService nugetService)
        {
            _packageInfoService = packageInfoService;
            _nugetLogger = nugetLogger;
            _nugetService = nugetService;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
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

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            // don't set configuration.Input or it will be passed to list

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single pin command must be listed. Please see the help menu for those commands");
            }

            var command = PinCommandType.unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
            Enum.TryParse(unparsedCommand, true, out command);

            if (command == PinCommandType.unknown)
            {
                if (!string.IsNullOrWhiteSpace(unparsedCommand)) this.Log().Warn("Unknown command {0}. Setting to list.".format_with(unparsedCommand));
                command = PinCommandType.list;
            }

            configuration.PinCommand.Command = command;
            configuration.Sources = ApplicationParameters.PackagesLocation;
            configuration.ListCommand.LocalOnly = true;
            configuration.AllVersions = true;
            configuration.Prerelease = true;
        }

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
            if (configuration.PinCommand.Command != PinCommandType.list && string.IsNullOrWhiteSpace(configuration.PinCommand.Name))
            {
                throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name.".format_with(configuration.PinCommand.Command.to_string()));
            }
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Pin Command");
            this.Log().Info(@"
Pin a package to suppress upgrades. 

This is especially helpful when running `choco upgrade` for all 
 packages, as it will automatically skip those packages. Another 
 alternative is `choco upgrade --except=""pkg1,pk2""`.
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

NOTE: See scripting in the command reference (`choco -?`) for how to 
 write proper scripts and integrations.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Exit Codes");
            "chocolatey".Log().Info(@"
Exit codes that normally result from running this command.

Normal:
 - 0: operation was successful, no issues detected
 - -1 or 1: an error has occurred

If you find other exit codes that we have not yet documented, please 
 file a ticket so we can document it at 
 https://github.com/chocolatey/choco/issues/new/choose.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            this.Log().Info("Pin would have called {0} with other options:{1} Name={2}{1} Version={3}".format_with(configuration.PinCommand.Command.to_string(), Environment.NewLine, configuration.PinCommand.Name.to_string(), configuration.Version.to_string()));
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            var packageManager = NugetCommon.GetPackageManager(configuration, _nugetLogger,
                                                               new PackageDownloader(),
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

        public virtual void list_pins(IPackageManager packageManager, ChocolateyConfiguration config)
        {
            var input = config.Input;
            config.Input = string.Empty;
            var quiet = config.QuietOutput;
            config.QuietOutput = true;
            var packages = _nugetService.list_run(config).ToList();
            config.QuietOutput = quiet;
            config.Input = input;

            foreach (var pkg in packages.or_empty_list_if_null())
            {
                var pkgInfo = _packageInfoService.get_package_information(pkg.Package);
                if (pkgInfo != null && pkgInfo.IsPinned)
                {
                    this.Log().Info(() => "{0}|{1}".format_with(pkgInfo.Package.Id, pkgInfo.Package.Version));
                }
            }
        }

        public virtual void set_pin(IPackageManager packageManager, ChocolateyConfiguration config)
        {
            var addingAPin = config.PinCommand.Command == PinCommandType.add;
            this.Log().Info("Trying to {0} a pin for {1}".format_with(config.PinCommand.Command.to_string(), config.PinCommand.Name));
            var versionUnspecified = string.IsNullOrWhiteSpace(config.Version);
            SemanticVersion semanticVersion = versionUnspecified ? null : new SemanticVersion(config.Version);
            IPackage installedPackage = packageManager.LocalRepository.FindPackage(config.PinCommand.Name, semanticVersion);
            if (installedPackage == null)
            {
                var pathResolver = packageManager.PathResolver as ChocolateyPackagePathResolver;
                if (pathResolver != null)
                {
                    pathResolver.UseSideBySidePaths = true;
                    installedPackage = packageManager.LocalRepository.FindPackage(config.PinCommand.Name, semanticVersion);
                }

                if (installedPackage == null)
                {
                    throw new ApplicationException("Unable to find package named '{0}'{1} to pin. Please check to ensure it is installed.".format_with(config.PinCommand.Name, versionUnspecified ? "" : " (version '{0}')".format_with(config.Version)));
                }
            }

            var pkgInfo = _packageInfoService.get_package_information(installedPackage);

            bool changeMessage = pkgInfo.IsPinned != addingAPin;

            pkgInfo.IsPinned = addingAPin;
            _packageInfoService.save_package_information(pkgInfo);

            if (changeMessage)
            {
                this.Log().Warn("Successfully {0} a pin for {1} v{2}.".format_with(addingAPin ? "added" : "removed", pkgInfo.Package.Id, pkgInfo.Package.Version.to_string()));
            }
            else
            {
                this.Log().Warn(NO_CHANGE_MESSAGE);
            }
        }

        public virtual bool may_require_admin_access()
        {
            var config = Config.get_configuration_settings();
            if (config == null) return true;

            return config.PinCommand.Command != PinCommandType.list;
        }
    }
}