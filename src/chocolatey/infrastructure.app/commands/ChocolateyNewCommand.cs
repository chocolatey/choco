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
                             configuration.NewCommand.Name = option.remove_surrounding_quotes();
                             configuration.NewCommand.TemplateProperties.Add(TemplateValues.NamePropertyName, option.remove_surrounding_quotes());
                         })
                .Add("version=",
                     "Version - the version of the package. Can also be passed as the property PackageVersion=somevalue",
                     option => configuration.NewCommand.TemplateProperties.Add(TemplateValues.VersionPropertyName, option.remove_surrounding_quotes()))
                .Add("maintainer=",
                     "Maintainer - the name of the maintainer. Can also be passed as the property MaintainerName=somevalue",
                     option => configuration.NewCommand.TemplateProperties.Add(TemplateValues.MaintainerPropertyName, option.remove_surrounding_quotes()))
                ;
            //todo: template type
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (string.IsNullOrWhiteSpace(configuration.NewCommand.Name))
            {
                configuration.NewCommand.Name = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
                var property = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault().Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);
                if (property.Count() == 1)
                {
                    configuration.NewCommand.TemplateProperties.Add(TemplateValues.NamePropertyName, configuration.NewCommand.Name);
                }
            }

            foreach (var unparsedArgument in unparsedArguments.or_empty_list_if_null())
            {
                var property = unparsedArgument.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);
                if (property.Count() == 2)
                {
                    var propName = property[0].trim_safe();
                    var propValue = property[1].trim_safe().remove_surrounding_quotes();

                    if (configuration.NewCommand.TemplateProperties.ContainsKey(propName))
                    {
                        this.Log().Warn(() => "A value for '{0}' has already been added with the value '{1}'. Ignoring {0}='{2}'.".format_with(propName, configuration.NewCommand.TemplateProperties[propName], propValue));
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
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco new <name> [<options/switches>] [<property=value> <propertyN=valueN>]

Possible properties to pass:
    packageversion
    maintainername
    maintainerrepo
    installertype
    url
    url64
    silentargs
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco new bob
    choco new bob -a --version 1.2.0 maintainername=""This guy""
    choco new bob silentargs=""/S"" url=""https://somewhere/out/there.msi""

");
          
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _templateService.noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _templateService.generate(configuration);
        }

        public bool may_require_admin_access()
        {
            return false;
        }
    }
}