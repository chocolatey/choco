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
                .Add("a|auto|automaticpackage",
                     "AutomaticPackage - Generate automatic package instead of normal. Defaults to false",
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

            if (string.IsNullOrWhiteSpace(configuration.NewCommand.Name))
            {
                configuration.NewCommand.Name = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
                var property = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault().Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (property.Count() == 1)
                {
                    configuration.NewCommand.TemplateProperties.Add(TemplateValues.NamePropertyName, configuration.NewCommand.Name);
                }
            }

            foreach (var unparsedArgument in unparsedArguments.or_empty_list_if_null())
            {
                var property = unparsedArgument.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (property.Count() == 2)
                {
                    var propName = property[0].trim_safe();
                    var propValue = property[1].trim_safe().remove_surrounding_quotes();

                    if (configuration.NewCommand.TemplateProperties.ContainsKey(propName))
                    {
                        this.Log().Warn(() => "A value for '{0}' has already been added with the value '{1}'. Ignoring {0}='{2}'.".format_with(propName, configuration.NewCommand.TemplateProperties[propName],propValue));
                    }
                    else
                    {
                        configuration.NewCommand.TemplateProperties.Add(propName, propValue);
                    }
                }
            }
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
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