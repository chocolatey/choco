namespace chocolatey.infrastructure.commandline
{
    using System;
    using System.Collections.Generic;
    using logging;

    public class InteractivePrompt
    {
        public static TChoice prompt_for_confirmation<TChoice>(string prompt, IEnumerable<TChoice> choices, TChoice defaultChoice, bool requireAnswer) where TChoice : class
        {
            "chocolatey".Log().Info(ChocolateyLoggers.Important,prompt);

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
            if (!int.TryParse(selection, out selected) || selected <= 0 || selected > counter)
            {
                "chocolatey".Log().Error(ChocolateyLoggers.Important, "Your choice of '{0}' is not a valid selection.".format_with(selection));
                if (requireAnswer)
                {
                    "chocolatey".Log().Warn(ChocolateyLoggers.Important, "You must select an answer");
                    return prompt_for_confirmation(prompt, choices, defaultChoice, requireAnswer);
                }
                return null;
            }

            return choiceDictionary[selected];
        }
    }
}