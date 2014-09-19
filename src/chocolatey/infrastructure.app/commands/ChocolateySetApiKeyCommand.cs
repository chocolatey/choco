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

    [CommandFor(CommandNameType.setapikey)]
    public sealed class ChocolateySetApiKeyCommand : ICommand
    {
        private readonly IChocolateyConfigSettingsService _configSettingsService;


        public ChocolateySetApiKeyCommand(IChocolateyConfigSettingsService configSettingsService)
        {
            _configSettingsService = configSettingsService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Source = ApplicationParameters.DefaultChocolateyPushSource;

            optionSet
                .Add("s=|source=",
                     "Source [REQUIRED] - The source the key applies to. Defaults to {0}".format_with(ApplicationParameters.DefaultChocolateyPushSource),
                     option => configuration.Source = option)
                .Add("k=|key=|apikey=|api-key=",
                     "ApiKey [REQUIRED] - The api key for the source.",
                     option => configuration.ApiKeyCommand.Key = option)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (string.IsNullOrWhiteSpace(configuration.ApiKeyCommand.Key))
            {
                throw new ApplicationException("You must specify both 'source' (if not {0}) and 'key' to set an api key.".format_with(ApplicationParameters.DefaultChocolateyPushSource));
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "ApiKey Command");
            this.Log().Info(@"
This sets an api key for a particular source so it doesn't need to be specified every time.

Usage: choco setapikey [options/switches]

Example:

 choco setapikey -k=""value""
 choco setapikey -s""https://somewhere/out/there/"" -k=""value""

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _configSettingsService.noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _configSettingsService.set_api_key(configuration);
        }
    }
}