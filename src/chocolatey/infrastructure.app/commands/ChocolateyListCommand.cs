namespace chocolatey.infrastructure.app.commands
{
    using System.Collections.Generic;
    using attributes;
    using commandline;
    using configuration;
    using infrastructure.commands;
    using logging;
    using services;

    [CommandFor(CommandNameType.list)]
    [CommandFor(CommandNameType.search)]
    public sealed class ChocolateyListCommand : ICommand
    {
        private readonly INugetService _nugetService;

        public ChocolateyListCommand(INugetService nugetService)
        {
            _nugetService = nugetService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - Source location for install. Can include special 'webpi'. Defaults to sources.",
                     option => configuration.Source = option)
                .Add("lo|localonly",
                     "LocalOnly - Only search in installed items",
                     option => configuration.LocalOnly = option != null)
                .Add("a|all|allversions",
                     "AllVersions - include results from all versions",
                     option => configuration.AllVersions = option != null)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Filter = string.Join(" ", unparsedArguments);

            if (configuration.LocalOnly)
            {
                configuration.Source = ApplicationParameters.PackagesLocation;
                configuration.Source = @"c:\chocolatey\lib"; //todo:temporary
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Warn(ChocolateyLoggers.Important, "List/Search Command");
            this.Log().Info(@"
Chocolatey will perform a search for a package local or remote.

Usage: choco search filter [options/switches]
Usage: choco list filter [options/switches]

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            if (configuration.Source.is_equal_to(SpecialSourceTypes.webpi.to_string()))
            {
                //todo: webpi
            }
            else
            {
                _nugetService.list_noop(configuration);
            }
        }

        public void run(ChocolateyConfiguration configuration)
        {
            this.Log().Debug(() => "Searching for package information");

            if (configuration.Source.is_equal_to(SpecialSourceTypes.webpi.to_string()))
            {
                //todo: webpi
                //install webpi if not installed
                //run the webpi command 
                this.Log().Info("Command not yet functional, stay tuned...");
            }
            else
            {
                _nugetService.list_run(configuration, logResults: true);
            }
        }
    }
}