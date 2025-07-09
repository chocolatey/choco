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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using chocolatey.infrastructure.adapters;
using chocolatey.infrastructure.commandline;
using Console = chocolatey.infrastructure.adapters.Console;

namespace chocolatey.infrastructure.app.configuration
{
    public static class ConfigurationOptions
    {
        private static Lazy<IConsole> _console = new Lazy<IConsole>(() => new Console());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void InitializeWith(Lazy<IConsole> console)
        {
            _console = console;
        }

        public static void ClearOptions()
        {
            OptionSet.Clear();
        }

        private static IConsole Console
        {
            get { return _console.Value; }
        }

        public static OptionSet OptionSet { get; } = new OptionSet();

        /// <summary>
        ///   Parses arguments and updates the configuration
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="setOptions">The set options.</param>
        /// <param name="afterParse">Actions to take after parsing</param>
        /// <param name="validateConfiguration">Validate the configuration</param>
        /// <param name="helpMessage">The help message.</param>
        public static void ParseArgumentsAndUpdateConfiguration(ICollection<string> args,
                                                                    ChocolateyConfiguration configuration,
                                                                    Action<OptionSet> setOptions,
                                                                    Action<IList<string>> afterParse,
                                                                    Action validateConfiguration,
                                                                    Action helpMessage)
        {
            IList<string> unparsedArguments = new List<string>();

            // add help only once.
            // OptionSet.Count only happens the first time
            // we set the options, not when we add additional options.
            if (OptionSet.Count == 0)
            {
                // Before we do any parsing, let us check if the
                // user has passed -v=value, -v:value, -v value or /v value.
                // If the user has passed this information, we will assume
                // that they intended to pass a version to the command, but
                // -v is a short name for --verbose, and as such we need to
                // throw an exception to prevent further processing.
                var joinedArguments = string.Join(" ", args);

                var match = Regex.Match(joinedArguments, @"(^|\s)[-/]v([=:]['""]?(?<value>[^\s'""]+)| ['""]?(?<value>[^\s-'""]+))", RegexOptions.Compiled | RegexOptions.CultureInvariant);

                if (match.Success)
                {
                    throw new ApplicationException($@"
A value was passed to the `-v` option, but this option does not support an
 explicit value.

Did you mean to use `--version='{match.Groups["value"].Value}'` instead?
");
                }

                OptionSet
                    .Add("?|help|h",
                        "Prints out the help menu.",
                        option => configuration.HelpRequested = option != null)
                    .Add("online",
                        "Online - Open help for specified command in default browser application. This option only works when used in combination with the -?/--help/-h option.  Available in 2.0.0+",
                        option => configuration.ShowOnlineHelp = option != null);
            }

            if (setOptions != null)
            {
                setOptions(OptionSet);
            }

            try
            {
                unparsedArguments = OptionSet.Parse(args);
            }
            catch (OptionException)
            {
                ShowHelp(OptionSet, helpMessage);
                configuration.UnsuccessfulParsing = true;
            }

            // the command argument
            if (string.IsNullOrWhiteSpace(configuration.CommandName) && unparsedArguments.Contains(args.FirstOrDefault()))
            {
                var commandName = args.FirstOrDefault();
                if (!Regex.IsMatch(commandName, @"^[-\/+]"))
                {
                    configuration.CommandName = commandName;
                }
                else if (commandName.IsEqualTo("-v") || commandName.IsEqualTo("--version"))
                {
                    // skip help menu
                }
                else
                {
                    configuration.HelpRequested = true;
                    configuration.UnsuccessfulParsing = true;
                }
            }

            if (afterParse != null)
            {
                afterParse(unparsedArguments);
            }

            if (configuration.HelpRequested)
            {
                if (configuration.ShowOnlineHelp)
                {
                    if (string.IsNullOrWhiteSpace(configuration.CommandName))
                    {
                        "chocolatey".Log().Warn("Unable to open command help as no command name has been provided.");
                        return;
                    }

                    var targetAddress = "https://ch0.co/c/{0}".FormatWith(configuration.CommandName.ToLowerSafe());

                    try
                    {
                        System.Diagnostics.Process.Start(new ProcessStartInfo(targetAddress) { UseShellExecute = true });
                    }
                    catch (Exception)
                    {
                        "chocolatey".Log().Warn("There was an error while attempting to open the following URL: {0} in the default browser.".FormatWith(targetAddress));
                    }

                    return;
                }

                ShowHelp(OptionSet, helpMessage);
            }
            else
            {
                // Only show this warning once
                if (configuration.ShowOnlineHelp)
                {
                    "chocolatey".Log().Warn("The --online option has been used, without the corresponding -?/--help/-h option.  Command execution will be completed without invoking help.");
                }

                if (validateConfiguration != null)
                {
                    validateConfiguration();
                }
            }
        }

        /// <summary>
        ///   Shows the help menu and prints the options
        /// </summary>
        /// <param name="optionSet">The option_set.</param>
        /// <param name="helpMessage">The action that displays the message</param>
        private static void ShowHelp(OptionSet optionSet, Action helpMessage)
        {
            if (helpMessage != null)
            {
                helpMessage.Invoke();
            }

            optionSet.WriteOptionDescriptions(Console.Out);
        }

#pragma warning disable IDE0022, IDE1006
        [Obsolete("This overload is deprecated and will be removed in v3.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IConsole> console)
            => InitializeWith(console);

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void reset_options()
            => ClearOptions();

        [Obsolete("This overload is deprecated and will be removed in v3.")]
        public static void parse_arguments_and_update_configuration(ICollection<string> args,
                                                                    ChocolateyConfiguration configuration,
                                                                    Action<OptionSet> setOptions,
                                                                    Action<IList<string>> afterParse,
                                                                    Action validateConfiguration,
                                                                    Action helpMessage)
            => ParseArgumentsAndUpdateConfiguration(args, configuration, setOptions, afterParse, validateConfiguration, helpMessage);
#pragma warning restore IDE0022, IDE1006
    }
}
