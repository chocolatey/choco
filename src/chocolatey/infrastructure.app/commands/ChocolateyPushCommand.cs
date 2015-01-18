﻿// Copyright © 2011 - Present RealDimensions Software, LLC
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
    using attributes;
    using commandline;
    using configuration;
    using domain;
    using infrastructure.commands;
    using logging;
    using services;

    [CommandFor(CommandNameType.push)]
    public sealed class ChocolateyPushCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateyPushCommand(IChocolateyPackageService packageService, IChocolateyConfigSettingsService configSettingsService)
        {
            _packageService = packageService;
            _configSettingsService = configSettingsService;
        }

        public void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Sources = null;
            //configuration.Source = ApplicationParameters.DefaultChocolateyPushSource;
            configuration.PushCommand.TimeoutInSeconds = 300;

            optionSet
                .Add("s=|source=",
                     "Source - The source we are pushing the package to. Use {0} to push to community feed.".format_with(ApplicationParameters.ChocolateyCommunityFeedPushSource),
                     option => configuration.Sources = option)
                .Add("k=|key=|apikey=|api-key=",
                     "ApiKey - The api key for the source. If not specified (and not local file source), does a lookup. If not specified and one is not found for an https source, push will fail.",
                     option => configuration.PushCommand.Key = option)
                .Add("t=|timeout=",
                     "Timeout (in seconds) - The time to allow a package push to occur before timing out. Defaults to 300 seconds (5 minutes).",
                     option =>
                         {
                             int timeout = 0;
                             int.TryParse(option, out timeout);
                             if (timeout > 0)
                             {
                                 configuration.PushCommand.TimeoutInSeconds = timeout;
                             }
                         })
                //.Add("b|disablebuffering|disable-buffering",
                //    "DisableBuffering -  Disable buffering when pushing to an HTTP(S) server to decrease memory usage. Note that when this option is enabled, integrated windows authentication might not work.",
                //    option => configuration.PushCommand.DisableBuffering = option)
                ;
            //todo: push command - allow disable buffering?
        }

        public void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments); // path to .nupkg - assume relative

            if (!string.IsNullOrWhiteSpace(configuration.Sources))
            {
                var remoteSource = new Uri(configuration.Sources);
                if (string.IsNullOrWhiteSpace(configuration.PushCommand.Key) && !remoteSource.IsUnc && !remoteSource.IsFile)
                {
                    // perform a lookup
                    configuration.PushCommand.Key = _configSettingsService.get_api_key(configuration, null);
                }
            }
        }

        public void handle_validation(ChocolateyConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(configuration.Sources))
            {
                throw new ApplicationException("Source is required. Please pass a source to push to, such as --source={0}".format_with(ApplicationParameters.ChocolateyCommunityFeedPushSource));
            }

            var remoteSource = new Uri(configuration.Sources);

            if (string.IsNullOrWhiteSpace(configuration.PushCommand.Key) && !remoteSource.IsUnc && !remoteSource.IsFile)
            {
                throw new ApplicationException("An ApiKey was not found for '{0}'. You must either set an api key in the configuration or specify one with --api-key.".format_with(configuration.Sources));
            }

            // security advisory
            if (!configuration.Force || configuration.Sources.to_lower().Contains("chocolatey.org"))
            {
                if (remoteSource.Scheme == "http" && remoteSource.Host != "localhost")
                {
                    string errorMessage =
                        @"WARNING! The specified source '{0}' is not secure.
 Sending apikey over insecure channels leaves your data susceptible to 
 hackers. Please update your source to a more secure source and try again.
 
 Use --force if you understand the implications of this warning or are 
 accessing an internal feed. If you are however doing this against an 
 internet feed, then the choco gods think you are crazy. ;-)
 
NOTE: For chocolatey.org, you must update the source to be secure.".format_with(configuration.Sources);
                    throw new ApplicationException(errorMessage);
                }
            }
        }

        public void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Push Command");
            this.Log().Info(@"
Chocolatey will attempt to push a compiled nupkg to a package feed. 
 That feed can be a local folder, a file share, the community feed 
 '{0}' or a custom/private feed.

Usage: choco push [path to nupkg] [options/switches]

NOTE: If there is more than one nupkg file in the folder, the command 
 will require specifying the path to the file.

Examples:

 choco push --source ""https://chocolatey.org/""
 choco push --source ""https://chocolatey.org/"" -t 500
 choco push --source ""https://chocolatey.org/"" -k=""123-123123-123""

".format_with(ApplicationParameters.ChocolateyCommunityFeedPushSource));
        }

        public void noop(ChocolateyConfiguration configuration)
        {
            _packageService.push_noop(configuration);
        }

        public void run(ChocolateyConfiguration configuration)
        {
            _packageService.push_run(configuration);
        }
    }
}