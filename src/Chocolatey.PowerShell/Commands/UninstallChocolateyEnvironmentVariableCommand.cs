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
using System.Management.Automation;
using Chocolatey.PowerShell.Helpers;
using Chocolatey.PowerShell.Shared;

namespace Chocolatey.PowerShell.Commands
{
    [Cmdlet(VerbsLifecycle.Uninstall, "ChocolateyEnvironmentVariable", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(void))]
    public class UninstallChocolateyEnvironmentVariableCommand : ChocolateyCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        [Alias("VariableName")]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        [Alias("VariableType")]
        public EnvironmentVariableTarget Scope { get; set; } = EnvironmentVariableTarget.User;

        protected override void End()
        {
            var scope = Scope;

            // Fall back to Machine scope when there is no User environment key to remove from.
            if (scope == EnvironmentVariableTarget.User
                && ProcessInformation.IsElevated()
                && !EnvironmentHelper.EnvironmentKeyExists(scope))
            {
                WriteDebug($"The User environment registry key does not exist, removing {Name} at Machine scope instead.");
                scope = EnvironmentVariableTarget.Machine;
            }

            void removeVariable()
            {
                EnvironmentHelper.SetVariable(this, Name, scope, value: null);
            }

            if (scope == EnvironmentVariableTarget.Machine)
            {
                Elevation.RunElevated(this, removeVariable, $"Uninstall-ChocolateyEnvironmentVariable -Name '{Name}' -Scope {scope}");
            }
            else
            {
                removeVariable();
            }

            if (scope != EnvironmentVariableTarget.Process)
            {
                EnvironmentHelper.SetVariable(this, Name, EnvironmentVariableTarget.Process, value: null);
            }
        }
    }
}
