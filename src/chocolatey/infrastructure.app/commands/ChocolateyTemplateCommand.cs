// Copyright © 2017 - 2021 Chocolatey Software, Inc
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
    using chocolatey.infrastructure.results;
    using commandline;
    using configuration;
    using domain;
    using infrastructure.commands;
    using logging;
    using services;
    using templates;

    [CommandFor("template", "get information about installed templates")]
    [CommandFor("templates", "get information about installed templates (alias for template)")]
    public class ChocolateyTemplateCommand : ICommand
    {
        private readonly ITemplateService _templateService;

        public ChocolateyTemplateCommand(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        public void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            optionSet
                .Add("n=|name=",
                    "The name of the template to get information about.",
                    option => configuration.TemplateCommand.Name = option.UnquoteSafe().ToLower());
            // todo: #2570 Allow for templates from an external path? Requires #1477
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            // don't set configuration.Input or it will be passed to list

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single template command must be listed. Please see the help menu for those commands");
            }

            var command = TemplateCommandType.Unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
            Enum.TryParse(unparsedCommand, true, out command);

            if (command == TemplateCommandType.Unknown)
            {
                if (!string.IsNullOrWhiteSpace(unparsedCommand)) this.Log().Warn("Unknown command {0}. Setting to list.".FormatWith(unparsedCommand));
                command = TemplateCommandType.List;
            }

            configuration.TemplateCommand.Command = command;
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
            if (configuration.TemplateCommand.Command != TemplateCommandType.List && string.IsNullOrWhiteSpace(configuration.TemplateCommand.Name))
            {
                throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name.".FormatWith(configuration.TemplateCommand.Command.ToStringSafe().ToLower()));
            }
        }

        public virtual void HelpMessage(ChocolateyConfiguration configuration)
        {
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Template Command");
            "chocolatey".Log().Info(@"
List information installed templates.

Both manually installed templates and templates installed via
 .template packages are displayed.");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco template [list]|info [<options/switches>]");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco template
    choco templates
    choco template list
    choco template info --name msi
    choco template list --reduce-output
    choco template list --verbose

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

        public virtual void DryRun(ChocolateyConfiguration configuration)
        {
            switch (configuration.TemplateCommand.Command)
            {
                case TemplateCommandType.List:
                    _templateService.ListDryRun(configuration);
                    break;
                case TemplateCommandType.Info:
                    _templateService.ListDryRun(configuration);
                    break;
            }
        }

        public virtual void Run(ChocolateyConfiguration configuration)
        {
            switch (configuration.TemplateCommand.Command)
            {
                case TemplateCommandType.List:
                    _templateService.List(configuration);
                    break;
                case TemplateCommandType.Info:
                    configuration.Verbose = true;
                    _templateService.List(configuration);
                    break;
            }
        }

        public virtual bool MayRequireAdminAccess()
        {
            return false;
        }

#pragma warning disable IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
            => ConfigureArgumentParser(optionSet, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
            => ParseAdditionalArguments(unparsedArguments, configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void handle_validation(ChocolateyConfiguration configuration)
            => Validate(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void help_message(ChocolateyConfiguration configuration)
            => HelpMessage(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void noop(ChocolateyConfiguration configuration)
            => DryRun(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual void run(ChocolateyConfiguration configuration)
            => Run(configuration);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual bool may_require_admin_access()
            => MayRequireAdminAccess();
#pragma warning restore IDE1006
    }
}
