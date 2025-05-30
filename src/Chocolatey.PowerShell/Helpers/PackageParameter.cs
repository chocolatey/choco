// Copyright © 2017 - 2025 Chocolatey Software, Inc
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

namespace Chocolatey.PowerShell.Helpers
{
    public static class PackageParameter
    {
        private const string PackageParameterPattern = @"(?:^|\s+)\/(?<ItemKey>[^\:\=\s)]+)(?:(?:\:|=){1}(?:\'|\""){0,1}(?<ItemValue>.*?)(?:\'|\""){0,1}(?:(?=\s+\/)|$))?";
        private static readonly Regex _packageParameterRegex = new Regex(PackageParameterPattern, RegexOptions.Compiled);
        
        public static Hashtable GetParameters(PSCmdlet cmdlet, string parameters)
        {
            var paramHash = new Hashtable(StringComparer.OrdinalIgnoreCase);
            
            if (!string.IsNullOrEmpty(parameters))
            {
                paramHash = AddParameters(cmdlet, parameters, paramHash);
            }
            else
            {
                var packageParameters = EnvironmentHelper.GetVariable(
                    cmdlet,
                    "ChocolateyPackageParameters",
                    EnvironmentVariableTarget.Process);
                if (!string.IsNullOrEmpty(packageParameters))
                {
                    paramHash = AddParameters(cmdlet, packageParameters, paramHash);
                }

                var sensitivePackageParameters = EnvironmentHelper.GetVariable(
                    cmdlet,
                    "ChocolateyPackageParametersSensitive",
                    EnvironmentVariableTarget.Process);
                if (!string.IsNullOrEmpty(sensitivePackageParameters))
                {
                    paramHash = AddParameters(cmdlet, sensitivePackageParameters, paramHash, logParams: false);
                }
            }

            return paramHash;
        }

        private static Hashtable AddParameters(PSCmdlet cmdlet, string paramString, Hashtable paramHash, bool logParams = true)
        {;
            foreach (Match match in _packageParameterRegex.Matches(paramString))
            {
                var name = match.Groups["ItemKey"].Value.Trim();
                var valueGroup = match.Groups["ItemValue"];

                object value;
                if (valueGroup.Success)
                {
                    value = valueGroup.Value.Trim();
                }
                else
                {
                    value = (object)true;
                }

                if (logParams)
                {
                    cmdlet.WriteDebug($"Adding package param '{name}'='{value}'");
                }
                else
                {
                    cmdlet.WriteDebug($"Adding package param '{name}' (value not logged)");
                }

                paramHash[name] = value;
            }

            return paramHash;
        }
    }
}