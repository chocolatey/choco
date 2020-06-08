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

namespace chocolatey.infrastructure.app.configuration
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text.RegularExpressions;
    using adapters;
    using commandline;
    using Console = adapters.Console;

    public static class ConfigurationOptions
    {
        private static Lazy<IConsole> _console = new Lazy<IConsole>(() => new Console());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IConsole> console)
        {
            _console = console;
        }

        public static void reset_options()
        {
            _optionSet.Clear();
        }

        private static IConsole Console
        {
            get { return _console.Value; }
        }

        private static readonly OptionSet _optionSet = new OptionSet();

        public static OptionSet OptionSet
        {
            get { return _optionSet; }
        }

        /// <summary>
        ///   Parses arguments and updates the configuration
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="configuration">The configuration</param>
        /// <param name="setOptions">The set options.</param>
        /// <param name="afterParse">Actions to take after parsing</param>
        /// <param name="validateConfiguration">Validate the configuration</param>
        /// <param name="helpMessage">The help message.</param>
        public static void parse_arguments_and_update_configuration(ICollection<string> args,
                                                                    ChocolateyConfiguration configuration,
                                                                    Action<OptionSet> setOptions,
                                                                    Action<IList<string>> afterParse,
                                                                    Action validateConfiguration,
                                                                    Action helpMessage)
        {
            IList<string> unparsedArguments = new List<string>();

            // add help only once
            if (_optionSet.Count == 0)
            {
                _optionSet
                    .Add("?|help|h",
                         "Prints out the help menu.",
                         option => configuration.HelpRequested = option != null);
            }

            if (setOptions != null)
            {
                setOptions(_optionSet);
            }

            try
            {
                unparsedArguments = _optionSet.Parse(args);
            }
            catch (OptionException)
            {
                show_help(_optionSet, helpMessage);
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
                else if (commandName.is_equal_to("-v") || commandName.is_equal_to("--version"))
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
                show_help(_optionSet, helpMessage);
            }
            else
            {
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
        private static void show_help(OptionSet optionSet, Action helpMessage)
        {
            if (helpMessage != null)
            {
                helpMessage.Invoke();
            }

            optionSet.WriteOptionDescriptions(Console.Out);
        }
    }
}