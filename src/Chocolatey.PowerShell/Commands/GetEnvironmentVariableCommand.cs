// Copyright © 2017 - 2024 Chocolatey Software, Inc
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
using System.Management.Automation;
using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "EnvironmentVariable")]
    [OutputType(typeof(string))]
    public sealed class GetEnvironmentVariableCommand : ChocolateyCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public EnvironmentVariableTarget Scope { get; set; }

        [Parameter]
        public SwitchParameter PreserveVariables { get; set; }

        // Avoid logging environment variable names by accident.
        protected override bool Logging { get; } = false;

        protected override void End()
        {
            if (PreserveVariables)
            {
                WriteVerbose("Choosing not to expand environment names");
            }

            WriteObject(EnvironmentHelper.GetVariable(this, Name, Scope, PreserveVariables));
        }
    }
}
