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
    using attributes;
    using commandline;
    using configuration;
    using logging;
    using services;

    [CommandFor("update", "[DEPRECATED] RESERVED for future use (you are looking for upgrade, these are not the droids you are looking for)")]
    public class ChocolateyUpdateCommand : ChocolateyUpgradeCommand
    {
        //todo: v1 Deprecation - update is removed or becomes package indexes
        public ChocolateyUpdateCommand(IChocolateyPackageService packageService) : base(packageService)
        {
        }

        public override void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - The source to find the package(s) to install. Special sources include: ruby, webpi, cygwin, windowsfeatures, and python. Defaults to default feeds.",
                     option => configuration.Sources = option.remove_surrounding_quotes())
                .Add("version=",
                     "Version - A specific version to install. Defaults to unspecified.",
                     option => configuration.Version = option.remove_surrounding_quotes())
                .Add("pre|prerelease",
                     "Prerelease - Include Prereleases? Defaults to false.",
                     option => configuration.Prerelease = option != null)
                ;
        }

        public override void handle_validation(ChocolateyConfiguration configuration)
        {
            this.Log().Warn(ChocolateyLoggers.Important, @"
DEPRECATION NOTICE - choco update is deprecated and will be removed or 
 replaced in version 1.0.0 with something that performs the functions 
 of updating package indexes. Please use `choco upgrade` instead.");

            base.handle_validation(configuration);
        }

        public override void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "[DEPRECATED] Update Command");
            this.Log().Info(@"
NOTE: Update has been deprecated and will be removed/replaced in version 
 1.0.0 with something that performs the functions of updating package
 indexes.  Please use `choco upgrade` instead.
");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }
    }
}