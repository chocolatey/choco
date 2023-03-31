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
    using infrastructure.configuration;
    using logging;
    using services;

    [CommandFor("source", "view and configure default sources")]
    [CommandFor("sources", "view and configure default sources (alias for source)")]
    public class ChocolateySourceCommand : IListCommand<ChocolateySource>
    {
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateySourceCommand(IChocolateyConfigSettingsService configSettingsService)
        {
            _configSettingsService = configSettingsService;
        }

        public virtual void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Sources = string.Empty;

            optionSet
                .Add("n=|name=",
                     "Name - the name of the source. Required with actions other than list. Defaults to empty.",
                     option => configuration.SourceCommand.Name = option.UnquoteSafe())
                .Add("s=|source=",
                     "Source - The source. This can be a folder/file share or an http location. If it is a url, it will be a location you can go to in a browser and it returns OData with something that says Packages in the browser, similar to what you see when you go to https://community.chocolatey.org/api/v2/. Required with add action. Defaults to empty.",
                     option => configuration.Sources = option.UnquoteSafe())
                .Add("u=|user=",
                     "User - used with authenticated feeds. Defaults to empty.",
                     option => configuration.SourceCommand.Username = option.UnquoteSafe())
                .Add("p=|password=",
                     "Password - the user's password to the source. Encrypted in chocolatey.config file.",
                     option => configuration.SourceCommand.Password = option.UnquoteSafe())
                .Add("cert=",
                     "Client certificate - PFX pathname for an x509 authenticated feeds. Defaults to empty.",
                     option => configuration.SourceCommand.Certificate = option.UnquoteSafe())
                .Add("cp=|certpassword=",
                     "Certificate Password - the client certificate's password to the source. Defaults to empty.",
                     option => configuration.SourceCommand.CertificatePassword = option.UnquoteSafe())
                .Add("priority=",
                     "Priority - The priority order of this source as compared to other sources, lower is better. Defaults to 0 (no priority). All priorities above 0 will be evaluated first, then zero-based values will be evaluated in config file order.",
                     option => configuration.SourceCommand.Priority = int.Parse(option.UnquoteSafe()))
                 .Add("bypassproxy|bypass-proxy",
                     "Bypass Proxy - Should this source explicitly bypass any explicitly or system configured proxies? Defaults to false.",
                     option => configuration.SourceCommand.BypassProxy = option != null)
                 .Add("allowselfservice|allow-self-service",
                     "Allow Self-Service - Should this source be allowed to be used with self-service? Requires business edition (v1.10.0+) with feature 'useBackgroundServiceWithSelfServiceSourcesOnly' turned on. Defaults to false.",
                     option => configuration.SourceCommand.AllowSelfService = option != null)
                 .Add("adminonly|admin-only",
                     "Visible to Administrators Only - Should this source be visible to non-administrators? Requires business edition (v1.12.2+). Defaults to false.",
                     option => configuration.SourceCommand.VisibleToAdminsOnly = option != null)
                ;
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single sources command must be listed. Please see the help menu for those commands");
            }

            var command = SourceCommandType.Unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();
            Enum.TryParse(unparsedCommand, true, out command);
            if (command == SourceCommandType.Unknown)
            {
                if (!string.IsNullOrWhiteSpace(unparsedCommand)) this.Log().Warn("Unknown command {0}. Setting to list.".FormatWith(unparsedCommand));
                command = SourceCommandType.List;
            }

            configuration.SourceCommand.Command = command;
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
            if (configuration.SourceCommand.Command != SourceCommandType.List && string.IsNullOrWhiteSpace(configuration.SourceCommand.Name))
            {
                throw new ApplicationException("When specifying the subcommand '{0}', you must also specify --name.".FormatWith(configuration.SourceCommand.Command.ToStringSafe().ToLower()));
            }

            if (configuration.SourceCommand.Command == SourceCommandType.Add && string.IsNullOrWhiteSpace(configuration.Sources))
            {
                throw new ApplicationException("When specifying the subcommand 'add', you must also specify --source.");
            }

            if (!string.IsNullOrWhiteSpace(configuration.SourceCommand.Username) && string.IsNullOrWhiteSpace(configuration.SourceCommand.Password))
            {
                this.Log().Debug(ChocolateyLoggers.LogFileOnly, "Username '{0}' provided. Asking for password.".FormatWith(configuration.SourceCommand.Username));
                System.Console.Write("User name '{0}' provided. Password: ".FormatWith(configuration.SourceCommand.Username));
                configuration.SourceCommand.Password = InteractivePrompt.GetPassword(configuration.PromptForConfirmation);
            }
        }

        public virtual void HelpMessage(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Source Command");
            this.Log().Info(@"
Chocolatey will allow you to interact with sources.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco source [list]|add|remove|disable|enable [<options/switches>]
    choco sources [list]|add|remove|disable|enable [<options/switches>]
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco source
    choco source list
    choco source add -n=bob -s=""https://somewhere/out/there/api/v2/""
    choco source add -n=bob -s ""'https://somewhere/out/there/api/v2/'"" -cert=\Users\bob\bob.pfx
    choco source add -n=bob -s ""'https://somewhere/out/there/api/v2/'"" -u=bob -p=12345
    choco source disable -n=bob
    choco source enable -n=bob
    choco source remove -n=bob

When it comes to the source location, this can be a folder/file share or an http
location. If it is a url, it will be a location you can go to in a browser and
it returns OData with something that says Packages in the browser, similar to
what you see when you go to https://community.chocolatey.org/api/v2/.

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
            _configSettingsService.DryRun(configuration);
        }

        public virtual void Run(ChocolateyConfiguration configuration)
        {
            switch (configuration.SourceCommand.Command)
            {
                case SourceCommandType.List:
                    _configSettingsService.ListSources(configuration);
                    break;
                case SourceCommandType.Add:
                    _configSettingsService.AddSource(configuration);
                    break;
                case SourceCommandType.Remove:
                    _configSettingsService.RemoveSource(configuration);
                    break;
                case SourceCommandType.Disable:
                    _configSettingsService.DisableSource(configuration);
                    break;
                case SourceCommandType.Enable:
                    _configSettingsService.EnableSource(configuration);
                    break;
            }
        }

        public virtual IEnumerable<ChocolateySource> List(ChocolateyConfiguration configuration)
        {
            return _configSettingsService.ListSources(configuration);
        }

        public virtual int Count(ChocolateyConfiguration config)
        {
            return List(config).Count();
        }

        public virtual bool MayRequireAdminAccess()
        {
            var config = Config.GetConfigurationSettings();
            if (config == null) return true;

            return config.SourceCommand.Command != SourceCommandType.List;
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

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual int count(ChocolateyConfiguration config)
            => Count(config);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public virtual IEnumerable<ChocolateySource> list(ChocolateyConfiguration config)
            => List(config);
#pragma warning restore IDE1006
    }
}
