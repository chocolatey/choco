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
    using attributes;
    using commandline;
    using configuration;
    using infrastructure.commands;
    using logging;
    using services;

    [CommandFor("push", "pushes a compiled nupkg")]
    public class ChocolateyPushCommand : ICommand
    {
        private readonly IChocolateyPackageService _packageService;
        private readonly IChocolateyConfigSettingsService _configSettingsService;

        public ChocolateyPushCommand(IChocolateyPackageService packageService, IChocolateyConfigSettingsService configSettingsService)
        {
            _packageService = packageService;
            _configSettingsService = configSettingsService;
        }

        public virtual void configure_argument_parser(OptionSet optionSet, ChocolateyConfiguration configuration)
        {
            configuration.Sources = null;

            optionSet
                .Add("s=|source=",
                     "Source - The source we are pushing the package to. Use {0} to push to community feed.".format_with(ApplicationParameters.ChocolateyCommunityFeedPushSource),
                     option => configuration.Sources = option.remove_surrounding_quotes())
                .Add("k=|key=|apikey=|api-key=",
                     "ApiKey - The api key for the source. If not specified (and not local file source), does a lookup. If not specified and one is not found for an https source, push will fail.",
                     option => configuration.PushCommand.Key = option.remove_surrounding_quotes())
                .Add("t=",
                     "Timeout (in seconds) - The time to allow a package push to occur before timing out. Defaults to execution timeout {0}.".format_with(configuration.CommandExecutionTimeoutSeconds),
                     option =>
                         {
                             this.Log().Warn("Using -t for timeout has been deprecated and will be removed in v1. Please update to use --timeout or --execution-timeout instead.");
                             int timeout = 0;
                             int.TryParse(option, out timeout);
                             if (timeout > 0)
                             {
                                 configuration.CommandExecutionTimeoutSeconds = timeout;
                             }
                         })
                //.Add("b|disablebuffering|disable-buffering",
                //    "DisableBuffering -  Disable buffering when pushing to an HTTP(S) server to decrease memory usage. Note that when this option is enabled, integrated windows authentication might not work.",
                //    option => configuration.PushCommand.DisableBuffering = option)
                ;
            //todo: push command - allow disable buffering?
        }

        public virtual void handle_additional_argument_parsing(IList<string> unparsedArguments, ChocolateyConfiguration configuration)
        {
            configuration.Input = string.Join(" ", unparsedArguments); // path to .nupkg - assume relative

            if (string.IsNullOrWhiteSpace(configuration.Sources))
            {
                configuration.Sources = ApplicationParameters.ChocolateyCommunityFeedPushSource;
                var newSourceKey = _configSettingsService.get_api_key(configuration, null);

                if (string.IsNullOrWhiteSpace(newSourceKey))
                {
                    configuration.Sources = ApplicationParameters.ChocolateyCommunityFeedPushSourceOld;
                    var oldSourceKey = _configSettingsService.get_api_key(configuration, null);

                    if (string.IsNullOrWhiteSpace(oldSourceKey))
                    {
                        configuration.Sources = ApplicationParameters.ChocolateyCommunityFeedPushSource;
                    }
                    else
                    {
                        this.Log().Warn(ChocolateyLoggers.Important, @"ACTION: Please update your apikey to use 
  '{0}' 
 instead of 
  '{1}'. 
 The latter source url is now considered deprecated and will not be 
 checked as the default source in Chocolatey v1.0. For details, run 
 `choco apikey -?`".format_with(ApplicationParameters.ChocolateyCommunityFeedPushSource, ApplicationParameters.ChocolateyCommunityFeedPushSourceOld));
                    }
                }
            }

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

        public virtual void handle_validation(ChocolateyConfiguration configuration)
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

        public virtual void help_message(ChocolateyConfiguration configuration)
        {
            this.Log().Info(ChocolateyLoggers.Important, "Push Command");
            this.Log().Info(@"
Chocolatey will attempt to push a compiled nupkg to a package feed. 
 Some may prefer to use `cpush` as a shortcut for `choco push`.

NOTE: 100% compatible with older chocolatey client (0.9.8.32 and below)
 with options and switches. Default push location is deprecated and 
 will be removed by v1. In most cases you can still pass options and 
 switches with one dash (`-`). For more details, see 
 the command reference (`choco -?`).

A feed can be a local folder, a file share, the community feed 
 ({0}), or a custom/private feed. For web
 feeds, it has a requirement that it implements the proper OData
 endpoints required for NuGet packages.
".format_with(ApplicationParameters.ChocolateyCommunityFeedPushSource));

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Usage");
            "chocolatey".Log().Info(@"
    choco push [<path to nupkg>] [<options/switches>]
    cpush [<path to nupkg>] [<options/switches>]

NOTE: If there is more than one nupkg file in the folder, the command 
 will require specifying the path to the file.
");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Examples");
            "chocolatey".Log().Info(@"
    choco push --source https://chocolatey.org/
    choco push --source ""'https://chocolatey.org/'"" -t 500
    choco push --source ""'https://chocolatey.org/'"" -k=""'123-123123-123'""

NOTE: See scripting in the command reference (`choco -?`) for how to 
 write proper scripts and integrations.

");

            "chocolatey".Log().Info(ChocolateyLoggers.Important, "Troubleshooting");
            "chocolatey".Log().Info(()=> @"
To use this command, you must have your API key saved for the community
 feed (chocolatey.org) or the source you want to push to. Or you can 
 explicitly pass the apikey to the command. See `apikey` command help 
 for instructions on saving your key:

    choco apikey -?

A common error is `Failed to process request. 'The specified API key 
 does not provide the authority to push packages.' The remote server 
 returned an error: (403) Forbidden..` This means the package already 
 exists with a different user (API key). The package could be unlisted. 
 You can verify by going to {0}packages/packageName. 
 Please contact the administrators of {0} if you see this 
 and you don't see a good reason for it.
".format_with(ApplicationParameters.ChocolateyCommunityFeedPushSource));

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
            _packageService.push_noop(configuration);
        }

        public virtual void run(ChocolateyConfiguration configuration)
        {
            _packageService.push_run(configuration);
        }

        public virtual bool may_require_admin_access()
        {
            return false;
        }
    }
}