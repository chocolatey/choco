namespace chocolatey.infrastructure.tokens
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;

    public sealed class TokenReplacer
    {
        public static string replace_tokens<TConfig>(TConfig configuration, string textToReplace, string tokenPrefix = "\\[\\[", string tokenSuffix = "\\]\\]")
        {
            if (string.IsNullOrEmpty(textToReplace)) return string.Empty;

            IDictionary<string, string> dictionary = create_dictionary_from_configuration(configuration);
            var regex = new Regex("{0}(?<key>\\w+){1}".format_with(tokenPrefix,tokenSuffix));

            string output = regex.Replace(textToReplace, m =>
                {
                    string key = "";

                    key = m.Groups["key"].Value.to_lower();
                    if (!dictionary.ContainsKey(key))
                    {
                        return tokenPrefix + key + tokenSuffix;
                    }

                    string value = dictionary[key];
                    return value;
                });

            return output;
        }

        private static IDictionary<string, string> create_dictionary_from_configuration<TConfig>(TConfig configuration)
        {
            var propertyDictionary = new Dictionary<string, string>();
            foreach (PropertyInfo property in configuration.GetType().GetProperties())
            {
                propertyDictionary.Add(property.Name.to_lower(), property.GetValue(configuration, null).to_string());
            }

            return propertyDictionary;
        }
    }
}