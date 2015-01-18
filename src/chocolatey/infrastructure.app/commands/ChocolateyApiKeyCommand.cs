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
    using attributes;
    using commandline;
    using configuration;
    using domain;
    using infrastructure.commands;
    using logging;
    using services;

    [CommandFor(CommandNameType.apikey)]
    public sealed class ChocolateyApiKeyCommand : ICommand
    {
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateyApiKeyCommand(IChocolateyConfigSettingsService configSettingsService)
        {
            _configSettingsService = configSettingsService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Sources = null;

            optionSet
                .Add("s=|source=",
                     "Source [REQUIRED] - The source location for the key",
                     option => configuration.Sources = option)
                .Add("k=|key=|apikey=|api-key=",
                     "ApiKey - The api key for the source.",
                     option => configuration.ApiKeyCommand.Key = option)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration.ApiKeyCommand.Key) && string.IsNullOrWhiteSpace(configuration.Sources))
            {
                throw new ApplicationException("You must specify both 'source' and 'key' to set an api key.");
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "ApiKey Command");
            this.Log().Info(@"
This lists api keys that are set or sets an api key for a particular   
 source so it doesn't need to be specified every time.

Anything that doesn't contain source and key will list api keys.

Usage: choco apikey [options/switches]

Examples:

 choco apikey
 choco apikey -s""https://somewhere/out/there""
 choco apikey -s""https://somewhere/out/there/"" -k=""value""
 choco apikey -s""https://chocolatey.org/"" -k=""123-123123-123""
");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _configSettingsService.noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.ApiKeyCommand.Key))
            {
                _configSettingsService.get_api_key(configuration, (key) =>
                    {
                        if (configuration.RegularOuptut)
                        {
                            this.Log().Info(() => "{0} - {1}".format_with(key.Source, key.Key));
                        }
                        else
                        {
                            this.Log().Info(() => "{0}|{1}".format_with(key.Source, key.Key));
                        }
                    });
            }
            else
            {
                _configSettingsService.set_api_key(configuration);
            }
        }
    }
}