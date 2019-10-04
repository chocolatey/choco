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
    using attributes;
    using commandline;
    using configuration;
    using domain;
    using infrastructure.commands;
    using infrastructure.configuration;
    using logging;
    using services;

    [CommandFor("config", "Retrieve and configure config file settings")]
    public class ChocolateyConfigCommand : ICommand
    {
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateyConfigCommand(IChocolateyConfigSettingsService configSettingsService)
        {
            _configSettingsService = configSettingsService;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Sources = string.Empty;

            optionSet
                .Add(
                    "name=",
                    "Name - the name of the config setting. Required with some actions. Defaults to empty.",
                    option => configuration.ConfigCommand.Name = option.remove_surrounding_quotes())
                .Add(
                    "value=",
                    "Value - the value of the config setting. Required with some actions. Defaults to empty.",
                    option => configuration.ConfigCommand.ConfigValue = option.remove_surrounding_quotes())
                ;
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
            var command = ConfigCommandType.unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault().to_string().Replace("-",string.Empty);
            Enum.TryParse(unparsedCommand, true, out command);
            if (command == ConfigCommandType.unknown)
            {
                if (!string.IsNullOrWhiteSpace(unparsedCommand)) this.Log().Warn("Unknown command {0}. Setting to list.".format_with(unparsedCommand));
                command = ConfigCommandType.list;
            }

            configuration.ConfigCommand.Command = command;

            if ((configuration.ConfigCommand.Command == ConfigCommandType.list
                 || !string.IsNullOrWhiteSpace(configuration.ConfigCommand.Name)
                )
                && unparsedArguments.Count > 1) throw new ApplicationException("A single features command must be listed. Please see the help menu for those commands");

            if (string.IsNullOrWhiteSpace(configuration.ConfigCommand.Name) && unparsedArguments.Count >=2)
            {
                configuration.ConfigCommand.Name = unparsedArguments[1];
            }
            if (string.IsNullOrWhiteSpace(configuration.ConfigCommand.ConfigValue) && unparsedArguments.Count >= 3)
            {
                configuration.ConfigCommand.ConfigValue = unparsedArguments[2];
            }
        }

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
            if (configuration.ConfigCommand.Command != ConfigCommandType.list && string.IsNullOrWhiteSpace(configuration.ConfigCommand.Name)) throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name by option or position.".format_with(configuration.ConfigCommand.Command.to_string()));
            if (configuration.ConfigCommand.Command == ConfigCommandType.set && string.IsNullOrWhiteSpace(configuration.ConfigCommand.ConfigValue)) throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --value by option or position.".format_with(configuration.ConfigCommand.Command.to_string()));
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Config Command");
            this.Log().Info(@"
Chocolatey will allow you to interact with the configuration file settings.

NOTE: Available in 0.9.9.9+.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco config [list]|get|set|unset [<options/switches>]

NOTE: `Unset` subcommand available in 0.9.10+.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco config
    choco config list
    choco config get cacheLocation
    choco config get --name cacheLocation
    choco config set cacheLocation c:\temp\choco
    choco config set --name cacheLocation --value c:\temp\choco
    choco config unset proxy
    choco config unset --name proxy

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

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "See It In Action");
            "chocolatey".Log().Info(@"
Config shown in action: https://raw.githubusercontent.com/wiki/chocolatey/choco/images/gifs/choco_config.gif

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            _configSettingsService.noop(configuration);
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            switch (configuration.ConfigCommand.Command)
            {
                case ConfigCommandType.list:
                    _configSettingsService.config_list(configuration);
                    break;
                case ConfigCommandType.get:
                    _configSettingsService.config_get(configuration);
                    break;
                case ConfigCommandType.set:
                    _configSettingsService.config_set(configuration);
                    break;
                case ConfigCommandType.unset:
                    _configSettingsService.config_unset(configuration);
                    break;
            }
        }

        public virtual bool may_require_admin_access()
        {
            var config = Config.get_configuration_settings();
            if (config == null) return true;

            return config.ConfigCommand.Command != ConfigCommandType.list;
        }
    }
}
