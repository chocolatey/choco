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

    [CommandFor(CommandNameType.pack)]
    public sealed class ChocolateyPackCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;

        public ChocolateyPackCommand(IChocolateyPackageService packageService)
        {
            _packageService = packageService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("version=",
                     "Version - The version you would like to insert into the package",
                     option => configuration.Version = option)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Pack Command");
            this.Log().Info(@"
Chocolatey will attempt to package a nuspec into a compiled nupkg.

Usage: choco pack [path to nuspec] [options/switches]
");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _packageService.pack_noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _packageService.pack_run(configuration);
        }
    }
}