// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

using System;
using System.Collections.Generic;
using System.Linq;
using SimpleInjector;
using chocolatey.infrastructure.app.configuration;
using chocolatey.infrastructure.validations;
using chocolatey.infrastructure.logging;
using chocolatey.infrastructure.app.utility;
using chocolatey.infrastructure.app.validations;

namespace chocolatey.infrastructure.app.runners
{
    /// <summary>
    ///   Console application responsible for running chocolatey
    /// </summary>
    public sealed class ConsoleApplication
    {
        public void Run(string[] args, ChocolateyConfiguration config, Container container)
        {
            var commandLine = Environment.CommandLine;

            if (ArgumentsUtility.SensitiveArgumentsProvided(commandLine))
            {
                this.Log().Debug(() => "Command line not shown - sensitive arguments may have been passed.");
            }
            else
            {
                this.Log().Debug(() => "Command line: {0}".FormatWith(commandLine));
                this.Log().Debug(() => "Received arguments: {0}".FormatWith(string.Join(" ", args)));
            }

            IList<string> commandArgs = new List<string>();
            //shift the first arg off
            var count = 0;
            foreach (var arg in args)
            {
                if (count == 0)
                {
                    count += 1;
                    continue;
                }

                commandArgs.Add(arg);
            }

            var runner = new GenericRunner();
            runner.Run(config, container, isConsole: true, parseArgs: command =>
                {
                    ConfigurationOptions.ParseArgumentsAndUpdateConfiguration(
                        commandArgs,
                        config,
                        (optionSet) => command.ConfigureArgumentParser(optionSet, config),
                        (unparsedArgs) =>
                        {
                            // if debug is bundled with local options, it may not get picked up when global
                            // options are parsed. Attempt to set it again once local options are set.
                            // This does mean some output from debug will be missed (but not much)
                            if (config.Debug)
                            {
                                Log4NetAppenderConfiguration.EnableDebugLoggingIf(config.Debug, "{0}LoggingColoredConsoleAppender".FormatWith(ChocolateyLoggers.Verbose.ToStringSafe()), "{0}LoggingColoredConsoleAppender".FormatWith(ChocolateyLoggers.Trace.ToStringSafe()));
                            }

                            command.ParseAdditionalArguments(unparsedArgs, config);

                            if (!config.Features.IgnoreInvalidOptionsSwitches)
                            {
                                // all options / switches should be parsed,
                                //  so show help menu if there are any left
                                foreach (var unparsedArg in unparsedArgs.OrEmpty())
                                {
                                    if (unparsedArg.StartsWith("-") || unparsedArg.StartsWith("/"))
                                    {
                                        config.HelpRequested = true;
                                        config.UnsuccessfulParsing = true;
                                    }
                                }
                            }
                        },
                        () =>
                        {
                            PreRunValidationChecks.Validate(command, config, container);
                        },
                        () => command.HelpMessage(config));
                });
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public void run(string[] args, ChocolateyConfiguration config, Container container)
            => Run(args, config, container);
#pragma warning restore IDE0022, IDE1006
    }
}
