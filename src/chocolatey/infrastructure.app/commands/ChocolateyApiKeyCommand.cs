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
            configuration.Source = null;

            optionSet
                .Add("s=|source=",
                     "Source [REQUIRED] - The source location for the key",
                     option => configuration.Source = option)
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
            if (!string.IsNullOrWhiteSpace(configuration.ApiKeyCommand.Key) && string.IsNullOrWhiteSpace(configuration.Source))
            {
                throw new ApplicationException("You must specify both 'source' and 'key' to set an api key.");
            }            
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "ApiKey Command");
            this.Log().Info(@"
This lists api keys that are set or sets an api key for a particular source so it doesn't need to be specified every time.

Anything that doesn't contain source and key will list api keys.

Usage: choco apikey [options/switches]

Example:

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
                            this.Log().Info(() => "{0} - {1}".format_with(key.Source,key.Key));
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