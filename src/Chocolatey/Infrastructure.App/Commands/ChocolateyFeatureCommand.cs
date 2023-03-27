﻿// Copyright © 2017 - 2021 Chocolatey Software, Inc
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

namespace Chocolatey.Infrastructure.App.Commands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Attributes;
    using CommandLine;
    using Configuration;
    using Domain;
    using Infrastructure.Commands;
    using Infrastructure.Configuration;
    using Logging;
    using Services;

    [CommandFor("feature", "view and configure choco features")]
    [CommandFor("features", "view and configure choco features (alias for feature)")]
    public class ChocolateyFeatureCommand : ICommand
    {
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateyFeatureCommand(IChocolateyConfigSettingsService configSettingsService)
        {
            _configSettingsService = configSettingsService;
        }

        public virtual void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Sources = string.Empty;

            optionSet
                .Add("n=|name=",
                     "Name - the name of the source. Required with actions other than list. Defaults to empty.",
                     option => configuration.FeatureCommand.Name = option.UnquoteSafe())
                ;
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single features command must be listed. Please see the help menu for those commands");
            }

            var command = FeatureCommandType.Unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
            Enum.TryParse(unparsedCommand, true, out command);
            if (command == FeatureCommandType.Unknown)
            {
                if (!string.IsNullOrWhiteSpace(unparsedCommand)) this.Log().Warn("Unknown command {0}. Setting to list.".FormatWith(unparsedCommand));
                command = FeatureCommandType.List;
            }

            configuration.FeatureCommand.Command = command;
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
            if (configuration.FeatureCommand.Command != FeatureCommandType.List && string.IsNullOrWhiteSpace(configuration.FeatureCommand.Name))
            {
                throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name.".FormatWith(configuration.FeatureCommand.Command.ToStringChecked()));
            }
        }

        public virtual void HelpMessage(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Feature Command");
            this.Log().Info(@"
Chocolatey will allow you to interact with features.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco feature [list]|disable|enable [<options/switches>]
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco feature
    choco feature list
    choco feature disable -n=bob
    choco feature enable -n=bob

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
            _configSettingsService.DryRun(configuration);
        }

        public virtual void Run(ChocolateyConfiguration configuration)
        {
            switch (configuration.FeatureCommand.Command)
            {
                case FeatureCommandType.List:
                    _configSettingsService.ListFeatures(configuration);
                    break;
                case FeatureCommandType.Disable:
                    _configSettingsService.DisableFeature(configuration);
                    break;
                case FeatureCommandType.Enable:
                    _configSettingsService.EnableFeature(configuration);
                    break;
            }
        }

        public virtual bool MayRequireAdminAccess()
        {
            var config = Config.GetConfigurationSettings();
            if (config == null) return true;

            return config.FeatureCommand.Command != FeatureCommandType.List;
        }
    }
}
