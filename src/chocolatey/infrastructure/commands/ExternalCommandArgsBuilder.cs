namespace chocolatey.infrastructure.commands
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    ///   Responsible for setting up arguments for an external command to be executed
    /// </summary>
    public static class ExternalCommandArgsBuilder
    {
        /// <summary>
        /// Builds a string containing the arguments for calling an external command based on public property values in the specified properties object.
        /// </summary>
        /// <param name="properties">The properties object. Public properties are inspected for names and values based on exact matches in the configToArgNames dictionary.</param>
        /// <param name="configToArgNames">Dictionary of external commands set in the exact order in which you want to get back arguments. Keys should match exactly (casing as well) with the property names of the properties object.</param>
        /// <returns>A string containing the arguments merged from the configToArgNames dictionary and the properties object.</returns>
        public static string build_arguments(object properties, IDictionary<string, ExternalCommandArgument> configToArgNames)
        {
            var arguments = new StringBuilder();
            var props = properties.GetType().GetProperties();
            var propValues = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
           
            foreach (var prop in props)
            {
                if (configToArgNames.ContainsKey(prop.Name))
                {
                    var arg = configToArgNames[prop.Name];
                    var propType = prop.PropertyType;
                    var propValue = prop.GetValue(properties, null).to_string().wrap_spaces_in_quotes();
                    if (propType == typeof (Boolean) && propValue.is_equal_to(bool.FalseString))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(arg.ArgumentValue) && propType != typeof (Boolean))
                    {
                        if (string.IsNullOrWhiteSpace(propValue))
                        {
                            continue;
                        }

                        arg.ArgumentValue = propValue;
                    }

                    propValues.Add(
                        prop.Name,
                        arg.UseValueOnly
                            ? "{0}".format_with(quote_arg_value_if_required(arg))
                            : "{0}{1}".format_with(arg.ArgumentOption, quote_arg_value_if_required(arg))
                        );
                }
            }

            foreach (var arg in configToArgNames)
            {
                if (propValues.ContainsKey(arg.Key))
                {
                    arguments.AppendFormat("{0} ", propValues[arg.Key]);
                }
                else
                {
                    if (arg.Value.Required)
                    {
                        var argValue = quote_arg_value_if_required(arg.Value).wrap_spaces_in_quotes();

                        if (arg.Value.UseValueOnly)
                        {
                            arguments.AppendFormat("{0} ", argValue);
                        }
                        else
                        {
                            arguments.AppendFormat("{0}{1} ", arg.Value.ArgumentOption, argValue);
                        }
                    }
                }
            }
            if (arguments.Length == 0) return string.Empty;

            return arguments.Remove(arguments.Length - 1, 1).ToString();
        }

        private static string quote_arg_value_if_required(ExternalCommandArgument argument)
        {
            if (argument.QuoteValue && ! argument.ArgumentValue.StartsWith("\""))
            {
                return "\"{0}\"".format_with(argument.ArgumentValue);
            }

            return argument.ArgumentValue;
        }
    }
}