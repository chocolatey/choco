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
    using templates;

    [CommandFor(CommandNameType.@new)]
    public sealed class ChocolateyNewCommand : ICommand
    {
        private readonly ITemplateService _templateService;

        public ChocolateyNewCommand(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("a|auto",
                     "Generate automatic package instead of normal. Defaults to false",
                     option => configuration.NewCommand.AutomaticPackage = option != null)
                .Add("name=",
                     "Name [Required]- the name of the package. Can be passed as first parameter without \"--name=\".",
                     option =>
                     {
                         configuration.NewCommand.Name = option;
                         configuration.NewCommand.TemplateProperties.Add(TemplateValues.NamePropertyName, option);
                     })
                .Add("version=",
                     "Version - the version of the package. Can also be passed as the property PackageVersion=somevalue",
                     option => configuration.NewCommand.TemplateProperties.Add(TemplateValues.VersionPropertyName, option))
                .Add("maintainer=",
                     "Maintainer - the name of the maintainer. Can also be passed as the property MaintainerName=somevalue",
                     option => configuration.NewCommand.TemplateProperties.Add(TemplateValues.MaintainerPropertyName, option))
                ;
            //todo: template type
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);
            foreach (var unparsedArgument in unparsedArguments)
            {
                var property = unparsedArgument.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (property.Count() == 2)
                {
                    configuration.NewCommand.TemplateProperties.Add(property[0], property[1]);
                }
                else if (string.IsNullOrWhiteSpace(configuration.NewCommand.Name))
                {
                    configuration.NewCommand.Name = unparsedArguments.FirstOrDefault();
                    configuration.NewCommand.TemplateProperties.Add(TemplateValues.NamePropertyName, configuration.NewCommand.Name);
                }
            }

            if (string.IsNullOrWhiteSpace(configuration.NewCommand.Name))
            {
                throw new ApplicationException("Name is required. Please pass in a name for the new package.");
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "New Command");
            this.Log().Info(@"
Chocolatey will generate package specification files for a new package.

Usage: choco new name [options/switches] [property=value]

Possible properties to pass:
 PackageVersion
 MaintainerName
 MaintainerRepo
 InstallerType
 Url
 Url64
 SilentArgs

");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _templateService.noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _templateService.generate(configuration);
        }
    }
}