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

    [CommandFor(CommandNameType.sources)]
    [CommandFor(CommandNameType.source)]
    public sealed class ChocolateySourceCommand : IListCommand<ChocolateySource>
    {
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateySourceCommand(IChocolateyConfigSettingsService configSettingsService)
        {
            _configSettingsService = configSettingsService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Sources = string.Empty;

            optionSet
                .Add("n=|name=",
                     "Name - the name of the source. Required with some actions. Defaults to empty.",
                     option => configuration.SourceCommand.Name = option.remove_surrounding_quotes())
                .Add("s=|source=",
                     "Source - The source. Defaults to empty.",
                     option => configuration.Sources = option.remove_surrounding_quotes())
                .Add("u=|user=",
                     "User - used with authenticated feeds. Defaults to empty.",
                     option => configuration.SourceCommand.Username = option.remove_surrounding_quotes())
                .Add("p=|password=",
                     "Password - the user's password to the source. Encrypted in chocolatey.config file.",
                     option => configuration.SourceCommand.Password = option.remove_surrounding_quotes())  
                .Add("priority=",
                     "Priority - The priority order of this source as compared to other sources, lower is better. Defaults to 0 (no priority). All priorities above 0 will be evaluated first, then zero-based values will be evaluated in config file order.",
                     option => configuration.SourceCommand.Priority = int.Parse(option.remove_surrounding_quotes()))
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single sources command must be listed. Please see the help menu for those commands");
            }

            var command = SourceCommandType.unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
            Enum.TryParse(unparsedCommand, true, out command);
            if (command == SourceCommandType.unknown)
            {
                if (!string.IsNullOrWhiteSpace(unparsedCommand)) this.Log().Warn("Unknown command {0}. Setting to list.".format_with(unparsedCommand));
                command = SourceCommandType.list;
            }

            configuration.SourceCommand.Command = command;
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
            if (configuration.SourceCommand.Command != SourceCommandType.list && string.IsNullOrWhiteSpace(configuration.SourceCommand.Name))
            {
                throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name.".format_with(configuration.SourceCommand.Command.to_string()));
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Source Command");
            this.Log().Info(@"
Chocolatey will allow you to interact with sources.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco source [list]|add|remove|disable|enable [<options/switches>]
    choco sources [list]|add|remove|disable|enable [<options/switches>]
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco source
    choco source list
    choco source add -n=bob -s""https://somewhere/out/there/api/v2/""
    choco source add -n=bob -s""https://somewhere/out/there/api/v2/"" -u=bob -p=12345
    choco source disable -n=bob
    choco source enable -n=bob
    choco source remove -n=bob
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _configSettingsService.noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            switch (configuration.SourceCommand.Command)
            {
                case SourceCommandType.list:
                    _configSettingsService.source_list(configuration);
                    break;
                case SourceCommandType.add:
                    _configSettingsService.source_add(configuration);
                    break;
                case SourceCommandType.remove:
                    _configSettingsService.source_remove(configuration);
                    break;
                case SourceCommandType.disable:
                    _configSettingsService.source_disable(configuration);
                    break;
                case SourceCommandType.enable:
                    _configSettingsService.source_enable(configuration);
                    break;
            }
        }

        public IEnumerable<ChocolateySource> list(ChocolateyConfiguration configuration)
        {
            return _configSettingsService.source_list(configuration);
        }

        public int count(ChocolateyConfiguration config)
        {
            return list(config).Count();
        }

        public bool may_require_admin_access()
        {
            return true;
        }
    }
}
