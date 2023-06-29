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

namespace chocolatey.infrastructure.app.commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using attributes;
    using commandline;
    using configuration;
    using domain;
    using infrastructure.commands;
    using infrastructure.configuration;
    using logging;
    using nuget;
    using NuGet.Common;
    using NuGet.PackageManagement;
    using NuGet.Versioning;
    using services;

    [CommandFor("pin", "suppress upgrades for a package")]
    public class ChocolateyPinCommand : ICommand
    {
        private readonly IChocolateyPackageInformationService _packageInfoService;
        private readonly ILogger _nugetLogger;
        private readonly INugetService _nugetService;
        private const string NoChangeMessage = "Nothing to change. Pin already set or removed.";

        public ChocolateyPinCommand(IChocolateyPackageInformationService packageInfoService, ILogger nugetLogger, INugetService nugetService)
        {
            _packageInfoService = packageInfoService;
            _nugetLogger = nugetLogger;
            _nugetService = nugetService;
        }

        public virtual void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("n=|name=",
                     "Name - the name of the package. Required with some actions. Defaults to empty.",
                     option => configuration.PinCommand.Name = option.UnquoteSafe())
                .Add("version=",
                     "Version - Used when multiple versions of a package are installed.  Defaults to empty.",
                     option => configuration.Version = option.UnquoteSafe())
                ;
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            // don't set configuration.Input or it will be passed to list

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single pin command must be listed. Please see the help menu for those commands");
            }

            var command = PinCommandType.Unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
            Enum.TryParse(unparsedCommand, true, out command);

            if (command == PinCommandType.Unknown)
            {
                if (!string.IsNullOrWhiteSpace(unparsedCommand)) this.Log().Warn("Unknown command {0}. Setting to list.".FormatWith(unparsedCommand));
                command = PinCommandType.List;
            }

            configuration.PinCommand.Command = command;
            configuration.Sources = ApplicationParameters.PackagesLocation;
            configuration.ListCommand.LocalOnly = true;
            configuration.AllVersions = true;
            configuration.Prerelease = true;
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
            if (configuration.PinCommand.Command != PinCommandType.List && string.IsNullOrWhiteSpace(configuration.PinCommand.Name))
            {
                throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name.".FormatWith(configuration.PinCommand.Command.ToStringSafe().ToLower()));
            }
        }

        public virtual void HelpMessage(ChocolateyConfiguration configuration)
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
    choco pin add -n git
    choco pin add --name=""'git'"" --version=""'1.2.3'""
    choco pin add --name=""'git'"" --version=""'1.2.3'"" --reason=""'reasons available in business editions only'""
    choco pin remove --name=""'git'""

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

        public virtual void DryRun(ChocolateyConfiguration configuration)
        {
            this.Log().Info("Pin would have called {0} with other options:{1} Name={2}{1} Version={3}".FormatWith(configuration.PinCommand.Command.ToStringSafe(), Environment.NewLine, configuration.PinCommand.Name, configuration.Version));
        }

        public virtual void Run(ChocolateyConfiguration configuration)
        {
            switch (configuration.PinCommand.Command)
            {
                case PinCommandType.List:
                    ListPins(configuration);
                    break;
                case PinCommandType.Add:
                case PinCommandType.Remove:
                    SetPin(configuration);
                    break;
            }
        }

        public virtual void ListPins(ChocolateyConfiguration config)
        {
            var input = config.Input;
            config.Input = string.Empty;
            var quiet = config.QuietOutput;
            config.QuietOutput = true;
            var packages = _nugetService.List(config).ToList();
            config.QuietOutput = quiet;
            config.Input = input;

            foreach (var pkg in packages.OrEmpty())
            {
                var pkgInfo = _packageInfoService.Get(pkg.PackageMetadata);
                if (pkgInfo != null && pkgInfo.IsPinned)
                {
                    this.Log().Info(() => "{0}|{1}".FormatWith(pkgInfo.Package.Id, pkgInfo.Package.Version));
                }
            }
        }

        public virtual void SetPin(ChocolateyConfiguration config)
        {
            var addingAPin = config.PinCommand.Command == PinCommandType.Add;
            this.Log().Info("Trying to {0} a pin for {1}".FormatWith(config.PinCommand.Command.ToStringSafe(), config.PinCommand.Name));
            var versionUnspecified = string.IsNullOrWhiteSpace(config.Version);
            NuGetVersion semanticVersion = versionUnspecified ? null : NuGetVersion.Parse(config.Version);

            var input = config.Input;
            config.Input = config.PinCommand.Name;
            config.Version = semanticVersion.ToFullStringChecked();
            config.ListCommand.ByIdOnly = true;
            var exact = config.ListCommand.Exact;
            config.ListCommand.Exact = true;
            var quiet = config.QuietOutput;
            config.QuietOutput = true;
            var installedPackage = _nugetService.List(config).FirstOrDefault();
            config.ListCommand.Exact = exact;
            config.QuietOutput = quiet;
            config.Input = input;

            if (installedPackage == null)
            {
                throw new ApplicationException("Unable to find package named '{0}'{1} to pin. Please check to ensure it is installed.".FormatWith(config.PinCommand.Name, versionUnspecified ? "" : " (version '{0}')".FormatWith(config.Version)));
            }

            var pkgInfo = _packageInfoService.Get(installedPackage.PackageMetadata);

            bool changeMessage = pkgInfo.IsPinned != addingAPin;

            pkgInfo.IsPinned = addingAPin;
            _packageInfoService.Save(pkgInfo);

            if (changeMessage)
            {
                this.Log().Warn("Successfully {0} a pin for {1} v{2}.".FormatWith(addingAPin ? "added" : "removed", pkgInfo.Package.Id, pkgInfo.Package.Version.ToFullStringChecked()));
            }
            else
            {
                this.Log().Warn(NoChangeMessage);
            }
        }

        public virtual bool MayRequireAdminAccess()
        {
            var config = Config.GetConfigurationSettings();
            if (config == null) return true;

            return config.PinCommand.Command != PinCommandType.List;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
            => ConfigureArgumentParser(optionSet, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
            => ParseAdditionalArguments(unparsedArguments, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_validation(ChocolateyConfiguration configuration)
            => Validate(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void help_message(ChocolateyConfiguration configuration)
            => HelpMessage(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void noop(ChocolateyConfiguration configuration)
            => DryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void run(ChocolateyConfiguration configuration)
            => Run(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual bool may_require_admin_access()
            => MayRequireAdminAccess();
#pragma warning restore IDE1006
    }
}
