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

using Chocolatey.PowerShell.Shared;
using System.Collections;
using System.Management.Automation;
using Chocolatey.PowerShell.Helpers;

namespace Chocolatey.PowerShell.Commands
{
    /// <summary>
    /// Parses a string and returns a hash table array of those values for use in package scripts.
    /// </summary>
    /// <param name="Parameters">A string containing parameters to be parsed.</param>
    /// <returns>A hashtable of parameters that have been passed to <paramref name="Parameters"> and parsed.</returns>
    [Cmdlet(VerbsCommon.Get, "PackageParameter")]
    [Alias("Get-PackageParameters")]
    [OutputType(typeof(Hashtable))]
    public class GetPackageParameterCommand : ChocolateyCmdlet
    {
        [Parameter(Position = 0)]
        [Alias("Params")]
        public string Parameters { get; set; } = string.Empty;

        protected override void End()
        {
            WriteObject(PackageParameter.GetParameters(this, Parameters));
        }
    }
}
