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

    [CommandFor(CommandNameType.source)]
    public sealed class ChocolateySourceCommand : ICommand
    {
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateySourceCommand(IChocolateyConfigSettingsService configSettingsService)
        {
            _configSettingsService = configSettingsService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("n=|name=",
                     "Name - the name of the source. Required with some actions. Defaults to empty.",
                     option => configuration.SourceCommand.Name = option)
                .Add("s=|source=",
                     "Source - The source. Defaults to empty.",
                     option => configuration.SourceCommand.Source = option) 
                .Add("u=|user=",
                     "User - used with authenticated feeds. Defaults to empty.",
                     option => configuration.SourceCommand.Username = option) 
                .Add("p=|password=",
                     "Password - the user's password to the source. Encrypted in file.",
                     option => configuration.SourceCommand.Password = option)
                ;
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single sources command must be listed. Please see the help menu for those commands");
            }

            configuration.SourceCommand.Command = unparsedArguments.FirstOrDefault();

            var command = configuration.SourceCommand.Command;

            if (command != "add" && command != "remove" && command != "disable" && command != "enable")
            {
                configuration.SourceCommand.Command = "list";
            }

            if (configuration.SourceCommand.Command != "list" && string.IsNullOrWhiteSpace(configuration.SourceCommand.Name))
            {
                throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name.".format_with(configuration.SourceCommand.Command));
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Sources Command");
            this.Log().Info(@"
Chocolatey will allow you to interact with sources.

Usage: choco source [list]|add|remove|disable|enable [options/switches]

Examples:

 choco source   
 choco source list  
 choco source add -n=bob -s""https://somewhere/out/there/api/v2/""
 choco source add -n=bob -s""https://somewhere/out/there/api/v2/"" -u=bob -p=12345
 choco source disable -n=bob
 choco source enable -n=bob
 choco source remove -n=bob 

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _configSettingsService.noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            switch (configuration.SourceCommand.Command)
            {
                case "list":
                    _configSettingsService.source_list(configuration);
                    break;
                case "add":
                    _configSettingsService.source_add(configuration);
                    break;
                case "remove":
                    _configSettingsService.source_remove(configuration);
                    break;
                case "disable":
                    _configSettingsService.source_disable(configuration);
                    break;
                case "enable":
                    _configSettingsService.source_enable(configuration);
                    break;
            }

        }
    }
}