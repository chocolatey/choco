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

namespace chocolatey.infrastructure.commandline
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using adapters;
    using guards;
    using logging;
    using Console = adapters.Console;

    public class InteractivePrompt
    {
        private static Lazy<IConsole> _console = new Lazy<IConsole>(() => new Console());

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void initialize_with(Lazy<IConsole> console)
        {
            _console = console;
        }

        private static IConsole Console
        {
            get { return _console.Value; }
        }

        public static string prompt_for_confirmation_short(string prompt, IEnumerable<string> choices, int repeat = 10)
        {
            if (repeat < 0) throw new ApplicationException("Too many bad attempts. Stopping before application crash.");
            Ensure.that(() => prompt).is_not_null();
            Ensure.that(() => choices).is_not_null();
            Ensure
                .that(() => choices)
                .meets(
                    c => c.Any(),
                    (name, value) => { throw new ApplicationException("No choices passed in. Please ensure you pass choices."); });
            Ensure
                .that(() => choices)
                .meets(
                    c => !c.Any(String.IsNullOrWhiteSpace),
                    (name, value) => { throw new ApplicationException("Some choices are empty. Please ensure you provide no empty choices."); });
            Ensure
                .that(() => choices)
                .meets(
                    c => c.Select(entry => entry.FirstOrDefault()).Distinct().Count() == c.Count(),
                    (name, value) => { throw new ApplicationException("Multiple choices have the same first letter. Please ensure you pass choices with different first letters."); });

            var promptWithChoices = "{0} ({1}): ".format_with(prompt, String.Join("/", choices));
            
            Console.Write(promptWithChoices);
            var selection = Console.ReadLine();

            "chocolatey".Log().Info(ChocolateyLoggers.LogFileOnly, "{0}{1}".format_with(promptWithChoices, selection));

            // check to see if value was passed
            foreach (var choice in choices)
            {
                if (choice.is_equal_to(selection) || choice.Substring(0, 1).is_equal_to(selection))
                {
                    selection = choice;
                    return selection;
                }
            }

            "chocolatey".Log().Error(ChocolateyLoggers.Important, "Your choice of '{0}' is not a valid selection.".format_with(selection));
            return prompt_for_confirmation_short(prompt, choices, repeat - 1);
        }

        public static string prompt_for_confirmation(string prompt, IEnumerable<string> choices, string defaultChoice, bool requireAnswer, int repeat = 10)
        {
            if (repeat < 0) throw new ApplicationException("Too many bad attempts. Stopping before application crash.");
            Ensure.that(() => prompt).is_not_null();
            Ensure.that(() => choices).is_not_null();
            Ensure
                .that(() => choices)
                .meets(
                    c => c.Count() > 0,
                    (name, value) => { throw new ApplicationException("No choices passed in. Please ensure you pass choices"); });
            if (defaultChoice != null)
            {
                Ensure
                    .that(() => choices)
                    .meets(
                        c => c.Contains(defaultChoice),
                        (name, value) => { throw new ApplicationException("Default choice value must be one of the given choices."); });
            }

            "chocolatey".Log().Info(ChocolateyLoggers.Important, prompt);

            int counter = 1;
            IDictionary<int, string> choiceDictionary = new Dictionary<int, string>();
            foreach (var choice in choices.or_empty_list_if_null())
            {
                choiceDictionary.Add(counter, choice);
                "chocolatey".Log().Info(" {0}) {1}{2}".format_with(counter, choice.to_string(), choice == defaultChoice ? " [Default - Press Enter]" : ""));
                counter++;
            }

            Console.Write("> ");
            var selection = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(selection) && defaultChoice != null)
            {
                return defaultChoice;
            }

            int selected = -1;

            if (!int.TryParse(selection, out selected) || selected <= 0 || selected > (counter - 1))
            {
                // check to see if value was passed
                var selectionFound = false;
                foreach (var pair in choiceDictionary)
                {
                    if (pair.Value.is_equal_to(selection))
                    {
                        selected = pair.Key;
                        selectionFound = true;   
                        break;
                    }
                }

                if (!selectionFound)
                {
                    "chocolatey".Log().Error(ChocolateyLoggers.Important, "Your choice of '{0}' is not a valid selection.".format_with(selection));
                    if (requireAnswer)
                    {
                        "chocolatey".Log().Warn(ChocolateyLoggers.Important, "You must select an answer");
                        return prompt_for_confirmation(prompt, choices, defaultChoice, requireAnswer, repeat - 1);
                    }
                    return null;
                }
            }

            return choiceDictionary[selected];
        }
    }
}