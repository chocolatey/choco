namespace chocolatey.infrastructure.app.builders
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using configuration;

    public static class ExternalCommandArgsBuilder
    {
        public static string BuildArguments(ChocolateyConfiguration config, IDictionary<string, ExternalCommandArgument> configToArgNames)
        {
            var arguments = new StringBuilder();
            var props = config.GetType().GetProperties();
            var propValues = new Dictionary<string, string>();

            foreach (var prop in props)
            {
                if (configToArgNames.ContainsKey(prop.Name))
                {
                    var arg = configToArgNames[prop.Name];
                    var propType = prop.PropertyType;
                    var propValue = prop.GetValue(config, null).to_string().wrap_spaces_in_quotes();
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