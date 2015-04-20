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
    using logging;
    using services;

    [CommandFor(CommandNameType.feature)]
    [CommandFor(CommandNameType.features)]
    public sealed class ChocolateyFeatureCommand : ICommand
    {
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateyFeatureCommand(IChocolateyConfigSettingsService configSettingsService)
        {
            _configSettingsService = configSettingsService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Sources = string.Empty;

            optionSet
                .Add("n=|name=",
                     "Name - the name of the source. Required with some actions. Defaults to empty.",
                     option => configuration.FeatureCommand.Name = option.remove_surrounding_quotes())
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single features command must be listed. Please see the help menu for those commands");
            }

            var command = FeatureCommandType.unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
            Enum.TryParse(unparsedCommand, true, out command);
            if (command == FeatureCommandType.unknown)
            {
                if (!string.IsNullOrWhiteSpace(unparsedCommand)) this.Log().Warn("Unknown command {0}. Setting to list.".format_with(unparsedCommand));
                command = FeatureCommandType.list;
            }

            configuration.FeatureCommand.Command = command;
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
            if (configuration.FeatureCommand.Command != FeatureCommandType.list && string.IsNullOrWhiteSpace(configuration.FeatureCommand.Name))
            {
                throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name.".format_with(configuration.FeatureCommand.Command.to_string()));
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
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
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _configSettingsService.noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            switch (configuration.FeatureCommand.Command)
            {
                case FeatureCommandType.list:
                    _configSettingsService.feature_list(configuration);
                    break;
                case FeatureCommandType.disable:
                    _configSettingsService.feature_disable(configuration);
                    break;
                case FeatureCommandType.enable:
                    _configSettingsService.feature_enable(configuration);
                    break;
            }
        }

        public bool may_require_admin_access()
        {
            return true;
        }
    }
}