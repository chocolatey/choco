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

    [CommandFor(CommandNameType.push)]
    public sealed class ChocolateyPushCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateyPushCommand(IChocolateyPackageService packageService, IChocolateyConfigSettingsService configSettingsService)
        {
            _packageService = packageService;
            _configSettingsService = configSettingsService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Source = ApplicationParameters.DefaultChocolateyPushSource;
            configuration.PushCommand.TimeoutInSeconds = 300;

            optionSet
                 .Add("s=|source=",
                     "Source - The source we are pushing the package to. Defaults to {0}".format_with(ApplicationParameters.DefaultChocolateyPushSource),
                     option => configuration.Source = option)
                 .Add("k=|key=|apikey=|api-key=",
                     "ApiKey - The api key for the source. If not specified (and not local file source), does a lookup. If one is not found, will fail.",
                     option => configuration.PushCommand.Key = option)
                 .Add("t=|timeout=",
                     "Timeout (in seconds) - The time to allow a package push to occur before timing out. Defaults to 300 seconds (5 minutes).",
                     option =>
                         {
                             int timeout = 0;
                             int.TryParse(option, out timeout);
                             if (timeout > 0)
                             {
                                 configuration.PushCommand.TimeoutInSeconds = timeout;
                             }
                         })     
                 //.Add("b|disablebuffering|disable-buffering",
                 //    "DisableBuffering -  Disable buffering when pushing to an HTTP(S) server to decrease memory usage. Note that when this option is enabled, integrated windows authentication might not work.",
                 //    option => configuration.PushCommand.DisableBuffering = option)
                ;
            //todo: push command - allow disable buffering?
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments); // path to .nupkg - assume relative

            var remoteSource = new Uri(configuration.Source);
            if (string.IsNullOrWhiteSpace(configuration.PushCommand.Key) && !remoteSource.IsUnc && !remoteSource.IsFile)
            {
                // perform a lookup
                configuration.PushCommand.Key = _configSettingsService.get_api_key(configuration);

                if (string.IsNullOrWhiteSpace(configuration.PushCommand.Key))
                {
                    throw new ApplicationException("An ApiKey was not found for '{0}'. You must either set an api key in the configuration or specify one with --api-key.".format_with(configuration.Source));
                }
            }

            // security advisory
            if (!configuration.Force || configuration.Source.to_lower().Contains("chocolatey.org"))
            {
                if (remoteSource.Scheme == "http" && remoteSource.Host != "localhost")
                {
                    string errorMessage = 
@"WARNING! The specified source '{0}' is not secure.
 Sending apikey over insecure channels leaves your data susceptible to hackers.
 Please update your source to a more secure source and try again.
 
 Use --force if you understand the implications of this warning or are 
 accessing an internal feed. If you are however doing this against an internet
 feed, then the choco gods think you are crazy. ;-)
 
NOTE: For chocolatey.org, you must update the source to be secure.".format_with(configuration.Source);
                    throw new ApplicationException(errorMessage);
                }
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Push Command");
            this.Log().Info(@"
Chocolatey will attempt to push a compiled nupkg to a package feed. That feed can be a local folder, a file share, the community feed '{0}' or a custom/private feed.

Usage: choco push [path to nupkg] [options/switches]

Note: If there is more than one nupkg file in the folder, the command will require specifying the path to the file.

".format_with(ApplicationParameters.DefaultChocolateyPushSource));
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _packageService.push_noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _packageService.push_run(configuration);
        }
    }
}