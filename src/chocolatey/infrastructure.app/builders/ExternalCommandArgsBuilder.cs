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

            foreach (var args in configToArgNames)
            {
                if (propValues.ContainsKey(args.Key))
                {
                    arguments.AppendFormat("{0} ", propValues[args.Key]);
                }
                else
                {
                    //we never set a value, so we are just using the name
                    if (args.Value.Required)
                    {
                        arguments.AppendFormat("{0} ", args.Value.ArgumentOption.wrap_spaces_in_quotes());
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