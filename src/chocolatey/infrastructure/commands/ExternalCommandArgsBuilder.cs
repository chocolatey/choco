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

namespace chocolatey.infrastructure.commands
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;

    /// <summary>
    ///   Responsible for setting up arguments for an external command to be executed
    /// </summary>
    public static class ExternalCommandArgsBuilder
    {
        /// <summary>
        ///   Builds a string containing the arguments for calling an external command based on public property values in the specified properties object.
        /// </summary>
        /// <param name="properties">The properties object. Public properties are inspected for names and values based on exact matches in the configToArgNames dictionary.</param>
        /// <param name="configToArgNames">Dictionary of external commands set in the exact order in which you want to get back arguments. Keys should match exactly (casing as well) with the property names of the properties object.</param>
        /// <returns>A string containing the arguments merged from the configToArgNames dictionary and the properties object.</returns>
        public static string build_arguments(object properties, IDictionary<string, ExternalCommandArgument> configToArgNames)
        {
            var arguments = new StringBuilder();
            var propValues = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            fill_args_dictionary(propValues, properties.GetType().GetProperties(), configToArgNames, properties, "");

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

        private static void fill_args_dictionary(Dictionary<string, string> propertyValues, IEnumerable<PropertyInfo> properties, IDictionary<string, ExternalCommandArgument> configToArgNames, object obj, string prepend)
        {
            foreach (var prop in properties.or_empty_list_if_null())
            {
                //todo: need a better way of handling
                if (prop.Name == "MachineSources") continue;

                if (prop.PropertyType.is_built_in_system_type())
                {
                    var propName = "{0}{1}".format_with(
                        string.IsNullOrWhiteSpace(prepend) ? "" : prepend + ".",
                        prop.Name
                        );

                    if (configToArgNames.ContainsKey(propName))
                    {
                        var arg = configToArgNames[propName];
                        var propType = prop.PropertyType;
                        var propValue = prop.GetValue(obj, null).to_string().wrap_spaces_in_quotes();
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

                        propertyValues.Add(
                            propName,
                            arg.UseValueOnly
                                ? "{0}".format_with(quote_arg_value_if_required(arg))
                                : "{0}{1}".format_with(arg.ArgumentOption, quote_arg_value_if_required(arg))
                            );
                    }
                }
                else
                {
                    fill_args_dictionary(propertyValues, prop.PropertyType.GetProperties(), configToArgNames, prop.GetValue(obj, null), prop.Name);
                }
            }
        }

        private static string quote_arg_value_if_required(ExternalCommandArgument argument)
        {
            if (argument.QuoteValue && !argument.ArgumentValue.StartsWith("\""))
            {
                return "\"{0}\"".format_with(argument.ArgumentValue);
            }

            return argument.ArgumentValue;
        }
    }
}