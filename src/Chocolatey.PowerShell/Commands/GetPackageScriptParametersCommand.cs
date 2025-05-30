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
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;

namespace Chocolatey.PowerShell.Commands
{
    /// <summary>
    /// Parses a script and returns a hash table of parameters that are present in the package params, along with their values.
    /// </summary>
    /// <param name="ScriptPath">A path to a script to parse parameters from.</param>
    /// <param name="Parameters">A string containing parameters to be parsed.</param>
    /// <returns>A hashtable of parameters present in <paramref name="ScriptPath"> and also in <paramref name="Parameters"> (or the envvar).</returns>
    [Cmdlet(VerbsCommon.Get, "PackageScriptParameters")]
    [OutputType(typeof(Hashtable))]
    public class GetScriptParametersCommand : ChocolateyCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string ScriptPath { get; set; }

        [Parameter(Mandatory = false, Position = 1)]
        public string Parameters { get; set; } = string.Empty;

        protected override void End()
        {
            var packageParameters = PackageParameter.GetParameters(this, Parameters);
            var splatHash = new Hashtable(StringComparer.OrdinalIgnoreCase);

            // Check what parameters the script has
            Token[] tokensRef = null;
            ParseError[] errorsRef = null;
            var parsedAst = Parser.ParseFile(ScriptPath, out tokensRef, out errorsRef);
            var scriptParameters = parsedAst.ParamBlock != null ? parsedAst.ParamBlock.Parameters.Select(p => p.Name.VariablePath.UserPath.ToString()).ToList() : new List<string>();
            WriteVerbose($"Found {scriptParameters.Count()} parameter(s) in '{ScriptPath}'");

            // For each of those in PackageParameters, add it to the splat
            foreach (var parameter in scriptParameters)
            {
                if (packageParameters.ContainsKey(parameter))
                {
                    splatHash.Add(parameter, packageParameters[parameter]);
                }
            }

            // Return the splat
            WriteObject(splatHash);
        }
    }
}