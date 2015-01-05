namespace chocolatey.infrastructure.app.commands
{
    using System.Collections.Generic;
    using attributes;
    using commandline;
    using configuration;
    using domain;
    using infrastructure.commands;
    using logging;
    using services;

    [CommandFor(CommandNameType.list)]
    [CommandFor(CommandNameType.search)]
    public sealed class ChocolateyListCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;

        public ChocolateyListCommand(IChocolateyPackageService packageService)
        {
            _packageService = packageService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("s=|source=",
                     "Source - Source location for install. Can include special 'webpi'. Defaults to sources.",
                     option => configuration.Sources = option)
                .Add("l|lo|localonly|local-only",
                     "LocalOnly - Only search in installed items",
                     option => configuration.ListCommand.LocalOnly = option != null)
                .Add("p|includeprograms|include-programs",
                     "IncludePrograms - Used in conjuction with LocalOnly, filters out apps chocolatey has listed as packages and includes those in the list. Defaults to false.",
                     option => configuration.ListCommand.IncludeRegistryPrograms = option != null)
                .Add("a|all|allversions|all-versions",
                     "AllVersions - include results from all versions",
                     option => configuration.AllVersions = option != null)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (configuration.ListCommand.LocalOnly)
            {
                configuration.Sources = ApplicationParameters.PackagesLocation;
            }
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "List/Search Command");
            this.Log().Info(@"
Chocolatey will perform a search for a package local or remote.

Usage: choco search filter [options/switches]
Usage: choco list filter [options/switches]

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _packageService.list_noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _packageService.list_run(configuration, logResults: true);
        }
    }
}