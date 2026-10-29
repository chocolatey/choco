// Copyright © 2017 - 2026 Chocolatey Software, Inc
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
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text.RegularExpressions;
using static chocolatey.StringResources.EnvironmentVariables;

namespace Chocolatey.PowerShell.Helpers
{
    /// <summary>
    /// Parses package parameters, such as <c>/Shortcut /InstallDir:'c:\program files\xyz'</c>, into a table of
    /// names and values.
    /// Used by <see cref="Commands.GetPackageParametersCommand"/>, and any other commands that need the package
    /// parameters.
    /// </summary>
    public static class PackageParameterParser
    {
        // A parameter is a name that follows a slash at the start of the text or after whitespace, with an
        // optional value after a colon or equals sign. A value can be wrapped in a single or double quote, and
        // runs until the next whitespace followed by a slash, or the end of the text.
        private static readonly Regex ParameterPattern = new Regex(
            @"(?:^|\s+)/(?<ItemKey>[^:=\s)]+)(?:[:=]['""]?(?<ItemValue>.*?)['""]?(?=\s+/|$))?");

        /// <summary>
        /// Gets the package parameters from the string given, or from the environment variables when there is no
        /// explicit string passed.
        /// </summary>
        /// <param name="cmdlet">The cmdlet that the parameters are for, which the debug messages are written with.</param>
        /// <param name="parameters">
        /// The text to parse. When this is <c>null</c> or empty, <c>ChocolateyPackageParameters</c> and
        /// <c>ChocolateyPackageParametersSensitive</c> are parsed instead, and a sensitive value wins over a
        /// normal one.
        /// </param>
        /// <returns>
        /// A table of the parameters, with names that are not case-sensitive. A value is a string, or
        /// <c>true</c> (as a boolean) for a parameter that has no value.
        /// </returns>
        public static Hashtable Parse(PSCmdlet cmdlet, string parameters)
        {
            var loggingAllowed = true;
            string[] parameterStrings;

            if (string.IsNullOrEmpty(parameters))
            {
                PSHelper.WriteDebug(cmdlet, "Parsing $env:ChocolateyPackageParameters and $env:ChocolateyPackageParametersSensitive for parameters");

                var sensitiveParameters = EnvironmentHelper.GetVariable(Package.ChocolateyPackageParametersSensitive);
                parameterStrings = new[] { EnvironmentHelper.GetVariable(Package.ChocolateyPackageParameters), sensitiveParameters };

                if (!string.IsNullOrEmpty(sensitiveParameters))
                {
                    PSHelper.WriteDebug(cmdlet, "Sensitive parameters detected, no logging of parameters.");
                    loggingAllowed = false;
                }
            }
            else
            {
                parameterStrings = new[] { parameters };
            }

            var result = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (var parameterString in parameterStrings)
            {
                foreach (var parameter in GetParameters(parameterString))
                {
                    if (loggingAllowed)
                    {
                        PSHelper.WriteDebug(cmdlet, $"Adding package param '{parameter.Name}'='{parameter.Value}'");
                    }

                    result[parameter.Name] = parameter.Value;
                }
            }

            return result;
        }

        private static IEnumerable<(string Name, object Value)> GetParameters(string parameters)
        {
            if (string.IsNullOrEmpty(parameters))
            {
                yield break;
            }

            foreach (Match match in ParameterPattern.Matches(parameters))
            {
                var name = match.Groups["ItemKey"].Value.Trim();
                var value = match.Groups["ItemValue"].Value.Trim();

                yield return (name, value.Length == 0 ? (object)true : value);
            }
        }
    }
}
