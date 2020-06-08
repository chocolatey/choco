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
    using System.Collections.Generic;
    using System.Linq;
    using attributes;
    using commandline;
    using configuration;
    using logging;
    using services;

    [CommandFor("version", "[DEPRECATED] will be removed in v1 - use `choco outdated` or `cup <pkg|all> -whatif` instead")]
    public class ChocolateyVersionCommand : ChocolateyUpgradeCommand
    {
        private readonly IChocolateyPackageService _packageService;

        //todo: v1 Deprecation - remove version

        public ChocolateyVersionCommand(IChocolateyPackageService packageService)
            : base(packageService)
        {
            _packageService = packageService;
        }

        public override void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - The source to find the package(s) to install. Special sources include: ruby, webpi, cygwin, windowsfeatures, and python. Defaults to default feeds.",
                     option => configuration.Sources = option.remove_surrounding_quotes())
                 .Add("lo|localonly",
                     "LocalOnly - Only search against local machine items.",
                     option => configuration.ListCommand.LocalOnly = option != null)
                .Add("pre|prerelease",
                     "Prerelease - Include Prereleases? Defaults to false.",
                     option => configuration.Prerelease = option != null)
                ;
        }

        public override void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            base.handle_additional_argument_parsing(unparsedArguments, configuration);

            configuration.Noop = true;

            if (configuration.ListCommand.LocalOnly)
            {
                configuration.Noop = false;
                configuration.Sources = ApplicationParameters.PackagesLocation;
                configuration.Prerelease = true;
            }

            if (string.IsNullOrWhiteSpace(configuration.PackageNames))
            {
                configuration.PackageNames = "chocolatey";
            }
        }

        public override void handle_validation(ChocolateyConfiguration configuration)
        {
            if (configuration.ListCommand.LocalOnly)
            {
                this.Log().Warn(ChocolateyLoggers.Important, @"
DEPRECATION NOTICE - `choco version -lo` is deprecated. version command
 will be removed in version 1.0.0. Please use `choco list -lo` instead.");

            }
            else
            {
                this.Log().Warn(ChocolateyLoggers.Important, @"
DEPRECATION NOTICE - choco version command is deprecated and will be 
 removed in version 1.0.0. Please use `choco upgrade pkgname --noop` 
 instead.");

            }

            base.handle_validation(configuration);
        }

        public override void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "[DEPRECATED] Version Command");
            this.Log().Info(@"
NOTE: Version has been deprecated and will be removed in version 1.0.0. 

 If you are attempting to get local installed items, use 
 `choco list -lo`. 

 If you want to know what has available upgrades, use 
 `choco upgrade <pkg|all> -whatif` or `choco outdated`.
"); 
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public override void noop(ChocolateyConfiguration configuration)
        {
            _packageService.upgrade_noop(configuration);
        }

        public override void run(ChocolateyConfiguration configuration)
        {
            if (configuration.ListCommand.LocalOnly)
            {
                // note: you must leave the .ToList() here or else the method may not be evaluated!
                _packageService.list_run(configuration).ToList();
            }
            else
            {
                _packageService.upgrade_run(configuration);
            }
            
        }
    }
}