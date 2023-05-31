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
    using chocolatey.infrastructure.app.domain;
    using commandline;
    using configuration;
    using infrastructure.commands;
    using infrastructure.configuration;
    using logging;
    using services;

    [CommandFor("apikey", "retrieves, saves or deletes an API key for a particular source")]
    [CommandFor("setapikey", "retrieves, saves or deletes an API key for a particular source (alias for apikey)")]
    public class ChocolateyApiKeyCommand : ICommand
    {
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateyApiKeyCommand(IChocolateyConfigSettingsService configSettingsService)
        {
            _configSettingsService = configSettingsService;
        }

        public virtual void ConfigureArgumentParser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Sources = null;

            optionSet
                .Add("s=|source=",
                     "Source [REQUIRED] - The source location for the key",
                     option => configuration.Sources = option.UnquoteSafe())
                .Add("k=|key=|apikey=|api-key=",
                     "ApiKey - The API key for the source. This is the authentication that identifies you and allows you to push to a source. With some sources this is either a key or it could be a user name and password specified as 'user:password'.",
                     option => configuration.ApiKeyCommand.Key = option.UnquoteSafe())
                ;
        }

        public virtual void ParseAdditionalArguments(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments);

            if (unparsedArguments.Count > 1)
            {
                throw new ApplicationException("A single apikey command must be listed. Please see the help menu for those commands.");
            }

            var command = ApiKeyCommandType.Unknown;
            string unparsedCommand = unparsedArguments.DefaultIfEmpty(string.Empty).FirstOrDefault();

            if (!Enum.TryParse(unparsedCommand, true, out command) || command == ApiKeyCommandType.Unknown)
            {
                command = ApiKeyCommandType.List;

                if (!string.IsNullOrWhiteSpace(unparsedCommand))
                {
                    this.Log().Warn("Unknown command {0}. Setting to list.".FormatWith(unparsedCommand));
                }
                else if (!string.IsNullOrWhiteSpace(configuration.ApiKeyCommand.Key))
                {
                    this.Log().Warn("API key provided. Setting command to add.");
                    command = ApiKeyCommandType.Add;
                }
            }

            configuration.ApiKeyCommand.Command = command;
        }

        public virtual void Validate(ChocolateyConfiguration configuration)
        {
            switch (configuration.ApiKeyCommand.Command)
            {
                case ApiKeyCommandType.Remove:
                    if (string.IsNullOrWhiteSpace(configuration.Sources))
                    {
                        throw new ApplicationException("You must specify 'source' to remove an API key.");
                    }

                    break;
                case ApiKeyCommandType.Add:
                    if (string.IsNullOrWhiteSpace(configuration.Sources) || string.IsNullOrWhiteSpace(configuration.ApiKeyCommand.Key))
                    {
                        throw new ApplicationException("You must specify both 'source' and 'key' to set an API key.");
                    }

                    break;
            }
        }

        public virtual void HelpMessage(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "ApiKey Command");
            this.Log().Info(@"
This lists API keys that are set or sets an api key for a particular
 source so it doesn't need to be specified every time.

Anything that doesn't contain source and key will list API keys.
");
            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco apikey [<options/switches>]
    choco setapikey [<options/switches>]
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco apikey
    choco apikey -s https://somewhere/out/there
    choco apikey list
    choco apikey list -s https://somewhere/out/there
    choco apikey add -s=""https://somewhere/out/there/"" -k=""value""
    choco apikey add -s ""https://push.chocolatey.org/"" -k=""123-123123-123""
    choco apikey add -s ""http://internal_nexus"" -k=""user:password""
    choco apikey remove -s https://somewhere/out/there

For source location, this can be a folder/file share or an
http location. When it comes to urls, they can be different from the packages
url (where packages are searched and installed from). As an example, for
Chocolatey's community package package repository, the package url is
https://community.chocolatey.org/api/v2/, but the push url is https://push.chocolatey.org
(and the deprecated https://chocolatey.org/ as a push url). Check the
documentation for your choice of repository to learn what the push url is.

For the key, this can be an apikey that is provided by your source repository.
With some sources, like Nexus, this can be a NuGet API key or it could be a
user name and password specified as 'user:password' for the API key. Please see
your repository's documentation (for Nexus, please see
https://ch0.co/nexus2apikey).

NOTE: See scripting in the command reference (`choco -?`) for how to
 write proper scripts and integrations.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Connecting to Chocolatey.org (Community Package Repository)");
            "chocolatey".Log().Info(() => @"
In order to save your API key for {0},
 log in (or register, confirm and then log in) to
 {0}, go to {0}account,
 copy the API Key, and then use it in the following command:

    choco apikey add -k <your key here> -s {0}

".FormatWith(ApplicationParameters.ChocolateyCommunityFeedPushSource));

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
            switch (configuration.ApiKeyCommand.Command)
            {
                case ApiKeyCommandType.Remove:
                    _configSettingsService.RemoveApiKey(configuration);
                    break;
                case ApiKeyCommandType.Add:
                    _configSettingsService.SetApiKey(configuration);
                    break;
                default:
                    _configSettingsService.GetApiKey(configuration, (key) =>
                    {
                        string authenticatedString = string.IsNullOrWhiteSpace(key.Key) ? string.Empty : "(Authenticated)";

                        if (configuration.RegularOutput)
                        {
                            this.Log().Info(() => "{0} - {1}".FormatWith(key.Source, authenticatedString));
                        }
                        else
                        {
                            this.Log().Info(() => "{0}|{1}".FormatWith(key.Source, authenticatedString));
                        }
                    });
                    break;
            }
        }

        public virtual bool MayRequireAdminAccess()
        {
            var config = Config.GetConfigurationSettings();
            if (config == null) return true;

            return !string.IsNullOrWhiteSpace(config.ApiKeyCommand.Key);
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
