// Copyright © 2017 - 2018 Chocolatey Software, Inc
// Copyright © 2011 - 2017 RealDimensions Software, LLC
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
    using infrastructure.commands;
    using logging;
    using services;
    using templates;

    [CommandFor("new", "generates files necessary for a chocolatey package from a template")]
    public class ChocolateyNewCommand : ICommand
    {
        private readonly ITemplateService _templateService;

        public ChocolateyNewCommand(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("a|auto|automaticpackage",
                     "AutomaticPackage - Generate automatic package instead of normal. Defaults to false",
                     option => configuration.NewCommand.AutomaticPackage = option != null)
                .Add("t=|template=|template-name=",
                     "TemplateName - Use a named template in {0}\\templates\\templatename instead of built-in template. Available in 0.9.9.9+. Manage templates as packages in 0.9.10+.".format_with(ApplicationParameters.InstallLocation),
                     option => configuration.NewCommand.TemplateName = option.remove_surrounding_quotes())
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
                .Add("out=|outdir=|outputdirectory=|output-directory=",
                    "OutputDirectory - Specifies the directory for the created Chocolatey package file. If not specified, uses the current directory. Available in 0.9.10+.",
                    option => configuration.OutputDirectory = option.remove_surrounding_quotes())
                .Add("built-in|built-in-template|originaltemplate|original-template|use-original-template|use-built-in-template",
                    "BuiltInTemplate - Use the original built-in template instead of any override. Available in 0.9.10+.",
                    option => configuration.NewCommand.UseOriginalTemplate = option != null)
                ;
            //todo: more built-in templates
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (string.IsNullOrWhiteSpace(configuration.NewCommand.Name))
            {
                configuration.NewCommand.Name = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault(arg => !arg.StartsWith("-") && !arg.contains("="));
                var property = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault().Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);
                if (property.Length == 1)
                {
                    configuration.NewCommand.TemplateProperties.Add(TemplateValues.NamePropertyName, configuration.NewCommand.Name);
                }
            }

            foreach (var unparsedArgument in unparsedArguments.or_empty_list_if_null())
            {
                var property = unparsedArgument.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (property.Length == 2)
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

        public virtual void handle_validation(ChocolateyConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.NewCommand.Name))
            {
                throw new ApplicationException("Name is required. Please pass in a name for the new package.");
            }

            if (configuration.NewCommand.Name.StartsWith("-file", StringComparison.OrdinalIgnoreCase) || configuration.NewCommand.Name.StartsWith("--file", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApplicationException(@"Automatic package creation from installer files only available in Business
 edition. See https://chocolatey.org/compare for details.");
            }
        }

        public virtual void help_message(ChocolateyConfiguration configuration)
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

NOTE: Starting in 0.9.10, you can pass arbitrary property value pairs
 through to templates. This really unlocks your ability to create
 packages automatically!

NOTE: Chocolatey for Business can create complete packages by just
 pointing the new command to native installers!

NOTE: Chocolatey for Business can also download and internalize remote
 resources from existing packages so that existing packages can be used 
 without being tied to the internet.
 This is called automatic recompile.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco new bob
    choco new bob -a --version 1.2.0 maintainername=""'This guy'""
    choco new bob silentargs=""'/S'"" url=""'https://somewhere/out/there.msi'""
    choco new bob --outputdirectory Packages

NOTE: See scripting in the command reference (`choco -?`) for how to 
 write proper scripts and integrations.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Exit Codes");
            "chocolatey".Log().Info(@"
Exit codes that normally result from running this command.

Normal:
 - 0: operation was successful, no issues detected
 - -1 or 1: an error has occurred

If you find other exit codes that we have not yet documented, please 
 file a ticket so we can document it at 
 https://github.com/chocolatey/choco/issues/new/choose.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Options and Switches");
        }

        public virtual void noop(ChocolateyConfiguration configuration)
        {
            _templateService.noop(configuration);
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            _templateService.generate(configuration);
        }

        public virtual bool may_require_admin_access()
        {
            return false;
        }
    }
}
