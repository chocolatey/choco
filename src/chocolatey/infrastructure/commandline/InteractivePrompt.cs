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

        public static TChoice prompt_for_confirmation<TChoice>(string prompt, IEnumerable<TChoice> choices, TChoice defaultChoice, bool requireAnswer, int repeat = 10) where TChoice : class
        {
            if (repeat < 0) throw new ApplicationException("Too many bad attempts. Stopping before application crash.");
            Ensure.that(() => prompt).is_not_null();
            Ensure.that(() => choices).is_not_null();
            Ensure
              .that(() => choices)
              .meets(
                c => c.Count() > 0,
                (name, value) => { throw new ApplicationException("No choices passed in. Please ensure you pass choices");
            });
            if (defaultChoice != null)
            {
                Ensure
                  .that(()=> choices)
                  .meets(
                    c=> c.Contains(defaultChoice),
                    (name, value) => {
                        throw new ApplicationException("Default choice value must be one of the given choices.");
                    });
            }

            "chocolatey".Log().Info(ChocolateyLoggers.Important, prompt);

            int counter = 1;
            IDictionary<int, TChoice> choiceDictionary = new Dictionary<int, TChoice>();
            foreach (var choice in choices.or_empty_list_if_null())
            {
                choiceDictionary.Add(counter, choice);
                "chocolatey".Log().Info(" {0}) {1}{2}".format_with(counter, choice.to_string(), choice == defaultChoice ? " [Default - Press Enter]" : ""));
                counter++;
            }

            var selection = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(selection) && defaultChoice != null)
            {
                return defaultChoice;
            }

            int selected = -1;
            if (!int.TryParse(selection, out selected) || selected <= 0 || selected > (counter - 1))
            {
                "chocolatey".Log().Error(ChocolateyLoggers.Important, "Your choice of '{0}' is not a valid selection.".format_with(selection));
                if (requireAnswer)
                {
                    "chocolatey".Log().Warn(ChocolateyLoggers.Important, "You must select an answer");
                    return prompt_for_confirmation(prompt, choices, defaultChoice, requireAnswer, repeat - 1);
                }
                return null;
            }

            return choiceDictionary[selected];
        }
    }
}